using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FOVFix
{
    [BepInPlugin("com.fontaine.fovfix", "FovFix", "1.11.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Dictionary<string, List<Dictionary<string, float>>> WeaponScopeValues = new Dictionary<string, List<Dictionary<string, float>>>();

        private static FieldInfo opticCrateFieldInfo;
        public static MethodInfo pwaParamsMethodInfo;

        public static float AimingSens = 1f;

        public static Vector2 camPanRotation = Vector2.zero;
        public static bool isRotating = false;
        private Vector2 targetRotation = Vector2.zero;

        public static bool DidToggleForFirstPlane = false;

        private static bool haveResetDict = false;

        public static bool IsFixedMag = false;
        public static bool CanToggle = false;
        public static bool CanToggleButNotFixed = false;
        public static bool IsFucky = false;
        public static float MinZoom = 1f;
        public static float MaxZoom = 1f;
        public static float CurrentZoom = 1f;

        public static string CurrentWeapInstanceID = "";
        public static string CurrentScopeInstanceID = "";
        public static string CurrentScopeTempID = "";

        public static bool IsOptic;
        public static bool HasRAPTAR = false;
        public static bool CalledZoom = false;
        public static bool ZoomReset = false;
        public static bool ShouldDoZoom = false;
        public static bool IsAiming = false;
        public static bool ChangeSight = false;
        public static bool ToggledMagnification = false;

        public static ConfigEntry<bool> TrueOneX { get; set; }
        public static ConfigEntry<float> GlobalOpticFOVMulti { get; set; }
        public static ConfigEntry<float> RangeFinderFOV { get; set; }
        public static ConfigEntry<bool> AllowReticleToggle { get; set; }
        public static ConfigEntry<bool> AllowToggleZoom { get; set; }

        public static ConfigEntry<float> OpticPosOffset { get; set; }
        public static ConfigEntry<float> NonOpticOffset { get; set; }
        public static ConfigEntry<float> PistolOffset { get; set; }

        public static ConfigEntry<float> GlobalADSMulti { get; set; }
        public static ConfigEntry<float> NonOpticFOVMulti { get; set; }
        public static ConfigEntry<float> OneADSMulti { get; set; }
        public static ConfigEntry<float> TwoADSMulti { get; set; }
        public static ConfigEntry<float> ThreeADSMulti { get; set; }
        public static ConfigEntry<float> FourADSMulti { get; set; }
        public static ConfigEntry<float> FiveADSMulti { get; set; }
        public static ConfigEntry<float> SixADSMulti { get; set; }
        public static ConfigEntry<float> EightADSMulti { get; set; }
        public static ConfigEntry<float> TwelveADSMulti { get; set; }
        public static ConfigEntry<float> FourteenADSMulti { get; set; }
        public static ConfigEntry<float> HighADSMulti { get; set; }
        public static ConfigEntry<float> RangeFinderADSMulti { get; set; }

        public static ConfigEntry<float> CameraSmoothTime { get; set; }
        public static ConfigEntry<float> PistolSmoothTime { get; set; }
        public static ConfigEntry<float> OpticSmoothTime { get; set; }
        public static ConfigEntry<float> CameraSmoothOut { get; set; }
        public static ConfigEntry<bool> HoldZoom { get; set; }
        public static ConfigEntry<bool> EnableExtraZoomOptic { get; set; }
        public static ConfigEntry<bool> EnableExtraZoomNonOptic { get; set; }
        public static ConfigEntry<bool> EnableZoomOutsideADS { get; set; }
        public static ConfigEntry<float> OpticExtraZoom { get; set; }
        public static ConfigEntry<float> NonOpticExtraZoom { get; set; }
        public static ConfigEntry<KeyboardShortcut> ZoomKeybind { get; set; }
        public static ConfigEntry<float> ToggleZoomOpticSensMulti { get; set; }
        public static ConfigEntry<float> ToggleZoomSensMulti { get; set; }
        public static ConfigEntry<bool> SamSwatVudu { get; set; }

        public static ConfigEntry<float> BaseScopeFOV { get; set; }
        public static ConfigEntry<float> MagPowerFactor { get; set; }
        public static ConfigEntry<bool> EnableVariableZoom { get; set; }
        public static ConfigEntry<bool> UseSmoothZoom { get; set; }
        public static ConfigEntry<float> ZoomSteps { get; set; }
        public static ConfigEntry<float> SmoothZoomSpeed { get; set; }
        public static ConfigEntry<bool> UseMouseWheel { get; set; }
        public static ConfigEntry<bool> UseMouseWheelPlusKey { get; set; }

        public static ConfigEntry<KeyboardShortcut> VariableZoomIn { get; set; }
        public static ConfigEntry<KeyboardShortcut> VariableZoomOut { get; set; }
        public static ConfigEntry<KeyboardShortcut> MouseWheelBind { get; set; }

        public static ConfigEntry<float> MouseSensFactor { get; set; }
        public static ConfigEntry<bool> ChangeMouseSens { get; set; }
        public static ConfigEntry<bool> UseBasicSensCalc { get; set; }

        public static ConfigEntry<float> NonOpticSensMulti { get; set; }
        public static ConfigEntry<float> OneSensMulti { get; set; }
        public static ConfigEntry<float> TwoSensMulti { get; set; }
        public static ConfigEntry<float> ThreeSensMulti { get; set; }
        public static ConfigEntry<float> FourSensMulti { get; set; }
        public static ConfigEntry<float> FiveSensMulti { get; set; }
        public static ConfigEntry<float> SixSensMulti { get; set; }
        public static ConfigEntry<float> EightSensMulti { get; set; }
        public static ConfigEntry<float> TenSensMulti { get; set; }
        public static ConfigEntry<float> TwelveSensMulti { get; set; }
        public static ConfigEntry<float> HighSensMulti { get; set; }

        public static ConfigEntry<float> FovScale { get; set; }
        public static ConfigEntry<bool> EnableFovScaleFix { get; set; }

        public static ConfigEntry<float> test1 { get; set; }
        public static ConfigEntry<float> test2 { get; set; }
        public static ConfigEntry<float> test3 { get; set; }
        public static ConfigEntry<float> test4 { get; set; }


        private void Awake()
        {
            opticCrateFieldInfo = AccessTools.Field(typeof(BattleUIScreen), "_opticCratePanel");
            pwaParamsMethodInfo = AccessTools.Method(typeof(ProceduralWeaponAnimation), "method_23");

            string variable = "1. Variable Zoom.";
            string adsFOV = "2. Player Camera ADS FOV";
            string cameraPostiion = "3. ADS Player Camera Position";
            string toggleZoom = "4. Toggleable Zoom";
            string sens = "5. Mouse Sensitivity.";
            string misc = "6. Misc.";
            string scopeFOV = "7. Scope Zoom (IF VARIABLE ZOOM IS DISABLED).";
            string testing = ".0. Testing";

            test1 = Config.Bind<float>(testing, "test 1", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 600, IsAdvanced = true }));
            test2 = Config.Bind<float>(testing, "test 2", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 500, IsAdvanced = true }));
            test3 = Config.Bind<float>(testing, "test 3", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 400, IsAdvanced = true }));
            test4 = Config.Bind<float>(testing, "test 4", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 300, IsAdvanced = true }));

            EnableVariableZoom = Config.Bind<bool>(variable, "Enable Variable Zoom", true, new ConfigDescription("Allows Scopes That Should Have Variable Zoom To Have It.", null, new ConfigurationManagerAttributes { Order = 100 }));
            BaseScopeFOV = Config.Bind<float>(variable, "Base Scope FOV", 25f, new ConfigDescription("Base FOV Value Which Magnification Modifies (Non-Linearly). Set This So That 1x Looks Like 1x, Unless You Want More Zoom.", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { Order = 80 }));
            MagPowerFactor = Config.Bind<float>(variable, "Magnificaiton Power Factor", 1.1f, new ConfigDescription("FOV Is Determined By Base FOV / Magnification Raised To This Power Factor. Higher Factor Means More Zoom At Higher Magnification", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 70 }));
            UseSmoothZoom = Config.Bind<bool>(variable, "Use Smooth Zoom", true, new ConfigDescription("Hold The Keybind To Smoothly Zoom In/Out.", null, new ConfigurationManagerAttributes { Order = 60 }));
            ZoomSteps = Config.Bind<float>(variable, "Magnificaiton Steps", 1.0f, new ConfigDescription("If Not Using Smooth Zoom Or Using Scroll Wheel, By How Much Magnification Changes Per Key Press. 1 = 1x Change Per Press.", new AcceptableValueRange<float>(0.01f, 5f), new ConfigurationManagerAttributes { Order = 50 }));
            SmoothZoomSpeed = Config.Bind<float>(variable, "Smooth Zoom Speed", 0.1f, new ConfigDescription("If Using Smooth Zoom, Determines How Fast The Zoom Is. Lower = Slower.", new AcceptableValueRange<float>(0.01f, 2f), new ConfigurationManagerAttributes { Order = 40 }));
            UseMouseWheel = Config.Bind<bool>(variable, "Use Mouse Wheel", false, new ConfigDescription("Mouse Scroll Changes Zoom. Must Change The Movement Speed Keybind.", null, new ConfigurationManagerAttributes { Order = 35 }));
            UseMouseWheelPlusKey = Config.Bind<bool>(variable, "Need To Hold Key With Mouse Wheel", false, new ConfigDescription("Required To Hold The Mousewheel Keybind + Scroll To Zoom. Must Change The Movement Speed Keybind.", null, new ConfigurationManagerAttributes { Order = 35 }));
            VariableZoomIn = Config.Bind(variable, "Zoom In Keybind", new KeyboardShortcut(KeyCode.KeypadPlus), new ConfigDescription("Hold To Zoom if Smooth Zoom Is Enabled, Otherwise Press.", null, new ConfigurationManagerAttributes { Order = 30 }));
            VariableZoomOut = Config.Bind(variable, "Zoom Out Keybind", new KeyboardShortcut(KeyCode.KeypadMinus), new ConfigDescription("Hold To Zoom if Smooth Zoom Is Enabled, Otherwise Press.", null, new ConfigurationManagerAttributes { Order = 20 }));
            MouseWheelBind = Config.Bind(variable, "Mouswheel + Keybind", new KeyboardShortcut(KeyCode.RightControl), new ConfigDescription("Hold While Using Mouse Wheel.", null, new ConfigurationManagerAttributes { Order = 10 }));
            SamSwatVudu = Config.Bind<bool>(variable, "SamSwat Vudu Compatibility", false, new ConfigDescription("Makes Variable Zoom Work With SamSwat's Vudu For True Variable Zoom.", null, new ConfigurationManagerAttributes { Order = 5 }));

            GlobalADSMulti = Config.Bind<float>(adsFOV, "Global ADS FOV Multi", 1f, new ConfigDescription("Applies On Top Of All Other ADS FOV Change Multies. Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 11 }));
            NonOpticFOVMulti = Config.Bind<float>(adsFOV, "Unmagnified Sight FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 10 }));
            OneADSMulti = Config.Bind<float>(adsFOV, "1x ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.41f, 1.3f), new ConfigurationManagerAttributes { Order = 9 }));
            TwoADSMulti = Config.Bind<float>(adsFOV, "2x ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 8 }));
            ThreeADSMulti = Config.Bind<float>(adsFOV, "3x ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 7 }));
            FourADSMulti = Config.Bind<float>(adsFOV, "4x ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 6 }));
            FiveADSMulti = Config.Bind<float>(adsFOV, "5x ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 6 }));
            SixADSMulti = Config.Bind<float>(adsFOV, "6x ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 5 }));
            EightADSMulti = Config.Bind<float>(adsFOV, "8x ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 4 }));
            TwelveADSMulti = Config.Bind<float>(adsFOV, "12x ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 3 }));
            FourteenADSMulti = Config.Bind<float>(adsFOV, "14x ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 3 }));
            HighADSMulti = Config.Bind<float>(adsFOV, "High Magnification ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 2 }));
            RangeFinderADSMulti = Config.Bind<float>(adsFOV, "Range Finder ADS FOV Multi", 1f, new ConfigDescription("Multiplier For The FOV Change When ADSing. Lower Multi = Lower FOV So More Zoom.", new AcceptableValueRange<float>(0.4f, 1.3f), new ConfigurationManagerAttributes { Order = 1 }));

            OpticPosOffset = Config.Bind<float>(cameraPostiion, "Optic Camera Distance Offset", 0.0f, new ConfigDescription("Distance Of The Camera To Optics When ADSed. Lower = Closer To Optic.", new AcceptableValueRange<float>(-1.0f, 1.0f), new ConfigurationManagerAttributes { Order = 1 }));
            NonOpticOffset = Config.Bind<float>(cameraPostiion, "Non-Optic Camera Distance Offset", 0.0f, new ConfigDescription("Distance Of The Camera To Sights When ADSed. Lower = Closer To Optic.", new AcceptableValueRange<float>(-1.0f, 1.0f), new ConfigurationManagerAttributes { Order = 2 }));
            PistolOffset = Config.Bind<float>(cameraPostiion, "Pistol Camera Distance Offset", 0.0f, new ConfigDescription("Distance Of The Camera To Sights When ADSed. Lower = Closer To Optic.", new AcceptableValueRange<float>(-1.0f, 1.0f), new ConfigurationManagerAttributes { Order = 3 }));

            ZoomKeybind = Config.Bind(toggleZoom, "Zoom Toggle", new KeyboardShortcut(KeyCode.M), new ConfigDescription("Toggle To Zoom.", null, new ConfigurationManagerAttributes { Order = 60 }));
            HoldZoom = Config.Bind<bool>(toggleZoom, "Hold To Zoom", false, new ConfigDescription("Change Zoom To A Hold Keybind.", null, new ConfigurationManagerAttributes { Order = 50 }));
            EnableExtraZoomOptic = Config.Bind<bool>(toggleZoom, "Enable Toggleable Zoom For Optics", false, new ConfigDescription("Using A Keybind, You Can Get Additional Zoom/Magnification When Aiming.", null, new ConfigurationManagerAttributes { Order = 40 }));
            EnableExtraZoomNonOptic = Config.Bind<bool>(toggleZoom, "Enable Toggleable Zoom For Non-Optics", false, new ConfigDescription("Using A Keybind, You Can Get Additional Zoom/Magnification When Aiming.", null, new ConfigurationManagerAttributes { Order = 35 }));
            EnableZoomOutsideADS = Config.Bind<bool>(toggleZoom, "Enable Toggleable Zoom While Not Aiming", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30 }));
            OpticExtraZoom = Config.Bind<float>(toggleZoom, "Optics Toggle FOV Multi.", 0.9f, new ConfigDescription("FOV Multiplier When Aiming With Optic.", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { Order = 20 }));
            NonOpticExtraZoom = Config.Bind<float>(toggleZoom, "Non-Optics Toggle FOV Multi.", 0.8f, new ConfigDescription("FOV Multiplier When Aiming with Non-Optic Or Not Aiming.", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { Order = 10 }));
            ToggleZoomSensMulti = Config.Bind<float>(toggleZoom, "Non-Optics Toggle Zoom Sens Multi", 0.7f, new ConfigDescription("Sens Modifier When Zoom Is Toggled.", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { Order = 2 }));
            ToggleZoomOpticSensMulti = Config.Bind<float>(toggleZoom, "Optics Toggle Zoom Sens Multi", 0.8f, new ConfigDescription("Sens Modifier When Zoom Toggled.", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { Order = 1 }));

            ChangeMouseSens = Config.Bind<bool>(sens, "Correct Mouse Sensitivity", true, new ConfigDescription("If Using Variable Zoom, Sets Mouse Sensitivity Based On The Scope's Current Magnificaiton. Non-Optical Sights Are Treated The Same As 1x.", null, new ConfigurationManagerAttributes { Order = 100 }));
            MouseSensFactor = Config.Bind<float>(sens, "Mouse Sensitivity Reduction Factor", 90f, new ConfigDescription("Lower = More Sensitivity Reduction Per Magnification Level.", new AcceptableValueRange<float>(1f, 200f), new ConfigurationManagerAttributes { Order = 60 }));
            UseBasicSensCalc = Config.Bind<bool>(sens, "Use Preset Sensitivity Reduction.", true, new ConfigDescription("Will Use The Values Below Instead Of Calculating Sensitivity Automatically. Mouse Sens Reduction Factor Will Be Ignored.", null, new ConfigurationManagerAttributes { Order = 20 }));
            NonOpticSensMulti = Config.Bind<float>(sens, "Unmagnified Sight Sens Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 11 }));
            OneSensMulti = Config.Bind<float>(sens, "1x Sens Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 10 }));
            TwoSensMulti = Config.Bind<float>(sens, "2x Sens Multi", 0.45f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 9 }));
            ThreeSensMulti = Config.Bind<float>(sens, "3x Sens Multi", 0.3f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 8 }));
            FourSensMulti = Config.Bind<float>(sens, "4x Sens Multi", 0.2f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 7 }));
            FiveSensMulti = Config.Bind<float>(sens, "5x Sens Multi", 0.15f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 6 }));
            SixSensMulti = Config.Bind<float>(sens, "6x Sens Multi", 0.125f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 5 }));
            EightSensMulti = Config.Bind<float>(sens, "8x Sens Multi", 0.08f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 4 }));
            TenSensMulti = Config.Bind<float>(sens, "10x Sens Multi", 0.04f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 3 }));
            TwelveSensMulti = Config.Bind<float>(sens, "12x Sens Multi", 0.03f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 2 }));
            HighSensMulti = Config.Bind<float>(sens, "High Sens Multi", 0.01f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 2f), new ConfigurationManagerAttributes { Order = 1 }));

            AllowToggleZoom = Config.Bind<bool>(misc, "Enable Toggle Zoom With Variable Optics", true, new ConfigDescription("Using The Change Magnificaiton Keybind Changes The Magnification Of Variable Optics To Min Or Max Zoom.", null, new ConfigurationManagerAttributes { Order = 4 }));
            AllowReticleToggle = Config.Bind<bool>(misc, "Force Use Zoomed Reticle", false, new ConfigDescription("Variable Optics Will Use The Largest Reticle By Default", null, new ConfigurationManagerAttributes { Order = 5 }));
            PistolSmoothTime = Config.Bind<float>(misc, "Pistol Camera Smooth Time", 8f, new ConfigDescription("The Speed Of ADS Camera Transitions. A Low Value Can Be Used To Smoothen Out The Overly Snappy Transitions Some Scope And Weapon Combinations Can Have At High FOV.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { Order = 10 }));
            OpticSmoothTime = Config.Bind<float>(misc, "Optic Camera Smooth Time", 8f, new ConfigDescription("The Speed Of ADS Camera Transitions. A Low Value Can Be Used To Smoothen Out The Overly Snappy Transitions Some Scope And Weapon Combinations Can Have At High FOV.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { Order = 20 }));
            CameraSmoothTime = Config.Bind<float>(misc, "Camera Smooth Time", 8f, new ConfigDescription("The Speed Of ADS Camera Transitions. A Low Value Can Be Used To Smoothen Out The Overly Snappy Transitions Some Scope And Weapon Combinations Can Have At High FOV.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { Order = 30 }));
            CameraSmoothOut = Config.Bind<float>(misc, "Camera Smooth Out", 6f, new ConfigDescription("The Speed Of ADS Camera Transitions. A Low Value Can Be Used To Smoothen Out The Overly Snappy Transitions Some Scope And Weapon Combinations Can Have At High FOV.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { Order = 40 }));
            FovScale = Config.Bind<float>(misc, "FOV Scale", 1f, new ConfigDescription("Requires Game Restart. A Value Of One Reduces The Distortion Caused By Higher FOV Settings, Significantly Reducing Issues With Laser Misallignment And Optics Recoil. Does Make Weapon Postion And Scale Look Different.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 50 }));
            EnableFovScaleFix = Config.Bind<bool>(misc, "Enable FOV Scale Fix", false, new ConfigDescription("Requires Game Restart. Lower Value = More Distortion.", null, new ConfigurationManagerAttributes { Order = 60 }));

            GlobalOpticFOVMulti = Config.Bind<float>(scopeFOV, "Global Optic Magnificaiton Multi (Deprecated)", 0.75f, new ConfigDescription("Only Used If Variable Zoom Is Disabled. Increases/Decreases The FOV/Magnification Within Optics. Lower Multi = Lower FOV So More Zoom. Requires Restart Or Going Into A New Raid To Update Magnification. If In Hideout, Load Into A Raid But Cancel Out Of Loading Immediately, This Will Update The FOV.", new AcceptableValueRange<float>(0.01f, 1.25f), new ConfigurationManagerAttributes { Order = 3 }));
            TrueOneX = Config.Bind<bool>(scopeFOV, "True 1x Magnification (Deprecated)", true, new ConfigDescription("Only Used If Variable Zoom Is Disabled. 1x Scopes Will Override 'Global Optic Magnificaiton Multi' And Stay At A True 1x Magnification. Requires Restart Or Going Into A New Raid To Update FOV. If In Hideout, Load Into A Raid But Cancel Out Of Loading Immediately, This Will Update The FOV.", null, new ConfigurationManagerAttributes { Order = 1 }));
            RangeFinderFOV = Config.Bind<float>(scopeFOV, "Range Finder Magnificaiton", 15f, new ConfigDescription("Set The Magnification For The Range Finder Seperately From The Global Multi. If The Magnification Is Too High, The Rang Finder Text Will Break. Lower Value = Lower FOV So More Zoom.", new AcceptableValueRange<float>(1f, 30f), new ConfigurationManagerAttributes { Order = 2 }));

            new PwaWeaponParamsPatch().Enable();
            new FreeLookPatch().Enable();
            new LerpCameraPatch().Enable();
            new IsAimingPatch().Enable();

            if (Plugin.EnableFovScaleFix.Value) 
            {
                new CalculateScaleValueByFovPatch().Enable();
            }

            if (Plugin.EnableVariableZoom.Value)
            {
                new ChangeAimingModePatch().Enable();
                new SetScopeModePatch().Enable();
                new OperationSetScopeModePatch().Enable();
                new KeyInputPatch().Enable();   

                if (Plugin.ChangeMouseSens.Value)
                {
                    new AimingSensitivityPatch().Enable();
                }
            }
            else 
            {
                new TacticalRangeFinderControllerPatch().Enable();
                new OnWeaponParametersChangedPatch().Enable();
                new OpticSightAwakePatch().Enable();
            }
        }

        public static void UpdateStoredMagnificaiton(string weapID, string scopeID, float currentZoom)
        {
            if (WeaponScopeValues.ContainsKey(CurrentWeapInstanceID))
            {
                List<Dictionary<string, float>> scopes = WeaponScopeValues[CurrentWeapInstanceID];
                foreach (Dictionary<string, float> scopeDict in scopes)
                {
                    if (scopeDict.ContainsKey(CurrentScopeInstanceID))
                    {
                      scopeDict[CurrentScopeInstanceID] = currentZoom;
                        break;
                    }
                }
            }
        }

        public static void HandleZoomInput(float zoomIncrement, bool toggleZoom = false) 
        {
            float zoomBefore = CurrentZoom;
            CurrentZoom = 
                !toggleZoom ? Mathf.Clamp(CurrentZoom + zoomIncrement, MinZoom, MaxZoom) :
                zoomIncrement;
            UpdateStoredMagnificaiton(CurrentWeapInstanceID, CurrentScopeInstanceID, CurrentZoom);
            ZoomScope(CurrentZoom);
            if (zoomBefore != CurrentZoom) 
            {
                DidToggleForFirstPlane = true;
            }
        }

        public static void ZoomScope(float currentZoom)
        {
            OpticCratePanel panelUI = (OpticCratePanel)opticCrateFieldInfo.GetValue(Singleton<GameUI>.Instance.BattleUiScreen);
            panelUI.Show(Math.Round(currentZoom, 1) + "x");
            Camera[] cams = Camera.allCameras;
            foreach (Camera cam in cams)
            {
                if (cam.name == "BaseOpticCamera(Clone)")
                {
                    cam.fieldOfView = BaseScopeFOV.Value / Mathf.Pow(currentZoom, MagPowerFactor.Value);
                }
            }

            pwaParamsMethodInfo.Invoke(Singleton<GameWorld>.Instance.MainPlayer.ProceduralWeaponAnimation, new object[] { false });
        }

        void Update()
        {
            Utils.CheckIsReady();

            if (Utils.IsReady && Utils.WeaponReady && Singleton<GameWorld>.Instance.MainPlayer != null && Singleton<GameWorld>.Instance.MainPlayer.HandsController != null)
            {
                haveResetDict = false;

                if (EnableVariableZoom.Value && !IsFixedMag && IsOptic && (!CanToggle || CanToggleButNotFixed) && IsAiming)
                {
                    if (UseSmoothZoom.Value)
                    {
                        if (Input.GetKey(VariableZoomOut.Value.MainKey))
                        {
                            HandleZoomInput(-SmoothZoomSpeed.Value);
                        }
                        if (Input.GetKey(VariableZoomIn.Value.MainKey))
                        {
                            HandleZoomInput(SmoothZoomSpeed.Value);
                        }
                    }
                    else 
                    {
                        if (Input.GetKeyDown(VariableZoomOut.Value.MainKey))
                        {
                            HandleZoomInput(-ZoomSteps.Value);
                        }
                        if (Input.GetKeyDown(VariableZoomIn.Value.MainKey))
                        {
                            HandleZoomInput(ZoomSteps.Value);
                        }
                    }
                    if (UseMouseWheel.Value) 
                    {
                        if (Input.GetKey(MouseWheelBind.Value.MainKey) || !UseMouseWheelPlusKey.Value)
                        {
                            float scrollDelta = Input.mouseScrollDelta.y * ZoomSteps.Value;
                            if (scrollDelta != 0f) 
                            {
                                HandleZoomInput(scrollDelta);
                            }
                        }
                    }
                }

                if ((((EnableExtraZoomOptic.Value && IsOptic) || (EnableExtraZoomNonOptic.Value && !IsOptic)) && IsAiming) || (EnableZoomOutsideADS.Value && !IsAiming))
                {
                    if (HoldZoom.Value)
                    {
                        if (Input.GetKey(ZoomKeybind.Value.MainKey) && !CalledZoom)
                        {
                            ShouldDoZoom = true;
                            pwaParamsMethodInfo.Invoke(Singleton<GameWorld>.Instance.MainPlayer.ProceduralWeaponAnimation, new object[] { false });
                            CalledZoom = true;
                            ShouldDoZoom = false;

                        }
                        if (!Input.GetKey(ZoomKeybind.Value.MainKey) && CalledZoom)
                        {
                            pwaParamsMethodInfo.Invoke(Singleton<GameWorld>.Instance.MainPlayer.ProceduralWeaponAnimation, new object[] { false });
                            CalledZoom = false;
                        }
                    }
                    else
                    {
                        if (Input.GetKeyDown(ZoomKeybind.Value.MainKey))
                        {
                            ShouldDoZoom = !ShouldDoZoom;
                            pwaParamsMethodInfo.Invoke(Singleton<GameWorld>.Instance.MainPlayer.ProceduralWeaponAnimation, new object[] { false });
                            CalledZoom = !CalledZoom;
                        }
                    }
                }
                if (!Plugin.IsAiming && Plugin.CalledZoom && !Plugin.HoldZoom.Value && !Plugin.EnableZoomOutsideADS.Value)
                {
                    Plugin.ShouldDoZoom = false;
                    pwaParamsMethodInfo.Invoke(Singleton<GameWorld>.Instance.MainPlayer.ProceduralWeaponAnimation, new object[] { false });
                    Plugin.CalledZoom = false;
                }
            }

            if (!Utils.IsReady && !Plugin.haveResetDict) 
            {
                Plugin.WeaponScopeValues.Clear();
                Plugin.haveResetDict = true;
            }
        }
    }
}
