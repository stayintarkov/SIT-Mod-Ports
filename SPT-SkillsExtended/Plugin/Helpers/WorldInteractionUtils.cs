using Comfort.Common;
using EFT;
using EFT.Interactive;
using System;
using System.Linq;

namespace SkillsExtended.Helpers
{
    public static class WorldInteractionUtils
    {
        public static bool IsBotInteraction(GamePlayerOwner owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner is null...");
            }

            if (owner?.Player?.Id != Singleton<GameWorld>.Instance?.MainPlayer?.Id)
            {
                return true;
            }

            return false;
        }

        public static void AddLockpickingInteraction(this WorldInteractiveObject interactiveObject, InteractionStates actionReturn, GamePlayerOwner owner)
        {
            LockPickingInteraction lockPickInteraction = new(interactiveObject, owner);

            if (!IsDoorValidForLockPicking(interactiveObject))
            {
                // Secondary check to prevent action showing on open or closed doors that have
                // already been picked.
                if (interactiveObject.DoorState == EDoorState.Open || interactiveObject.DoorState == EDoorState.Shut)
                {
                    return;
                }

                Action1 notValidAction = new()
                {
                    Name = "Door cannot be opened",
                    Disabled = interactiveObject.Operatable
                };

                notValidAction.Action = new Action(lockPickInteraction.DoorNotValid);
                actionReturn.Actions.Add(notValidAction);

                return;
            }

            Action1 ValidAction = new()
            {
                Name = "Pick lock",
                Disabled = !interactiveObject.Operatable && !LockPickingHelpers.GetLockPicksInInventory().Any()
            };

            ValidAction.Action = new Action(lockPickInteraction.TryPickLock);
            actionReturn.Actions.Add(ValidAction);
        }

        public static void AddInspectInteraction(this WorldInteractiveObject interactiveObject, InteractionStates actionReturn, GamePlayerOwner owner)
        {
            if (!IsValidDoorForInspect(interactiveObject))
            {
                return;
            }

            Action1 action = new()
            {
                Name = "Inspect Lock",
                Disabled = !interactiveObject.Operatable
            };

            LockInspectInteraction keyInfoAction = new(interactiveObject, owner);

            action.Action = new Action(keyInfoAction.TryInspectLock);
            actionReturn.Actions.Add(action);
        }

        private static bool IsDoorValidForLockPicking(WorldInteractiveObject interactiveObject)
        {
            if (interactiveObject.DoorState != EDoorState.Locked || !interactiveObject.Operatable || !Plugin.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
            {
                return false;
            }

            return true;
        }

        private static bool IsValidDoorForInspect(WorldInteractiveObject interactiveObject)
        {
            if (interactiveObject.KeyId == null || interactiveObject.KeyId == string.Empty
                || !interactiveObject.Operatable || interactiveObject.DoorState != EDoorState.Locked
                || !Plugin.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
            {
                return false;
            }

            return true;
        }

        public sealed class LockPickingInteraction
        {
            public GamePlayerOwner owner;
            public WorldInteractiveObject interactiveObject;

            public LockPickingInteraction()
            { }

            public LockPickingInteraction(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
            {
                this.interactiveObject = interactiveObject ?? throw new ArgumentNullException("Interactive Object is Null...");
                this.owner = owner ?? throw new ArgumentNullException("Owner is null...");
            }

            public void TryPickLock()
            {
                LockPickingHelpers.PickLock(interactiveObject, owner);
            }

            public void DoorNotValid()
            {
                owner.DisplayPreloaderUiNotification("This door is cannot be opened.");
            }
        }

        public sealed class LockInspectInteraction
        {
            public GamePlayerOwner owner;
            public WorldInteractiveObject interactiveObject;

            public LockInspectInteraction()
            { }

            public LockInspectInteraction(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
            {
                this.interactiveObject = interactiveObject ?? throw new ArgumentNullException("Interactive Object is Null...");
                this.owner = owner ?? throw new ArgumentNullException("Owner is null...");
            }

            public void TryInspectLock()
            {
                if (Plugin.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
                {
                    LockPickingHelpers.InspectDoor(interactiveObject, owner);
                    return;
                }

                Plugin.Log.LogError($"Missing locale data for door {interactiveObject.Id} and key {interactiveObject.KeyId}");
            }
        }
    }
}