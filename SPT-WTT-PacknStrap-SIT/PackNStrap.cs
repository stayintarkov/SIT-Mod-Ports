#if !UNITY_EDITOR
using PackNStrap.Patches;
using BepInEx;
using EFT.InventoryLogic;
using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace PackNStrap
{
    [BepInPlugin("com.aaaWTT-PacknStrap.Core", "WTT-PackNStrap", "1.0.0")]

    internal class PackNStrap : BaseUnityPlugin
    {

        public static PackNStrap instance;

        public static string modPath = Path.Combine(Environment.CurrentDirectory, "user", "mods", "WTT-PackNStrap");

        #region Proper Armband Slots Info
        public FieldInfo fastAccessSlots { get; set; }

        public static EquipmentSlot[] newFastAccessSlots = { 
            EquipmentSlot.Pockets, 
            EquipmentSlot.TacticalVest, 
            EquipmentSlot.ArmBand 
        };
        public FieldInfo bindAvailableSlots { get; set; }

        public static EquipmentSlot[] newBindAvailableSlots = {
            EquipmentSlot.FirstPrimaryWeapon,
            EquipmentSlot.SecondPrimaryWeapon,
            EquipmentSlot.Holster,
            EquipmentSlot.Scabbard,
            EquipmentSlot.Pockets,
            EquipmentSlot.TacticalVest,
            EquipmentSlot.ArmBand 
        };
        #endregion

        internal void Awake()
        {
            instance = this;

            #region Proper Belt Fast Access
            fastAccessSlots = fastAccessSlots ?? typeof(Inventory).GetField("FastAccessSlots");
            fastAccessSlots?.SetValue(fastAccessSlots, newFastAccessSlots);

            bindAvailableSlots = bindAvailableSlots ?? typeof(Inventory).GetField("BindAvailableSlotsExtended");
            bindAvailableSlots?.SetValue(bindAvailableSlots, newBindAvailableSlots);


            new ConsolePatch().Enable();


            #endregion

        }
    }
}
#endif