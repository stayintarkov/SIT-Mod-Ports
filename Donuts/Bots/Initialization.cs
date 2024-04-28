using EFT.Communications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Donuts.DonutComponent;
using static Donuts.Gizmos;

#pragma warning disable IDE0007, IDE0044

namespace Donuts
{
    internal class Initialization
    {
        internal static void InitializeStaticVariables()
        {
            fightLocations = new FightLocations()
            {
                Locations = new List<Entry>()
            };

            sessionLocations = new FightLocations()
            {
                Locations = new List<Entry>()
            };

            fileLoaded = false;
            groupedHotspotTimers = new Dictionary<int, List<HotspotTimer>>();
            groupedFightLocations = new List<List<Entry>>();
            hotspotTimers = new List<HotspotTimer>();
            PMCBotLimit = 0;
            SCAVBotLimit = 0;
            currentInitialPMCs = 0;
            currentInitialSCAVs = 0;

            Gizmos.drawnCoordinates = new HashSet<Vector3>();
            gizmoSpheres = new List<GameObject>();


        }
        internal static void SetupBotLimit(string folderName)
        {
            Folder raidFolderSelected = DonutsPlugin.GrabDonutsFolder(folderName);
            switch (maplocation)
            {
                case "factory4_day":
                case "factory4_night":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.FactoryBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.FactoryBotLimit;
                    break;
                case "bigmap":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.CustomsBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.CustomsBotLimit;
                    break;
                case "interchange":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.InterchangeBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.InterchangeBotLimit;
                    break;
                case "rezervbase":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.ReserveBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.ReserveBotLimit;
                    break;
                case "laboratory":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.LaboratoryBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.LaboratoryBotLimit;
                    break;
                case "lighthouse":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.LighthouseBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.LighthouseBotLimit;
                    break;
                case "shoreline":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.ShorelineBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.ShorelineBotLimit;
                    break;
                case "woods":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.WoodsBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.WoodsBotLimit;
                    break;
                case "tarkovstreets":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.TarkovStreetsBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.TarkovStreetsBotLimit;
                    break;
                case "sandbox":
                    PMCBotLimit = raidFolderSelected.PMCBotLimitPresets.GroundZeroBotLimit;
                    SCAVBotLimit = raidFolderSelected.SCAVBotLimitPresets.GroundZeroBotLimit;
                    break;
                default:
                    PMCBotLimit = 8;
                    SCAVBotLimit = 5;
                    break;
            }
        }

        internal static void InitializeHotspotTimers()
        {
            // Group the fight locations by groupNum
            foreach (var listHotspots in groupedFightLocations)
            {
                foreach (var hotspot in listHotspots)
                {
                    var hotspotTimer = new HotspotTimer(hotspot);

                    int groupNum = hotspot.GroupNum;

                    if (!groupedHotspotTimers.ContainsKey(groupNum))
                    {
                        groupedHotspotTimers[groupNum] = new List<HotspotTimer>();
                    }

                    groupedHotspotTimers[groupNum].Add(hotspotTimer);
                }
            }

            // Assign the groupedHotspotTimers dictionary back to hotspotTimers
            hotspotTimers = groupedHotspotTimers.SelectMany(kv => kv.Value).ToList();
        }

        internal static void LoadFightLocations()
        {
            if (!fileLoaded)
            {
                MethodInfo displayMessageNotificationMethod;
                methodCache.TryGetValue("DisplayMessageNotification", out displayMessageNotificationMethod);

                string dllPath = Assembly.GetExecutingAssembly().Location;
                string directoryPath = Path.GetDirectoryName(dllPath);

                string jsonFolderPath = Path.Combine(directoryPath, "patterns");

                //in SelectedPatternFolderPath, grab the folder name from DonutsPlugin.scenarioSelection.Value

                var selectionName = runWeightedScenarioSelection();

                SetupBotLimit(selectionName);

                if (selectionName == null)
                {
                    var txt = "Donuts Plugin: No valid Scenario Selection found for map";
                    DonutComponent.Logger.LogError(txt);
                    EFT.UI.ConsoleScreen.LogError(txt);
                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Alert, Color.yellow });
                    return;
                }

                string PatternFolderPath = Path.Combine(jsonFolderPath, selectionName);

