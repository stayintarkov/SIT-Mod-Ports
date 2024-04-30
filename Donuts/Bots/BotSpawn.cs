using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StayInTarkov;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;
using static Donuts.DonutComponent;
using BotCacheClass = Data1;
using IProfileData = Data8;

#pragma warning disable IDE0007, IDE0044

namespace Donuts
{
    internal class BotSpawn
    {
        internal static AICorePoint GetClosestCorePoint(Vector3 position)
        {
            var botGame = Singleton<IBotGame>.Instance;
            var coversData = botGame.BotsController.CoversData;
            var groupPoint = coversData.GetClosest(position);
            return groupPoint.CorePointInGame;
        }

        private const string PmcSpawnTypes = "pmc,sptusec,sptbear";
        private const string ScavSpawnType = "assault";

        internal static async Task SpawnBots(HotspotTimer hotspotTimer, Vector3 coordinate)
        {
            string hotspotSpawnType = hotspotTimer.Hotspot.WildSpawnType;
            WildSpawnType wildSpawnType = DetermineWildSpawnType(hotspotTimer, hotspotSpawnType);

            // Check Spawn Hard Stop
            if ((PmcSpawnTypes.Contains(hotspotSpawnType) && DonutsPlugin.hardStopOptionPMC.Value) ||
                (hotspotSpawnType == ScavSpawnType && DonutsPlugin.hardStopOptionSCAV.Value))
            {

                if (!IsRaidTimeRemaining(hotspotSpawnType))
                {
                    DonutComponent.Logger.LogDebug("Spawn not allowed due to raid time conditions - skipping this spawn");
                    return;
                }
            }

            // Handle starting bots first
            int maxCount = DetermineMaxBotCount(hotspotSpawnType, hotspotTimer.Hotspot.MaxRandomNumBots);
            if (hotspotTimer.Hotspot.BotTimerTrigger > 9999)
            {
                maxCount = BotCountManager.AllocateBots(hotspotSpawnType, maxCount);
                if (maxCount == 0)
                {
                    DonutComponent.Logger.LogDebug("Starting bot cap reached - no bots can be spawned");
                    return;
                }
            }

            // Check Hard Cap
            if (DonutsPlugin.HardCapEnabled.Value)
            {
                maxCount = BotCountManager.HandleHardCap(hotspotSpawnType, maxCount);
                if (maxCount == 0)
                {
                    DonutComponent.Logger.LogDebug("Hard cap exceeded - no bots can be spawned");
                    return;
                }
            }

            bool isGroup = maxCount > 1;

            await SetupSpawn(hotspotTimer, maxCount, isGroup, wildSpawnType, coordinate);

        }

        private static bool IsRaidTimeRemaining(string hotspotSpawnType)
        {
            int hardStopTime = GetHardStopTime(hotspotSpawnType);
            int raidTimeLeft = (int)StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
            return raidTimeLeft >= hardStopTime;
        }

        private static int GetHardStopTime(string hotspotSpawnType)
        {
            if ((PmcSpawnTypes.Contains(hotspotSpawnType) && DonutsPlugin.hardStopOptionPMC.Value) ||
                (hotspotSpawnType == ScavSpawnType && DonutsPlugin.hardStopOptionSCAV.Value))
            {
                return hotspotSpawnType == ScavSpawnType ? DonutsPlugin.hardStopTimeSCAV.Value : DonutsPlugin.hardStopTimePMC.Value;
            }
            return int.MaxValue;
        }

        private static int DetermineMaxBotCount(string spawnType, int defaultMaxCount)
        {
            string groupChance = spawnType == "assault" ? DonutsPlugin.scavGroupChance.Value : DonutsPlugin.pmcGroupChance.Value;
            return getActualBotCount(groupChance, defaultMaxCount);
        }

        private static async Task SetupSpawn(HotspotTimer hotspotTimer, int maxCount, bool isGroup, WildSpawnType wildSpawnType, Vector3 coordinate)
        {
            DonutComponent.Logger.LogDebug($"Attempting to spawn {(isGroup ? "group" : "solo")} with bot count {maxCount}");
            if (isGroup)
            {
                await SpawnGroupBots(hotspotTimer, maxCount, wildSpawnType, coordinate);
            }
            else
            {
                await SpawnSingleBot(hotspotTimer, wildSpawnType, coordinate);
            }
        }

