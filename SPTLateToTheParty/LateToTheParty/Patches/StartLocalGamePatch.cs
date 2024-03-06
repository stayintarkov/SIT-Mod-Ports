using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using EFT;
using StayInTarkov;
using ModulePatch = Aki.Reflection.Patching.ModulePatch;

namespace LateToTheParty.Patches
{
    public class StartLocalGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(StayInTarkovPlugin).Assembly.GetType("StayInTarkov.Coop.SITGameModes.CoopSITGame").GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static);
        }
        
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("LateToTheParty");
        
        [Aki.Reflection.Patching.PatchPrefix]
        private static void PatchPrefix(ref LocationSettingsClass.Location location)
        {
            logger.LogInfo("LATE TO THE PARTY GAME START PATCH ? >..................................................................");
            Controllers.LocationSettingsController.SetCurrentLocation(location);

            float raidTimeRemainingFraction = (float)location.EscapeTimeLimit / Controllers.LocationSettingsController.GetOriginalEscapeTime(location);
            Controllers.LoggingController.LogInfo("Time remaining fraction: " + raidTimeRemainingFraction);

            Controllers.LocationSettingsController.AdjustBossSpawnChances(location, raidTimeRemainingFraction);

            // Only used to test car-extract departures
            //Controllers.LocationSettingsController.AdjustVExChance(location, 100);
        }
    }
}
