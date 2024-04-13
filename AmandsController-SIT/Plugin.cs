using StayInTarkov;
using BepInEx;
using System.Reflection;
using UnityEngine;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Linq;
using EFT.InventoryLogic;
using System.Collections.Generic;
using EFT;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine.UI;
using System.Threading.Tasks;
using Sirenix.Utilities;
using EFT.InputSystem;
using UnityEngine.EventSystems;
using StayInTarkov.Coop;

namespace AmandsController
{
    [BepInPlugin("com.Amanda.Controller", "Controller", "0.3.3")]
    public class AmandsControllerPlugin : BaseUnityPlugin
    {
        public static GameObject Hook;
        public static AmandsControllerClass AmandsControllerClassComponent;
        public static InputGetKeyDownLeftControlPatch LeftControl;

        // AmandsController
        public static ConfigEntry<int> UserIndex { get; set; }

        // Input
        public static ConfigEntry<float> LSDeadzone { get; set; }
        public static ConfigEntry<float> RSDeadzone { get; set; }
        public static ConfigEntry<float> LTDeadzone { get; set; }
        public static ConfigEntry<float> RTDeadzone { get; set; }
        public static ConfigEntry<float> DoubleClickDelay { get; set; }
        public static ConfigEntry<float> HoldDelay { get; set; }

        // Aim
        public static ConfigEntry<Vector2> Sensitivity { get; set; }
        public static ConfigEntry<Vector2> AimingSensitivity { get; set; }
        public static ConfigEntry<bool> InvertY { get; set; }
        public static ConfigEntry<float> AimDeadzone { get; set; }

        // Aim Assist
        public static ConfigEntry<bool> Magnetism { get; set; }
        public static ConfigEntry<float> Stickiness { get; set; }
        public static ConfigEntry<float> StickinessSmooth { get; set; }
        public static ConfigEntry<float> AutoAim { get; set; }
        public static ConfigEntry<float> AutoAimSmooth { get; set; }
        public static ConfigEntry<float> MagnetismRadius { get; set; }
        public static ConfigEntry<float> StickinessRadius { get; set; }
        public static ConfigEntry<float> AutoAimRadius { get; set; }
        public static ConfigEntry<float> Radius { get; set; }


        // Movement
        public static ConfigEntry<float> MovementDeadzone { get; set; }
        public static ConfigEntry<float> DeadzoneBuffer { get; set; }
        public static ConfigEntry<float> LeanSensitivity { get; set; }

        // UI
        public static ConfigEntry<bool> DualsenseIcons { get; set; }
        public static ConfigEntry<float> ScrollSensitivity { get; set; }

        // UI Selected Box
        public static ConfigEntry<Color> SelectColor { get; set; }

        // UI Button Blocks
        public static ConfigEntry<Vector2> BlockPosition { get; set; }
        public static ConfigEntry<Vector2> BlockSize { get; set; }
        public static ConfigEntry<int> BlockSpacing { get; set; }
        public static ConfigEntry<int> BlockIconSpacing { get; set; }
        public static ConfigEntry<int> PressFontSize { get; set; }
        public static ConfigEntry<int> HoldDoubleClickFontSize { get; set; }

        private void Awake()
        {
            Debug.LogError("Controller Awake()");
            Hook = new GameObject();
            Hook.name = "AmandsController";
            AmandsControllerClassComponent = Hook.AddComponent<AmandsControllerClass>();
            DontDestroyOnLoad(Hook);
        }

        private void Start()
        {

            UserIndex = Config.Bind("AmandsController", "User Index", 1, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));