        private static async Task SpawnGroupBots(HotspotTimer hotspotTimer, int count, WildSpawnType wildSpawnType, Vector3 coordinate)
        {
            DonutComponent.Logger.LogDebug($"Spawning a group of {count} bots.");
            EPlayerSide side = GetSideForWildSpawnType(wildSpawnType);
            var cancellationTokenSource = AccessTools.Field(typeof(BotSpawner), "_cancellationTokenSource").GetValue(botSpawnerClass) as CancellationTokenSource;
            BotDifficulty botDifficulty = GetBotDifficulty(wildSpawnType);
            var BotCacheDataList = DonutsBotPrep.GetWildSpawnData(wildSpawnType, botDifficulty);

            ShallBeGroupParams groupParams = new ShallBeGroupParams(true, true, count);
            if (DonutsBotPrep.FindCachedBots(wildSpawnType, botDifficulty, count) != null)
            {
#if DEBUG
                DonutComponent.Logger.LogWarning("Found grouped cached bots, spawning them.");
#endif
                Vector3? spawnPosition = await SpawnChecks.GetValidSpawnPosition(hotspotTimer.Hotspot, coordinate, DonutsPlugin.maxSpawnTriesPerBot.Value);
                if (!spawnPosition.HasValue)
                {
                    DonutComponent.Logger.LogDebug("No valid spawn position found - skipping this spawn");
                    return;
                }
                await SpawnBotForGroup(BotCacheDataList, wildSpawnType, side, botCreator, botSpawnerClass, (Vector3)spawnPosition, cancellationTokenSource, botDifficulty, count, hotspotTimer);
            }
            else
            {
#if DEBUG
                DonutComponent.Logger.LogWarning($"No grouped cached bots found, generating on the fly for: {hotspotTimer.Hotspot.Name} for {count} grouped number of bots.");
#endif
                await DonutsBotPrep.CreateGroupBots(side, wildSpawnType, botDifficulty, groupParams, count, 1);

                Vector3? spawnPosition = await SpawnChecks.GetValidSpawnPosition(hotspotTimer.Hotspot, coordinate, DonutsPlugin.maxSpawnTriesPerBot.Value);
                if (!spawnPosition.HasValue)
                {
                    DonutComponent.Logger.LogDebug("No valid spawn position found - skipping this spawn");
                    return;
                }
                await SpawnBotForGroup(BotCacheDataList, wildSpawnType, side, botCreator, botSpawnerClass, (Vector3)spawnPosition, cancellationTokenSource, botDifficulty, count, hotspotTimer);
            }
        }

        private static async Task SpawnSingleBot(HotspotTimer hotspotTimer, WildSpawnType wildSpawnType, Vector3 coordinate)
        {
            DonutComponent.Logger.LogDebug($"Spawning a single bot.");
            EPlayerSide side = GetSideForWildSpawnType(wildSpawnType);
            var cancellationTokenSource = AccessTools.Field(typeof(BotSpawner), "_cancellationTokenSource").GetValue(botSpawnerClass) as CancellationTokenSource;
            BotDifficulty botDifficulty = GetBotDifficulty(wildSpawnType);
            var BotCacheDataList = DonutsBotPrep.GetWildSpawnData(wildSpawnType, botDifficulty);

            Vector3? spawnPosition = await SpawnChecks.GetValidSpawnPosition(hotspotTimer.Hotspot, coordinate, DonutsPlugin.maxSpawnTriesPerBot.Value);
            if (!spawnPosition.HasValue)
            {
                DonutComponent.Logger.LogDebug("No valid spawn position found - skipping this spawn");
                return;
            }

            await SpawnBotFromCacheOrCreateNew(BotCacheDataList, wildSpawnType, side, botCreator, botSpawnerClass, (Vector3)spawnPosition, cancellationTokenSource, botDifficulty, hotspotTimer);
        }

