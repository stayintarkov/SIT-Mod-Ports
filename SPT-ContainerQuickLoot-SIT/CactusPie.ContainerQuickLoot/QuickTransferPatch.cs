using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using StayInTarkov;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;

namespace CactusPie.ContainerQuickLoot
{
    public class QuickTransferPatch : ModulePatch
    {
        private static readonly Regex LootTagRegex = new Regex("@loot[0-9]*", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
        
        protected override MethodBase GetTargetMethod()
        {
            MethodInfo method = typeof(ItemMovementHandler).GetMethod("QuickFindAppropriatePlace", BindingFlags.Public | BindingFlags.Static);
            return method;
        }

        [PatchPrefix]
        public static bool PatchPrefix(
            ref SOperationResult2<IPopNewAmmoResult> __result,
            object __instance,
            Item item,
            ItemController controller,
            IEnumerable<CompoundItem> targets,
            ItemMovementHandler.EMoveItemOrder order,
            bool simulate)
        {
            // If is ctrl+click loot
            if (order == ItemMovementHandler.EMoveItemOrder.MoveToAnotherSide)
            {
                if (!ContainerQuickLootPlugin.EnableForCtrlClick.Value)
                {
                    return true;
                }
            }

            // If is loose loot pick up
            else if (order == ItemMovementHandler.EMoveItemOrder.PickUp && controller.OwnerType == EOwnerType.Profile)
            {
                if (!ContainerQuickLootPlugin.EnableForLooseLoot.Value)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
            
            GameWorld gameWorld = Singleton<GameWorld>.Instance;

            // If gameWorld is null that means the game is currently not in progress, for instance you're in your hideout
            if (gameWorld == null)
            {
                return true;
            }
            
            // This check needs to be done only in game - otherwise we will not be able to receive quest rewards!
            if (item.QuestItem)
            {
                return true;
            }
                
            Player player = GetLocalPlayerFromWorld(gameWorld);
            Inventory inventory = player.Inventory;
                
            if (inventory == null)
            {
                return true;
            }
            
            IEnumerable<IContainer> targetContainers = FindTargetContainers(item, inventory);

            foreach (IContainer collectionContainer in targetContainers)
            {
                if (!(collectionContainer is StashGrid container))
                {
                    return true;
                }
                    
                // ReSharper disable once PossibleMultipleEnumeration
                if (!(targets.SingleOrDefaultWithoutException() is Equipment))
                {
                    continue;
                }

                if (ContainerQuickLootPlugin.AutoMergeStacks.Value && item.StackMaxSize > 1 && item.StackObjectsCount != item.StackMaxSize)
                {
                    foreach (KeyValuePair<Item, LocationInGrid> containedItem in container.ContainedItems)
                    {
                        if (containedItem.Key.Template._id != item.Template._id)
                        {
                            continue;
                        }
                        
                        if (containedItem.Key.StackObjectsCount + item.StackObjectsCount > item.StackMaxSize)
                        {
                            continue;
                        }

                        SOperationResult2<PopNewAmmoResult> mergeResult = ItemMovementHandler.Merge(item, containedItem.Key, controller, simulate);
                        __result = new SOperationResult2<IPopNewAmmoResult>(mergeResult.Value);
                        return false;
                    }
                }


                GridItemAddress location = container.FindLocationForItem(item);
                if (location == null)
                {
                    continue;
                }

                SOperationResult2<MoveOldMagResult> moveResult = ItemMovementHandler.Move(item, location, controller, simulate);
                if (moveResult.Failed)
                {
                    return true;
                }

                if (!moveResult.Value.ItemsDestroyRequired)
                {
                    __result = moveResult.Cast<MoveOldMagResult, IPopNewAmmoResult>();
                }
                    
                return false;
            }

            return true;
        }

        private static IEnumerable<IContainer> FindTargetContainers(Item item, Inventory inventory)
        {
            var matchingContainerCollections = new List<(ContainerCollection containerCollection, int priority)>();
            
            foreach (Item inventoryItem in inventory.Equipment.GetAllItems())
            {
                // It has to be a container collection - an item that we can transfer the loot into
                if (!inventoryItem.IsContainer)
                {
                    continue;
                }

                // The container has to have a tag - later we will check it's the @loot tag
                if (!inventoryItem.TryGetItemComponent(out TagComponent tagComponent))
                {
                    continue;
                }

                // We check if there is a @loot tag
                Match regexMatch = LootTagRegex.Match(tagComponent.Name);

                if (!regexMatch.Success)
                {
                    continue;
                }

                // We check if any of the containers in the collection can hold our item
                var containerCollection = inventoryItem as ContainerCollection;

                if (containerCollection == null || !containerCollection.Containers.Any(container => container.CanAccept(item)))
                {
                    continue;
                }

                // We extract the suffix - if not suffix provided, we assume 0
                // Length of @loot - we only want the number suffix
                const int lootTagLength = 5;

                string priorityString = regexMatch.Value.Substring(lootTagLength);
                int priority = priorityString.Length == 0 ? 0 : int.Parse(priorityString);
                
                matchingContainerCollections.Add((containerCollection, priority));
            }

            IEnumerable<IContainer> result = matchingContainerCollections
                .OrderBy(x => x.priority)
                .SelectMany(x => x.containerCollection.Containers);

            return result;
        }

        private static Player GetLocalPlayerFromWorld(GameWorld gameWorld)
        {
            if (gameWorld == null || gameWorld.MainPlayer == null)
            {
                return null;
            }

            return gameWorld.MainPlayer;
        }
    }
}
