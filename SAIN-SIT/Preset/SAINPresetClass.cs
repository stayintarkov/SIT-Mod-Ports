using Aki.Common.Utils;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using SAIN.Editor;
using SAIN.Editor.GUISections;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.Personalities;
using SAIN.Preset.BotSettings;
using System;
using static SAIN.Helpers.JsonUtility;

namespace SAIN.Preset
{
    public class SAINPresetClass
    {
        public SAINPresetClass(SAINPresetDefinition preset, bool isCopy = false)
        {
            if (isCopy && SAINPlugin.LoadedPreset != null)
            {
                SAINPresetDefinition oldDefinition = SAINPlugin.LoadedPreset.Info;
                SAINPlugin.LoadedPreset.Info = preset;
                ExportAll(SAINPlugin.LoadedPreset);
                SAINPlugin.LoadedPreset.Info = oldDefinition;
            }
            Info = preset;
            GlobalSettings = GlobalSettingsClass.ImportGlobalSettings(preset);
            BotSettings = new BotSettings.SAINBotSettingsClass(this);
            PersonalityManager = new PersonalityManagerClass(this);
        }

        public static void ExportAll(SAINPresetClass preset)
        {
            ExportDefinition(preset.Info);

            ExportGlobalSettings(preset.GlobalSettings, preset.Info.Name, false);
            ExportPersonalities(preset.PersonalityManager, preset.Info.Name, false);
            ExportBotSettings(preset.BotSettings, preset.Info.Name, false);

            try
            {
                PresetHandler.UpdateExistingBots();
            }
            catch (Exception updateEx)
            {
                Logger.LogError(updateEx);
            }
        }

        public static void ExportDefinition(SAINPresetDefinition info)
        {
            try
            {
                Export(info, info.Name, "Info");
            }
            catch (Exception updateEx)
            {
                LogExportError(updateEx);
            }
        }

        public static bool ExportGlobalSettings(GlobalSettingsClass globalSettings, string presetName, bool sendToBots = true)
        {
            bool success = false;
            try
            {
                Export(globalSettings, presetName, "GlobalSettings");
                if (sendToBots)
                {
                    PresetHandler.UpdateExistingBots();
                }
                success = true;
                GUITabs.GlobalSettingsWereEdited = false;
            }
            catch (Exception ex)
            {
                LogExportError(ex);
            }
            return success;
        }

        public static bool ExportPersonalities(PersonalityManagerClass personClass, string presetName, bool sendToBots = true)
        {
            bool success = false;
            try
            {
                foreach (var pers in personClass.Personalities)
                {
                    if (pers.Value != null && Export(pers.Value, presetName, pers.Key.ToString(), nameof(Personalities)))
                    {
                        continue;
                    }
                    else if (pers.Value == null)
                    {
                        Logger.LogError("Personality Settings Are Null");
                    }
                    else
                    {
                        Logger.LogError($"Failed to Export {pers.Key}");
                    }
                }
                if (sendToBots)
                {
                    PresetHandler.UpdateExistingBots();
                }
                success = true;
                BotPersonalityEditor.PersonalitiesWereEdited = false;
            }
            catch (Exception ex)
            {
                LogExportError(ex);
            }
            return success;
        }

        public static bool ExportBotSettings(SAINBotSettingsClass botSettings, string presetName, bool sendToBots = true)
        {
            bool success = false;
            try
            {
                foreach (SAINSettingsGroupClass settings in botSettings.SAINSettings.Values)
                {
                    Export(settings, presetName, settings.Name, "BotSettings");
                }
                if (sendToBots)
                {
                    PresetHandler.UpdateExistingBots();
                }
                success = true;
                BotSelectionClass.BotSettingsWereEdited = false;
            }
            catch (Exception ex)
            {
                LogExportError(ex);
            }
            return success;
        }

        public static bool Export(object obj, string presetName, string fileName, string subFolder = null)
        {
            bool success = false;
            try
            {
                string[] folders = Folders(presetName, subFolder);
                SaveObjectToJson(obj, fileName, folders);
                success = true;

                string debugFolders = string.Empty;
                for (int i = 0; i < folders.Length; i++)
                {
                    debugFolders += $"/{folders[i]}";
                }
                Logger.LogDebug($"Successfully Exported [{obj.GetType().Name}] : Name: [{fileName}] To: [{debugFolders}]");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed Export of Type [{obj.GetType().Name}] Name: [{fileName}]");
                LogExportError(ex);
            }
            return success;
        }

        public static bool Import<T>(out T result, string presetName, string fileName, string subFolder = null)
        {
            string[] folders = Folders(presetName, subFolder);
            if (Load.LoadJsonFile(out string json, fileName, folders))
            {
                try
                {
                    result = Load.DeserializeObject<T>(json);

                    string debugFolders = string.Empty;
                    for (int i = 0; i < folders.Length; i++)
                    {
                        debugFolders += $"/{folders[i]}";
                    }
                    Logger.LogDebug($"Successfully Imported [{typeof(T).Name}] File Name: [{fileName}] To Path: [{debugFolders}]");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed import Item of Type {typeof(T)}");
                    LogExportError(ex);
                }
            }
            result = default;
            return false;
        }

        public static string[] Folders(string presetName, string subFolder = null)
        {
            string presets = "Presets";
            string[] result;
            if (subFolder == null)
            {
                result = new string[]
                {
                    presets,
                    presetName
                };
            }
            else
            {
                result = new string[]
                {
                    presets,
                    presetName,
                    subFolder
                };
            }
            return result;
        }

        public SAINPresetDefinition Info;
        public GlobalSettingsClass GlobalSettings;
        public BotSettings.SAINBotSettingsClass BotSettings;
        public PersonalityManagerClass PersonalityManager;

        private static void LogExportError(Exception ex)
        {
            Logger.LogError($"Export Error: {ex}");
        }
    }

    public abstract class BasePreset
    {
        public BasePreset(SAINPresetClass presetClass)
        {
            Preset = presetClass;
            Info = presetClass.Info;
        }

        public readonly SAINPresetClass Preset;
        public readonly SAINPresetDefinition Info;
    }
}