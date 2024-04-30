using Aki.Common.Utils;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SAIN.Preset;
using SAIN.Editor;
using SAIN.Editor.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SAIN.Preset.Personalities;
using System.Diagnostics;

namespace SAIN.Helpers
{
    public enum JsonEnum
    {
        Presets,
        GlobalSettings,
    }

    public static class JsonUtility
    {
        public static readonly Dictionary<JsonEnum, string> FileAndFolderNames = new Dictionary<JsonEnum, string> 
        {
            { JsonEnum.Presets, "Presets" },
            { JsonEnum.GlobalSettings, "GlobalSettings" },
        };

        public const string PresetsFolder = "Presets";
        public const string JSON = ".json";
        public const string JSONSearch = "*" + JSON;
        public const string Info = "Info";

        public static void SaveObjectToJson(object objectToSave, string fileName, params string[] folders)
        {
            if (objectToSave == null)
            {
                return;
            }

            try
            {
                if (!GetFoldersPath(out string foldersPath, folders))
                {
                    Directory.CreateDirectory(foldersPath);
                }
                string filePath = Path.Combine(foldersPath, fileName);
                filePath += ".json";

                string jsonString = JsonConvert.SerializeObject(objectToSave, Formatting.Indented);
                File.Create(filePath).Dispose();

                StreamWriter streamWriter = new StreamWriter(filePath);
                streamWriter.Write(jsonString);
                streamWriter.Flush();
                streamWriter.Close();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public static bool DoesFileExist(string fileName, params string[] folders)
        {
            if (!GetFoldersPath(out string foldersPath, folders))
            {
                return false;
            }
            string filePath = Path.Combine(foldersPath, fileName);
            filePath += ".json";
            return File.Exists(filePath);
        }

        public static class Load
        {
            public static void LoadAllJsonFiles<T>(List<T> list, params string[] folders)
            {
                LoadAllFiles(list, "*.json", folders);
            }

            public static void GetPresetOptions(List<SAINPresetDefinition> list)
            {
                list.Clear();
                if (!GetFoldersPath(out string foldersPath, PresetsFolder))
                {
                    Directory.CreateDirectory(foldersPath);
                }
                var array = Directory.GetDirectories(foldersPath);
                foreach ( var item in array )
                {
                    string path = Path.Combine(item, Info) + JSON;
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        var obj = DeserializeObject<SAINPresetDefinition>(json);
                        list.Add(obj);
                    }
                    else
                    {
                        Logger.LogError($"Could not Import Info.json at path [{path}]. Is the file missing?");
                    }
                }
            }

            public static void LoadAllFiles<T>(List<T> list, string searchPattern = null , params string[] folders)
            {
                if (!GetFoldersPath(out string foldersPath, folders))
                {
                    return;
                }
                string[] files;
                if (searchPattern != null)
                {
                    files = Directory.GetFiles(foldersPath, searchPattern);
                }
                else
                {
                    files = Directory.GetFiles(foldersPath);
                }

                if (list == null)
                {
                    list = new List<T>();
                }
                foreach (var file in files)
                {
                    string jsonContent = File.ReadAllText(file);
                    list.Add(JsonConvert.DeserializeObject<T>(jsonContent));
                }
            }

            public static T DeserializeObject<T>(string file)
            {
                return JsonConvert.DeserializeObject<T>(file);
            }

            public static string LoadTextFile(string fileExtension, string fileName, params string[] folders)
            {
                if (GetFoldersPath(out string foldersPath, folders))
                {
                    string filePath = Path.Combine(foldersPath, fileName);

                    filePath += fileExtension;

                    if (File.Exists(filePath))
                    {
                        return File.ReadAllText(filePath);
                    }
                }
                return null;
            }

            public static bool LoadJsonFile(out string json, string fileName, params string[] folders)
            {
                json = LoadTextFile(JSON, fileName, folders);
                return json != null;
            }

            public static bool LoadObject<T>(out T obj, string fileName, params string[] folders)
            {
                string json = LoadTextFile(JSON, fileName, folders);
                if (json != null)
                {
                    obj = DeserializeObject<T>(json);
                    return true;
                }
                obj = default;
                return false;
            }
        }

        private static void CheckCreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static bool GetFoldersPath(out string path, params string[] folders)
        {
            path = GetSAINPluginPath();
            for (int i = 0; i < folders.Length; i++)
            {
                path = Path.Combine(path, folders[i]);
            }
            return Directory.Exists(path);
        }

        public static string GetSAINPluginPath()
        {
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //var path = Path.Combine(pluginFolder, nameof(SAIN));
            CheckCreateFolder(pluginFolder);
            return pluginFolder;
        }
    }
}