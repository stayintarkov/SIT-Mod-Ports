using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using EFT.UI;
using StayInTarkov;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using static WindowsManager;

namespace PIRM
{
    public class PIRMMethod17Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemSpecificationPanel), "method_17");

        [PatchPrefix]
        public static bool PatchPrefix(ref KeyValuePair<EModLockedState, ModSlotView.GStruct399> __result, Slot slot)
        {
            string text = ((slot.ContainedItem != null) ? slot.ContainedItem.Name.Localized() : string.Empty);

            ModSlotView.GStruct399 structValue = new ModSlotView.GStruct399
            {
                ItemName = text,
            };

            __result = new KeyValuePair<EModLockedState, ModSlotView.GStruct399>(EModLockedState.Unlocked, structValue);

            return false;
        }
    }

    public class InteractionsHandlerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemMovementHandler), "smethod_1");

        [PatchPrefix]
        public static bool Prefix(Item item, ItemAddress to, TraderControllerClass itemController, ref SOperationResult123<GClass3359> __result)
        {

            if (GClass1859.InRaid)
            {
                __result = GClass3359._;
                return false;
            }

            return true;
        }
    }

    public class ItemCheckAction : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EFT.InventoryLogic.Item), "CheckAction");

        [PatchPrefix]
        public static bool Prefix(ItemAddress location, ref bool __result)
        {
            // Set the result to true and return false to skip the original method
            __result = true;
            return false;
        }

    }


    //need this one
    public class EFTInventoryLogicModPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EFT.InventoryLogic.Mod), "CanBeMoved");

        [PatchPrefix]
        public static bool Prefix(IContainer toContainer, ref SOperationResult123<bool> __result)
        {
            __result = true;
            return false;
        }

    }


    public class SlotMethod_2Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Slot), "method_2");

        [PatchPrefix]
        private static bool Prefix(ref Item item, ref bool ignoreRestrictions, ref bool ignoreMalfunction)
        {
            ignoreRestrictions = true;
            ignoreMalfunction = true;
            return true;
        }

    }

    public class LootItemApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(LootItemClass), "Apply");

        [PatchPrefix]
        private static bool Prefix(ref LootItemClass __instance, ref SOperationResult __result, TraderControllerClass itemController, Item item, int count, bool simulate)
        {
            if (!item.ParentRecursiveCheck(__instance))
            {
                __result = new GInventoryError17(item, __instance);
                return false;
            }
            //bool inRaid = GClass1849.InRaid;
            bool inRaid = false;

            Error error = null;
            Error error2 = null;

            Mod mod = item as Mod;
            Slot[] array = ((mod != null && inRaid) ? __instance.VitalParts.ToArray<Slot>() : null);
            Slot.GInventoryError31 gclass;

            if (inRaid && mod != null && !mod.RaidModdable)
            {
                error2 = new GInventoryError14(mod);
            }
            else if (!ItemMovementHandler.CheckMissingParts(mod, __instance.CurrentAddress, itemController, out gclass))
            {
                error2 = gclass;
            }

            bool flag = false;
            foreach (Slot slot in __instance.AllSlots)
            {
                if ((error2 == null || !flag) && slot.CanAccept(item))
                {
                    if (error2 != null)
                    {
                        Slot.GInventoryError31 gclass2;
                        if ((gclass2 = error2 as Slot.GInventoryError31) != null)
                        {
                            error2 = new Slot.GInventoryError31(gclass2.Item, slot, gclass2.MissingParts);
                        }
                        flag = true;
                    }
                    else if (array != null && array.Contains(slot))
                    {
                        error = new GInventoryError15(mod);
                    }
                    else
                    {
                        SlotItemAddress gclass3 = new SlotItemAddress(slot);
                        SOperationResult12<MoveOldMagResult> gstruct = ItemMovementHandler.Move(item, gclass3, itemController, simulate);
                        if (gstruct.Succeeded)
                        {
                            __result = gstruct;
                            return false;
                        }
                        SOperationResult12<GPopNewAmmoResult2> gstruct2 = ItemMovementHandler.SplitMax(item, int.MaxValue, gclass3, itemController, itemController, simulate);
                        if (gstruct2.Succeeded)
                        {
                            __result = gstruct2;
                            return false;
                        }
                        error = gstruct.Error;
                        if (!GClass753.DisabledForNow && GClass2786.CanSwap(item, slot))
                        {
                            __result = null;
                            return false;
                        }
                    }
                }
            }
            if (!flag)
            {
                error2 = null;
            }
            SOperationResult12<IPopNewAmmoResult> gstruct3 = ItemMovementHandler.QuickFindAppropriatePlace(item, itemController, __instance.ToEnumerable<LootItemClass>(), ItemMovementHandler.EMoveItemOrder.Apply, simulate);
            if (gstruct3.Succeeded)
            {
                __result = gstruct3;
                return false;
            }
            if (!(gstruct3.Error is GInventoryError10))
            {
                error = gstruct3.Error;
            }
            Error error3;
            if ((error3 = error2) == null)
            {
                error3 = error ?? new GInventoryError17(item, __instance);
            }
            __result = error3;
            return false;
        }

    }

}












