using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using LateToTheParty.CoroutineExtensions;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class InteractiveObjectController : MonoBehaviour
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool IsTogglingInteractiveObjescts { get; private set; } = false;
        public static bool IsFindingInteractiveObjects { get; private set; } = false;
        public static bool HasToggledInitialInteractiveObjects { get; private set; } = false;

        private static List<WorldInteractiveObject> toggleableInteractiveObjects = new List<WorldInteractiveObject>();
        private static List<WorldInteractiveObject> eligibleInteractiveObjects = new List<WorldInteractiveObject>();
        private static Dictionary<WorldInteractiveObject, bool> allowedToToggleInteractiveObject = new Dictionary<WorldInteractiveObject, bool>();
        private GamePlayerOwner gamePlayerOwner = null;
        private static MethodInfo canStartInteractionMethodInfo = typeof(WorldInteractiveObject).GetMethod("CanStartInteraction", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Stopwatch updateTimer = Stopwatch.StartNew();
        private static Stopwatch interactiveObjectOpeningsTimer = new Stopwatch();
        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.OpenDoorsDuringRaid.MaxCalcTimePerFrame);
        private static int InteractiveObjectsToToggle = 1;

        public static int ToggleableInteractiveObjectCount
        {
            get { return IsFindingInteractiveObjects ? 0 : toggleableInteractiveObjects.Count; }
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Update()
        {
            if (IsClearing)
            {
                return;
            }

            if (!ConfigController.Config.OpenDoorsDuringRaid.Enabled)
            {
                // Need to do this or it will prevent loot from being despawned
                HasToggledInitialInteractiveObjects = true;

                return;
            }

            // Clear all arrays if not in a raid to reset them for the next raid
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                StartCoroutine(Clear());
                interactiveObjectOpeningsTimer.Reset();

                return;
            }

            if (!LocationSettingsController.HasRaidStarted)
            {
                return;
            }

            // Wait until the previous task completes
            if (IsTogglingInteractiveObjescts || IsFindingInteractiveObjects)
            {
                return;
            }

            // Ensure enough time has passed since the last door event
            if (HasToggledInitialInteractiveObjects && (updateTimer.ElapsedMilliseconds < ConfigController.Config.OpenDoorsDuringRaid.TimeBetweenEvents * 1000))
            {
                return;
            }

            if (!Singleton<AbstractGame>.Instance.GameTimer.Started())
            //if (!StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                return;
            }

            float raidTimeElapsed = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();
            float raidTimeRemaining = StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();

            // Don't run the script before the raid begins
            if (raidTimeElapsed < 3)
            {
                return;
            }

            // Don't check for door eligibility until initial switches have been toggled. Otherwise, some that need to be powered will not be allowed to be opened. 
            if (!SwitchController.HasToggledInitialSwitches)
            {
                return;
            }

            // Only find doors once per raid
            if (ToggleableInteractiveObjectCount == 0)
            {
                gamePlayerOwner = FindObjectOfType<GamePlayerOwner>();
                StartCoroutine(FindAllEligibleInteractiveObjects());
                return;
            }

            // Determine how many doors to toggled accounting for the raid time elapsed when the player spawns in
            InteractiveObjectsToToggle = (int)Math.Max(1, Math.Round(eligibleInteractiveObjects.Count * ConfigController.Config.OpenDoorsDuringRaid.PercentageOfDoorsPerEvent / 100.0));
            if (!HasToggledInitialInteractiveObjects)
            {
                InteractiveObjectsToToggle *= (int)Math.Ceiling(Math.Max(raidTimeElapsed - ConfigController.Config.OpenDoorsDuringRaid.MinRaidET, 0) / ConfigController.Config.OpenDoorsDuringRaid.TimeBetweenEvents);
                InteractiveObjectsToToggle = Math.Min(InteractiveObjectsToToggle, eligibleInteractiveObjects.Count);
            }

            // Do not change doors too early or late into the raid
            if ((raidTimeElapsed < ConfigController.Config.OpenDoorsDuringRaid.MinRaidET) || (raidTimeRemaining < ConfigController.Config.OpenDoorsDuringRaid.MinRaidTimeRemaining))
            {
                if (!HasToggledInitialInteractiveObjects)
                {
                    LoggingController.LogInfo("Interactive objects cannot be opened at this time in the raid");
                    HasToggledInitialInteractiveObjects = true;
                }

                return;
            }

            // Ensure there are doors to toggle
            if (InteractiveObjectsToToggle == 0)
            {
                return;
            }

            // Try to change the state of doors
            StartCoroutine(ToggleRandomInteractiveObjects(InteractiveObjectsToToggle));
            updateTimer.Restart();
            interactiveObjectOpeningsTimer.Start();
        }

        public static IEnumerator Clear()
        {
            IsClearing = true;

            if (IsFindingInteractiveObjects)
            {
                enumeratorWithTimeLimit.Abort();

                EnumeratorWithTimeLimit conditionWaiter = new EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsFindingInteractiveObjects, nameof(IsFindingInteractiveObjects), 3000);

                IsFindingInteractiveObjects = false;
            }
            if (IsTogglingInteractiveObjescts)
            {
                enumeratorWithTimeLimit.Abort();

                EnumeratorWithTimeLimit conditionWaiter = new EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsTogglingInteractiveObjescts, nameof(IsTogglingInteractiveObjescts), 3000);

                IsTogglingInteractiveObjescts = false;
            }

            toggleableInteractiveObjects.Clear();
            eligibleInteractiveObjects.Clear();
            allowedToToggleInteractiveObject.Clear();
            updateTimer.Restart();

            HasToggledInitialInteractiveObjects = false;

            IsClearing = false;
        }

        public static bool IsToggleableInteractiveObject(WorldInteractiveObject interactiveObject)
        {
            return toggleableInteractiveObjects.Any(d => d.Id == interactiveObject.Id);
        }

        public bool ToggleInteractiveObject(WorldInteractiveObject interactiveObject, EDoorState newState)
        {
            // Check if the door is already in the desired state
            if (newState == EDoorState.Shut && (interactiveObject.DoorState == EDoorState.Shut || interactiveObject.DoorState == EDoorState.Locked))
            {
                return false;
            }
            if (newState == EDoorState.Open && interactiveObject.DoorState == EDoorState.Open)
            {
                return false;
            }

            // Unlock or "breach" the door if necessary
            if ((interactiveObject.DoorState == EDoorState.Locked) && (newState == EDoorState.Open))
            {
                if (interactiveObject.KeyId.Length > 0)
                {
                    // Skip the event if changes need to be limited after a certain time has elapsed after spawning
                    if (shouldlimitEvents() && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.OpeningLockedDoors)
                    {
                        return true;
                    }

                    // Check if the door can be unlocked based on chance
                    System.Random randomObj = new System.Random();
                    if (randomObj.Next(0, 100) > ConfigController.Config.OpenDoorsDuringRaid.ChanceOfUnlockingDoors)
                    {
                        return false;
                    }

                    LoggingController.LogInfo("Unlocking interactive object: " + interactiveObject.Id + " (Key ID: " + interactiveObject.KeyId + ")");
                }
                else
                {
                    // Skip the event if changes need to be limited after a certain time has elapsed after spawning
                    if (shouldlimitEvents() && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.OpeningUnlockedDoors)
                    {
                        return true;
                    }

                    Door door = interactiveObject as Door;
                    if (door?.CanBeBreached == true)
                    {
                        LoggingController.LogInfo("Preparing to breach door: " + door.Id);
                    }
                    else
                    {
                        LoggingController.LogInfo("Cannot breach interactive object: " + interactiveObject.Id);
                        return false;
                    }
                }

                interactiveObject.DoorState = EDoorState.Shut;
                interactiveObject.OnEnable();
            }

            // Ignore doors that are currently being opened/closed                    
            if (!(bool)canStartInteractionMethodInfo.Invoke(interactiveObject, new object[] { newState, true }))
            {
                return false;
            }

            if ((interactiveObject.DoorState != EDoorState.Open) && (interactiveObject.DoorState != EDoorState.Locked) && (newState == EDoorState.Open))
            {
                // Skip the event if changes need to be limited after a certain time has elapsed after spawning
                if (shouldlimitEvents() && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.OpeningUnlockedDoors)
                {
                    return true;
                }

                LoggingController.LogInfo("Opening interactive object: " + interactiveObject.Id);

                // This plays the opening noise and animation
                interactiveObject.Interact(new InteractionResult(EInteractionType.Open));
                return true;
            }

            if ((interactiveObject.DoorState == EDoorState.Open) && (newState == EDoorState.Shut))
            {
                // Skip the event if changes need to be limited after a certain time has elapsed after spawning
                if (shouldlimitEvents() && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.ClosingDoors)
                {
                    return true;
                }

                LoggingController.LogInfo("Closing interactive object: " + interactiveObject.Id);

                // This plays the opening noise and animation
                interactiveObject.Interact(new InteractionResult(EInteractionType.Close));
                return true;
            }

            return false;
        }

        public static IEnumerable<WorldInteractiveObject> FindNearbyInteractiveObjects(Vector3 position, float maxDistance)
        {
            return FindNearbyInteractiveObjects(position, maxDistance, typeof(WorldInteractiveObject));
        }

        public static IEnumerable<WorldInteractiveObject> FindNearbyInteractiveObjects(Vector3 position, float maxDistance, Type interactiveObjectType)
        {
            List<WorldInteractiveObject> nearbyInteractiveObjects = new List<WorldInteractiveObject>();

            foreach (WorldInteractiveObject obj in eligibleInteractiveObjects)
            {
                // Check if the interactive object is too far away
                if (Vector3.Distance(obj.transform.position, position) > maxDistance)
                {
                    continue;
                }

                // Check if the interactive object is the correct type
                if (!checkIfObjectIsSubclassOfType(obj, interactiveObjectType))
                {
                    continue;
                }

                nearbyInteractiveObjects.Add(obj);
            }

            return nearbyInteractiveObjects;
        }

        private static bool checkIfObjectIsSubclassOfType(object obj, Type type)
        {
            Type objType = obj.GetType();

            if (objType == type)
            {
                return true;
            }

            if (objType.IsSubclassOf(type))
            {
                return true;
            }

            return false;
        }

        private IEnumerator ToggleRandomInteractiveObjects(int interactiveObjectsToToggle)
        {
            try
            {
                IsTogglingInteractiveObjescts = true;

                // Check which doors are eligible to be toggled
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(eligibleInteractiveObjects.AsEnumerable(), UpdateIfInteractiveObjectIsAllowedToBeToggle);
                IEnumerable<WorldInteractiveObject> doorsThatCanBeToggled = allowedToToggleInteractiveObject.Where(d => d.Value).Select(d => d.Key);

                // Toggle requested number of doors
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Repeat(interactiveObjectsToToggle, ToggleRandomInteractiveObject, doorsThatCanBeToggled, ConfigController.Config.OpenDoorsDuringRaid.MaxCalcTimePerFrame);
            }
            finally
            {
                IsTogglingInteractiveObjescts = false;
                HasToggledInitialInteractiveObjects = true;
            }
        }

        private IEnumerator FindAllEligibleInteractiveObjects()
        {
            try
            {
                IsFindingInteractiveObjects = true;
                eligibleInteractiveObjects.Clear();

                LoggingController.LogInfo("Searching for valid interactive objects...");

                WorldInteractiveObject[] allNormalDoors = FindObjectsOfType<Door>();
                WorldInteractiveObject[] allKaycardDoors = FindObjectsOfType<KeycardDoor>();
                WorldInteractiveObject[] allTrunks = FindObjectsOfType<Trunk>();
                IEnumerable<WorldInteractiveObject> allDoors = allNormalDoors.Concat(allKaycardDoors).Concat(allTrunks);

                LoggingController.LogInfo("Searching for valid interactive objects...found " + allDoors.Count() + " possible interactive objects.");

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allDoors, CheckIfInteractiveObjectIsEligible);

                LoggingController.LogInfo("Searching for valid interactive objects...found " + eligibleInteractiveObjects.Count + " interactive objects.");
            }
            finally
            {
                IsFindingInteractiveObjects = false;
            }
        }

        private void CheckIfInteractiveObjectIsEligible(WorldInteractiveObject interactiveObject)
        {
            // If the door can be toggled, add it to the dictionary
            if (!CheckIfInteractiveObjectCanBeToggled(interactiveObject, true))
            {
                return;
            }
            toggleableInteractiveObjects.Add(interactiveObject);

            // If the door is eligible for toggling during the raid, add it to the dictionary
            if (!IsEligibleInteractiveObject(interactiveObject, true))
            {
                return;
            }
            eligibleInteractiveObjects.Add(interactiveObject);
        }

        private void UpdateIfInteractiveObjectIsAllowedToBeToggle(WorldInteractiveObject interactiveObject)
        {
            bool isAllowedToBeToggled = IsInteractiveObjectAllowedToBeToggled(interactiveObject);

            if (allowedToToggleInteractiveObject.ContainsKey(interactiveObject))
            {
                allowedToToggleInteractiveObject[interactiveObject] = isAllowedToBeToggled;
            }
            else
            {
                allowedToToggleInteractiveObject.Add(interactiveObject, isAllowedToBeToggled);
            }
        }

        private bool IsInteractiveObjectAllowedToBeToggled(WorldInteractiveObject interactiveObject)
        {
            // Ensure you're still in the raid to avoid NRE's when it ends
            if ((Camera.main == null) || (interactiveObject.transform == null))
            {
                return false;
            }

            // Ignore doors that are too close to you
            Vector3 yourPosition = Camera.main.transform.position;
            float doorDist = Vector3.Distance(yourPosition, interactiveObject.transform.position);
            if (doorDist < ConfigController.Config.OpenDoorsDuringRaid.ExclusionRadius)
            {
                return false;
            }

            return true;
        }

        private bool IsEligibleInteractiveObject(WorldInteractiveObject interactiveObject, bool logResult = false)
        {
            // Get all items to search for key ID's
            Dictionary<string, Item> allItems = ItemHelpers.GetAllItems();

            if (interactiveObject.DoorState == EDoorState.Locked)
            {
                if (allItems.ContainsKey(interactiveObject.KeyId) && !ConfigController.Config.OpenDoorsDuringRaid.CanOpenLockedDoors)
                {
                    if (logResult) LoggingController.LogInfo("Searching for valid interactive objects...interactive object " + interactiveObject.Id + " is locked and not allowed to be opened.");
                    return false;
                }

                Door door = interactiveObject as Door;
                if ((door?.CanBeBreached == true) && !ConfigController.Config.OpenDoorsDuringRaid.CanBreachDoors)
                {
                    if (logResult) LoggingController.LogInfo("Searching for valid interactive objects...door " + door.Id + " is not allowed to be breached.");
                    return false;
                }
            }

            return true;
        }

        private bool CheckIfInteractiveObjectCanBeToggled(WorldInteractiveObject interactiveObject, bool logResult = false)
        {
            if (!interactiveObject.Operatable)
            {
                if (logResult) LoggingController.LogInfo("Searching for valid interactive objects...interactive object " + interactiveObject.Id + " is inoperable.");
                return false;
            }

            if (interactiveObject.gameObject.layer != LayerMask.NameToLayer("Interactive"))
            {
                if (logResult) LoggingController.LogInfo("Searching for valid interactive objects...interactive object " + interactiveObject.Id + " is inoperable (wrong layer).");
                return false;
            }

            // Ensure there are context menu options for the door
            InteractionState1 availableActions = GetActionsClass.GetAvailableActions(gamePlayerOwner, interactiveObject);
            if ((availableActions == null) || (availableActions.Actions.Count == 0))
            {
                if (logResult) LoggingController.LogInfo("Searching for valid interactive objects...interactive object " + interactiveObject.Id + " has no interaction options.");
                return false;
            }

            // This is a sanity check but never seems to actually happen
            if (interactiveObject.DoorState != EDoorState.Open && interactiveObject.DoorState != EDoorState.Shut && interactiveObject.DoorState != EDoorState.Locked)
            {
                if (logResult) LoggingController.LogInfo("Searching for valid interactive objects...interactive object " + interactiveObject.Id + " has an invalid state: " + interactiveObject.DoorState);
                return false;
            }

            // Get all items to search for key ID's
            Dictionary<string, Item> allItems = ItemHelpers.GetAllItems();

            if (interactiveObject.DoorState == EDoorState.Locked)
            {
                Door door = interactiveObject as Door;
                if ((door?.CanBeBreached == false) && !allItems.ContainsKey(door.KeyId))
                {
                    if (logResult) LoggingController.LogInfo("Searching for valid interactive objects...door " + door.Id + " is locked and has no valid key.");
                    return false;
                }
            }

            return true;
        }

        private void ToggleRandomInteractiveObject(IEnumerable<WorldInteractiveObject> eligibleInteractiveObjects, int maxCalcTime_ms)
        {
            // Randomly sort eligible doors
            System.Random randomObj = new System.Random();
            IEnumerable<WorldInteractiveObject> randomlyOrderedKeys = eligibleInteractiveObjects.OrderBy(e => randomObj.NextDouble());

            // Try to find a door to toggle, but do not wait too long
            Stopwatch calcTimer = Stopwatch.StartNew();
            while (calcTimer.ElapsedMilliseconds < maxCalcTime_ms)
            {
                // Randomly select a new door state
                EDoorState newState = EDoorState.Open;
                if (randomObj.Next(0, 100) < ConfigController.Config.OpenDoorsDuringRaid.ChanceOfClosingDoors)
                {
                    newState = EDoorState.Shut;
                }

                // Try to make the desired change to each door in the randomly-sorted enumerator
                foreach (WorldInteractiveObject obj in randomlyOrderedKeys)
                {
                    //LoggingController.LogInfo("Attempting to change interactive object " + door.Id + " to " + newState + "...");
                    if (ToggleInteractiveObject(obj, newState))
                    {
                        return;
                    }
                }
            }
        }

        private static bool shouldlimitEvents()
        {
            bool shouldLimit = HasToggledInitialInteractiveObjects
                && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.Enabled
                && (interactiveObjectOpeningsTimer.ElapsedMilliseconds / 1000.0 > ConfigController.Config.OnlyMakeChangesJustAfterSpawning.TimeLimit);

            return shouldLimit;
        }

    }
}
