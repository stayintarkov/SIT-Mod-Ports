using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using LateToTheParty.CoroutineExtensions;
using LateToTheParty.Models;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public static class LootManager
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool IsFindingAndDestroyingLoot { get; private set; } = false;
        public static bool HasInitialLootBeenDestroyed { get; private set; } = false;

        private static List<LootableContainer> AllLootableContainers = new List<LootableContainer>();
        private static object lootableContainerLock = new object();

        private static Dictionary<Item, Models.LootInfo> LootInfo = new Dictionary<Item, Models.LootInfo>();
        private static List<Item> ItemsDroppedByMainPlayer = new List<Item>();
        private static string[] secureContainerIDs = new string[0];
        private static Stopwatch lastLootDestroyedTimer = Stopwatch.StartNew();
        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
        private static string currentLocationName = "";
        private static int destroyedLootSlots = 0;
        private static double lootValueRandomFactor = 0;

        public static int LootableContainerCount
        {
            get { return AllLootableContainers.Count; }
        }

        public static int TotalLootItemsCount
        {
            get { return LootInfo.Count; }
        }

        public static int RemainingLootItemsCount
        {
            get { return LootInfo.Where(l => !l.Value.IsDestroyed && !l.Value.IsInPlayerInventory).Count(); }
        }

        public static IEnumerator Clear()
        {
            if (IsFindingAndDestroyingLoot)
            {
                enumeratorWithTimeLimit.Abort();

                EnumeratorWithTimeLimit conditionWaiter = new EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsFindingAndDestroyingLoot, nameof(IsFindingAndDestroyingLoot), 3000);

                IsFindingAndDestroyingLoot = false;
            }

            if (ConfigController.Config.Debug.Enabled && (LootInfo.Count > 0))
            {
                WriteLootLogFile();
            }

            PathRender.Clear();

            lock (lootableContainerLock)
            {
                AllLootableContainers.Clear();
            }

            LootInfo.Clear();
            ItemsDroppedByMainPlayer.Clear();

            HasInitialLootBeenDestroyed = false;
            currentLocationName = "";
            destroyedLootSlots = 0;
            lootValueRandomFactor = 0;

            lastLootDestroyedTimer.Restart();
        }

        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("LateToTheParty");
        
        public static int FindAllLootableContainers(string _currentMapName)
        {
            logger.LogInfo("FindAllLootableContainers");
            // Only run this once per map
            if (currentLocationName == _currentMapName)
            {
                return LootableContainerCount;
            }

            LoggingController.LogInfo("Searching for lootable containers in the map...");
            AllLootableContainers = GameWorld.FindObjectsOfType<LootableContainer>().ToList();
            LoggingController.LogInfo("Searching for lootable containers in the map...found " + LootableContainerCount + " lootable containers.");

            currentLocationName = _currentMapName;

            return LootableContainerCount;
        }

        public static void AddLootableContainer(LootableContainer container)
        {
            lock (lootableContainerLock)
            {
                LoggingController.LogInfo("Including container " + container.name + " when searching for loot.");
                AllLootableContainers.Add(container);
            }
        }

        public static void RegisterItemDroppedByPlayer(Item item, bool preventFromDespawning = false)
        {
            if (item == null)
            {
                LoggingController.LogError("Cannot register a null item dropped by a player or bot");
                return;
            }

            // If the item is a container (i.e. a backpack), all of the items it contains also need to be added to the ignore list
            foreach (Item relevantItem in item.FindAllItemsInContainer(true))
            {
                if (LootInfo.ContainsKey(relevantItem))
                {
                    LootInfo[relevantItem].IsInPlayerInventory = false;
                    LootInfo[relevantItem].NearbyInteractiveObject = null;
                }

                if (preventFromDespawning && !ItemsDroppedByMainPlayer.Contains(relevantItem))
                {
                    LoggingController.LogInfo("Preventing dropped item from despawning: " + relevantItem.LocalizedName());
                    ItemsDroppedByMainPlayer.Add(relevantItem);
                }
            }
        }

        public static void RegisterItemPickedUpByPlayer(Item item)
        {
            if (item == null)
            {
                LoggingController.LogError("Cannot register a null item picked up by a player or bot");
                return;
            }

            // If the item is a container (i.e. a backpack), all of the items it contains also need to be added to the ignore list
            foreach (Item relevantItem in item.ToEnumerable().FindAllRelatedItems())
            {
                //LoggingController.LogInfo("Checking for picked-up item in eligible loot: " + relevantItem.LocalizedName());
                if (LootInfo.Any(i => i.Key.Id == relevantItem.Id))
                {
                    if (LootInfo[relevantItem].IsInPlayerInventory)
                    {
                        continue;
                    }

                    LoggingController.LogInfo("Removing picked-up item from eligible loot: " + relevantItem.LocalizedName());
                    LootInfo[relevantItem].IsInPlayerInventory = true;
                    LootInfo[relevantItem].NearbyInteractiveObject = null;
                    LootInfo[item].PathData.Clear();
                    LootInfo[relevantItem].PathData.Clear();
                }
            }
        }

        public static IEnumerator FindAndDestroyLoot(Vector3 yourPosition, float timeRemainingFraction, double raidET)
        {
            try
            {
                IsFindingAndDestroyingLoot = true;

                // Check if this is the first time looking for loot in the map
                bool firstLootSearch = LootInfo.Count == 0;

                // Find all loose loot
                LootItem[] allLootItems = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToArray();
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allLootItems, ProcessFoundLooseLootItem, firstLootSearch ? 0 : raidET);

                // Search all lootable containers for loot
                enumeratorWithTimeLimit.Reset();
                lock (lootableContainerLock)
                {
                    yield return enumeratorWithTimeLimit.Run(AllLootableContainers, ProcessStaticLootContainer, firstLootSearch ? 0 : raidET);
                }

                // Ensure there is still loot on the map
                if ((LootInfo.Count == 0) || LootInfo.All(l => l.Value.IsDestroyed || l.Value.IsInPlayerInventory))
                {
                    yield break;
                }

                // After loot has initially been destroyed, limit the destruction rate
                double maxItemsToDestroy = 99999;
                if (HasInitialLootBeenDestroyed)
                {
                    maxItemsToDestroy = Math.Floor(ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Rate * lastLootDestroyedTimer.ElapsedMilliseconds / 1000.0);
                }

                // Find amount of loot to destroy
                double targetLootRemainingFraction = LocationSettingsController.GetLootRemainingFactor(timeRemainingFraction);
                int lootItemsToDestroy = (int)Math.Min(GetNumberOfLootItemsToDestroy(targetLootRemainingFraction), maxItemsToDestroy);
                if (lootItemsToDestroy > ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Items)
                {
                    LoggingController.LogInfo("Limiting the number of items to destroy to " + ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Items);
                    lootItemsToDestroy = ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Items;
                }
                if ((lootItemsToDestroy == 0) && (lastLootDestroyedTimer.ElapsedMilliseconds >= ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot * 1000.0))
                {
                    LoggingController.LogInfo("Max time of " + ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot + "s elapsed since destroying loot. Forcing at least 1 item to be removed...");
                    lootItemsToDestroy = 1;
                }
                if (lootItemsToDestroy == 0)
                {
                    if (!HasInitialLootBeenDestroyed)
                    {
                        LoggingController.LogInfo("Initial loot has been destroyed");
                        HasInitialLootBeenDestroyed = true;
                    }

                    yield break;
                }

                // Find amount of loot slots to destroy
                int targetTotalLootSlotsDestroyed = LocationSettingsController.GetTargetLootSlotsDestroyed(timeRemainingFraction);
                int targetLootSlotsToDestroy = targetTotalLootSlotsDestroyed - GetTotalDestroyedSlots();
                if (targetLootSlotsToDestroy > ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Slots)
                {
                    LoggingController.LogInfo("Limiting the number of item slots to destroy to " + ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Slots);
                    targetLootSlotsToDestroy = ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Slots;
                }
                if (targetLootSlotsToDestroy <= 0)
                {
                    if (!HasInitialLootBeenDestroyed)
                    {
                        LoggingController.LogInfo("Initial loot has been destroyed");
                        HasInitialLootBeenDestroyed = true;
                    }

                    yield break;
                }

                // Enumerate loot that hasn't been destroyed and hasn't previously been deemed accessible
                IEnumerable<KeyValuePair<Item, LootInfo>> remainingItems = LootInfo.Where(l => !l.Value.IsDestroyed && !l.Value.IsInPlayerInventory);
                Item[] inaccessibleItems = remainingItems.Where(l => !l.Value.PathData.IsAccessible).Select(l => l.Key).ToArray();

                // Check which items are accessible
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(inaccessibleItems, UpdateLootAccessibility);

                // Determine which loot is eligible to destroy
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(LootInfo.Keys.ToArray(), UpdateLootEligibility, yourPosition, raidET);

                // Sort eligible loot
                IEnumerable <KeyValuePair<Item, Models.LootInfo>> eligibleItems = LootInfo.Where(l => l.Value.CanDestroy && l.Value.PathData.IsAccessible);
                Item[] sortedLoot = SortLoot(eligibleItems).Select(i => i.Key).ToArray();

                // Identify items to destroy
                List<Item> itemsToDestroy = new List<Item>();
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(sortedLoot, FindItemsToDestroy, lootItemsToDestroy, targetLootSlotsToDestroy, itemsToDestroy);
                
                // Show the percentage of accessible loot before destroying any of it
                if (itemsToDestroy.Count > 0)
                {
                    int slotsToDestroy = itemsToDestroy.Sum(i => i.GetItemSlots());
                    double percentAccessible = Math.Round(100.0 * remainingItems.Where(i => i.Value.PathData.IsAccessible).Count() / remainingItems.Count(), 1);

                    string slotsDestroyedText = "Destroying " + itemsToDestroy.Count + "/" + maxItemsToDestroy + " items filling " + slotsToDestroy + "/" + targetLootSlotsToDestroy + " slots";
                    string lootFractionDestroyedText = Math.Round(GetCurrentLootRemainingFraction()  * 100.0, 2) + "%/" + Math.Round(targetLootRemainingFraction * 100.0, 2) + "%";
                    string lootSlotsDestroyedText = GetTotalDestroyedSlots() + "/" + targetTotalLootSlotsDestroyed + " slots.";
                    LoggingController.LogInfo(percentAccessible + "% of " + remainingItems.Count() + " items are accessible. " + slotsDestroyedText + ". Loot remaining: " + lootFractionDestroyedText + ", " + lootSlotsDestroyedText);
                }

                // Destroy items
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(itemsToDestroy, DestroyLoot, raidET);

                itemsToDestroy.Clear();
            }
            finally
            {
                IsFindingAndDestroyingLoot = false;
            }
        }

        private static void ProcessFoundLooseLootItem(LootItem lootItem, double raidET)
        {
            // Ensure you're still in the raid to avoid NRE's when it ends
            if ((Camera.main == null) || (lootItem.transform == null))
            {
                return;
            }

            // Ignore quest items like the bronze pocket watch for "Checking"
            if (lootItem.Item.QuestItem)
            {
                return;
            }

            // Find the nearest spawn point. If none is found, the map is invalid or the raid has ended
            Vector3? nearestSpawnPoint = LocationSettingsController.GetNearestSpawnPointPosition(lootItem.transform.position, EPlayerSideMask.Pmc);
            if (!nearestSpawnPoint.HasValue)
            {
                return;
            }
            double distanceToNearestSpawnPoint = Vector3.Distance(lootItem.transform.position, nearestSpawnPoint.Value);

            // Find all items associated with lootItem that are eligible for despawning
            IEnumerable<Item> allItems = lootItem.Item.FindAllItemsInContainer(true).RemoveExcludedItems().RemoveItemsDroppedByPlayer();
            foreach (Item item in allItems)
            {
                if (!LootInfo.ContainsKey(item))
                {
                    Models.LootInfo newLoot = new Models.LootInfo(
                            Models.ELootType.Loose,
                            lootItem.ItemOwner,
                            lootItem.transform,
                            distanceToNearestSpawnPoint,
                            GetLootFoundTime(raidET)
                    );

                    findNearbyContainters(item, newLoot);

                    LootInfo.Add(item, newLoot);
                    //LoggingController.LogInfo("Found loose loot item: " + item.LocalizedName());
                }
            }
        }

        private static void ProcessStaticLootContainer(LootableContainer lootableContainer, double raidET)
        {
            if (lootableContainer.ItemOwner == null)
            {
                return;
            }

            // Ensure you're still in the raid to avoid NRE's when it ends
            if ((Camera.main == null) || (lootableContainer.transform == null))
            {
                return;
            }

            // Find the nearest spawn point. If none is found, the map is invalid or the raid has ended
            Vector3? nearestSpawnPoint = LocationSettingsController.GetNearestSpawnPointPosition(lootableContainer.transform.position, EPlayerSideMask.Pmc);
            if (!nearestSpawnPoint.HasValue)
            {
                return;
            }
            double distanceToNearestSpawnPoint = Vector3.Distance(lootableContainer.transform.position, nearestSpawnPoint.Value);

            // NOTE: This level is for containers like weapon boxes, not like backpacks
            foreach (Item containerItem in lootableContainer.ItemOwner.Items)
            {
                foreach (Item item in containerItem.FindAllItemsInContainer().RemoveItemsDroppedByPlayer())
                {
                    if (!LootInfo.ContainsKey(item))
                    {
                        Models.LootInfo newLoot = new Models.LootInfo(
                            Models.ELootType.Static,
                            lootableContainer.ItemOwner,
                            lootableContainer.transform,
                            distanceToNearestSpawnPoint,
                            GetLootFoundTime(raidET)
                        );

                        if (lootableContainer.DoorState == EDoorState.Locked)
                        {
                            newLoot.ParentContainer = lootableContainer;
                        }

                        findNearbyContainters(item, newLoot);

                        LootInfo.Add(item, newLoot);
                    }
                }
            }
        }

        private static void findNearbyContainters(Item lootItem, Models.LootInfo lootInfo)
        {
            Type typeToSearch = ConfigController.Config.DestroyLootDuringRaid.OnlySearchForNearbyTrunks ? typeof(Trunk) : typeof(WorldInteractiveObject);

            IEnumerable <WorldInteractiveObject> nearbyInteractiveObjects = InteractiveObjectController
                .FindNearbyInteractiveObjects(lootInfo.Transform.position, ConfigController.Config.DestroyLootDuringRaid.NearbyInteractiveObjectSearchDistance, typeToSearch)
                .OrderBy(o => Vector3.Distance(lootInfo.Transform.position, o.transform.position));

            if (nearbyInteractiveObjects.Any())
            {
                lootInfo.NearbyInteractiveObject = nearbyInteractiveObjects.First();
                LoggingController.LogInfo(lootItem.LocalizedName() + " is nearby " + lootInfo.NearbyInteractiveObject.GetType().Name + " " + lootInfo.NearbyInteractiveObject.Id);
            }
        }

        private static double GetLootFoundTime(double raidET)
        {
            return raidET == 0 ? -1.0 * ConfigController.Config.DestroyLootDuringRaid.MinLootAge : raidET;
        }

        private static void UpdateLootEligibility(Item item, Vector3 yourPosition, double raidET)
        {
            LootInfo[item].CanDestroy = CanDestroyItem(item, yourPosition, raidET);
        }

        private static int GetNumberOfLootItemsToDestroy(double targetLootRemainingFraction)
        {
            // Calculate the fraction of loot that should be removed from the map
            double currentLootRemainingFraction = GetCurrentLootRemainingFraction();
            double lootFractionToDestroy = currentLootRemainingFraction - targetLootRemainingFraction;
            //LoggingController.LogInfo("Target loot remaining: " + targetLootRemainingFraction + ", Current loot remaining: " + GetCurrentLootRemainingFraction());

            // Calculate the number of loot items to destroy
            IEnumerable<KeyValuePair<Item, Models.LootInfo>> accessibleItems = LootInfo.Where(l => l.Value.PathData.IsAccessible);
            int lootItemsToDestroy = (int)Math.Floor(Math.Max(0, lootFractionToDestroy) * accessibleItems.Count());

            return lootItemsToDestroy;
        }

        private static double GetCurrentLootRemainingFraction()
        {
            IEnumerable<KeyValuePair<Item, Models.LootInfo>> accessibleItems = LootInfo.Where(l => l.Value.PathData.IsAccessible);
            IEnumerable<KeyValuePair<Item, Models.LootInfo>> remainingItems = accessibleItems
                .Where(v => !v.Value.IsDestroyed)
                .Where(v => !v.Value.IsInPlayerInventory)
                .Where(v => !ItemsDroppedByMainPlayer.Contains(v.Key));

            return (double)remainingItems.Count() / accessibleItems.Count();
        }

        private static int GetTotalDestroyedSlots()
        {
            IEnumerable<Item> collectedItems = LootInfo
                .Where(i => i.Value.IsInPlayerInventory)
                .Select(i => i.Key)
                .Concat(ItemsDroppedByMainPlayer);

            return destroyedLootSlots + collectedItems.Select(i => i.GetItemSlots()).Count();
        }

        private static IEnumerable<KeyValuePair<Item, Models.LootInfo>> SortLoot(IEnumerable<KeyValuePair<Item, Models.LootInfo>> loot)
        {
            System.Random random = new System.Random();

            // Get the loot ranking data from the server, but this only needs to be done once
            if (ConfigController.LootRanking == null)
            {
                ConfigController.GetLootRankingData();
            }

            // If loot ranking is disabled or invalid, simply sort the loot randomly
            if
            (
                !ConfigController.Config.DestroyLootDuringRaid.LootRanking.Enabled
                || (ConfigController.LootRanking == null)
                || (ConfigController.LootRanking.Items.Count == 0)
            )
            {
                return loot.OrderBy(i => random.NextDouble());
            }

            // Determine how much randomness to apply to loot sorting
            if (lootValueRandomFactor == 0)
            {
                double lootValueRange = getLootValueRange(loot);
                lootValueRandomFactor = lootValueRange * ConfigController.Config.DestroyLootDuringRaid.LootRanking.Randomness / 100.0;
            }

            //LoggingController.LogInfo("Randomness factor: " + lootValueRandomFactor);

            // Return loot sorted by value but with randomness applied
            IEnumerable<KeyValuePair<Item, Models.LootInfo>> sortedLoot = loot.OrderByDescending(i => ConfigController.LootRanking.Items[i.Key.TemplateId].Value + (random.Range(-1, 1) * lootValueRandomFactor));
            return sortedLoot.Skip(ConfigController.Config.DestroyLootDuringRaid.LootRanking.TopValueRetainCount);
        }

        private static double getLootValueRange(IEnumerable<KeyValuePair<Item, Models.LootInfo>> loot)
        {
            // Calculate the values of all of the loot on the map
            List<double> lootValues = new List<double>();
            foreach (KeyValuePair<Item, Models.LootInfo> lootItem in loot)
            {
                if (!ConfigController.LootRanking.Items.ContainsKey(lootItem.Key.TemplateId))
                {
                    LoggingController.LogWarning("Cannot find " + lootItem.Key.LocalizedName() + " in loot-ranking data.");
                    continue;
                }

                double? value = ConfigController.LootRanking.Items[lootItem.Key.TemplateId].Value;
                if (!value.HasValue)
                {
                    LoggingController.LogWarning("The value of " + lootItem.Key.LocalizedName() + " is null in the loot-ranking data.");
                    continue;
                }

                lootValues.Add(value.Value);
            }

            // Calculate the standard deviation of the loot values on the map
            double lootValueAvg = lootValues.Average();
            double lootValueStdev = 0;
            foreach (double val in lootValues)
            {
                lootValueStdev += Math.Pow(val - lootValueAvg, 2);
            }
            lootValueStdev = Math.Sqrt(lootValueStdev / lootValues.Count);

            // Return the range of 2*sigma of the loot values on the map
            return lootValueStdev * 4;
        }

        private static bool CanDestroyItem(this Item item, Vector3 yourPosition, double raidET)
        {
            if (!LootInfo.ContainsKey(item))
            {
                return false;
            }

            if (LootInfo[item].IsDestroyed || LootInfo[item].IsInPlayerInventory || ItemsDroppedByMainPlayer.Contains(item))
            {
                return false;
            }

            // Ensure enough time has elapsed since the loot was first placed on the map (to prevent loot on dead bots from being destroyed too soon)
            double lootAge = raidET - LootInfo[item].RaidETWhenFound;
            if (lootAge < ConfigController.Config.DestroyLootDuringRaid.MinLootAge)
            {
                //LoggingController.LogInfo("Ignoring " + item.LocalizedName() + " (Loot age: " + lootAge + ")");
                return false;
            }

            // Ensure you're still in the raid to avoid NRE's when it ends
            if ((Camera.main == null) || (LootInfo[item].Transform == null))
            {
                return false;
            }

            // Ignore loot that's too close to you
            float lootDist = Vector3.Distance(yourPosition, LootInfo[item].Transform.position);
            if (lootDist < ConfigController.Config.DestroyLootDuringRaid.ExclusionRadius)
            {
                return false;
            }

            // Ignore loot that's too close to bots
            Player nearestPlayer = NavMeshController.GetNearestPlayer(LootInfo[item].Transform.position);
            if (nearestPlayer == null)
            {
                return false;
            }
            lootDist = Vector3.Distance(nearestPlayer.Position, LootInfo[item].Transform.position);
            if (lootDist < ConfigController.Config.DestroyLootDuringRaid.ExclusionRadiusBots)
            {
                return false;
            }

            // Ignore loot that players couldn't have possibly reached yet
            double maxBotRunDistance = raidET * ConfigController.Config.DestroyLootDuringRaid.MapTraversalSpeed;
            if (maxBotRunDistance < LootInfo[item].DistanceToNearestSpawnPoint)
            {
                //LoggingController.LogInfo("Ignoring " + item.LocalizedName() + " (Loot Distance: " + LootInfo[item].DistanceToNearestSpawnPoint + ", Current Distance: " + maxBotRunDistance + ")");
                return false;
            }

            return true;
        }

        private static void UpdateLootAccessibility(Item item)
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            string lootPathName = GetLootPathName(item);
            Vector3 itemPosition = LootInfo[item].Transform.position;            
            
            // Draw a sphere around the loot item
            if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.OutlineLoot)
            {
                Vector3[] targetCirclePoints = PathRender.GetSpherePoints
                (
                    itemPosition,
                    ConfigController.Config.Debug.LootPathVisualization.LootOutlineRadius,
                    ConfigController.Config.Debug.LootPathVisualization.PointsPerCircle
                );
                PathVisualizationData lootOutline = new PathVisualizationData(lootPathName + "_itemOutline", targetCirclePoints, Color.white);
                if (LootInfo[item].PathData.LootOutlineData == null)
                {
                    LootInfo[item].PathData.LootOutlineData = lootOutline;
                }
                else
                {
                    LootInfo[item].PathData.LootOutlineData.Replace(lootOutline);
                }
                LootInfo[item].PathData.Update();
            }

            // Mark the loot as inaccessible if it is inside a locked container
            if ((LootInfo[item].ParentContainer != null) && (LootInfo[item].ParentContainer.DoorState == EDoorState.Locked))
            {
                LootInfo[item].PathData.IsAccessible = false;

                if (LootInfo[item].PathData.LootOutlineData != null)
                {
                    LootInfo[item].PathData.LootOutlineData.LineColor = Color.red;
                }
                LootInfo[item].PathData.Clear(true);
                LootInfo[item].PathData.Update();

                return;
            }

            // Mark the loot as inaccessible if it is likely behind a locked interactive object
            if ((LootInfo[item].NearbyInteractiveObject != null) && (LootInfo[item].NearbyInteractiveObject.DoorState == EDoorState.Locked))
            {
                LootInfo[item].PathData.IsAccessible = false;

                if (LootInfo[item].PathData.LootOutlineData != null)
                {
                    LootInfo[item].PathData.LootOutlineData.LineColor = Color.red;
                }
                LootInfo[item].PathData.Clear(true);
                LootInfo[item].PathData.Update();

                return;
            }

            // Make everything accessible if the accessibility-checking system is disabled
            if (!ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.Enabled)
            {
                LootInfo[item].PathData.IsAccessible = true;

                if (LootInfo[item].PathData.LootOutlineData != null)
                {
                    LootInfo[item].PathData.LootOutlineData.LineColor = Color.green;
                }

                LootInfo[item].PathData.Clear(true);
                LootInfo[item].PathData.Update();

                return;
            }

            // If the item appeared after the start of the raid, assume it must be accessible (it's likely on a dead bot)
            if (LootInfo[item].RaidETWhenFound > 0)
            {
                LootInfo[item].PathData.IsAccessible = true;

                if (LootInfo[item].PathData.LootOutlineData != null)
                {
                    LootInfo[item].PathData.LootOutlineData.LineColor = Color.green;
                }

                LootInfo[item].PathData.Clear(true);
                LootInfo[item].PathData.Update();

                return;
            }

            // Check if the loot is near a locked door. If not, assume it's accessible. 
            float distanceToNearestLockedDoor = NavMeshController.GetDistanceToNearestLockedDoor(itemPosition);
            if
            (
                (distanceToNearestLockedDoor < float.MaxValue)
                && (distanceToNearestLockedDoor > ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.ExclusionRadius)
            )
            {
                LootInfo[item].PathData.IsAccessible = true;

                if (LootInfo[item].PathData.LootOutlineData != null)
                {
                    LootInfo[item].PathData.LootOutlineData.LineColor = Color.green;
                }

                LootInfo[item].PathData.Clear(true);
                LootInfo[item].PathData.Update();

                return;
            }

            // Find the nearest position where a player could realistically exist
            Player nearestPlayer = NavMeshController.GetNearestPlayer(itemPosition);
            if (nearestPlayer == null)
            {
                return;
            }
            Vector3? nearestSpawnPointPosition = LocationSettingsController.GetNearestSpawnPointPosition(itemPosition);
            Vector3 nearestPosition = nearestPlayer.Transform.position;
            if (nearestSpawnPointPosition.HasValue && (Vector3.Distance(itemPosition, nearestSpawnPointPosition.Value) < Vector3.Distance(itemPosition, nearestPosition)))
            {
                nearestPosition = nearestSpawnPointPosition.Value;
            }

            // Do not try finding a NavMesh path if the item is too far away due to performance concerns
            if (Vector3.Distance(nearestPosition, itemPosition) > ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.MaxPathSearchDistance)
            {
                return;
            }

            // Try to find a path to the loot item via the NavMesh from the nearest realistic position determined above
            PathAccessibilityData fullAccessibilityData = NavMeshController.GetPathAccessibilityData(nearestPosition, itemPosition, lootPathName);
            LootInfo[item].PathData.Merge(fullAccessibilityData);

            // If the last search resulted in an incomplete path, remove the marker for the previous target NavMesh position
            if (LootInfo[item].PathData.IsAccessible && (LootInfo[item].PathData.LastNavPointOutline != null))
            {
                LootInfo[item].PathData.LastNavPointOutline.Clear();
            }

            LootInfo[item].PathData.Update();
        }

        private static string GetLootPathName(Item item)
        {
            return item.LocalizedName() + "_" + item.Id;
        }

        private static void FindItemsToDestroy(Item item, int totalItemsToDestroy, int lootSlotsToDestroy, List<Item> allItemsToDestroy)
        {
            // Do not search for more items if enough have already been identified
            if (allItemsToDestroy.Count >= totalItemsToDestroy)
            {
                return;
            }

            // Make sure the item isn't already in the queue to be destroyed
            if (allItemsToDestroy.Contains(item))
            {
                return;
            }

            // Do not search for more items if enough slots will be destroyed for the items in the queue
            if (allItemsToDestroy.Sum(i => i.GetItemSlots()) >= lootSlotsToDestroy)
            {
                return;
            }

            // Find all parents of the item. Need to do this in case the item is (for example) a gun. If only the gun item is destroyed,
            // all of the mods, magazines, etc. on it will be orphaned and cause errors
            IEnumerable<Item> parentItems = item.ToEnumerable();
            try
            {
                IEnumerable<Item> _parentItems = item.GetAllParentItems();
                parentItems = parentItems.Concat(_parentItems);
            }
            catch (Exception)
            {
                LoggingController.LogError("Could not get parents of " + item.LocalizedName() + " (" + item.TemplateId + ")");
                throw;
            }

            // Remove all invalid items from the parent list (secure containers, fixed loot containers, etc.)
            try
            {
                parentItems = parentItems.RemoveExcludedItems();
            }
            catch (Exception)
            {
                LoggingController.LogError("Could not removed excluded items from " + string.Join(",", parentItems.Select(i => i.LocalizedName())));
                throw;
            }

            // Check if there aren't any items remaining after filtering
            if (parentItems.Count() == 0)
            {
                return;
            }

            // Get all child items of the parent item. The array needs to be reversed to prevent any of the items from becoming orphaned. 
            Item parentItem = parentItems.Last();

            // Check if the item cannot be removed from its parent
            Item[] allItems;
            if (CanRemoveItemFromParent(item, parentItem))
            {
                allItems = item.GetAllItems().Reverse().ToArray();                
                if (allItems.Length > ConfigController.Config.DestroyLootDuringRaid.LootRanking.ChildItemLimits.Count)
                {
                    LoggingController.LogInfo(item.LocalizedName() + " has too many child items to destroy.");
                    return;
                }

                double allItemsWeight = allItems.Select(i => i.Weight).Sum();
                if ((allItems.Length > 1) && (allItemsWeight > ConfigController.Config.DestroyLootDuringRaid.LootRanking.ChildItemLimits.TotalWeight))
                {
                    LoggingController.LogInfo(item.LocalizedName() + " and its child items are too heavy to destroy.");
                    return;
                }

                AddItemsToDespawnList(allItems, item, allItemsToDestroy);
                return;
            }
            LoggingController.LogInfo(item.LocalizedName() + " cannot be removed from " + parentItem.LocalizedName() + ". Destroying parent item and all children.");

            // Get all children of the parent item and add them to the despawn list
            allItems = parentItem.GetAllItems().Reverse().ToArray();
            AddItemsToDespawnList(allItems, parentItem, allItemsToDestroy);
        }

        private static bool CanRemoveItemFromParent(Item item, Item parentItem)
        {
            if (item.TemplateId == parentItem.TemplateId)
            {
                return true;
            }

            LootItemClass lootItemClass;
            if ((lootItemClass = (parentItem as LootItemClass)) == null)
            {
                return true;
            }

            foreach(Slot slot in lootItemClass.Slots)
            {
                /*if (!slot.Required)
                {
                    continue;
                }

                if (slot.Items.Contains(item))
                {
                    return false;
                }*/

                if (slot.RemoveItem(true).Failed)
                {
                    return false;
                }
            }

            return true;
        }

        private static int AddItemsToDespawnList(Item[] items, Item parentItem, List<Item> allItemsToDestroy)
        {
            int despawnCount = 0;
            foreach (Item item in items)
            {
                despawnCount += AddItemToDespawnList(item, parentItem, allItemsToDestroy) ? 1: 0;
            }
            return despawnCount;
        }

        private static bool AddItemToDespawnList(Item item, Item parentItem, List<Item> allItemsToDestroy)
        {
            if (allItemsToDestroy.Contains(item))
            {
                return false;
            }

            if (!LootInfo.ContainsKey(item))
            {
                LoggingController.LogWarning("Could not find entry for " + item.LocalizedName());
                return false;
            }

            if (item.CurrentAddress == null)
            {
                LoggingController.LogWarning("Invalid parent for " + item.LocalizedName());
                return false;
            }

            // Ensure child items are destroyed before parent items
            LootInfo[item].ParentItem = parentItem;
            if ((item.Parent.Item != null) && allItemsToDestroy.Contains(item.Parent.Item))
            {
                allItemsToDestroy.Insert(allItemsToDestroy.IndexOf(item.Parent.Item), item);
            }
            else
            {
                allItemsToDestroy.Add(item);
            }

            return true;
        }

        private static void DestroyLoot(Item item, double raidET)
        {
            try
            {
                // If the item is likely behind an interactive object, open it first
                if (LootInfo[item].NearbyInteractiveObject != null)
                {
                    if (LootInfo[item].NearbyInteractiveObject.DoorState == EDoorState.Locked)
                    {
                        throw new InvalidOperationException("Cannot destroy loot behind a locked interactive object");
                    }

                    if (LootInfo[item].NearbyInteractiveObject.DoorState == EDoorState.Shut)
                    {
                        LoggingController.LogInfo("Opening interactive object: " + LootInfo[item].NearbyInteractiveObject.Id + "...");
                        LootInfo[item].NearbyInteractiveObject.Interact(new InteractionResult(EInteractionType.Open));
                    }
                }

                LootInfo[item].TraderController.DestroyItem(item);
                LootInfo[item].IsDestroyed = true;
                LootInfo[item].RaidETWhenDestroyed = raidET;
                lastLootDestroyedTimer.Restart();
                destroyedLootSlots += item.GetItemSlots();

                LoggingController.LogInfo(
                    "Destroyed " + LootInfo[item].LootType + " loot"
                    + (((LootInfo[item].ParentItem != null) && (LootInfo[item].ParentItem.TemplateId != item.TemplateId)) ? " in " + LootInfo[item].ParentItem.LocalizedName() : "")
                    + (ConfigController.LootRanking.Items.ContainsKey(item.TemplateId) ? " (Value=" + ConfigController.LootRanking.Items[item.TemplateId].Value + ")" : "")
                    + ": " + item.LocalizedName()
                );

                LootInfo[item].PathData.Clear();
            }
            catch (Exception ex)
            {
                LoggingController.LogError("Could not destroy " + item.LocalizedName());
                LoggingController.LogError(ex.ToString());
                LootInfo.Remove(item);
            }
        }

        private static IEnumerable<Item> RemoveItemsDroppedByPlayer(this IEnumerable<Item> items)
        {
            return items.Where(i => !ItemsDroppedByMainPlayer.Contains(i));
        }

        private static IEnumerable<Item> RemoveExcludedItems(this IEnumerable<Item> items)
        {
            // This should only be run once to generate the array of secure container ID's
            if (secureContainerIDs.Length == 0)
            {
                secureContainerIDs = ItemHelpers.GetSecureContainerIDs().ToArray();
            }

            IEnumerable<Item> filteredItems = items
                .Where(i => i.Template.Parent == null || !ConfigController.Config.DestroyLootDuringRaid.ExcludedParents.Any(p => i.Template.IsChildOf(p)))
                .Where(i => !ConfigController.Config.DestroyLootDuringRaid.ExcludedParents.Any(p => p == i.TemplateId))
                .Where(i => !secureContainerIDs.Contains(i.TemplateId));

            return filteredItems;
        }

        private static void WriteLootLogFile()
        {
            LoggingController.LogInfo("Writing loot log file...");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Item,Template ID,Value,Raid ET When Found,Raid ET When Destroyed,Accessible");
            foreach(Item item in LootInfo.Keys)
            {
                sb.Append(item.LocalizedName().Replace(",", "") + ",");
                sb.Append(item.TemplateId + ",");
                sb.Append(ConfigController.LootRanking.Items[item.TemplateId].Value + ",");
                sb.Append((LootInfo[item].RaidETWhenFound >= 0 ? LootInfo[item].RaidETWhenFound : 0) + ",");
                sb.Append(LootInfo[item].RaidETWhenDestroyed >= 0 ? LootInfo[item].RaidETWhenDestroyed.ToString() : "");
                sb.AppendLine("," + LootInfo[item].PathData.IsAccessible.ToString());
            }

            string filename = LoggingController.LoggingPath
                + "loot_"
                + currentLocationName.Replace(" ", "")
                + "_"
                + DateTime.Now.ToFileTimeUtc()
                + ".csv";

            try
            {
                if (!Directory.Exists(LoggingController.LoggingPath))
                {
                    Directory.CreateDirectory(LoggingController.LoggingPath);
                }

                File.WriteAllText(filename, sb.ToString());

                LoggingController.LogInfo("Writing loot log file...done.");
            }
            catch (Exception e)
            {
                e.Data.Add("Filename", filename);
                LoggingController.LogError("Writing loot log file...failed!");
                LoggingController.LogError(e.ToString());
            }
        }
    }
}