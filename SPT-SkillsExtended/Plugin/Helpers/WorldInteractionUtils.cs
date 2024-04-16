using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillsExtended.Helpers
{
    public static class WorldInteractionUtils
    {
        public static bool IsDoorValidForLockPicking(WorldInteractiveObject interactiveObject)
        {
            if (!interactiveObject.Operatable)
            {
                return false;
            }

            if (interactiveObject.DoorState != EDoorState.Locked)
            {
                return false;
            }

            return true;
        }

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

        public static void AddGetKeyIdToActionList(this WorldInteractiveObject interactiveObject, InteractionStates actionReturn, GamePlayerOwner owner)
        {
            if (interactiveObject.DoorState != EDoorState.Locked || interactiveObject.KeyId == "")
            {
                return;
            }

            Action1 action = new Action1();

            action.Name = "Get key information";
            action.Disabled = !interactiveObject.Operatable;
         
            Interaction keyInfoAction = new Interaction(interactiveObject, owner);
            
            action.Action = new Action(keyInfoAction.GetKeyIdAction);
            actionReturn.Actions.Add(action);
        }

        public sealed class Interaction
        {
            public GamePlayerOwner owner;
            public WorldInteractiveObject interactiveObject;

            public Interaction() { }

            public Interaction(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
            {
                if (interactiveObject == null)
                {
                    throw new ArgumentNullException("Interactive Object is Null...");
                }

                if (owner == null)
                {
                    throw new ArgumentNullException("Owner is null...");
                }

                this.interactiveObject = interactiveObject;
                this.owner = owner;
            }

            public void GetKeyIdAction()
            {
                if (Constants.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
                {
                    NotificationManagerClass.DisplayMessageNotification($"Key for door is {Constants.Keys.KeyLocale[interactiveObject.KeyId]}");
                }

                
                AccessTools.Method(typeof(WorldInteractiveObject), "Unlock").Invoke(interactiveObject, null);

                Plugin.Log.LogDebug($"Key ID for door {interactiveObject.Id} is {interactiveObject.KeyId}");
            }

            public static bool IsValidBlankKeyInInventory(string Id)
            {
                return Plugin.Session.Profile.Inventory.EquippedInSlotsTemplateIds.Where(x => x == Id).Any();
            }

            public static void SetBlankKeyToDoorKey(Item item, string doorKeyId)
            {
                if (item.GetItemComponent<KeyComponent>() != null)
                {
                    item.Template._id = doorKeyId;
                }
            }
        }
    }  
}