        private static WildSpawnType DetermineWildSpawnType(HotspotTimer hotspotTimer, string hotspotSpawnType)
        {
            WildSpawnType wildSpawnType;
            if (DonutsPlugin.forceAllBotType.Value == "PMC")
            {
                wildSpawnType = GetWildSpawnType("pmc");
            }
            else if (DonutsPlugin.forceAllBotType.Value == "SCAV")
            {
                wildSpawnType = GetWildSpawnType("assault");
            }
            else
            {
                wildSpawnType = GetWildSpawnType(hotspotTimer.Hotspot.WildSpawnType);
            }

            if (wildSpawnType == WildSpawnType.sptUsec || wildSpawnType == WildSpawnType.sptBear)
            {
                if (DonutsPlugin.pmcFaction.Value == "USEC")
                {
                    wildSpawnType = WildSpawnType.sptUsec;
                }
                else if (DonutsPlugin.pmcFaction.Value == "BEAR")
                {
                    wildSpawnType = WildSpawnType.sptBear;
                }
            }
            return wildSpawnType;
        }

        private static int GetBotLimit(string spawnType)
        {
            // Limits are defined elsewhere, here we use them directly
            if (spawnType.Contains("pmc"))
                return PMCBotLimit;
            else if (spawnType.Contains("assault"))
                return SCAVBotLimit;
            return 0;
        }


        #region botHelperMethods

        #region botDifficulty
        internal static BotDifficulty GetBotDifficulty(WildSpawnType wildSpawnType)
        {
            if (wildSpawnType == WildSpawnType.assault)
            {
                return grabSCAVDifficulty();
            }
            else if (wildSpawnType == sptUsec || wildSpawnType == sptBear || wildSpawnType == WildSpawnType.pmcBot)
            {
                return grabPMCDifficulty();
            }
            else
            {
                return grabOtherDifficulty();
            }
        }
        internal static BotDifficulty grabPMCDifficulty()
        {
            switch (DonutsPlugin.botDifficultiesPMC.Value.ToLower())
            {
                case "asonline":
                    //return random difficulty from array of easy, normal, hard
                    BotDifficulty[] randomDifficulty = {
                        BotDifficulty.easy,
                        BotDifficulty.normal,
                        BotDifficulty.hard
                    };
                    var diff = UnityEngine.Random.Range(0, 3);
                    return randomDifficulty[diff];
                case "easy":
                    return BotDifficulty.easy;
                case "normal":
                    return BotDifficulty.normal;
                case "hard":
                    return BotDifficulty.hard;
                case "impossible":
                    return BotDifficulty.impossible;
                default:
                    return BotDifficulty.normal;
            }
        }
        internal static BotDifficulty grabSCAVDifficulty()
        {
            switch (DonutsPlugin.botDifficultiesSCAV.Value.ToLower())
            {
                case "asonline":
                    //return random difficulty from array of easy, normal, hard
                    BotDifficulty[] randomDifficulty = {
                        BotDifficulty.easy,
                        BotDifficulty.normal,
                        BotDifficulty.hard
                    };
                    var diff = UnityEngine.Random.Range(0, 3);
                    return randomDifficulty[diff];
                case "easy":
                    return BotDifficulty.easy;
                case "normal":
                    return BotDifficulty.normal;
                case "hard":
                    return BotDifficulty.hard;
                case "impossible":
                    return BotDifficulty.impossible;
                default:
                    return BotDifficulty.normal;
            }
        }
        internal static BotDifficulty grabOtherDifficulty()
        {
            switch (DonutsPlugin.botDifficultiesOther.Value.ToLower())
            {
                case "asonline":
                    //return random difficulty from array of easy, normal, hard
                    BotDifficulty[] randomDifficulty = {
                        BotDifficulty.easy,
                        BotDifficulty.normal,
                        BotDifficulty.hard
                    };
                    var diff = UnityEngine.Random.Range(0, 3);
                    return randomDifficulty[diff];
                case "easy":
                    return BotDifficulty.easy;
                case "normal":
                    return BotDifficulty.normal;
                case "hard":
                    return BotDifficulty.hard;
                case "impossible":
                    return BotDifficulty.impossible;
                default:
                    return BotDifficulty.normal;
            }
        }

        #endregion

