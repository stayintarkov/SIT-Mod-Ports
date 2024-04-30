using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Preset;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SAIN.Helpers.JsonUtility;

namespace SAIN.Plugin
{
    internal class PresetHandler
    {
        public const string DefaultPreset = "3. Default";
        public const string DefaultPresetDescription = "Bots are difficult but fair, the way SAIN was meant to played.";

        private const string Settings = "Settings";

        public static Action PresetsUpdated;

        public static readonly List<SAINPresetDefinition> PresetOptions = new List<SAINPresetDefinition>();
        
        public static SAINPresetClass LoadedPreset;

        public static PresetEditorDefaults EditorDefaults;

        public static void LoadPresetOptions()
        {
            Load.GetPresetOptions(PresetOptions);
        }

        public static void Init()
        {
            ImportEditorDefaults();
            LoadPresetOptions();

            if (!LoadPresetDefinition(EditorDefaults.SelectedPreset, 
                out SAINPresetDefinition presetDefinition))
            {
                if (!LoadPresetDefinition(DefaultPreset, 
                    out presetDefinition))
                {
                    LoadedPreset = CreateDefaultPresets();
                    return;
                }
            }
            InitPresetFromDefinition(presetDefinition);

            CheckForDefaultPresets();
        }

        public static bool LoadPresetDefinition(string presetKey, out SAINPresetDefinition definition)
        {
            for (int i = 0; i < PresetOptions.Count; i++)
            {
                var preset = PresetOptions[i];
                if (preset.Name == presetKey)
                {
                    definition = preset;
                    return true;
                }
            }
            if (Load.LoadObject(out definition, "Info", PresetsFolder, presetKey))
            {
                PresetOptions.Add(definition);
                return true;
            }
            return false;
        }

        public static void SavePresetDefinition(SAINPresetDefinition definition)
        {
            for (int i = 0; i < 100; i++)
            {
                if (DoesFileExist("Info", PresetsFolder, definition.Name))
                {
                    definition.Name = definition.Name + $" Copy()";
                    continue;
                }
                break;
            }

            PresetOptions.Add(definition);
            SaveObjectToJson(definition, "Info", PresetsFolder, definition.Name);
        }

        public static void InitPresetFromDefinition(SAINPresetDefinition def, bool isCopy = false)
        {
            try
            {
                LoadedPreset = new SAINPresetClass(def, isCopy);
            }
            catch (Exception ex)
            {
                Sounds.PlaySound(EFT.UI.EUISoundType.ErrorMessage);
                Logger.LogError(ex);

                LoadPresetDefinition(DefaultPreset, out def);
                LoadedPreset = new SAINPresetClass(def);
            }
            UpdateExistingBots();
            ExportEditorDefaults();
        }

        public static void ExportEditorDefaults()
        {
            EditorDefaults.SelectedPreset = LoadedPreset.Info.Name;
            SaveObjectToJson(EditorDefaults, Settings, PresetsFolder);
        }

        public static void ImportEditorDefaults()
        {
            if (Load.LoadObject(out PresetEditorDefaults editorDefaults, Settings, PresetsFolder))
            {
                EditorDefaults = editorDefaults;
            }
            else
            {
                EditorDefaults = new PresetEditorDefaults(DefaultPreset);
            }
        }

        public static void UpdateExistingBots()
        {
            if (SAINPlugin.BotController?.Bots != null && SAINPlugin.BotController.Bots.Count > 0)
            {
                PresetsUpdated();
            }
        }

        private static void CheckForDefaultPresets()
        {
            if (!CheckIfPresetLoaded(PresetNameEasy))
            {
                Logger.LogWarning("Default Easy Preset Missing, generating...");
                CreateEasyPreset();
            }
            if (!CheckIfPresetLoaded(PresetNameNormal))
            {
                Logger.LogWarning("Default Normal Preset Missing, generating...");
                CreateNormalPreset();
            }
            if (!CheckIfPresetLoaded(PresetNameHard))
            {
                Logger.LogWarning("Default Hard Preset Missing, generating...");
                CreateHardPreset();
            }
            if (!CheckIfPresetLoaded(PresetNameVeryHard))
            {
                Logger.LogWarning("Default Very Hard Preset Missing, generating...");
                CreateVeryHardPreset();
            }
            if (!CheckIfPresetLoaded(PresetNameImpossible))
            {
                Logger.LogWarning("Default Impossible Preset Missing, generating...");
                CreateImpossiblePreset();
            }
        }

        private static bool CheckIfPresetLoaded(string presetName)
        {
            for (int i = 0; i < PresetOptions.Count; i++)
            {
                var preset = PresetOptions[i];
                if (preset.Name.Contains(presetName) || preset.Name == presetName)
                {
                    return true;
                }
            }
            return false;
        }

        private static SAINPresetClass CreateDefaultPresets()
        {
            CreateEasyPreset();
            CreateNormalPreset();
            var hard = CreateHardPreset();
            CreateVeryHardPreset();
            CreateImpossiblePreset(); 
            return hard;
        }

