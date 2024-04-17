using System;
using System.Collections.Generic;
using System.Reflection;
//using Aki.Reflection.Patching;
using Bsg.GameSettings;
using Comfort.Common;
using DynamicExternalResolution.Configs;
using EFT;
using EFT.Animations;
using EFT.CameraControl;
using EFT.Settings.Graphics;
using HarmonyLib;
using UnityEngine;
using StayInTarkov;

namespace DynamicExternalResolution
{
    internal class Patcher
    {
        public static void PatchAll()
        {
            new PatchManager().RunPatches();
        }

        public static void UnpatchAll()
        {
            new PatchManager().RunUnpatches();
        }
    }

    public class PatchManager
    {
        public PatchManager()
        {
            _patches = new List<ModulePatch>
            {
                new DynamicExternalResolutionPatches.OpticSightOnEnablePath(),
                new DynamicExternalResolutionPatches.OpticSightOnDisablePath(),
                new DynamicExternalResolutionPatches.ClientFirearmControllerChangeAimingModePath(),
            };
        }

        public void RunPatches()
        {
            foreach (ModulePatch patch in _patches)
            {
                patch.Enable();
            }
        }

        public void RunUnpatches()
        {
            foreach (ModulePatch patch in _patches)
            {
                patch.Disable();
            }
        }

        private readonly List<ModulePatch> _patches;
    }

    public static class DynamicExternalResolutionPatches
    {
        private static readonly FieldInfo _graphicsField;
        private static readonly FieldInfo _graphicsSettingsField;
        private static readonly PropertyInfo _dlssEnabledProperty;
        private static readonly PropertyInfo _fsrEnabledProperty;
        private static readonly PropertyInfo _fsr2EnabledProperty;
        private static readonly PropertyInfo _superSamplingFactorProperty;
        private static readonly FieldInfo _antiAliasingField;
        private static readonly FieldInfo _dlssModeField;
        private static readonly FieldInfo _fsrModeField;
        private static readonly FieldInfo _fsr2ModeField;

        private static readonly PropertyInfo _isAimingProperty;
        private static readonly PropertyInfo _currentAimingModProperty;
        private static readonly PropertyInfo _currentScopeProperty;
        private static readonly PropertyInfo _isOpticProperty;

        // Fetch field/property references to avoid GClass references
        static DynamicExternalResolutionPatches()
        {
            Type gameSettingsType = typeof(SettingsManager);

            // Singleton<SettingsManager>.Instance.Graphics
            _graphicsField = AccessTools.Field(gameSettingsType, "Graphics");
            Type graphicsFieldType = _graphicsField.FieldType;

            // Singleton<SettingsManager>.Instance.Graphics.Settings
            _graphicsSettingsField = AccessTools.Field(graphicsFieldType, "Settings");
            Type graphicsSettingsFieldType = _graphicsSettingsField.FieldType;

            // Singleton<SettingsManager>.Instance.Graphics.Settings properties
            _dlssEnabledProperty = AccessTools.Property(graphicsSettingsFieldType, "DLSSEnabled");
            _fsrEnabledProperty = AccessTools.Property(graphicsSettingsFieldType, "FSREnabled");
            _fsr2EnabledProperty = AccessTools.Property(graphicsSettingsFieldType, "FSR2Enabled");
            _superSamplingFactorProperty = AccessTools.Property(graphicsSettingsFieldType, "SuperSamplingFactor");
            _antiAliasingField = AccessTools.Field(graphicsSettingsFieldType, "AntiAliasing");
            _dlssModeField = AccessTools.Field(graphicsSettingsFieldType, "DLSSMode");
            _fsrModeField = AccessTools.Field(graphicsSettingsFieldType, "FSRMode");
            _fsr2ModeField = AccessTools.Field(graphicsSettingsFieldType, "FSR2Mode");

            // ProceduralWeaponAnimation properties
            Type procWeaponAnimType = typeof(ProceduralWeaponAnimation);
            _isAimingProperty = AccessTools.Property(procWeaponAnimType, "IsAiming");
            _currentAimingModProperty = AccessTools.Property(procWeaponAnimType, "CurrentAimingMod");
            _currentScopeProperty = AccessTools.Property(procWeaponAnimType, "CurrentScope");
            Type currentScopeType = _currentScopeProperty.PropertyType;
            _isOpticProperty = AccessTools.Property(currentScopeType, "IsOptic");
        }

