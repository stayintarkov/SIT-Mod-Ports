using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class CarExtractController : MonoBehaviour
    {
        private static ExfiltrationPoint VEXExfil = null;
        private static double carLeaveTime = double.MinValue;
        private static bool carActivated = false;
        private static bool carNotPresent = false;

        private static Stopwatch carDepartureTimer = Stopwatch.StartNew();
        private static Stopwatch updateTimer = Stopwatch.StartNew();
        private static double updateDelay = 0;

        private void Update()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                VEXExfil = null;
                carLeaveTime = double.MinValue;
                carActivated = false;
                carNotPresent = false;

                carDepartureTimer.Restart();
                updateDelay = 0;

                return;
            }

            if (carNotPresent || (updateTimer.ElapsedMilliseconds < updateDelay))
            {
                return;
            }

            updateTimer.Restart();
            updateDelay = 100;

            // Need to wait until the raid starts or Singleton<GameWorld>.Instance.ExfiltrationController will be null
            if (!Singleton<AbstractGame>.Instance.GameTimer.Started())
            {
                carDepartureTimer.Restart();
                return;
            }

            if (!carActivated && shouldlimitEvents())
            {
                return;
            }

            // Try to find the VEX
            if (VEXExfil == null)
            {
                VEXExfil = LocationSettingsController.FindVEX();
                carNotPresent = (VEXExfil == null) || (VEXExfil?.Status == EExfiltrationStatus.NotPresent);

                LoggingController.LogInfo("VEX Found: " + !carNotPresent);

                if (carNotPresent)
                {
                    return;
                }
            }

            // Select the time when the car should leave if one is present on the map
            if (!carNotPresent && (carLeaveTime == double.MinValue))
            {
                System.Random random = new System.Random();
                Configuration.MinMaxConfig leaveTimeRange = ConfigController.Config.CarExtractDepartures.RaidFractionWhenLeaving;

                carLeaveTime = -1;
                if (random.Next(1, 100) <= ConfigController.Config.CarExtractDepartures.ChanceOfLeaving)
                {
                    double leaveTimeFraction = leaveTimeRange.Min + ((leaveTimeRange.Max - leaveTimeRange.Min) * random.NextDouble());
                    carLeaveTime = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds * leaveTimeFraction;

                    LoggingController.LogInfo("The VEX will try to leave at " + TimeSpan.FromSeconds(carLeaveTime).ToString("mm':'ss"));
                }
                else
                {
                    LoggingController.LogInfo("The VEX will not leave during this raid");
                }
            }

            float distance = Vector3.Distance(Singleton<GameWorld>.Instance.MainPlayer.Position, VEXExfil.transform.position);
            if (distance < ConfigController.Config.CarExtractDepartures.ExclusionRadius)
            {
                // Wait until you're a little closer to the car to add some hysteresis
                if (carActivated && (distance < ConfigController.Config.CarExtractDepartures.ExclusionRadius * ConfigController.Config.CarExtractDepartures.ExclusionRadiusHysteresis))
                {
                    // Stop the countdown so you don't get a free ride
                    LocationSettingsController.DeactivateExfilForPlayer(VEXExfil, Singleton<GameWorld>.Instance.MainPlayer);
                    carActivated = false;

                    // Wait a while before the car is allowed to leave again so it's less obvious that this mod is faking it
                    updateDelay = ConfigController.Config.CarExtractDepartures.DelayAfterCountdownReset * 1000;
                }

                return;
            }

            if (carActivated)
            {
                return;
            }

            // Wait until the car should leave
            float raidTimeRemaining = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
            if (raidTimeRemaining > carLeaveTime)
            {
                return;
            }

            // Make the car leave
            VEXExfil.Settings.ExfiltrationTime = ConfigController.Config.CarExtractDepartures.CountdownTime;
            LocationSettingsController.ActivateExfilForPlayer(VEXExfil, Singleton<GameWorld>.Instance.MainPlayer);
            carActivated = true;
        }

        private static bool shouldlimitEvents()
        {
            bool shouldLimit = ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.CarDepartures
                && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.Enabled
                && (carDepartureTimer.ElapsedMilliseconds / 1000.0 > ConfigController.Config.OnlyMakeChangesJustAfterSpawning.TimeLimit);

            return shouldLimit;
        }
    }
}
