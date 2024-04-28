using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Comfort.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Donuts.DonutComponent;
using BotCacheClass = Data1;
using CorePointFinder = AICorePointHolder;
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


        internal static async Task SpawnBots(HotspotTimer hotspotTimer, Vector3 coordinate)
        {
            string hotspotSpawnType = hotspotTimer.Hotspot.WildSpawnType;
            if (DonutsPlugin.hardStopOptionPMC.Value && (hotspotSpawnType == "pmc" || hotspotSpawnType == "SPTUsec" || hotspotSpawnType == "sptbear"))
            {
#if DEBUG
                DonutComponent.Logger.LogDebug($"Hard stop PMCs is enabled, checking raid time");
#endif
                var pluginRaidTimeLeft = DonutsPlugin.hardStopTimePMC.Value;
                var raidTimeLeft = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
                if (raidTimeLeft < DonutsPlugin.hardStopTimePMC.Value)
                {
#if DEBUG
                    DonutComponent.Logger.LogDebug($"Time left {raidTimeLeft} is less than your hard stop time {DonutsPlugin.hardStopTimePMC.Value} - skipping this spawn");
#endif
                    return;
                }
            }

            else if (DonutsPlugin.hardStopOptionSCAV.Value && hotspotSpawnType == "assault")
            {
#if DEBUG
                DonutComponent.Logger.LogDebug($"Hard stop SCAVs is enabled, checking raid time");
#endif
                var pluginRaidTimeLeft = DonutsPlugin.hardStopTimeSCAV;
                var raidTimeLeft = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
                if (raidTimeLeft < DonutsPlugin.hardStopTimeSCAV.Value)
                {
#if DEBUG
                    DonutComponent.Logger.LogDebug($"Time left {raidTimeLeft} is less than your hard stop time {DonutsPlugin.hardStopTimeSCAV.Value} - skipping this spawn");
#endif
                    return;
                }
            }

            int maxCount = hotspotTimer.Hotspot.MaxRandomNumBots;
            if (hotspotSpawnType == "pmc" || hotspotSpawnType == "SPTUsec" || hotspotSpawnType == "sptbear")
            {
                string pluginGroupChance = DonutsPlugin.pmcGroupChance.Value;
                maxCount = BotSpawn.getActualBotCount(pluginGroupChance, maxCount);
            }
            else if (hotspotSpawnType == "assault")
            {
                string pluginGroupChance = DonutsPlugin.scavGroupChance.Value;
                maxCount = BotSpawn.getActualBotCount(pluginGroupChance, maxCount);
            }

            bool group = maxCount > 1;
            int maxSpawnAttempts = DonutsPlugin.maxSpawnTriesPerBot.Value;

            //check if we are spawning a group or a single bot
            if (group)
            {
                Vector3? spawnPosition = await SpawnChecks.GetValidSpawnPosition(hotspotTimer.Hotspot, coordinate, maxSpawnAttempts);

                int maxInitialPMCs = PMCBotLimit;
                int maxInitialSCAVs = SCAVBotLimit;

                // quick and dirty, this will likely become some sort of new spawn parameter eventually
                if (hotspotTimer.Hotspot.BotTimerTrigger > 9999)
                {
                    if (hotspotTimer.Hotspot.WildSpawnType == "pmc" || hotspotTimer.Hotspot.WildSpawnType == "SPTUsec" || hotspotTimer.Hotspot.WildSpawnType == "sptbear")
                    {
                        // current doesn't reset until the next raid
                        // doesn't matter right now since we only care about starting bots
                        if (currentInitialPMCs >= maxInitialPMCs)
                        {
#if DEBUG
                            DonutComponent.Logger.LogDebug($"currentInitialPMCs {currentInitialPMCs} is >= than maxInitialPMCs {maxInitialPMCs}, skipping this spawn");
#endif
                            return;
                        }
                        else
                        {
                            int originalInitialPMCs = currentInitialPMCs;
                            currentInitialPMCs += maxCount;
                            // if the next spawn takes it count over the limit, then find the difference and fill up to the cap instead
                            if (currentInitialPMCs > maxInitialPMCs)
                            {
                                maxCount = maxInitialPMCs - originalInitialPMCs;

#if DEBUG
                                DonutComponent.Logger.LogDebug($"Reaching maxInitialPMCs {maxInitialPMCs}, spawning {maxCount} instead");
#endif
                            }
                        }
                    }
                    else if (hotspotTimer.Hotspot.WildSpawnType == "assault")
                    {
                        // current doesn't reset until the next raid
                        // doesn't matter right now since we only care about starting bots
                        if (currentInitialSCAVs >= maxInitialSCAVs)
                        {
#if DEBUG
                            DonutComponent.Logger.LogDebug($"currentInitialSCAVs {currentInitialSCAVs} is >= than maxInitialSCAVs {maxInitialSCAVs}, skipping this spawn");
#endif
                            return;
                        }
                        else
                        {
                            int originalInitialSCAVs = currentInitialSCAVs;
                            currentInitialSCAVs += maxCount;
                            // if the next spawn takes it count over the limit, then find the difference and fill up to the cap instead
                            if (currentInitialSCAVs > maxInitialSCAVs)
                            {
                                maxCount = maxInitialSCAVs - originalInitialSCAVs;
#if DEBUG
                                DonutComponent.Logger.LogDebug($"Reaching maxInitialSCAVs {maxInitialSCAVs}, spawning {maxCount} instead");
#endif
                            }
                        }
                    }
                }

                const string PmcSpawnTypes = "pmc,SPTUsec,sptbear";
                const string ScavSpawnType = "assault";

                bool IsPMC(WildSpawnType role)
                {
                    return role == SPTUsec || role == SPTBear;
                }

                bool IsSCAV(WildSpawnType role)
                {
                    return role == WildSpawnType.assault;
                }

                bool IsSpawnLimitExceeded(string spawnType, int currentBots, int botLimit, int count)
                {
                    if (currentBots + count > botLimit)
                    {
                        count = botLimit - currentBots;

                        // as long as we "owe" bots then we need to spawn them up to the cap
                        if (count > 0)
                        {
#if DEBUG
                            DonutComponent.Logger.LogDebug($"Reaching {spawnType} BotLimit {botLimit}, spawning {count} instead");
#endif
                            return false;
                        }

                        // if count is <= 0 for some reason then we're already at or over the cap so we can skip
                        else
                        {
                            return true;
                        }
                    }
                    return false;
                }

                if (DonutsPlugin.HardCapEnabled.Value)
                {
#if DEBUG
                    DonutComponent.Logger.LogDebug($"Hard cap is enabled, checking bot counts before spawn");
#endif

                    int currentPMCsAlive = 0;
                    int currentSCAVsAlive = 0;
                    foreach (Player bot in gameWorld.RegisteredPlayers)
                    {
                        // is this a PMC or SCAV raid?
                        if (!bot.IsYourPlayer)
                        {
                            if (IsPMC(bot.Profile.Info.Settings.Role))
                            {
                                currentPMCsAlive++;
                            }
                            else if (IsSCAV(bot.Profile.Info.Settings.Role))
                            {
                                currentSCAVsAlive++;
                            }
                        }
                    }

                    if (PmcSpawnTypes.Contains(hotspotTimer.Hotspot.WildSpawnType))
                    {
                        if (IsSpawnLimitExceeded("PMC", currentPMCsAlive, PMCBotLimit, maxCount) && !DonutsPlugin.hotspotIgnoreHardCapPMC.Value)
                        {
                            return;
                        }
                        // if Ignore Hard Cap is enabled then skip
                        else
                        {
#if DEBUG
                            DonutComponent.Logger.LogWarning("PMC Ignore Hard Cap is enabled - spawning bots over hard cap anyway");
#endif
                        }
                    }
                    else if (hotspotTimer.Hotspot.WildSpawnType == ScavSpawnType)
                    {
                        if (!DonutsPlugin.hotspotIgnoreHardCapSCAV.Value && IsSpawnLimitExceeded("SCAV", currentSCAVsAlive, SCAVBotLimit, maxCount))
                        {
                            return;
                        }
                        else
                        {
#if DEBUG
                            DonutComponent.Logger.LogWarning("PMC Ignore Hard Cap is enabled - spawning bots over hard cap anyway");
#endif
                        }
                    }
                }

                WildSpawnType wildSpawnType;
                if (DonutsPlugin.forceAllBotType.Value == "PMC")
                {
                    wildSpawnType = BotSpawn.GetWildSpawnType("pmc");
                }
                else if (DonutsPlugin.forceAllBotType.Value == "SCAV")
                {
                    wildSpawnType = BotSpawn.GetWildSpawnType("assault");
                }
                else
                {
                    wildSpawnType = BotSpawn.GetWildSpawnType(hotspotTimer.Hotspot.WildSpawnType);
                }

                if (wildSpawnType == (WildSpawnType)SPTUsec || wildSpawnType == (WildSpawnType)SPTBear)
                {
                    if (DonutsPlugin.pmcFaction.Value == "USEC")
                    {
                        wildSpawnType = (WildSpawnType)SPTUsec;
                    }
                    else if (DonutsPlugin.pmcFaction.Value == "BEAR")
                    {
                        wildSpawnType = (WildSpawnType)SPTBear;
                    }
                }

                EPlayerSide side = BotSpawn.GetSideForWildSpawnType(wildSpawnType);
                var cancellationTokenSource = AccessTools.Field(typeof(BotSpawner), "_cancellationTokenSource").GetValue(botSpawnerClass) as CancellationTokenSource;
                BotDifficulty botDifficulty = BotSpawn.GetBotDifficulty(wildSpawnType);
                var BotCacheDataList = DonutsBotPrep.GetWildSpawnData(wildSpawnType, botDifficulty);

                ShallBeGroupParams groupParams = new ShallBeGroupParams(true, true, maxCount);

                //check if group bots exist in cache or else create it
                if (DonutsBotPrep.FindCachedBots(wildSpawnType, botDifficulty, maxCount) != null)
                {
#if DEBUG
                    DonutComponent.Logger.LogWarning("Found grouped cached bots, spawning them.");
#endif
                    await BotSpawn.SpawnBotForGroup(BotCacheDataList, wildSpawnType, side, botCreator, botSpawnerClass, (Vector3)spawnPosition, cancellationTokenSource, botDifficulty, maxCount, hotspotTimer);
                }
                else
                {
#if DEBUG
                    DonutComponent.Logger.LogWarning($"No grouped cached bots found, generating on the fly for: {hotspotTimer.Hotspot.Name} for {maxCount} grouped number of bots.");
#endif
                    await DonutsBotPrep.CreateGroupBots(side, wildSpawnType, botDifficulty, groupParams, maxCount, 1);
                    await BotSpawn.SpawnBotForGroup(BotCacheDataList, wildSpawnType, side, botCreator, botSpawnerClass, (Vector3)spawnPosition, cancellationTokenSource, botDifficulty, maxCount, hotspotTimer);
                }
            }
            else
            {
                Vector3? spawnPosition = await SpawnChecks.GetValidSpawnPosition(hotspotTimer.Hotspot, coordinate, maxSpawnAttempts);

                int maxInitialPMCs = PMCBotLimit;
                int maxInitialSCAVs = SCAVBotLimit;

                // quick and dirty, this will likely become some sort of new spawn parameter eventually
                if (hotspotTimer.Hotspot.BotTimerTrigger > 9999)
                {
                    if (hotspotTimer.Hotspot.WildSpawnType == "pmc" || hotspotTimer.Hotspot.WildSpawnType == "SPTUsec" || hotspotTimer.Hotspot.WildSpawnType == "sptbear")
                    {
                        // current doesn't reset until the next raid
                        // doesn't matter right now since we only care about starting bots
                        if (currentInitialPMCs >= maxInitialPMCs)
                        {
#if DEBUG
                            DonutComponent.Logger.LogDebug($"currentInitialPMCs {currentInitialPMCs} is >= than maxInitialPMCs {maxInitialPMCs}, skipping this spawn");
#endif
                            return;
                        }
                        else
                        {
                            int originalInitialPMCs = currentInitialPMCs;
                            currentInitialPMCs += maxCount;
                            // if the next spawn takes it count over the limit, then find the difference and fill up to the cap instead
                            if (currentInitialPMCs > maxInitialPMCs)
                            {
                                maxCount = maxInitialPMCs - originalInitialPMCs;

#if DEBUG
                                DonutComponent.Logger.LogDebug($"Reaching maxInitialPMCs {maxInitialPMCs}, spawning {maxCount} instead");
#endif
                            }
                        }
                    }
                    else if (hotspotTimer.Hotspot.WildSpawnType == "assault")
                    {
                        // current doesn't reset until the next raid
                        // doesn't matter right now since we only care about starting bots
                        if (currentInitialSCAVs >= maxInitialSCAVs)
                        {
#if DEBUG
                            DonutComponent.Logger.LogDebug($"currentInitialSCAVs {currentInitialSCAVs} is >= than maxInitialSCAVs {maxInitialSCAVs}, skipping this spawn");
#endif
                            return;
                        }
                        else
                        {
                            int originalInitialSCAVs = currentInitialSCAVs;
                            currentInitialSCAVs += maxCount;
                            // if the next spawn takes it count over the limit, then find the difference and fill up to the cap instead
                            if (currentInitialSCAVs > maxInitialSCAVs)
                            {
                                maxCount = maxInitialSCAVs - originalInitialSCAVs;
#if DEBUG
                                DonutComponent.Logger.LogDebug($"Reaching maxInitialSCAVs {maxInitialSCAVs}, spawning {maxCount} instead");
#endif
                            }
                        }
                    }
                }

                const string PmcSpawnTypes = "pmc,SPTUsec,sptbear";
                const string ScavSpawnType = "assault";

                bool IsPMC(WildSpawnType role)
                {
                    return role == (WildSpawnType)SPTUsec || role == (WildSpawnType)SPTBear;
                }

                bool IsSCAV(WildSpawnType role)
                {
                    return role == WildSpawnType.assault;
                }

                bool IsSpawnLimitExceeded(string spawnType, int currentBots, int botLimit, int count)
                {
                    if (currentBots + count > botLimit)
                    {
                        count = botLimit - currentBots;

                        // as long as we "owe" bots then we need to spawn them up to the cap
                        if (count > 0)
                        {
#if DEBUG
                            DonutComponent.Logger.LogDebug($"Reaching {spawnType} BotLimit {botLimit}, spawning {count} instead");
#endif
                            return false;
                        }

                        // if count is <= 0 for some reason then we're already at or over the cap so we can skip
                        else
                        {
                            return true;
                        }
                    }
                    return false;
                }

                if (DonutsPlugin.HardCapEnabled.Value)
                {
#if DEBUG
                    DonutComponent.Logger.LogDebug($"Hard cap is enabled, checking bot counts before spawn");
#endif

                    int currentPMCsAlive = 0;
                    int currentSCAVsAlive = 0;
                    foreach (Player bot in gameWorld.RegisteredPlayers)
                    {
                        if (!bot.IsYourPlayer)
                        {
                            if (IsPMC(bot.Profile.Info.Settings.Role))
                            {
                                currentPMCsAlive++;
                            }
                            else if (IsSCAV(bot.Profile.Info.Settings.Role))
                            {
                                currentSCAVsAlive++;
                            }
                        }
                    }

                    if (PmcSpawnTypes.Contains(hotspotTimer.Hotspot.WildSpawnType))
                    {
                        if (IsSpawnLimitExceeded("PMC", currentPMCsAlive, PMCBotLimit, maxCount) && !DonutsPlugin.hotspotIgnoreHardCapPMC.Value)
                        {
                            return;
                        }
                        // if Ignore Hard Cap is enabled then skip
                        else
                        {
#if DEBUG
                            DonutComponent.Logger.LogWarning("PMC Ignore Hard Cap is enabled - spawning bots over hard cap anyway");
#endif
                        }
                    }
                    else if (hotspotTimer.Hotspot.WildSpawnType == ScavSpawnType)
                    {
                        if (!DonutsPlugin.hotspotIgnoreHardCapSCAV.Value && IsSpawnLimitExceeded("SCAV", currentSCAVsAlive, SCAVBotLimit, maxCount))
                        {
                            return;
                        }
                        else
                        {
#if DEBUG
                            DonutComponent.Logger.LogWarning("PMC Ignore Hard Cap is enabled - spawning bots over hard cap anyway");
#endif
                        }
                    }
                }


                WildSpawnType wildSpawnType;
                if (DonutsPlugin.forceAllBotType.Value == "PMC")
                {
                    wildSpawnType = BotSpawn.GetWildSpawnType("pmc");
                }
                else if (DonutsPlugin.forceAllBotType.Value == "SCAV")
                {
                    wildSpawnType = BotSpawn.GetWildSpawnType("assault");
                }
                else
                {
                    wildSpawnType = BotSpawn.GetWildSpawnType(hotspotTimer.Hotspot.WildSpawnType);
                }

                if (wildSpawnType == (WildSpawnType)SPTUsec || wildSpawnType == (WildSpawnType)SPTBear)
                {
                    if (DonutsPlugin.pmcFaction.Value == "USEC")
                    {
                        wildSpawnType = (WildSpawnType)SPTUsec;
                    }
                    else if (DonutsPlugin.pmcFaction.Value == "BEAR")
                    {
                        wildSpawnType = (WildSpawnType)SPTBear;
                    }
                }

                EPlayerSide side = BotSpawn.GetSideForWildSpawnType(wildSpawnType);
                var cancellationTokenSource = AccessTools.Field(typeof(BotSpawner), "_cancellationTokenSource").GetValue(botSpawnerClass) as CancellationTokenSource;
                BotDifficulty botDifficulty = BotSpawn.GetBotDifficulty(wildSpawnType);
                var BotCacheDataList = DonutsBotPrep.GetWildSpawnData(wildSpawnType, botDifficulty);

                await BotSpawn.SpawnBotFromCacheOrCreateNew(BotCacheDataList, wildSpawnType, side, botCreator, botSpawnerClass, (Vector3)spawnPosition, cancellationTokenSource, botDifficulty, hotspotTimer);
            }
        }



        #region botHelperMethods

        #region botDifficulty
        internal static BotDifficulty GetBotDifficulty(WildSpawnType wildSpawnType)
        {
            if (wildSpawnType == WildSpawnType.assault)
            {
                return grabSCAVDifficulty();
            }
            else if (wildSpawnType == SPTUsec || wildSpawnType == sptBear || wildSpawnType == WildSpawnType.pmcBot)
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
                float distanceSquared = (gameWorld.MainPlayer.Position - position).sqrMagnitude;
                float activationDistanceSquared = hotspot.BotTriggerDistance * hotspot.BotTriggerDistance;
                return distanceSquared <= activationDistanceSquared;
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
                    return SPTUsec;
                case "bear":
                    return SPTBear;
                case "SPTUsec":
                    return SPTUsec;
                case "sptbear":
                    return SPTBear;
                case "followerbigpipe":
                    return WildSpawnType.followerBigPipe;
                case "followerbirdeye":
                    return WildSpawnType.followerBirdEye;
                case "bossknight":
                    return WildSpawnType.bossKnight;
                case "pmc":
                    //random wildspawntype is either assigned SPTUsec or sptbear at 50/50 chance
                    return (UnityEngine.Random.Range(0, 2) == 0) ? (WildSpawnType)SPTUsec : (WildSpawnType)SPTBear;
                default:
                    return WildSpawnType.assault;
            }

        }
        internal static EPlayerSide GetSideForWildSpawnType(WildSpawnType spawnType)
        {
            //define spt wildspawn
            WildSpawnType sptUsec = SPTUsec;
            WildSpawnType sptBear = SPTBear;

            if (spawnType == WildSpawnType.pmcBot || spawnType == SPTUsec)
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
