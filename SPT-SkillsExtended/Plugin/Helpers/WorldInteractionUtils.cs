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
            if (!IsDoorValidForLockPicking(interactiveObject))
            {
                return;
            }

            Action1 action = new()
            {
                Name = "Pick lock",
                Disabled = !interactiveObject.Operatable && !LockPickingHelpers.GetLockPicksInInventory().Any()
            };

            LockPickingInteraction pickLockAction = new(interactiveObject, owner);

            action.Action = new Action(pickLockAction.TryPickLock);
            actionReturn.Actions.Add(action);
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