using BepInEx;
using DrakiaXYZ.VersionChecker;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using static UseItemsFromAnywhere.UseItemsFromAnywhere;

namespace UseItemsFromAnywhere
{
    [BepInPlugin("com.dirtbikercj.useFromAnywhere", "Use items anywhere", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 29351;

        public static Plugin instance;

        public static EquipmentSlot[] extendedFastAccessSlots =
        {
            EquipmentSlot.Pockets,
            EquipmentSlot.TacticalVest,
            EquipmentSlot.Backpack,
            EquipmentSlot.SecuredContainer,
            EquipmentSlot.ArmBand
        };

        internal void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            instance = this;
            DontDestroyOnLoad(this);

            var fastAccessSlots = AccessTools.Field(typeof(Inventory), "FastAccessSlots");
            fastAccessSlots.SetValue(fastAccessSlots, extendedFastAccessSlots);

            new IsAtBindablePlace().Enable();
            new IsAtReachablePlace().Enable();
        }
    }
}