            LSDeadzone = Config.Bind("Inputs", "LSDeadzone", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150, IsAdvanced = true }));
            RSDeadzone = Config.Bind("Inputs", "RSDeadzone", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140, IsAdvanced = true }));
            LTDeadzone = Config.Bind("Inputs", "LTDeadzone", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true }));
            RTDeadzone = Config.Bind("Inputs", "RTDeadzone", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));
            DoubleClickDelay = Config.Bind("Inputs", "DoubleClickDelay", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true }));
            HoldDelay = Config.Bind("Inputs", "HoldDelay", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));

            Sensitivity = Config.Bind("Aim", "Sensitivity", new Vector2(20f, 12f), new ConfigDescription("EFT Mouse Sensivity affects this value", null, new ConfigurationManagerAttributes { Order = 140 }));
            AimingSensitivity = Config.Bind("Aim", "AimingSensitivity", new Vector2(20f, 12f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130 }));
            InvertY = Config.Bind("Aim", "InvertY", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120 }));
            AimDeadzone = Config.Bind("Aim", "AimDeadzone", 0.08f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));

            Magnetism = Config.Bind("Aim Assist", "Magnetism", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180 }));
            Stickiness = Config.Bind("Aim Assist", "Stickiness", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));
            AutoAim = Config.Bind("Aim Assist", "AutoAim", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160 }));
            StickinessSmooth = Config.Bind("Aim Assist", "StickinessSmooth", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150, IsAdvanced = true }));
            AutoAimSmooth = Config.Bind("Aim Assist", "AutoAimSmooth", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140, IsAdvanced = true }));
            MagnetismRadius = Config.Bind("Aim Assist", "MagnetismRadius", 0.1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true }));
            StickinessRadius = Config.Bind("Aim Assist", "StickinessRadius", 0.2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));
            AutoAimRadius = Config.Bind("Aim Assist", "AutoAimRadius", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true }));
            Radius = Config.Bind("Aim Assist", "Radius", 5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));

            MovementDeadzone = Config.Bind("Movement", "MovementDeadzone", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130 }));
            DeadzoneBuffer = Config.Bind("Movement", "DeadzoneBuffer", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));
            LeanSensitivity = Config.Bind("Movement", "LeanSensitivity", 50f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));

            DualsenseIcons = Config.Bind("UI", "DualsenseIcons", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));
            ScrollSensitivity = Config.Bind("UI", "ScrollSensitivity", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));

            SelectColor = Config.Bind("UI Selected Box", "SelectColor", new Color(1f, 0.7659f, 0.3518f, 1), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));

            BlockPosition = Config.Bind("UI Button Blocks", "BlockPosition", new Vector2(-30f, 57f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));
            BlockSize = Config.Bind("UI Button Blocks", "BlockSize", new Vector2(40f, 40f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140, IsAdvanced = true }));
            BlockSpacing = Config.Bind("UI Button Blocks", "BlockSpacing", 16, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true }));
            BlockIconSpacing = Config.Bind("UI Button Blocks", "BlockIconSpacing", 8, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));
            PressFontSize = Config.Bind("UI Button Blocks", "PressFontSize", 20, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true }));
            HoldDoubleClickFontSize = Config.Bind("UI Button Blocks", "HoldDoubleClickFontSize", 12, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));

            new AmandsCoopPlayerPatchController().Enable();
            new AmandsLocalPlayerPatchController().Enable();
            new AmandsTarkovApplicationPatch().Enable();
            new AmandsSSAAPatch().Enable();
            new AmandsInventoryScreenShowPatch().Enable();
            new AmandsInventoryScreenClosePatch().Enable();
            new AmandsActionPanelPatch().Enable();
            new BattleStancePanelPatch().Enable();
            // Controller UI
            new TemplatedGridsViewShowPatch().Enable();
            new GeneratedGridsViewShowPatch().Enable();
            //new TradingGridViewShowPatch().Enable();
            //new TradingGridViewTraderShowPatch().Enable();
            //new TradingTableGridViewShowPatch().Enable();
            new GridViewHidePatch().Enable();
            new ContainedGridsViewClosePatch().Enable();
            new ItemSpecificationPanelShowPatch().Enable();
            new ItemSpecificationPanelClosePatch().Enable();
            new EquipmentTabShowPatch().Enable();
            new EquipmentTabHidePatch().Enable();
            new ContainersPanelShowPatch().Enable();
            new ContainersPanelClosePatch().Enable();
            new SearchButtonShowPatch().Enable();
            new SearchButtonClosePatch().Enable();
            new ContextMenuButtonShowPatch().Enable();
            new ContextMenuButtonClosePatch().Enable();
            new ScrollRectNoDragOnEnable().Enable();
            new ScrollRectNoDragOnDisable().Enable();

            new ItemViewOnBeginDrag().Enable();
            new ItemViewOnEndDrag().Enable();
            new ItemViewUpdate().Enable();
            new DraggedItemViewMethod_3().Enable();
            new TooltipMethod_0().Enable();
            new SimpleStashPanelShowPatch().Enable();
            new SplitDialogShowPatch().Enable();
            new SplitDialogHidePatch().Enable();
            new SearchableSlotViewShowPatch().Enable();
            new SearchableSlotViewHidePatch().Enable();

            LeftControl = new InputGetKeyDownLeftControlPatch();
        }
    }

    public class AmandsCoopPlayerPatchController : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(StayInTarkovPlugin).Assembly.GetType("StayInTarkov.Coop.SITGameModes.CoopSITGame").GetMethod("vmethod_2", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void WaitForCoopGame(Task<LocalPlayer> task)
        {
            task.Wait();

            LocalPlayer localPlayer = task.Result;

            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.UpdateController(localPlayer);
            }
        }

        [PatchPostfix]
        private static void PatchPostFix(Task<LocalPlayer> __result)
        {
            Task.Run(() => WaitForCoopGame(__result));
        }
    }
    public class AmandsLocalPlayerPatchController : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalPlayer).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Task<LocalPlayer> __result)
        {
            LocalPlayer localPlayer = __result.Result;
            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.UpdateController(localPlayer);
            }
        }
    }
    public class AmandsTarkovApplicationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TarkovApplication __instance, InputTree inputTree)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.inputTree = inputTree;
        }
    }
    public class AmandsSSAAPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SSAA).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SSAA __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.currentSSAA = __instance;
        }
    }
    public class AmandsInventoryScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen).GetMethods(BindingFlags.Instance | BindingFlags.Public).First(x => x.Name == "Show");
        }
        [PatchPostfix]
        private static void PatchPostFix(ref InventoryScreen __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.Tabs = Traverse.Create(__instance).Field("dictionary_0").GetValue<Dictionary<InventoryScreen.EInventoryTab, Tab>>();
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateInterfaceBinds(true);
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateInterface(__instance);
            AsynControllerUIMoveToClosest();
        }
        private static async void AsynControllerUIMoveToClosest()
        {
            await Task.Delay(200);
            AmandsControllerPlugin.AmandsControllerClassComponent.ControllerUIMoveToClosest(false);
        }
    }
    public class AmandsInventoryScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref InventoryScreen __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateInterfaceBinds(false);
        }
    }
    public class AmandsActionPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ActionPanel).GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ActionPanel __instance)
        {
            // Possible Explosion of the whole mod
            bool Enabled = Traverse.Create(__instance).Field("bool_0").GetValue<bool>();
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateActionPanelBinds(Enabled);
        }
    }
    public class BattleStancePanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BattleStancePanel).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref BattleStancePanel __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.speedSlider = Traverse.Create(__instance).Field("_speedSlider").GetValue<Slider>();
        }
    }
    // Controller UI
    public class TemplatedGridsViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TemplatedGridsView).GetMethods(BindingFlags.Instance | BindingFlags.Public).First(x => x.Name == "Show");
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TemplatedGridsView __instance)
        {
            if (__instance.GetComponentInParent<GridWindow>() != null)
            {
                if (AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Add(__instance);
            }
            else
            {
                foreach (GridView gridView in __instance.GridViews)
                {
                    if (AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(gridView)) continue;
                    AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Add(gridView);
                }
            }
        }
    }
    public class GeneratedGridsViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GeneratedGridsView).GetMethods(BindingFlags.Instance | BindingFlags.Public).First(x => x.Name == "Show");
        }
        [PatchPostfix]
        private static void PatchPostFix(ref GeneratedGridsView __instance)
        {
            if (__instance.GetComponentInParent<GridWindow>() != null)
            {
                if (AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Add(__instance);
            }
            else
            {
                foreach (GridView gridView in __instance.GridViews)
                {
                    if (AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(gridView)) continue;
                    AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Add(gridView);
                }
            }
        }
    }
    public class TradingGridViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TradingGridView).GetMethods().First((MethodInfo x) => x.Name == "Show" && x.GetParameters().Count() == 5);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TradingGridView __instance)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Add(__instance);
        }
    }
    public class TradingGridViewTraderShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TradingGridView).GetMethods().First((MethodInfo x) => x.Name == "Show" && x.GetParameters().Count() == 6 && x.GetParameters()[5].Name == "raiseEvents");
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TradingGridView __instance)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Add(__instance);
        }
    }
    public class TradingTableGridViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TradingTableGridView).GetMethods().First((MethodInfo x) => x.Name == "Show" && x.GetParameters().Count() == 4);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref TradingTableGridView __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.tradingTableGridView = __instance;
        }
    }
    public class ContainedGridsViewClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ContainedGridsView).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ContainedGridsView __instance)
        {
            if (__instance == null) return;

            if (__instance.GetComponentInParent<GridWindow>() != null)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.containedGridsViews.Remove(__instance);
            }
        }
    }
    public class GridViewHidePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GridView).GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref GridView __instance)
        {
            if (__instance == null) return;
            if (AmandsControllerPlugin.AmandsControllerClassComponent.tradingTableGridView == __instance)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.tradingTableGridView = null;
                return;
            }
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.gridViews.Remove(__instance);
        }
    }
    public class EquipmentTabShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EquipmentTab).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref EquipmentTab __instance, InventoryControllerClass inventoryController)
        {
            if (__instance.gameObject.name == "Gear Panel")
            {
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("_slotViews").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key == EquipmentSlot.FirstPrimaryWeapon || slotView.Key == EquipmentSlot.SecondPrimaryWeapon)
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.weaponsSlotViews.Add(slotView.Value);
                    }
                    else if (slotView.Key == EquipmentSlot.ArmBand)
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.armbandSlotView = slotView.Value;
                    }
                    else
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.equipmentSlotViews.Add(slotView.Value);
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("_slotViews").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key == EquipmentSlot.FirstPrimaryWeapon || slotView.Key == EquipmentSlot.SecondPrimaryWeapon)
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.lootWeaponsSlotViews.Add(slotView.Value);
                    }
                    else if (slotView.Key == EquipmentSlot.ArmBand)
                    {
                        if (slotView.Value.Slot != null) AmandsControllerPlugin.AmandsControllerClassComponent.lootArmbandSlotView = slotView.Value;
                    }
                    else
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.lootEquipmentSlotViews.Add(slotView.Value);
                    }
                }
            }
        }
    }
    public class EquipmentTabHidePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EquipmentTab).GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref EquipmentTab __instance)
        {
            if (__instance.gameObject.name == "Gear Panel")
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.equipmentSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.weaponsSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.armbandSlotView = null;
            }
            else
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.lootEquipmentSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.lootWeaponsSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.lootArmbandSlotView = null;
            }
        }
    }
    public class ContainersPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ContainersPanel).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ContainersPanel __instance)
        {
            if (__instance.transform.parent.gameObject.name == "Scrollview Parent")
            {
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("dictionary_0").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key != EquipmentSlot.Pockets)
                    {
                        AmandsControllerPlugin.AmandsControllerClassComponent.containersSlotViews.Add(slotView.Value);
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<EquipmentSlot, SlotView> slotView in Traverse.Create(__instance).Field("dictionary_0").GetValue<Dictionary<EquipmentSlot, SlotView>>())
                {
                    if (slotView.Key != EquipmentSlot.Pockets) AmandsControllerPlugin.AmandsControllerClassComponent.lootContainersSlotViews.Add(slotView.Value);
                }
                SlotView dogtagSlotView = Traverse.Create(__instance).Field("slotView_0").GetValue<SlotView>();
                if (dogtagSlotView != null && dogtagSlotView.gameObject.activeSelf)
                {
                    AmandsControllerPlugin.AmandsControllerClassComponent.dogtagSlotView = dogtagSlotView;
                }
            }
        }
    }
    public class ContainersPanelClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ContainersPanel).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ContainersPanel __instance)
        {
            if (__instance.transform.parent.gameObject.name == "Scrollview Parent")
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.containersSlotViews.Clear();
                AmandsControllerPlugin.AmandsControllerClassComponent.specialSlotSlotViews.Clear();
            }
            else
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.lootContainersSlotViews.Clear();
            }
        }
    }

    public class ItemSpecificationPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemSpecificationPanel).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ItemSpecificationPanel __instance)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.itemSpecificationPanels.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.itemSpecificationPanels.Add(__instance);
        }
    }
    public class ItemSpecificationPanelClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemSpecificationPanel).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ItemSpecificationPanel __instance)
        {
            if (__instance == null) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.itemSpecificationPanels.Remove(__instance);
        }
    }
    public class SearchButtonShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SearchButton).GetMethod("SetEnabled", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SearchButton __instance, bool value)
        {
            if (__instance.gameObject.activeSelf)
            {
                if (AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Add(__instance);
            }
            else
            {
                if (!AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Contains(__instance)) return;
                AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Remove(__instance);
            }
        }
    }
    public class SearchButtonClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SearchButton).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SearchButton __instance)
        {
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Contains(__instance)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.searchButtons.Remove(__instance);
        }
    }
    public class ItemViewOnBeginDrag : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemView).GetMethod("OnBeginDrag", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static void PatchPreFix(ref ItemView __instance, PointerEventData eventData)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging && AmandsControllerPlugin.AmandsControllerClassComponent.InRaid)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.ControllerCancelDrag();
            }
        }
    }
    public class ItemViewOnEndDrag : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemView).GetMethod("OnEndDrag", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ItemView __instance, PointerEventData eventData)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging && AmandsControllerPlugin.AmandsControllerClassComponent.InRaid)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.ControllerCancelDrag();
            }
        }
    }
    public class ItemViewUpdate : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemView).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool PatchPreFix(ref ItemView __instance)
        {
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.InRaid) return true;
            return (!AmandsControllerPlugin.AmandsControllerClassComponent.Dragging);
        }
    }
    public class DraggedItemViewMethod_3 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DraggedItemView).GetMethod("method_3", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool PatchPreFix(ref DraggedItemView __instance)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.Dragging && AmandsControllerPlugin.AmandsControllerClassComponent.InRaid)
            {
                RectTransform RectTransform_0 = Traverse.Create(__instance).Property("RectTransform_0").GetValue<RectTransform>();
                RectTransform_0.position = AmandsControllerPlugin.AmandsControllerClassComponent.globalPosition;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    public class TooltipMethod_0 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Tooltip).GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static void PatchPreFix(ref ItemView __instance, ref Vector2 position)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.connected && AmandsControllerPlugin.AmandsControllerClassComponent.InRaid) position = AmandsControllerPlugin.AmandsControllerClassComponent.globalPosition + new Vector2(32f,-19f);
        }
    }
    public class ScrollRectNoDragOnEnable : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ScrollRectNoDrag).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ScrollRectNoDrag __instance)
        {
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.scrollRectNoDrags.Contains(__instance))
            {
                if (AmandsControllerPlugin.AmandsControllerClassComponent.scrollRectNoDrags.Count == 0)
                {
                    RectTransform rectTransform;
                    rectTransform = __instance.GetComponent<RectTransform>();
                    AmandsControllerPlugin.AmandsControllerClassComponent.currentScrollRectNoDrag = __instance;
                    AmandsControllerPlugin.AmandsControllerClassComponent.currentScrollRectNoDragRectTransform = rectTransform;
                }
                AmandsControllerPlugin.AmandsControllerClassComponent.scrollRectNoDrags.Add(__instance);
            }
        }
    }
    public class ScrollRectNoDragOnDisable : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ScrollRectNoDrag).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ScrollRectNoDrag __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.scrollRectNoDrags.Remove(__instance);
        }
    }
    public class SimpleStashPanelShowPatch : ModulePatch
    {
        public static bool Searching = false;
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SimpleStashPanel).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SimpleStashPanel __instance)
        {
            if (!Searching && AmandsControllerPlugin.AmandsControllerClassComponent.InRaid) ShowAsync(__instance);
        }
        private async static void ShowAsync(SimpleStashPanel instance)
        {
            Searching = true;
            await Task.Delay(100);
            SearchableItemView searchableItemView = Traverse.Create(instance).Field("_simplePanel").GetValue<SearchableItemView>();
            if (searchableItemView != null)
            {
                GeneratedGridsView generatedGridsView = Traverse.Create(searchableItemView).Field("containedGridsView_0").GetValue<GeneratedGridsView>();
                if (generatedGridsView != null)
                {
                    if (generatedGridsView.GridViews.Count() == 0)
                    {
                        ShowAsync(instance);
                        return;
                    }
                    GridView gridView = generatedGridsView.GridViews[0];
                    if (gridView != null)
                    {
                        Searching = false;
                        AmandsControllerPlugin.AmandsControllerClassComponent.SimpleStashGridView = gridView;
                        AmandsControllerPlugin.AmandsControllerClassComponent.ControllerUISelect(gridView);
                    }
                }
            }
            Searching = false;
        }
    }
    public class ContextMenuButtonShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ContextMenuButton).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ContextMenuButton __instance)
        {
            if (AmandsControllerPlugin.AmandsControllerClassComponent.contextMenuButtons.Contains(__instance)) return;
            SimpleContextMenu simpleContextMenu;
            if (!__instance.transform.parent.parent.TryGetComponent<SimpleContextMenu>(out simpleContextMenu)) return;
            AmandsControllerPlugin.AmandsControllerClassComponent.contextMenuButtons.Add(__instance);
            if (!AmandsControllerPlugin.AmandsControllerClassComponent.ContextMenu)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.UpdateContextMenuBinds(true);
                if (AmandsControllerPlugin.AmandsControllerClassComponent.InRaid) AmandsControllerPlugin.AmandsControllerClassComponent.ControllerUISelect(__instance);
            }
        }
    }
    public class ContextMenuButtonClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ContextMenuButton).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref ContextMenuButton __instance)
        {
            if (__instance == null)
            {
                goto Skip;
            }
            AmandsControllerPlugin.AmandsControllerClassComponent.contextMenuButtons.Remove(__instance);
            Skip:
            if (AmandsControllerPlugin.AmandsControllerClassComponent.contextMenuButtons.Count == 0)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.UpdateContextMenuBinds(false);
            }
        }
    }
    public class SplitDialogShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SplitDialog).GetMethods().First(x => x.Name == "Show" && x.GetParameters().Count() > 7);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SplitDialog __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.splitDialog = __instance;
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateSplitDialogBinds(true);
        }
    }
    public class SplitDialogHidePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SplitDialog).GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SplitDialog __instance)
        {
            AmandsControllerPlugin.AmandsControllerClassComponent.splitDialog = null;
            AmandsControllerPlugin.AmandsControllerClassComponent.UpdateSplitDialogBinds(false);
        }
    }
    public class SearchableSlotViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SearchableSlotView).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SearchableSlotView __instance)
        {
            if (__instance.Slot != null && __instance.Slot.IsSpecial)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.specialSlotSlotViews.Add(__instance);
            }
        }
    }
    public class SearchableSlotViewHidePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SearchableSlotView).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref SearchableSlotView __instance)
        {
            if (__instance == null) return;
            if (__instance.Slot != null && __instance.Slot.IsSpecial)
            {
                AmandsControllerPlugin.AmandsControllerClassComponent.specialSlotSlotViews.Remove(__instance);
            }

        }
    }
    public class InputGetKeyDownLeftControlPatch : ModulePatch
    {
        MethodInfo methodInfo;
        public InputGetKeyDownLeftControlPatch()
        {
            methodInfo = typeof(Input).GetMethods().First(x => x.Name == "GetKey" && x.GetParamsNames().Contains("key"));
        }
        protected override MethodBase GetTargetMethod()
        {
            return methodInfo;
        }
        [PatchPrefix]
        private static bool PatchPreFix(ref bool __result, KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftControl:
                    __result = true;
                    return false;
            }
            return true;
        }
    }
}