        internal static async Task SpawnBotFromCacheOrCreateNew(List<BotCacheClass> botCacheList, WildSpawnType wildSpawnType, EPlayerSide side, IBotCreator ibotCreator,
            BotSpawner botSpawnerClass, Vector3 spawnPosition, CancellationTokenSource cancellationTokenSource, BotDifficulty botDifficulty, HotspotTimer hotspotTimer)
        {
#if DEBUG
            DonutComponent.Logger.LogDebug("Bot Cache is not empty. Finding Cached Bot");
#endif
            var botCacheElement = DonutsBotPrep.FindCachedBots(wildSpawnType, botDifficulty, 1);

            if (botCacheElement != null)
            {
                botCacheList.Remove(botCacheElement);

                var closestBotZone = botSpawnerClass.GetClosestZone(spawnPosition, out float dist);
                var closestCorePoint = GetClosestCorePoint(spawnPosition);
                // may need to check if null?
                botCacheElement.AddPosition(spawnPosition, closestCorePoint.Id);

#if DEBUG
                DonutComponent.Logger.LogWarning($"Spawning bot at distance to player of: {Vector3.Distance(spawnPosition, DonutComponent.gameWorld.MainPlayer.Position)} " +
                    $"of side: {botCacheElement.Side} and difficulty: {botDifficulty} for hotspot {hotspotTimer.Hotspot.Name} ");
#endif
                ActivateBot(closestBotZone, botCacheElement, cancellationTokenSource);
            }

            else
            {
#if DEBUG
                DonutComponent.Logger.LogDebug("Bot Cache is empty for solo bot. Creating a new bot.");
#endif
                CreateNewBot(wildSpawnType, side, ibotCreator, botSpawnerClass, spawnPosition, cancellationTokenSource);
            }
        }

        internal static async Task SpawnBotForGroup(List<BotCacheClass> botCacheList, WildSpawnType wildSpawnType, EPlayerSide side, IBotCreator ibotCreator,
            BotSpawner botSpawnerClass, Vector3 spawnPosition, CancellationTokenSource cancellationTokenSource, BotDifficulty botDifficulty, int maxCount, HotspotTimer hotspotTimer)
        {
            if (botCacheList != null && botCacheList.Count > 0)
            {
                //since last element was the group that was just added, remove it
                var botCacheElement = DonutsBotPrep.FindCachedBots(wildSpawnType, botDifficulty, maxCount);
                botCacheList.Remove(botCacheElement);

                var closestBotZone = botSpawnerClass.GetClosestZone(spawnPosition, out float dist);
                var closestCorePoint = GetClosestCorePoint(spawnPosition);
                // may need to check if null?
                botCacheElement.AddPosition(spawnPosition, closestCorePoint.Id);

#if DEBUG
                DonutComponent.Logger.LogWarning($"Spawning grouped bots at distance to player of: {Vector3.Distance(spawnPosition, DonutComponent.gameWorld.MainPlayer.Position)} " +
                    $"of side: {botCacheElement.Side} and difficulty: {botDifficulty} at hotspot: {hotspotTimer.Hotspot.Name}");
#endif

                ActivateBot(closestBotZone, botCacheElement, cancellationTokenSource);
            }
        }
        internal static async Task CreateNewBot(WildSpawnType wildSpawnType, EPlayerSide side, IBotCreator ibotCreator, BotSpawner botSpawnerClass, Vector3 spawnPosition, CancellationTokenSource cancellationTokenSource)
        {
            BotDifficulty botdifficulty = GetBotDifficulty(wildSpawnType);

            IProfileData botData = new IProfileData(side, wildSpawnType, botdifficulty, 0f, null);
            BotCacheClass bot = await BotCacheClass.Create(botData, ibotCreator, 1, botSpawnerClass);
            var closestCorePoint = GetClosestCorePoint(spawnPosition);
            bot.AddPosition((Vector3)spawnPosition, closestCorePoint.Id);

            var closestBotZone = botSpawnerClass.GetClosestZone((Vector3)spawnPosition, out float dist);
#if DEBUG
            DonutComponent.Logger.LogWarning($"Spawning bot at distance to player of: {Vector3.Distance((Vector3)spawnPosition, DonutComponent.gameWorld.MainPlayer.Position)} " +
                $"of side: {bot.Side} and difficulty: {botdifficulty}");
#endif

            ActivateBot(closestBotZone, bot, cancellationTokenSource);
        }