        private static readonly string PresetNameEasy = "1. Baby Bots";
        private static readonly string PresetNameNormal = "2. Less Difficult";
        private static readonly string PresetNameHard = DefaultPreset;
        private static readonly string PresetNameVeryHard = "4. I Like Pain";
        private static readonly string PresetNameImpossible = "5. Death Wish";

        private static SAINPresetClass CreateEasyPreset()
        {
            var preset = SAINPresetDefinition.CreateDefault(PresetNameEasy, "Bots react slowly and are incredibly inaccurate.");

            var global = preset.GlobalSettings;
            global.Shoot.GlobalRecoilMultiplier = 2.5f;
            global.Shoot.GlobalScatterMultiplier = 1.5f;
            global.Aiming.AccuracySpreadMultiGlobal = 2f;
            global.Aiming.FasterCQBReactionsGlobal = false;
            global.General.GlobalDifficultyModifier = 0.5f;
            global.Look.GlobalVisionDistanceMultiplier = 0.66f;
            global.Look.GlobalVisionSpeedModifier = 1.75f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 0.5f, 0.01f, 1f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 120f;
                    setting.Value.Shoot.FireratMulti *= 0.75f;
                    setting.Value.Core.VisibleDistance *= 1.5f;
                }
            }

            SAINPresetClass.ExportGlobalSettings(preset.GlobalSettings, preset.Info.Name);
            return preset;
        }

        private static SAINPresetClass CreateNormalPreset()
        {
            var preset = SAINPresetDefinition.CreateDefault(PresetNameNormal, "Bots react more slowly, and are less accurate than usual.");

            var global = preset.GlobalSettings;
            global.Shoot.GlobalRecoilMultiplier = 1.6f;
            global.Shoot.GlobalScatterMultiplier = 1.2f;
            global.Aiming.AccuracySpreadMultiGlobal = 1.5f;
            global.Aiming.FasterCQBReactionsGlobal = false;
            global.General.GlobalDifficultyModifier = 0.75f;
            global.Look.GlobalVisionDistanceMultiplier = 0.85f;
            global.Look.GlobalVisionSpeedModifier = 1.25f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 0.85f, 0.01f, 1f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 150f;
                    setting.Value.Core.VisibleDistance *= 1.5f;
                }
            }

            SAINPresetClass.ExportGlobalSettings(preset.GlobalSettings, preset.Info.Name);
            return preset;
        }

        private static SAINPresetClass CreateHardPreset()
        {
            var preset = SAINPresetDefinition.CreateDefault(PresetNameHard, DefaultPresetDescription);

            var botSettings = preset.BotSettings;
            foreach (var botsetting in botSettings.SAINSettings)
            {
                foreach (var diff in botsetting.Value.Settings)
                {
                    diff.Value.Core.VisibleDistance *= 1.5f;
                }
            }
            SAINPresetClass.ExportGlobalSettings(preset.GlobalSettings, preset.Info.Name);
            return preset;
        }

        private static SAINPresetClass CreateVeryHardPreset()
        {
            var preset = SAINPresetDefinition.CreateDefault(PresetNameVeryHard, "Bots react faster, are more accurate, and can see further.");

            var global = preset.GlobalSettings;
            global.Shoot.GlobalRecoilMultiplier = 0.66f;
            global.Shoot.GlobalScatterMultiplier = 0.85f;
            global.Aiming.AccuracySpreadMultiGlobal = 0.8f;
            global.General.GlobalDifficultyModifier = 1.35f;
            global.Look.GlobalVisionDistanceMultiplier = 1.33f;
            global.Look.GlobalVisionSpeedModifier = 0.8f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 1.33f, 0.01f, 1f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 170f;
                    setting.Value.Shoot.FireratMulti *= 1.2f;
                    setting.Value.Core.VisibleDistance *= 1.5f;
                }
            }

            SAINPresetClass.ExportGlobalSettings(preset.GlobalSettings, preset.Info.Name);
            return preset;
        }

        private static SAINPresetClass CreateImpossiblePreset()
        {
            var preset = SAINPresetDefinition.CreateDefault(PresetNameImpossible, "Prepare To Die. Bots have almost no scatter, get less recoil from their weapon while shooting, are more accurate, and react deadly fast.");

            var global = preset.GlobalSettings;
            global.Shoot.GlobalRecoilMultiplier = 0.25f;
            global.Shoot.GlobalScatterMultiplier = 0.01f;
            global.Aiming.AccuracySpreadMultiGlobal = 0.33f;
            global.General.GlobalDifficultyModifier = 3f;
            global.Look.GlobalVisionDistanceMultiplier = 2.5f;
            global.Look.GlobalVisionSpeedModifier = 0.5f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Sqrt(bot.Value.DifficultyModifier).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 180f;
                    setting.Value.Shoot.FireratMulti *= 2f;
                    setting.Value.Core.VisibleDistance *= 1.5f;
                }
            }

            SAINPresetClass.ExportGlobalSettings(preset.GlobalSettings, preset.Info.Name);
            return preset;
        }
    }
}