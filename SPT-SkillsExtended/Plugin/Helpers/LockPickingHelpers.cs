using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkillsExtended.Helpers
{
    internal static class LockPickingHelpers
    {
        public static Dictionary<string, int> DoorAttempts = [];
        public static List<string> InspectedDoors = [];

        private static SkillManager _skills => Utils.GetActiveSkillManager();
        private static Player _player => Singleton<GameWorld>.Instance.MainPlayer;

        private static LockPickingData _lockPicking => Plugin.SkillData.LockPickingSkill;

        private static readonly Dictionary<string, Dictionary<string, int>> LocationDoorIdLevels = new()
        {
            {"factory4_day", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Factory},
            {"factory4_night", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Factory},
            {"Woods", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Woods},
            {"bigmap", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Customs},
            {"Interchange", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Interchange},
            {"RezervBase", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Reserve},
            {"Shoreline", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Shoreline},
            {"laboratory", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Labs},
            {"lighthouse", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Lighthouse},
            {"TarkovStreets", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Streets},
            {"Sandbox", Plugin.SkillData.LockPickingSkill.DoorPickLevels.GroundZero},
        };

        public static void PickLock(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            // Check if a lock pick exists in the inventory
            if (!GetLockPicksInInventory().Any())
            {
                owner.DisplayPreloaderUiNotification("You must have a lock pick in your inventory to pick a lock...");
                return;
            }

            // Check if the locks broken
            if (DoorAttempts.ContainsKey(interactiveObject.Id))
            {
                if (DoorAttempts[interactiveObject.Id] > 3)
                {
                    owner.DisplayPreloaderUiNotification("You cannot pick a broken lock...");
                    return;
                }
            }

            // Only allow lockpicking if the player is stationary
            if (Utils.IdleStateType.IsAssignableFrom(owner.Player.CurrentState.GetType()))
            {
                var currentManagedState = owner.Player.CurrentManagedState;
                var lpTime = CalculateTimeForAction(_lockPicking.PickBaseTime);
                int level = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

                // Return out if the door level is not found
                if (level == -1)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        $"ERROR: Door {interactiveObject.Id} on map {owner.Player.Location} not found in lookup table, sceenshot and report this error to the developer.",
                        EFT.Communications.ENotificationDurationType.Long,
                        EFT.Communications.ENotificationIconType.Alert);

                    return;
                }

                float chanceForSuccess = CalculateChanceForSuccess(interactiveObject, owner);

                owner.ShowObjectivesPanel("Picking lock {0:F1}", lpTime);

                if (chanceForSuccess > 80f)
                {
                    owner.DisplayPreloaderUiNotification("This lock is easy for your level");
                }
                else if (chanceForSuccess < 80f && chanceForSuccess > 0f)
                {
                    owner.DisplayPreloaderUiNotification("This lock is hard for your level");
                }
                else if (chanceForSuccess == 0f)
                {
                    owner.DisplayPreloaderUiNotification("This lock is impossible for your level");
                }

                LockPickActionHandler handler = new()
                {
                    Owner = owner,
                    InteractiveObject = interactiveObject,
                };

                Action<bool> action = new(handler.PickLockAction);
                currentManagedState.Plant(true, false, lpTime, action);
            }
            else
            {
                owner.DisplayPreloaderUiNotification("Cannot pick the lock while moving.");
            }
        }

        public static void InspectDoor(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            int level = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

            // Return out if the door level is not found
            if (level == -1)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    $"ERROR: Door {interactiveObject.Id} on map {owner.Player.Location} not found in lookup table, sceenshot and report this error to the developer.",
                    EFT.Communications.ENotificationDurationType.Long,
                    EFT.Communications.ENotificationIconType.Alert);

                return;
            }

            // Only allow inspecting if the player is stationary
            if (Utils.IdleStateType.IsAssignableFrom(owner.Player.CurrentState.GetType()))
            {
                // If we have not inspected this door yet, inspect it
                if (!InspectedDoors.Contains(interactiveObject.Id))
                {
                    InspectLockActionHandler handler = new()
                    {
                        Owner = owner,
                        InteractiveObject = interactiveObject,
                    };

                    Action<bool> action = new(handler.InspectLockAction);
                    var currentManagedState = owner.Player.CurrentManagedState;
                    var inspectTime = CalculateTimeForAction(_lockPicking.InspectBaseTime);

                    owner.ShowObjectivesPanel("Inspecting lock {0:F1}", inspectTime);
                    currentManagedState.Plant(true, false, inspectTime, action);
                    return;
                }

                DisplayInspectInformation(interactiveObject, owner);
            }
            else
            {
                owner.DisplayPreloaderUiNotification("Cannot inspect the lock while moving.");
            }
        }

        /// <summary>
        /// Get the door level given a location ID and door ID
        /// </summary>
        /// <param name="locationId"></param>
        /// <param name="doorId"></param>
        /// <returns>Door level if found, -1 if not found</returns>
        public static int GetLevelForDoor(string locationId, string doorId)
        {
            if (!LocationDoorIdLevels.ContainsKey(locationId))
            {
                Plugin.Log.LogError($"Could not find location ID: {locationId}");
                return -1;
            }

            var locationLevels = LocationDoorIdLevels[locationId];

            if (!locationLevels.ContainsKey(doorId))
            {
                Plugin.Log.LogError($"Could not find Door ID: {doorId} in location {locationId}");
                return -1;
            }

            return locationLevels[doorId];
        }

        /// <summary>
        /// Get any lockpick in the players equipment inventory
        /// </summary>
        /// <returns>All lockpick items in the players inventory</returns>
        public static IEnumerable<Item> GetLockPicksInInventory()
        {
            return Plugin.Session.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment)
                .Where(x => x.TemplateId == "6622c28aed7e3bc72e301e22");
        }

        private static void ApplyLockPickActionXp(WorldInteractiveObject interactiveObject, GamePlayerOwner owner, bool isInspect = false, bool IsFailure = false)
        {
            var doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

            bool xpExists = Plugin.SkillData.LockPickingSkill.XpTable.TryGetValue(doorLevel.ToString(), out float xp);

            if (xpExists)
            {
                var xpToApply = isInspect
                    ? xp * (Plugin.SkillData.LockPickingSkill.InspectLockXpRatio)
                    : xp;

                // Failures recieve 25% xp
                xpToApply = IsFailure
                    ? xpToApply * 0.25f
                    : xpToApply;

                Plugin.Log.LogInfo($"Lockpicking xp found in table : {xpToApply} experience for door level {doorLevel} : IsInspect {isInspect} : IsFailure {IsFailure}");

                _skills.Lockpicking.Current += xpToApply;

                return;
            }

            Plugin.Log.LogWarning($"Lockpicking xp not found in table.. defaulting to {6f} experience");
            _skills.Lockpicking.Current += 6f;
        }

        private static void DisplayInspectInformation(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            int doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

            // Display inspection info
            NotificationManagerClass.DisplayMessageNotification($"Key for door is {Plugin.Keys.KeyLocale[interactiveObject.KeyId]}");
            NotificationManagerClass.DisplayMessageNotification($"Lock level {doorLevel} chance for success {CalculateChanceForSuccess(interactiveObject, owner)}%");
        }

        private static float CalculateChanceForSuccess(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            int doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

            int levelDifference = _skills.Lockpicking.Level - doorLevel;

            float baseSuccessChance = InspectedDoors.Contains(interactiveObject.Id)
                ? _lockPicking.PickBaseSuccessChance + 10
                : _lockPicking.PickBaseSuccessChance;

            float difficultyModifier = _lockPicking.PickBaseDifficultyMod;

            // Never below 0, never above 100
            float successChance = UnityEngine.Mathf.Clamp(baseSuccessChance + (levelDifference * difficultyModifier), 0f, 100f);

            return successChance;
        }

        private static float CalculateTimeForAction(float baseTime)
        {
            int level = _skills.Lockpicking.Level;
            bool isElite = _skills.Lockpicking.IsEliteLevel;

            float accumulatedRecution = isElite
                ? Mathf.Max(level * _lockPicking.TimeReduction + _lockPicking.TimeReductionElite, 0f)
                : Mathf.Max(level * _lockPicking.TimeReduction, 0f);

            return (baseTime * (1 - accumulatedRecution));
        }

        private sealed class LockPickActionHandler
        {
            public GamePlayerOwner Owner;
            public WorldInteractiveObject InteractiveObject;

            private static SkillManager _skills => Utils.GetActiveSkillManager();

            public void PickLockAction(bool actionCompleted)
            {
                int doorLevel = GetLevelForDoor(Owner.Player.Location, InteractiveObject.Id);

                // If the player completed the full timer uninterrupted
                if (actionCompleted)
                {
                    // Attempt was not successful
                    if (!IsAttemptSuccessful(doorLevel))
                    {
                        Owner.DisplayPreloaderUiNotification("You failed to pick the lock...");

                        // Add to the counter
                        if (!DoorAttempts.ContainsKey(InteractiveObject.Id))
                        {
                            DoorAttempts.Add(InteractiveObject.Id, 1);
                        }
                        else
                        {
                            DoorAttempts[InteractiveObject.Id]++;
                        }

                        // Break the lock if more than 3 failed attempts
                        if (DoorAttempts[InteractiveObject.Id] > 3)
                        {
                            Owner.DisplayPreloaderUiNotification("You broke the lock...");
                            InteractiveObject.KeyId = string.Empty;
                            InteractiveObject.Operatable = false;
                            InteractiveObject.DoorStateChanged(EDoorState.None);
                        }

                        // Apply failure xp
                        ApplyLockPickActionXp(InteractiveObject, Owner, false, true);
                        RemoveUseFromLockpick(doorLevel);

                        return;
                    }

                    RemoveUseFromLockpick(doorLevel);
                    ApplyLockPickActionXp(InteractiveObject, Owner);
                    AccessTools.Method(typeof(WorldInteractiveObject), "Unlock").Invoke(InteractiveObject, null);
                }
                else
                {
                    Owner.CloseObjectivesPanel();
                }
            }

            private void RemoveUseFromLockpick(int doorLevel)
            {
                int levelDifference = _skills.Lockpicking.Level - doorLevel;

                if (doorLevel >= 10)
                {
                    return;
                }

                // Remove a use from a lockpick in the inventory
                var lockPicks = GetLockPicksInInventory();
                Item lockpick = lockPicks.First();

                if (lockpick is GItem13 pick)
                {
                    pick.KeyComponent.NumberOfUsages++;

                    // lockpick has no uses left, destroy it
                    if (pick.KeyComponent.NumberOfUsages >= pick.KeyComponent.Template.MaximumNumberOfUsage && pick.KeyComponent.Template.MaximumNumberOfUsage > 0)
                    {
                        InventoryControllerClass inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(Owner.Player);

                        inventoryController.DestroyItem(lockpick);
                    }
                }
            }

            /// <summary>
            /// Returns true if the pick attempt succeeded
            /// </summary>
            /// <returns></returns>
            private bool IsAttemptSuccessful(int doorLevel)
            {
                int levelDifference = _skills.Lockpicking.Level - doorLevel;

                // Player level is high enough to always pick this lock
                if (levelDifference > 10)
                {
                    Plugin.Log.LogDebug("Pick attempt success chance: Player out leveled this lock: SUCCEED ");
                    return true;
                }

                // Never below 0, never above 100
                float successChance = CalculateChanceForSuccess(InteractiveObject, Owner);
                float roll = UnityEngine.Random.Range(0f, 100f);

                Plugin.Log.LogDebug($"Pick attempt success chance: {successChance}, Roll: {roll}");

                if (successChance > roll)
                {
                    return true;
                }

                return false;
            }
        }

        private sealed class InspectLockActionHandler
        {
            public GamePlayerOwner Owner;
            public WorldInteractiveObject InteractiveObject;

            public void InspectLockAction(bool actionCompleted)
            {
                int doorLevel = GetLevelForDoor(Owner.Player.Location, InteractiveObject.Id);

                // If the player completed the full timer uninterrupted
                if (actionCompleted)
                {
                    // Only apply xp once per door per raid
                    if (!InspectedDoors.Contains(InteractiveObject.Id))
                    {
                        InspectedDoors.Add(InteractiveObject.Id);
                        ApplyLockPickActionXp(InteractiveObject, Owner, true, false);
                    }

                    DisplayInspectInformation(InteractiveObject, Owner);
                }
                else
                {
                    Owner.CloseObjectivesPanel();
                }
            }
        }
    }
}