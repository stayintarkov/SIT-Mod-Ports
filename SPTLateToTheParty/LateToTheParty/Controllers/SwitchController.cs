using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using LateToTheParty.CoroutineExtensions;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class SwitchController : MonoBehaviour
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool HasFoundSwitches { get; private set; } = false;
        public static bool IsTogglingSwitches { get; private set; } = false;
        public static bool HasToggledInitialSwitches { get; private set; } = true;

        private static Dictionary<EFT.Interactive.Switch, bool> hasToggledSwitch = new Dictionary<EFT.Interactive.Switch, bool>();
        private static Dictionary<EFT.Interactive.Switch, double> raidTimeRemainingToToggleSwitch = new Dictionary<EFT.Interactive.Switch, double>();

        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.ToggleSwitchesDuringRaid.MaxCalcTimePerFrame);
        private static Stopwatch switchTogglingTimer = Stopwatch.StartNew();
        private static Stopwatch updateTimer = Stopwatch.StartNew();
        private static System.Random staticRandomGen = new System.Random();

        public static string GetSwitchText(EFT.Interactive.Switch sw) => sw.Id + " (" + (sw.gameObject?.name ?? "???") + ")";
        public static bool CanToggleSwitch(EFT.Interactive.Switch sw) => sw.Operatable && (sw.gameObject.layer == LayerMask.NameToLayer("Interactive"));
        
        private void Update()
        {
            if (IsClearing)
            {
                return;
            }

            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                StartCoroutine(Clear());
                switchTogglingTimer.Restart();

                return;
            }

            if (updateTimer.ElapsedMilliseconds < ConfigController.Config.ToggleSwitchesDuringRaid.TimeBetweenEvents)
            {
                return;
            }
            updateTimer.Restart();

            // Need to wait until the raid starts or Singleton<GameWorld>.Instance.ExfiltrationController will be null
            if (!Singleton<AbstractGame>.Instance.GameTimer.Started())
            {
                switchTogglingTimer.Restart();
                return;
            }

            if (!HasFoundSwitches)
            {
                try
                {
                    findSwitches();
                }
                catch (Exception)
                {
                    // If findSwitches() fails for some reason, HasToggledInitialSwitches must be set to true or all sounds from WorldInteractiveObjects will\
                    // be suppressed by WorldInteractiveObjectPlaySoundPatch
                    HasFoundSwitches = true;
                    HasToggledInitialSwitches = true;
                    
                    throw;
                }
            }

            if (!HasToggledInitialSwitches && shouldlimitEvents())
            {
                return;
            }

            if (!IsTogglingSwitches)
            {
                StartCoroutine(tryToggleAllSwitches());
            }
        }

        public static IEnumerator Clear()
        {
            IsClearing = true;

            if (IsTogglingSwitches)
            {
                enumeratorWithTimeLimit.Abort();

                EnumeratorWithTimeLimit conditionWaiter = new EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsTogglingSwitches, nameof(IsTogglingSwitches), 3000);

                IsTogglingSwitches = false;
            }

            HasFoundSwitches = false;
            IsTogglingSwitches = false;
            HasToggledInitialSwitches = false;

            hasToggledSwitch.Clear();
            raidTimeRemainingToToggleSwitch.Clear();

            IsClearing = false;
        }

        private static void findSwitches()
        {
            // Randomly sort all switches that players can toggle
            EFT.Interactive.Switch[] allSwitches = FindObjectsOfType<EFT.Interactive.Switch>()
                .Where(s => CanToggleSwitch(s))
                .OrderBy(x => staticRandomGen.NextDouble())
                .ToArray();

            // Select a random number of total switches to toggle throughout the raid
            Configuration.MinMaxConfig fractionOfSwitchesToToggleRange = ConfigController.Config.ToggleSwitchesDuringRaid.FractionOfSwitchesToToggle;
            Configuration.MinMaxConfig switchesToToggleRange = fractionOfSwitchesToToggleRange * allSwitches.Length;
            switchesToToggleRange.Round();
            int switchesToToggle = staticRandomGen.Next((int)switchesToToggleRange.Min, (int)switchesToToggleRange.Max);

            for (int i = 0; i < allSwitches.Length; i++)
            {
                hasToggledSwitch.Add(allSwitches[i], false);
                setTimeToToggleSwitch(allSwitches[i], 0, i >= switchesToToggle);
            }

            HasFoundSwitches = true;
        }

        private static void setTimeToToggleSwitch(EFT.Interactive.Switch sw, float minTimeFromNow = 0, bool neverToggle = false)
        {
            // Select a random time during the raid to toggle the switch
            Configuration.MinMaxConfig raidFractionWhenTogglingRange = ConfigController.Config.ToggleSwitchesDuringRaid.RaidFractionWhenToggling;
            double timeRemainingToToggle = -1;
            if (!neverToggle)
            {
                double timeRemainingFractionToToggle = raidFractionWhenTogglingRange.Min + ((raidFractionWhenTogglingRange.Max - raidFractionWhenTogglingRange.Min) * staticRandomGen.NextDouble());
                timeRemainingToToggle = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds * timeRemainingFractionToToggle;
            }

            // If the switch controls an extract point (i.e. the Labs cargo elevator), don't toggle it until after a certain time
            if (Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints.Any(x => x.Switch == sw))
            {
                LoggingController.LogInfo("Switch " + GetSwitchText(sw) + " is used for an extract point");

                float maxTimeRemainingToToggle = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds - ConfigController.Config.ToggleSwitchesDuringRaid.MinRaidETForExfilSwitches;
                timeRemainingToToggle = Math.Min(timeRemainingToToggle, maxTimeRemainingToToggle);
            }

            // If needed, cap the minimum time into the raid when the switch will be toggled
            if (minTimeFromNow > 0)
            {
                float raidTimeRemaining = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
                float maxTimeRemainingToToggle = raidTimeRemaining - minTimeFromNow;

                timeRemainingToToggle = Math.Min(timeRemainingToToggle, maxTimeRemainingToToggle);
            }

            if (raidTimeRemainingToToggleSwitch.ContainsKey(sw))
            {
                raidTimeRemainingToToggleSwitch[sw] = timeRemainingToToggle;
            }
            else
            {
                raidTimeRemainingToToggleSwitch.Add(sw, timeRemainingToToggle);
            }
            LoggingController.LogInfo("Switch " + GetSwitchText(sw) + " will be toggled at " + TimeSpan.FromSeconds(timeRemainingToToggle).ToString("mm':'ss"));
        }

        private static float getSwitchDelayTime(EFT.Interactive.Switch sw1, EFT.Interactive.Switch sw2)
        {
            // Get the delay (in seconds) for one switch to be toggled after another one
            float distance = Vector3.Distance(sw1.transform.position, sw2.transform.position);
            return ConfigController.Config.ToggleSwitchesDuringRaid.DelayAfterPressingPrereqSwitch * distance;
        }

        private static IEnumerator tryToggleAllSwitches()
        {
            try
            {
                IsTogglingSwitches = true;

                float raidTimeRemaining = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();

                // Enumerate all switches that haven't been toggled yet but should
                EFT.Interactive.Switch[] remainingSwitches = hasToggledSwitch
                    .Where(s => !s.Value)
                    .Where(s => raidTimeRemaining < raidTimeRemainingToToggleSwitch[s.Key])
                    .Select(s => s.Key)
                    .ToArray();

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(remainingSwitches, tryToggleSwitch);

                // Add a delay before setting HasToggledInitialSwitches to true to make sure doors have power before they're (possibly) toggled
                yield return new WaitForSeconds(1);
            }
            finally
            {
                IsTogglingSwitches = false;
                HasToggledInitialSwitches = true;
            }
        }

        private static void tryToggleSwitch(EFT.Interactive.Switch sw)
        {
            // Make sure the raid hasn't ended
            Vector3? yourPosition = Singleton<GameWorld>.Instance?.MainPlayer?.Position;
            if (!yourPosition.HasValue)
            {
                return;
            }

            if (sw.DoorState == EDoorState.Interacting)
            {
                //LoggingController.LogInfo("Somebody is already interacting with switch " + GetSwitchText(sw));

                return;
            }

            if (sw.DoorState == EDoorState.Open)
            {
                if (hasToggledSwitch.ContainsKey(sw))
                {
                    hasToggledSwitch[sw] = true;
                }

                return;
            }

            if ((sw.DoorState == EDoorState.Locked) || !CanToggleSwitch(sw))
            {
                // Check if another switch needs to be toggled first before this one is available
                if (sw.PreviousSwitch != null)
                {
                    LoggingController.LogInfo("Switch " + GetSwitchText(sw.PreviousSwitch) + " must be toggled before switch " + sw.Id);

                    tryToggleSwitch(sw.PreviousSwitch);

                    // If this is beginning of a Scav raid, toggle the switch immediately after the prerequisite switch. Otherwise, add a minimum delay. 
                    if (HasToggledInitialSwitches)
                    {
                        float delayBeforeSwitchCanBeToggled = getSwitchDelayTime(sw, sw.PreviousSwitch);
                        //LoggingController.LogInfo("Switch " + GetSwitchText(sw) + " cannot be toggled for another " + delayBeforeSwitchCanBeToggled + "s");

                        setTimeToToggleSwitch(sw, delayBeforeSwitchCanBeToggled, false);

                        return;
                    }
                }
                else
                {
                    LoggingController.LogWarning("Cannot toggle switch " + GetSwitchText(sw));
                }
            }

            // Check if the switch is too close to you to toggle
            float distance = Vector3.Distance(yourPosition.Value, sw.transform.position);
            if (distance < ConfigController.Config.ToggleSwitchesDuringRaid.ExclusionRadius)
            {
                //LoggingController.LogInfo("Switch " + GetSwitchText(sw) + " is too close to you");

                return;
            }

            LoggingController.LogInfo("Toggling switch " + GetSwitchText(sw) + "...");
            sw.Interact(new InteractionResult(EInteractionType.Open));

            if (hasToggledSwitch.ContainsKey(sw))
            {
                hasToggledSwitch[sw] = true;
            }
        }

        private static bool shouldlimitEvents()
        {
            bool shouldLimit = ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.TogglingSwitches
                && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.Enabled
                && (switchTogglingTimer.ElapsedMilliseconds / 1000.0 > ConfigController.Config.OnlyMakeChangesJustAfterSpawning.TimeLimit);

            return shouldLimit;
        }
    }
}
