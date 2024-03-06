using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using HarmonyLib;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public static class LocationSettingsController
    {
        public static bool HasRaidStarted { get; set; } = false;
        public static LocationSettingsClass.Location CurrentLocation { get; private set; } = null;

        private static string[] CarExtractNames = new string[0];
        private static Dictionary<string, Models.LocationSettings> OriginalSettings = new Dictionary<string, Models.LocationSettings>();
        private static Dictionary<EPlayerSideMask, Dictionary<Vector3, Vector3>> nearestSpawnPointPositions = new Dictionary<EPlayerSideMask, Dictionary<Vector3, Vector3>>();
        
        public static void ClearOriginalSettings()
        {
            LoggingController.LogInfo("Discarding cached location parameters...");
            nearestSpawnPointPositions.Clear();
            OriginalSettings.Clear();
            CurrentLocation = null;
            HasRaidStarted = false;
        }
        
        public static void SetCurrentLocation(LocationSettingsClass.Location location)
        {
            CurrentLocation = location;
        }

        public static Vector3? GetNearestSpawnPointPosition(Vector3 position, EPlayerSideMask playerSideMask = EPlayerSideMask.All)
        {
            if (CurrentLocation == null)
            {
                return null;
            }

            // Use the cached nearest position if available
            if (nearestSpawnPointPositions.ContainsKey(playerSideMask) && nearestSpawnPointPositions[playerSideMask].ContainsKey(position))
            {
                return nearestSpawnPointPositions[playerSideMask][position];
            }

            Vector3? nearestPosition = null;
            float nearestDistance = float.MaxValue;

            // Find the nearest spawn point to the desired position
            foreach (SpawnPointParams spawnPoint in CurrentLocation.SpawnPointParams)
            {
                // Make sure the spawn point is valid for at least one of the specified player sides
                if (!spawnPoint.Sides.Any(playerSideMask))
                {
                    continue;
                }

                Vector3 spawnPointPosition = spawnPoint.Position.ToUnityVector3();
                float distance = Vector3.Distance(position, spawnPointPosition);
                if (distance < nearestDistance)
                {
                    nearestPosition = spawnPointPosition;
                    nearestDistance = distance;
                }
            }

            // If a spawn point was selected, cache it
            if (nearestPosition.HasValue)
            {
                if (!nearestSpawnPointPositions.ContainsKey(playerSideMask))
                {
                    nearestSpawnPointPositions.Add(playerSideMask, new Dictionary<Vector3, Vector3>());
                }

                nearestSpawnPointPositions[playerSideMask].Add(position, nearestPosition.Value);
            }

            return nearestPosition;
        }

        public static double InterpolateForFirstCol(double[][] array, double value)
        {
            if (array.Length == 1)
            {
                return array.Last()[1];
            }

            if (value <= array[0][0])
            {
                return array[0][1];
            }

            for (int i = 1; i < array.Length; i++)
            {
                if (array[i][0] >= value)
                {
                    if (array[i][0] - array[i - 1][0] == 0)
                    {
                        return array[i][1];
                    }

                    return array[i - 1][1] + (value - array[i - 1][0]) * (array[i][1] - array[i - 1][1]) / (array[i][0] - array[i - 1][0]);
                }
            }

            return array.Last()[1];
        }

        public static double GetLootRemainingFactor(double timeRemainingFactor)
        {
            return InterpolateForFirstCol(ConfigController.Config.LootMultipliers, timeRemainingFactor);
        }

        public static double GetTargetPlayersFullOfLoot(double timeRemainingFactor)
        {
            double fraction = InterpolateForFirstCol(ConfigController.Config.FractionOfPlayersFullOfLoot, timeRemainingFactor);
            
            // Reduce the amount of loot "slots" that can be destroyed if player Scavs are not allowed to spwan into the map
            if (CurrentLocation.DisabledForScav)
            {
                fraction *= ConfigController.Config.DestroyLootDuringRaid.PlayersWithLootFactorForMapsWithoutPScavs;
            }

            return fraction;
        }

        public static int GetTargetLootSlotsDestroyed(double timeRemainingFactor)
        {
            if (CurrentLocation == null)
            {
                return 0;
            }

            double totalSlots = CurrentLocation.MaxPlayers * ConfigController.Config.DestroyLootDuringRaid.AvgSlotsPerPlayer;
            return (int)Math.Round(GetTargetPlayersFullOfLoot(timeRemainingFactor) * totalSlots);
        }

        public static void AdjustVExChance(LocationSettingsClass.Location location, float chance)
        {
            if (CarExtractNames.Length == 0)
            {
                LoggingController.Logger.LogInfo("Getting car extract names...");
                CarExtractNames = ConfigController.GetCarExtractNames();
            }

            foreach (Settings7 exit in location.exits)
            {
                if (CarExtractNames.Contains(exit.Name))
                {
                    exit.Chance = chance;
                    LoggingController.LogInfo("Vehicle extract " + exit.Name + " chance adjusted to " + Math.Round(exit.Chance, 1) + "%");
                }
            }
        }

        public static void AdjustBossSpawnChances(LocationSettingsClass.Location location, double timeReductionFactor)
        {
            if (!ConfigController.Config.AdjustBotSpawnChances.Enabled || !ConfigController.Config.AdjustBotSpawnChances.AdjustBosses)
            {
                return;
            }

            // Calculate the reduction in boss spawn chances
            float reductionFactor = (float)InterpolateForFirstCol(ConfigController.Config.BossSpawnChanceMultipliers, timeReductionFactor);

            foreach (BossLocationSpawn bossLocation in location.BossLocationSpawn)
            {
                if (ConfigController.Config.AdjustBotSpawnChances.ExcludedBosses.Contains(bossLocation.BossName))
                {
                    continue;
                }

                bossLocation.BossChance *= reductionFactor;
                LoggingController.LogInfo("Boss " + bossLocation.BossName + " spawn adjusted to " + Math.Round(bossLocation.BossChance, 1) + "%");
            }
        }

        public static void CacheLocationSettings(LocationSettingsClass.Location location)
        {
            if (OriginalSettings.ContainsKey(location.Id))
            {
                LoggingController.LogInfo("Recalling original raid settings for " + location.Name + "...");

                location.EscapeTimeLimit = OriginalSettings[location.Id].EscapeTimeLimit;

                foreach (Settings7 exit in location.exits)
                {
                    if (CarExtractNames.Contains(exit.Name))
                    {
                        exit.Chance = OriginalSettings[location.Id].VExChance;
                        LoggingController.LogInfo("Recalling original raid settings for " + location.Name + "...Restored VEX chance to " + exit.Chance);
                    }
                }

                if (location.BossLocationSpawn.Length != OriginalSettings[location.Id].BossSpawnChances.Length)
                {
                    throw new InvalidOperationException("Mismatch in length between boss location array and cached array.");
                }

                for (int i = 0; i < location.BossLocationSpawn.Length; i++)
                {
                    location.BossLocationSpawn[i].BossChance = OriginalSettings[location.Id].BossSpawnChances[i];
                    LoggingController.LogInfo("Recalling original raid settings for " + location.Name + "...Restored " + location.BossLocationSpawn[i].BossName + " spawn chance to " + location.BossLocationSpawn[i].BossChance);
                }

                return;
            }

            LoggingController.LogInfo("Storing original raid settings for " + location.Name + "... (Escape time: " + location.EscapeTimeLimit + ")");

            Models.LocationSettings settings = new Models.LocationSettings(location.EscapeTimeLimit);
            
            foreach (Settings7 exit in location.exits)
            {
                if (CarExtractNames.Contains(exit.Name))
                {
                    settings.VExChance = exit.Chance;
                }
            }

            settings.BossSpawnChances = location.BossLocationSpawn.Select(x => x.BossChance).ToArray();

            OriginalSettings.Add(location.Id, settings);
        }

        public static int GetOriginalEscapeTime(LocationSettingsClass.Location location)
        {
            if (OriginalSettings.ContainsKey(location.Id))
            {
                return OriginalSettings[location.Id].EscapeTimeLimit;
            }

            throw new InvalidOperationException("The original settings for " + location.Id + " were never stored");
        }

        public static ExfiltrationPoint FindVEX()
        {
            if (Singleton<GameWorld>.Instance?.ExfiltrationController?.ExfiltrationPoints == null)
            {
                return null;
            }

            return FindVEX(Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints);
        }

        public static ExfiltrationPoint FindVEX(ExfiltrationPoint[] allExfils)
        {
            if (CarExtractNames.Length == 0)
            {
                LoggingController.Logger.LogInfo("Getting car extract names...");
                CarExtractNames = ConfigController.GetCarExtractNames();
            }

            foreach (ExfiltrationPoint exfil in allExfils)
            {
                if (CarExtractNames.Contains(exfil.Settings.Name))
                {
                    return exfil;
                }
            }

            return null;
        }

        public static void ActivateExfilForPlayer(ExfiltrationPoint exfil, IPlayer player)
        {
            // Needed to start the car extract
            exfil.OnItemTransferred(player);

            // Copied from the end of ExfiltrationPoint.Proceed()
            if (exfil.Status == EExfiltrationStatus.UncompleteRequirements)
            {
                switch (exfil.Settings.ExfiltrationType)
                {
                    case EExfiltrationType.Individual:
                        exfil.SetStatusLogged(EExfiltrationStatus.RegularMode, "Proceed-3");
                        break;
                    case EExfiltrationType.SharedTimer:
                        exfil.SetStatusLogged(EExfiltrationStatus.Countdown, "Proceed-1");
                        break;
                    case EExfiltrationType.Manual:
                        exfil.SetStatusLogged(EExfiltrationStatus.AwaitsManualActivation, "Proceed-2");
                        break;
                }
            }

            LoggingController.LogInfo("Extract " + exfil.Settings.Name + " activated for player " + player.Profile.Nickname);
        }

        public static void DeactivateExfilForPlayer(ExfiltrationPoint exfil, IPlayer player)
        {
            string methodName = "method_5";
            MethodInfo playerDiedMethod = AccessTools.Method(typeof(ExfiltrationPoint), methodName, new Type[] { typeof(EFT.IPlayer) });
            if (playerDiedMethod == null)
            {
                throw new MissingMethodException(nameof(ExfiltrationPoint), methodName);
            }

            playerDiedMethod.Invoke(exfil, new object[] { player });

            LoggingController.LogInfo("Extract " + exfil.Settings.Name + " deactivated for player " + player.Profile.Nickname);
        }
    }
}