        private static void SetResolutionAim()
        {
            bool DLSSSupport = DLSSWrapper.IsDLSSSupported();

            object graphics = _graphicsField.GetValue(Singleton<SettingsManager>.Instance);
            object graphicsSettings = _graphicsSettingsField.GetValue(graphics);

            bool DLSSEnabled = DLSSSupport && (bool)_dlssEnabledProperty.GetValue(graphicsSettings);
            bool FSREnabled = (bool)_fsrEnabledProperty.GetValue(graphicsSettings);
            bool FSR2Enabled = (bool)_fsr2EnabledProperty.GetValue(graphicsSettings);

            float defaultSuperSamplingFactor = (float)_superSamplingFactorProperty.GetValue(graphicsSettings);
            float configSuperSamplingFactor = DynamicExternalResolutionConfig.SuperSampling.Value;

            EAntialiasingMode defaultAAMode = GetGameSetting<EAntialiasingMode>(graphicsSettings, _antiAliasingField);

            EDLSSMode defaultDLSSMode = GetGameSetting<EDLSSMode>(graphicsSettings, _dlssModeField);
            EDLSSMode configDLSSMode = DynamicExternalResolutionConfig.DLSSMode.Value;

            EFSRMode defaultFSRMode = GetGameSetting<EFSRMode>(graphicsSettings, _fsrModeField);
            EFSRMode configFSRMode = DynamicExternalResolutionConfig.FSRMode.Value;

            EFSR2Mode defaultFSR2Mode = GetGameSetting<EFSR2Mode>(graphicsSettings, _fsr2ModeField);
            EFSR2Mode configFSR2Mode = DynamicExternalResolutionConfig.FSR2Mode.Value;

            // DLSS and FSR1|2 are both disabled, use the default sampling factor
            if (!DLSSEnabled && !FSREnabled && !FSR2Enabled && (configSuperSamplingFactor < defaultSuperSamplingFactor))
            {
                SetSuperSampling(1f - configSuperSamplingFactor);
            }
            // DLSS is enabled, and the selected scale mode doesn't match
            else if (DLSSEnabled && (configDLSSMode != defaultDLSSMode))
            {
                SetAntiAliasing(defaultAAMode, configDLSSMode, defaultFSR2Mode);
            }
            // FSR1 is enabled, and the configured scale mode doesn't match
            else if (FSREnabled && (configFSRMode != defaultFSRMode))
            {
                SetFSR(configFSRMode);
            }
            // FSR2 is enabled, and the configured scale mode doesn't match
            else if (FSR2Enabled && (configFSR2Mode != defaultFSR2Mode))
            {
                SetFSR2(configFSR2Mode);
            }
        }

        private static void SetResolutionDefault()
        {
            bool DLSSSupport = DLSSWrapper.IsDLSSSupported();

            object graphics = _graphicsField.GetValue(Singleton<SettingsManager>.Instance);
            object graphicsSettings = _graphicsSettingsField.GetValue(graphics);

            bool DLSSEnabled = DLSSSupport && (bool)_dlssEnabledProperty.GetValue(graphicsSettings);
            bool FSREnabled = (bool)_fsrEnabledProperty.GetValue(graphicsSettings);
            bool FSR2Enabled = (bool)_fsr2EnabledProperty.GetValue(graphicsSettings);

            float defaultSuperSamplingFactor = (float)_superSamplingFactorProperty.GetValue(graphicsSettings);

            EAntialiasingMode defaultAAMode = GetGameSetting<EAntialiasingMode>(graphicsSettings, _antiAliasingField);
            EDLSSMode defaultDLSSMode = GetGameSetting<EDLSSMode>(graphicsSettings, _dlssModeField);
            EFSRMode defaultFSRMode = GetGameSetting<EFSRMode>(graphicsSettings, _fsrModeField);
            EFSR2Mode defaultFSR2Mode = GetGameSetting<EFSR2Mode>(graphicsSettings, _fsr2ModeField);

            if (!DLSSEnabled && !FSREnabled && !FSR2Enabled)
            {
                SetSuperSampling(defaultSuperSamplingFactor);
            }
            else if (DLSSEnabled)
            {
                SetAntiAliasing(defaultAAMode, defaultDLSSMode, defaultFSR2Mode);
            }
            else if (FSREnabled)
            {
                SetFSR(defaultFSRMode);
            }
            else if (FSR2Enabled)
            {
                SetFSR2(defaultFSR2Mode);
            }
        }