                // Check if the folder exists
                if (!Directory.Exists(PatternFolderPath))
                {
                    var txt = ("Donuts Plugin: Folder from ScenarioConfig.json does not actually exist: " + PatternFolderPath + "\nDisabling the donuts plugin for this raid.");
                    DonutComponent.Logger.LogError(txt);
                    EFT.UI.ConsoleScreen.LogError(txt);
                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Alert, Color.yellow });
                    fileLoaded = false;
                    return;
                }

                string[] jsonFiles = Directory.GetFiles(PatternFolderPath, "*.json");

                if (jsonFiles.Length == 0)
                {
                    var txt = ("Donuts Plugin: No JSON Pattern files found in folder: " + PatternFolderPath + "\nDisabling the donuts plugin for this raid.");
                    DonutComponent.Logger.LogError(txt);
                    EFT.UI.ConsoleScreen.LogError(txt);
                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Alert, Color.yellow });
                    fileLoaded = false;
                    return;
                }

                List<Entry> combinedLocations = new List<Entry>();

                foreach (string file in jsonFiles)
                {
                    FightLocations fightfile = JsonConvert.DeserializeObject<FightLocations>(File.ReadAllText(file));
                    combinedLocations.AddRange(fightfile.Locations);
                }

                if (combinedLocations.Count == 0)
                {
                    var txt = "Donuts Plugin: No Entries found in JSON files, disabling plugin for raid.";
                    DonutComponent.Logger.LogError(txt);
                    EFT.UI.ConsoleScreen.LogError(txt);
                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Alert, Color.yellow });
                    fileLoaded = false;
                    return;
                }

                DonutComponent.Logger.LogDebug("Loaded " + combinedLocations.Count + " Bot Fight Entries");

                // Assign the combined fight locations to the fightLocations variable.
                fightLocations = new FightLocations { Locations = combinedLocations };

                //filter fightLocations for maplocation
                fightLocations.Locations.RemoveAll(x => x.MapName != maplocation);

                if (fightLocations.Locations.Count == 0)
                {
                    //show error message so user knows why donuts is not working
                    var txt = "Donuts Plugin: There are no valid Spawn Marker Entries for the current map. Disabling the plugin for this raid.";
                    DonutComponent.Logger.LogError(txt);
                    EFT.UI.ConsoleScreen.LogError(txt);
                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Alert, Color.yellow });
                    fileLoaded = false;
                    return;
                }

                DonutComponent.Logger.LogDebug("Valid Bot Fight Entries For Current Map: " + fightLocations.Locations.Count);

                fileLoaded = true;
            }

            //group fightLocations by groupnum
            foreach (Entry entry in fightLocations.Locations)
            {
                bool groupExists = false;
                foreach (List<Entry> group in groupedFightLocations)
                {
                    if (group.Count > 0 && group.First().GroupNum == entry.GroupNum)
                    {
                        group.Add(entry);
                        groupExists = true;
                        break;
                    }
                }

                if (!groupExists)
                {
                    groupedFightLocations.Add(new List<Entry> { entry });
                }
            }
        }

        internal static string runWeightedScenarioSelection()
        {
            try
            {
                var scenarioSelection = DonutsPlugin.scenarioSelection.Value;

                // check if this is a SCAV raid; this only works during raid load
                if (StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
                {
#if DEBUG
                    DonutComponent.Logger.LogDebug($"This is a SCAV raid, using SCAV raid preset selector");
#endif
                    scenarioSelection = DonutsPlugin.scavScenarioSelection.Value;
                }

                foreach (Folder folder in DonutsPlugin.scenarios)
                {
                    if (folder.Name == scenarioSelection)
                    {
#if DEBUG
                        DonutComponent.Logger.LogDebug("Selected Preset: " + scenarioSelection);
#endif
                        return folder.Name; // Return the chosen preset from the UI
                    }
                }

                // Check if a RandomScenarioConfig was selected from the UI
                foreach (Folder folder in DonutsPlugin.randomScenarios)
                {
                    if (folder.RandomScenarioConfig == scenarioSelection)
                    {
                        // Calculate the total weight of all presets for the selected RandomScenarioConfig
                        int totalWeight = folder.presets.Sum(preset => preset.Weight);

                        int randomWeight = UnityEngine.Random.Range(0, totalWeight);

                        // Select the preset based on the random weight
                        string selectedPreset = null;
                        int accumulatedWeight = 0;

                        foreach (var preset in folder.presets)
                        {
                            accumulatedWeight += preset.Weight;
                            if (randomWeight <= accumulatedWeight)
                            {
                                selectedPreset = preset.Name;
                                break;
                            }
                        }

                        if (selectedPreset != null)
                        {
                            Console.WriteLine("Donuts: Random Selected Preset: " + selectedPreset);

                            if (DonutsPlugin.ShowRandomFolderChoice.Value)
                            {
                                MethodInfo displayMessageNotificationMethod;
                                if (DonutComponent.methodCache.TryGetValue("DisplayMessageNotification", out displayMessageNotificationMethod))
                                {
                                    var txt = $"Donuts Random Selected Preset: {selectedPreset}";
                                    EFT.UI.ConsoleScreen.Log(txt);
                                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Default, Color.yellow });
                                }
                            }

                            return selectedPreset;
                        }
                    }
                }

                return null;



            }
            catch (Exception e)
            {
                DonutComponent.Logger.LogError("Error in runWeightedScenarioSelection: " + e.Message);
                DonutComponent.Logger.LogError("Stack Trace: " + e.StackTrace);
                DonutComponent.Logger.LogError("Target Site: " + e.TargetSite);
                return null;
            }

        }
    }
}
