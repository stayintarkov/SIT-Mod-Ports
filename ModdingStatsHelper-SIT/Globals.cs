using EFT.InventoryLogic;
using EFT.UI;
using System.Collections.Generic;

namespace ShowMeTheStats
{
    public static class Globals
    {
        public static bool isWeaponModding = false;

        public static Item mod = null;

        public static List<Slot> allSlots = new List<Slot>();

        public static SimpleTooltip simpleTooltip = null;

        //public static Slot slotType = null;

        public static Item dropDownCurrentItem = null;

        public static bool isKeyPressed = false;

        //some stats are not very interesting to see and will clog up the ui more than anything, so we blacklist them
        public static string[] statBlacklist = {
            EItemAttributeId.CompatibleWith.ToString(),
            EItemAttributeId.Weight.ToString(),
            EItemAttributeId.Size.ToString(),
            //EItemAttributeId.Caliber.ToString(),
            //EItemAttributeId.BulletSpeed.ToString(),
            EItemAttributeId.RaidModdable.ToString(),
            EItemAttributeId.OpticCrate.ToString(),
            //EItemAttributeId.EffectiveDist.ToString(),
            //EItemAttributeId.EffectiveDistance.ToString(),
            //EItemAttributeId.Velocity.ToString(),
            EItemAttributeId.SightingRange.ToString(),
            //EItemAttributeId.AmmoCaliber.ToString(),
            EItemAttributeId.SingleFireRate.ToString(),
            EItemAttributeId.FireRate.ToString(),
            EItemAttributeId.DurabilityBurn.ToString(),
            EItemAttributeId.HeatFactor.ToString(),
            EItemAttributeId.CoolFactor.ToString(),

            EItemAttributeId.MalfFeedChance.ToString(),
            EItemAttributeId.MalfMisfireChance.ToString(),
            EItemAttributeId.LoadUnloadSpeed.ToString(),
            EItemAttributeId.CheckTimeSpeed.ToString(),
            "AutoROF",
            "SemiROF",
        };

        public static void ClearAllGlobals()
        {
            isWeaponModding = false;
            mod = null;
            allSlots.Clear();
            simpleTooltip = null;
            //slotType = null;
            dropDownCurrentItem = null;
            isKeyPressed = false;
        }
    }
}