        private static T GetGameSetting<T>(object instance, FieldInfo field)
        {
            return ((GameSetting<T>)field.GetValue(instance)).GetValue();
        }

        private static void SetSuperSampling(float sampling)
        {
            FPSCamera camera = DynamicExternalResolution.getCameraInstance();

            if (camera != null)
            {
                //camera.SetSuperSampling(Mathf.Clamp(sampling, 0f, 1f));
                camera.SSAA.GetComponent<SSAAImpl>().Switch(Mathf.Clamp(sampling, 0f, 1f));
            }
        }

        private static void SetAntiAliasing(EAntialiasingMode quality, EDLSSMode dlssMode, EFSR2Mode fsr2Mode)
        {
            FPSCamera camera = DynamicExternalResolution.getCameraInstance();

            if (camera != null)
            {
                camera.SetAntiAliasing(quality, dlssMode, fsr2Mode);
            }
        }

        private static void SetFSR(EFSRMode fsrMode)
        {
            FPSCamera camera = DynamicExternalResolution.getCameraInstance();

            if (camera != null)
            {
                camera.SetFSR(fsrMode);
            }
        }

        private static void SetFSR2(EFSR2Mode fsr2Mode)
        {
            FPSCamera camera = DynamicExternalResolution.getCameraInstance();

            if (camera != null)
            {
                camera.SetFSR2(fsr2Mode);
            }
        }

        public class OpticSightOnEnablePath : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OpticSight), "OnEnable");
            }

            [PatchPostfix]
            private static void PatchPostfix()
            {
                if (DynamicExternalResolutionConfig.EnableMod.Value)
                {
                    Player localPlayer = DynamicExternalResolution.getPlayerInstance();

                    if (localPlayer != null && localPlayer.ProceduralWeaponAnimation != null)
                    {
                        bool isAiming = (bool)_isAimingProperty.GetValue(localPlayer.ProceduralWeaponAnimation);

                        object currentAimingMod = _currentAimingModProperty.GetValue(localPlayer.ProceduralWeaponAnimation);
                        object currentScope = _currentScopeProperty.GetValue(localPlayer.ProceduralWeaponAnimation);

                        if (isAiming && currentAimingMod != null && currentScope != null)
                        {
                            if ((bool)_isOpticProperty.GetValue(currentScope))
                            {
                                SetResolutionAim();
                            }
                        }
                    }
                }
            }
        }

        public class OpticSightOnDisablePath : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OpticSight), "OnDisable");
            }

            [PatchPostfix]
            private static void PatchPostfix()
            {
                Player localPlayer = DynamicExternalResolution.getPlayerInstance();

                if (localPlayer != null && localPlayer.ProceduralWeaponAnimation != null)
                {
                    SetResolutionDefault();
                }
            }
        }

        public class ClientFirearmControllerChangeAimingModePath : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(Player.FirearmController), "ChangeAimingMode");
            }

            [PatchPostfix]
            private static void PatchPostfix()
            {
                Player localPlayer = DynamicExternalResolution.getPlayerInstance();

                if (localPlayer != null && localPlayer.ProceduralWeaponAnimation != null)
                {
                    bool isAiming = (bool)_isAimingProperty.GetValue(localPlayer.ProceduralWeaponAnimation);

                    object currentAimingMod = _currentAimingModProperty.GetValue(localPlayer.ProceduralWeaponAnimation);
                    object currentScope = _currentScopeProperty.GetValue(localPlayer.ProceduralWeaponAnimation);

                    if (isAiming && currentAimingMod != null && currentScope != null)
                    {
                        if ((bool)_isOpticProperty.GetValue(currentScope))
                        {
                            SetResolutionAim();
                        }
                        else
                        {
                            SetResolutionDefault();
                        }
                    }
                }
            }
        }
    }
}