        internal static void ActivateBot(BotZone botZone, BotCacheClass botData, CancellationTokenSource cancellationTokenSource)
        {
            CreateBotCallbackWrapper createBotCallbackWrapper = new CreateBotCallbackWrapper();
            createBotCallbackWrapper.botData = botData;

            GetGroupWrapper getGroupWrapper = new GetGroupWrapper();

            // Call ActivateBot directly, using our own group callback and bot created callback
            // NOTE: Make sure to pass "false" for the third parameter to avoid "assaultGroup" conversion
            botCreator.ActivateBot(botData, botZone, false, new Func<BotOwner, BotZone, BotsGroup>(getGroupWrapper.GetGroupAndSetEnemies), new Action<BotOwner>(createBotCallbackWrapper.CreateBotCallback), cancellationTokenSource.Token);
        }

        internal static bool IsWithinBotActivationDistance(Entry hotspot, Vector3 position)
        {
            try
            {
                foreach (var player in playerList)
                {
                    if (player == null || player.HealthController == null)
                    {
                        continue;
                    }
                    if (!player.HealthController.IsAlive)
                    {
                        continue;
                    }
                    float distanceSquared = (player.Position - position).sqrMagnitude;

                    float activationDistanceSquared = hotspot.BotTriggerDistance * hotspot.BotTriggerDistance;
                    if (distanceSquared <= activationDistanceSquared)
                    {
                        //TODO - this may be true when it shouldn't, e.g. player 1 is in correct range, player 2 is standing next to spawn point
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        internal static WildSpawnType GetWildSpawnType(string spawnType)
        {
            switch (spawnType.ToLower())
            {
                case "arenafighterevent":
                    return WildSpawnType.arenaFighterEvent;
                case "assault":
                    return WildSpawnType.assault;
                case "assaultgroup":
                    return WildSpawnType.assaultGroup;
                case "bossboar":
                    return WildSpawnType.bossBoar;
                case "bossboarsniper":
                    return WildSpawnType.bossBoarSniper;
                case "bossbully":
                    return WildSpawnType.bossBully;
                case "bossgluhar":
                    return WildSpawnType.bossGluhar;
                case "bosskilla":
                    return WildSpawnType.bossKilla;
                case "bosskojaniy":
                    return WildSpawnType.bossKojaniy;
                case "bosssanitar":
                    return WildSpawnType.bossSanitar;
                case "bosstagilla":
                    return WildSpawnType.bossTagilla;
                case "bosszryachiy":
                    return WildSpawnType.bossZryachiy;
                case "crazyassaultevent":
                    return WildSpawnType.crazyAssaultEvent;
                case "cursedassault":
                    return WildSpawnType.cursedAssault;
                case "exusec-rogues":
                    return WildSpawnType.exUsec;
                case "followerboar":
                    return WildSpawnType.followerBoar;
                case "followerbully":
                    return WildSpawnType.followerBully;
                case "followergluharassault":
                    return WildSpawnType.followerGluharAssault;
                case "followergluharscout":
                    return WildSpawnType.followerGluharScout;
                case "followergluharsecurity":
                    return WildSpawnType.followerGluharSecurity;
                case "followergluharsnipe":
                    return WildSpawnType.followerGluharSnipe;
                case "followerkojaniy":
                    return WildSpawnType.followerKojaniy;
                case "followersanitar":
                    return WildSpawnType.followerSanitar;
                case "followertagilla":
                    return WildSpawnType.followerTagilla;
                case "followerzryachiy":
                    return WildSpawnType.followerZryachiy;
                case "gifter":
                    return WildSpawnType.gifter;
                case "marksman":
                    return WildSpawnType.marksman;
                case "raiders":
                    return WildSpawnType.pmcBot;
                case "sectantpriest":
                    return WildSpawnType.sectantPriest;
                case "sectantwarrior":
                    return WildSpawnType.sectantWarrior;
                case "usec":
                    return WildSpawnType.sptUsec;
                case "bear":
                    return WildSpawnType.sptBear;
                case "sptusec":
                    return WildSpawnType.sptUsec;
                case "sptbear":
                    return WildSpawnType.sptBear;
                case "followerbigpipe":
                    return WildSpawnType.followerBigPipe;
                case "followerbirdeye":
                    return WildSpawnType.followerBirdEye;
                case "bossknight":
                    return WildSpawnType.bossKnight;
                case "pmc":
                    //random wildspawntype is either assigned sptusec or sptbear at 50/50 chance
                    return (UnityEngine.Random.Range(0, 2) == 0) ? WildSpawnType.sptUsec : WildSpawnType.sptBear;
                default:
                    return WildSpawnType.assault;
            }

        }
        internal static EPlayerSide GetSideForWildSpawnType(WildSpawnType spawnType)
        {
            //define spt wildspawn
            WildSpawnType sptUsec = WildSpawnType.sptUsec;
            WildSpawnType sptBear = WildSpawnType.sptBear;

            if (spawnType == WildSpawnType.pmcBot || spawnType == sptUsec)
            {
                return EPlayerSide.Usec;
            }
            else if (spawnType == sptBear)
            {
                return EPlayerSide.Bear;
            }
            else
            {
                return EPlayerSide.Savage;
            }
        }

        #endregion

        #region botGroups

        internal static int getActualBotCount(string pluginGroupChance, int count)
        {
            if (pluginGroupChance == "None")
            {
                return 1;
            }
            else if (pluginGroupChance == "Max")
            {
                return count;
            }
            else if (pluginGroupChance == "Random")
            {
                string[] groupChances = { "None", "Low", "Default", "High", "Max" };
                pluginGroupChance = groupChances[UnityEngine.Random.Range(0, groupChances.Length)];
            }
            else
            {
                // Assuming getGroupChance is the actual implementation for non-random cases
                int actualGroupChance = getGroupChance(pluginGroupChance, count);
                return actualGroupChance;
            }

            // Recursively call the function with the updated pluginGroupChance
            return getActualBotCount(pluginGroupChance, count);
        }

        // i'm not sure how all this works, ChatGPT wrote this for me
        internal static int getGroupChance(string pmcGroupChance, int maxCount)
        {
            int actualMaxCount = maxCount;

            // Adjust probabilities based on maxCount
            double[] probabilities;
            try
            {
                probabilities = GetProbabilityArray(pmcGroupChance);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error getting probability array for group chance {pmcGroupChance}. Using default.");
                probabilities = GetDefaultProbabilityArray(pmcGroupChance);
            }

            System.Random random = new System.Random();

            // Determine actualMaxCount based on pmcGroupChance and probabilities
            actualMaxCount = getOutcomeWithProbability(random, probabilities, maxCount) + 1;

            return Math.Min(actualMaxCount, maxCount);
        }

        internal static double[] GetProbabilityArray(string pmcGroupChance)
        {
            if (DonutsPlugin.groupChanceWeights.TryGetValue(pmcGroupChance, out var relativeWeights))
            {
                double totalWeight = relativeWeights.Sum(); // Sum of all weights
                return relativeWeights.Select(weight => weight / totalWeight).ToArray();
            }

            throw new ArgumentException($"Invalid pmcGroupChance: {pmcGroupChance}");
        }

        internal static double[] GetDefaultProbabilityArray(string pmcGroupChance)
        {
            if (DonutsPlugin.groupChanceWeights.TryGetValue(pmcGroupChance, out var relativeWeights))
            {
                double totalWeight = relativeWeights.Sum(); // Sum of all weights
                return relativeWeights.Select(weight => weight / totalWeight).ToArray();
            }

            throw new ArgumentException($"Invalid pmcGroupChance: {pmcGroupChance}");
        }

        internal static int getOutcomeWithProbability(System.Random random, double[] probabilities, int maxCount)
        {
            double probabilitySum = 0.0;
            foreach (var probability in probabilities)
            {
                probabilitySum += probability;
            }

            if (Math.Abs(probabilitySum - 1.0) > 0.0001)
            {
                throw new InvalidOperationException("Probabilities should sum up to 1.");
            }

            double probabilityThreshold = random.NextDouble();
            double cumulative = 0.0;
            for (int i = 0; i < maxCount; i++)
            {
                cumulative += probabilities[i];
                if (probabilityThreshold < cumulative)
                {
                    return i;
                }
            }
            // Default outcome if probabilities are not well-defined
            return maxCount - 1;
        }

        #endregion



    }
}
