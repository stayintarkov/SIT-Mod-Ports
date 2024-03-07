using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aki.PrePatch;
using Aki.Reflection.Utils;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Communications;
using HarmonyLib;
using Newtonsoft.Json;
using Systems.Effects;
using UnityEngine;
using UnityEngine.AI;

//custom using
using BotCacheClass = GClass513;
using IProfileData = GClass514;

#pragma warning disable IDE0007, IDE0044
namespace Donuts
{
    public class DonutComponent : MonoBehaviour
    {

        internal static FightLocations fightLocations;
        internal static FightLocations sessionLocations;

        internal static List<List<Entry>> groupedFightLocations;
        internal static Dictionary<int, List<HotspotTimer>> groupedHotspotTimers;

        internal List<WildSpawnType> validDespawnListPMC = new List<WildSpawnType>()
        {
            (WildSpawnType)AkiBotsPrePatcher.sptUsecValue,
            (WildSpawnType)AkiBotsPrePatcher.sptBearValue
        };

        internal List<WildSpawnType> validDespawnListScav = new List<WildSpawnType>()
        {
            WildSpawnType.assault,
            WildSpawnType.cursedAssault
        };

        private bool fileLoaded = false;
        public static string maplocation;
        private int PMCBotLimit = 0;
        private int SCAVBotLimit = 0;
        private int currentInitialPMCs = 0;
        private int currentInitialSCAVs = 0;

        public static GameWorld gameWorld;
        private static BotSpawner botSpawnerClass;
        private static IBotCreator botCreator;

        private float PMCdespawnCooldown = 0f;
        private float PMCdespawnCooldownDuration = 10f;

        private float SCAVdespawnCooldown = 0f;
        private float SCAVdespawnCooldownDuration = 10f;

        internal static List<HotspotTimer> hotspotTimers;
        internal static Dictionary<string, MethodInfo> methodCache;
        private static MethodInfo displayMessageNotificationMethod;

        //gizmo stuff
        private bool isGizmoEnabled = false;
        internal static HashSet<Vector3> drawnCoordinates;
        internal static List<GameObject> gizmoSpheres;
        private static Coroutine gizmoUpdateCoroutine;
        internal static IBotCreator ibotCreator;

        WildSpawnType sptUsec;
        WildSpawnType sptBear;

        internal static ManualLogSource Logger
        {
            get; private set;
        }

        public DonutComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(DonutComponent));
            }

        }

        #region StartUpInit
        public void Awake()
        {
            botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            botCreator = AccessTools.Field(botSpawnerClass.GetType(), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;
            methodCache = new Dictionary<string, MethodInfo>();

            // Retrieve displayMessageNotification MethodInfo
            var displayMessageNotification = PatchConstants.EftTypes.Single(x => x.GetMethod("DisplayMessageNotification") != null).GetMethod("DisplayMessageNotification");
            if (displayMessageNotification != null)
            {
                displayMessageNotificationMethod = displayMessageNotification;
                methodCache["DisplayMessageNotification"] = displayMessageNotification;
            }

            var methodInfo = typeof(BotSpawner).GetMethod("method_9", BindingFlags.Instance | BindingFlags.NonPublic);
            if (methodInfo != null)
            {
                methodCache[methodInfo.Name] = methodInfo;
            }

            methodInfo = AccessTools.Method(typeof(BotSpawner), "method_10");
            if (methodInfo != null)
            {
                methodCache[methodInfo.Name] = methodInfo;
            }

            // Remove despawned bots from bot EnemyInfos list.
            botSpawnerClass.OnBotRemoved += removedBot =>
            {
                // Clear the enemy list, and memory about the main player
                removedBot.Memory.DeleteInfoAboutEnemy(gameWorld.MainPlayer);
                removedBot.EnemiesController.EnemyInfos.Clear();

                // Loop through the rest of the bots on the map, andd clear this bot from its memory/group info

                foreach (var player in gameWorld.AllAlivePlayersList)
                {
                    if (!player.IsAI)
                    {
                        continue;
                    }

                    // Clear the bot from all other bots enemy info
                    var botOwner = player.AIData.BotOwner;
                    botOwner.Memory.DeleteInfoAboutEnemy(removedBot);
                    botOwner.BotsGroup.RemoveInfo(removedBot);
                    botOwner.BotsGroup.RemoveEnemy(removedBot, EBotEnemyCause.death);
                    botOwner.BotsGroup.RemoveAlly(removedBot);
                }
            };
        }

        private void Start()
        {
            // setup the rest of donuts for the selected folder
            InitializeStaticVariables();
            maplocation = gameWorld.MainPlayer.Location.ToLower();
            Logger.LogDebug("Setup maplocation: " + maplocation);
            LoadFightLocations();
            if (DonutsPlugin.PluginEnabled.Value && fileLoaded)
            {
                InitializeHotspotTimers();
            }

            Logger.LogDebug("Setup PMC Bot limit: " + PMCBotLimit);
            Logger.LogDebug("Setup SCAV Bot limit: " + SCAVBotLimit);
        }
        private void InitializeStaticVariables()
        {
            fightLocations = new FightLocations()
            {
                Locations = new List<Entry>()
            };

            sessionLocations = new FightLocations()
            {
                Locations = new List<Entry>()
            };

            groupedHotspotTimers = new Dictionary<int, List<HotspotTimer>>();
            groupedFightLocations = new List<List<Entry>>();
            hotspotTimers = new List<HotspotTimer>();

            drawnCoordinates = new HashSet<Vector3>();
            gizmoSpheres = new List<GameObject>();
            ibotCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;

            sptUsec = (WildSpawnType)AkiBotsPrePatcher.sptUsecValue;
            sptBear = (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
        }
        private void SetupBotLimit(string folderName)
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
                default:
                    PMCBotLimit = 8;
                    SCAVBotLimit = 5;
                    break;
            }
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<DonutComponent>();

                Logger.LogDebug("Donuts Enabled");
            }
        }

        private void InitializeHotspotTimers()
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


        private void LoadFightLocations()
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
                    Logger.LogError(txt);
                    EFT.UI.ConsoleScreen.LogError(txt);
                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Alert, Color.yellow });
                    return;
                }

                string PatternFolderPath = Path.Combine(jsonFolderPath, selectionName);

                // Check if the folder exists
                if (!Directory.Exists(PatternFolderPath))
                {
                    var txt = ("Donuts Plugin: Folder from ScenarioConfig.json does not actually exist: " + PatternFolderPath + "\nDisabling the donuts plugin for this raid.");
                    Logger.LogError(txt);
                    EFT.UI.ConsoleScreen.LogError(txt);
                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Alert, Color.yellow });
                    fileLoaded = false;
                    return;
                }

                string[] jsonFiles = Directory.GetFiles(PatternFolderPath, "*.json");

                if (jsonFiles.Length == 0)
                {
                    var txt = ("Donuts Plugin: No JSON Pattern files found in folder: " + PatternFolderPath + "\nDisabling the donuts plugin for this raid.");
                    Logger.LogError(txt);
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
                    Logger.LogError(txt);
                    EFT.UI.ConsoleScreen.LogError(txt);
                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Alert, Color.yellow });
                    fileLoaded = false;
                    return;
                }

                Logger.LogDebug("Loaded " + combinedLocations.Count + " Bot Fight Entries");

                // Assign the combined fight locations to the fightLocations variable.
                fightLocations = new FightLocations { Locations = combinedLocations };

                //filter fightLocations for maplocation
                fightLocations.Locations.RemoveAll(x => x.MapName != maplocation);

                if (fightLocations.Locations.Count == 0)
                {
                    //show error message so user knows why donuts is not working
                    var txt = "Donuts Plugin: There are no valid Spawn Marker Entries for the current map. Disabling the plugin for this raid.";
                    Logger.LogError(txt);
                    EFT.UI.ConsoleScreen.LogError(txt);
                    displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Alert, Color.yellow });
                    fileLoaded = false;
                    return;
                }

                Logger.LogDebug("Valid Bot Fight Entries For Current Map: " + fightLocations.Locations.Count);

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

        private string runWeightedScenarioSelection()
        {
            var scenarioSelection = DonutsPlugin.scenarioSelection.Value;

            // check if this is a SCAV raid; this only works during raid load
            if (Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                #if DEBUG
                    Logger.LogDebug($"This is a SCAV raid, using SCAV raid preset selector");
                #endif
                scenarioSelection = DonutsPlugin.scavScenarioSelection.Value;
            }

            foreach (Folder folder in DonutsPlugin.scenarios)
            {
                if (folder.Name == scenarioSelection)
                {
                    #if DEBUG
                        Logger.LogDebug("Selected Preset: " + scenarioSelection);
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
                            if (methodCache.TryGetValue("DisplayMessageNotification", out displayMessageNotificationMethod))
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
        #endregion

        private void Update()
        {
            if (DonutsPlugin.PluginEnabled.Value && fileLoaded)
            {
                //every hotspottimer should be updated every frame
                foreach (var hotspotTimer in hotspotTimers)
                {
                    hotspotTimer.UpdateTimer();
                }

                if (groupedHotspotTimers.Count > 0)
                {
                    foreach (var groupHotspotTimers in groupedHotspotTimers.Values)
                    {
                        //check if randomIndex is possible
                        if (!(groupHotspotTimers.Count > 0))
                        {
                            continue;
                        }

                        // Get a random hotspotTimer from the group (grouped by groupNum}
                        var randomIndex = UnityEngine.Random.Range(0, groupHotspotTimers.Count);
                        var hotspotTimer = groupHotspotTimers[randomIndex];

                        if (hotspotTimer.ShouldSpawn())
                        {
                            var hotspot = hotspotTimer.Hotspot;
                            var coordinate = new Vector3(hotspot.Position.x, hotspot.Position.y, hotspot.Position.z);

                            if (IsWithinBotActivationDistance(hotspot, coordinate) && maplocation == hotspot.MapName)
                            {
                                // Check if passes hotspot.spawnChance
                                if (UnityEngine.Random.Range(0, 100) >= hotspot.SpawnChance)
                                {
                                    #if DEBUG
                                        Logger.LogDebug("SpawnChance of " + hotspot.SpawnChance + "% Failed for hotspot: " + hotspot.Name);
                                    #endif

                                    //reset timer if spawn chance fails for all hotspots with same groupNum
                                    foreach (var timer in groupedHotspotTimers[hotspot.GroupNum])
                                    {
                                        timer.ResetTimer();

                                        if (timer.Hotspot.IgnoreTimerFirstSpawn)
                                        {
                                            timer.Hotspot.IgnoreTimerFirstSpawn = false;
                                        }

                                        #if DEBUG
                                            Logger.LogDebug($"Resetting all grouped timers for groupNum: {hotspot.GroupNum} for hotspot: {timer.Hotspot.Name} at time: {timer.GetTimer()}");
                                        #endif
                                    }
                                    continue;
                                }

                                if (hotspotTimer.inCooldown)
                                {
                                    #if DEBUG
                                        Logger.LogDebug("Hotspot: " + hotspot.Name + " is in cooldown, skipping spawn");
                                    #endif
                                    continue;
                                }

                                #if DEBUG
                                    Logger.LogWarning("SpawnChance of " + hotspot.SpawnChance + "% Passed for hotspot: " + hotspot.Name);
                                #endif

                                SpawnBots(hotspotTimer, coordinate);
                                hotspotTimer.timesSpawned++;

                                // Make sure to check the times spawned in hotspotTimer and set cooldown bool if needed
                                if (hotspotTimer.timesSpawned >= hotspot.MaxSpawnsBeforeCoolDown)
                                {
                                    hotspotTimer.inCooldown = true;
                                    #if DEBUG
                                        Logger.LogDebug("Hotspot: " + hotspot.Name + " is now in cooldown");
                                    #endif
                                }

                                #if DEBUG
                                    Logger.LogDebug("Resetting Regular Spawn Timer (after successful spawn): " + hotspotTimer.GetTimer() + " for hotspot: " + hotspot.Name);
                                #endif

                                //reset timer if spawn chance passes for all hotspots with same groupNum
                                foreach (var timer in groupedHotspotTimers[hotspot.GroupNum])
                                {
                                    timer.ResetTimer();

                                    if (timer.Hotspot.IgnoreTimerFirstSpawn)
                                    {
                                        timer.Hotspot.IgnoreTimerFirstSpawn = false;
                                    }

                                    #if DEBUG
                                        Logger.LogDebug($"Resetting all grouped timers for groupNum: {hotspot.GroupNum} for hotspot: {timer.Hotspot.Name} at time: {timer.GetTimer()}");
                                    #endif
                                }
                            }
                        }
                    }
                }

                DisplayMarkerInformation();

                if (DonutsPlugin.DespawnEnabled.Value)
                {
                    DespawnFurthestBot("pmc");
                    DespawnFurthestBot("scav");
                }
            }
        }

        private bool IsWithinBotActivationDistance(Entry hotspot, Vector3 position)
        {
            try
            {
                float distanceSquared = (gameWorld.MainPlayer.Position - position).sqrMagnitude;
                float activationDistanceSquared = hotspot.BotTriggerDistance * hotspot.BotTriggerDistance;
                return distanceSquared <= activationDistanceSquared;
            }
            catch { }

            return false;
        }
        private async Task SpawnBots(HotspotTimer hotspotTimer, Vector3 coordinate)
        {
            string hotspotSpawnType = hotspotTimer.Hotspot.WildSpawnType;
            if (DonutsPlugin.hardStopOptionPMC.Value && (hotspotSpawnType == "pmc" || hotspotSpawnType == "sptusec" || hotspotSpawnType == "sptbear"))
            {
                #if DEBUG
                    Logger.LogDebug($"Hard stop PMCs is enabled, checking raid time");
                #endif
                var pluginRaidTimeLeft = DonutsPlugin.hardStopTimePMC.Value;
                var raidTimeLeft = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
                if (raidTimeLeft < DonutsPlugin.hardStopTimePMC.Value)
                {
                    #if DEBUG
                        Logger.LogDebug($"Time left {raidTimeLeft} is less than your hard stop time {DonutsPlugin.hardStopTimePMC.Value} - skipping this spawn");
                    #endif
                    return;
                }
            }

            else if (DonutsPlugin.hardStopOptionSCAV.Value && hotspotSpawnType == "assault")
            {
                #if DEBUG
                    Logger.LogDebug($"Hard stop SCAVs is enabled, checking raid time");
                #endif
                var pluginRaidTimeLeft = DonutsPlugin.hardStopTimeSCAV;
                var raidTimeLeft = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
                if (raidTimeLeft < DonutsPlugin.hardStopTimeSCAV.Value)
                {
                    #if DEBUG
                        Logger.LogDebug($"Time left {raidTimeLeft} is less than your hard stop time {DonutsPlugin.hardStopTimeSCAV.Value} - skipping this spawn");
                    #endif
                    return;
                }
            }

            int maxCount = hotspotTimer.Hotspot.MaxRandomNumBots;
            if (hotspotSpawnType == "pmc" || hotspotSpawnType == "sptusec" || hotspotSpawnType == "sptbear")
            {
                string pluginGroupChance = DonutsPlugin.pmcGroupChance.Value;
                maxCount = getActualBotCount(pluginGroupChance, maxCount);
            }
            else if (hotspotSpawnType == "assault")
            {
                string pluginGroupChance = DonutsPlugin.scavGroupChance.Value;
                maxCount = getActualBotCount(pluginGroupChance, maxCount);
            }

            int maxInitialPMCs = PMCBotLimit;
            int maxInitialSCAVs = SCAVBotLimit;

            // quick and dirty, this will likely become some sort of new spawn parameter eventually
            if (hotspotTimer.Hotspot.BotTimerTrigger > 9999)
            {
                if (hotspotTimer.Hotspot.WildSpawnType == "pmc" || hotspotTimer.Hotspot.WildSpawnType == "sptusec" || hotspotTimer.Hotspot.WildSpawnType == "sptbear")
                {
                    // current doesn't reset until the next raid
                    // doesn't matter right now since we only care about starting bots
                    if (currentInitialPMCs >= maxInitialPMCs)
                    {
                        #if DEBUG
                            DonutComponent.Logger.LogDebug($"currentInitialPMCs {currentInitialPMCs} is >= than maxInitialPMCs {maxInitialPMCs}, skipping this spawn");
                        #endif
                        return;
                    }
                    else
                    {
                        int originalInitialPMCs = currentInitialPMCs;
                        currentInitialPMCs += maxCount;
                        // if the next spawn takes it count over the limit, then find the difference and fill up to the cap instead
                        if (currentInitialPMCs > maxInitialPMCs)
                        {
                            maxCount = maxInitialPMCs - originalInitialPMCs;

                            #if DEBUG
                                DonutComponent.Logger.LogDebug($"Reaching maxInitialPMCs {maxInitialPMCs}, spawning {maxCount} instead");
                            #endif
                        }
                    }
                }
                else if (hotspotTimer.Hotspot.WildSpawnType == "assault")
                {
                    // current doesn't reset until the next raid
                    // doesn't matter right now since we only care about starting bots
                    if (currentInitialSCAVs >= maxInitialSCAVs)
                    {
                        #if DEBUG
                            DonutComponent.Logger.LogDebug($"currentInitialSCAVs {currentInitialSCAVs} is >= than maxInitialSCAVs {maxInitialSCAVs}, skipping this spawn");
                        #endif
                        return;
                    }
                    else
                    {
                        int originalInitialSCAVs = currentInitialSCAVs;
                        currentInitialSCAVs += maxCount;
                        // if the next spawn takes it count over the limit, then find the difference and fill up to the cap instead
                        if (currentInitialSCAVs > maxInitialSCAVs)
                        {
                            maxCount = maxInitialSCAVs - originalInitialSCAVs;
                            #if DEBUG
                                DonutComponent.Logger.LogDebug($"Reaching maxInitialSCAVs {maxInitialSCAVs}, spawning {maxCount} instead");
                            #endif
                        }
                    }
                }
            }

            const string PmcSpawnTypes = "pmc,sptusec,sptbear";
            const string ScavSpawnType = "assault";

            bool IsPMC(WildSpawnType role)
            {
                return role == (WildSpawnType)AkiBotsPrePatcher.sptUsecValue || role == (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
            }

            bool IsSCAV(WildSpawnType role)
            {
                return role == WildSpawnType.assault;
            }

            bool IsSpawnLimitExceeded(string spawnType, int currentBots, int botLimit, int count)
            {
                if (currentBots + count > botLimit)
                {
                    count = botLimit - currentBots;
                    #if DEBUG
                        DonutComponent.Logger.LogDebug($"Reaching {spawnType} BotLimit {botLimit}, spawning {maxCount} instead");
                    #endif
                    return true;
                }
                return false;
            }

            if (DonutsPlugin.HardCapEnabled.Value)
            {
                #if DEBUG
                    DonutComponent.Logger.LogDebug($"Hard cap is enabled, checking bot counts before spawn");
                #endif

                int currentPMCsAlive = 0;
                int currentSCAVsAlive = 0;
                foreach (Player bot in gameWorld.RegisteredPlayers)
                {
                    if (!bot.IsYourPlayer)
                    {
                        if (IsPMC(bot.Profile.Info.Settings.Role))
                        {
                            currentPMCsAlive++;
                        }
                        else if (IsSCAV(bot.Profile.Info.Settings.Role))
                        {
                            currentSCAVsAlive++;
                        }
                    }
                }

                if (PmcSpawnTypes.Contains(hotspotTimer.Hotspot.WildSpawnType))
                {
                    if (IsSpawnLimitExceeded("PMC", currentPMCsAlive, PMCBotLimit, maxCount))
                    {
                        return;
                    }
                }
                else if (hotspotTimer.Hotspot.WildSpawnType == ScavSpawnType)
                {
                    if (IsSpawnLimitExceeded("SCAV", currentSCAVsAlive, SCAVBotLimit, maxCount))
                    {
                        return;
                    }
                }
            }

            bool group = maxCount > 1;
            int maxSpawnAttempts = DonutsPlugin.maxSpawnTriesPerBot.Value;

            WildSpawnType wildSpawnType = GetWildSpawnType(hotspotTimer.Hotspot.WildSpawnType);

            // check here for faction option, only applies to pmcs
            if (hotspotTimer.Hotspot.WildSpawnType == "pmc" || hotspotTimer.Hotspot.WildSpawnType == "sptbear" || hotspotTimer.Hotspot.WildSpawnType == "sptusec")
            {
                if (DonutsPlugin.pmcFaction.Value == "USEC")
                {
                    wildSpawnType = GetWildSpawnType("sptusec");
                }
                else if (DonutsPlugin.pmcFaction.Value == "BEAR")
                {
                    wildSpawnType = GetWildSpawnType("sptbear");
                }
            }

            EPlayerSide side = GetSideForWildSpawnType(wildSpawnType);
            var cancellationTokenSource = AccessTools.Field(typeof(BotSpawner), "_cancellationTokenSource").GetValue(botSpawnerClass) as CancellationTokenSource;
            BotDifficulty botDifficulty = GetBotDifficulty(wildSpawnType);
            var BotCacheDataList = DonutsBotPrep.GetWildSpawnData(wildSpawnType, botDifficulty);

            //check if we are spawning a group or a single bot
            if (group)
            {
                Vector3? spawnPosition = await GetValidSpawnPosition(hotspotTimer.Hotspot, coordinate, maxSpawnAttempts);

                if (!spawnPosition.HasValue)
                {
                    // Failed to get a valid spawn position, move on to generating the next bot
                    #if DEBUG
                        Logger.LogDebug($"Actually Failed to get a valid spawn position for {hotspotTimer.Hotspot.Name} after {maxSpawnAttempts}, for {maxCount} grouped number of bots, moving on to next bot anyways");
                    #endif
                }

                ShallBeGroupParams groupParams = new ShallBeGroupParams(true, true, maxCount);

                //check if group bots exist in cache or else create it
                if(DonutsBotPrep.FindCachedBots(wildSpawnType, botDifficulty, maxCount) != null)
                {
                    #if DEBUG
                        Logger.LogWarning("Found grouped cached bots, spawning them.");
                    #endif
                    await SpawnBotForGroup(BotCacheDataList, wildSpawnType, side, ibotCreator, botSpawnerClass, (Vector3)spawnPosition, cancellationTokenSource, botDifficulty, maxCount, hotspotTimer);
                }
                else
                {
                    #if DEBUG
                        Logger.LogWarning($"No grouped cached bots found, generating on the fly for: {hotspotTimer.Hotspot.Name} for {maxCount} grouped number of bots.");
                    #endif
                    await DonutsBotPrep.CreateGroupBots(side, wildSpawnType, botDifficulty, groupParams, maxCount, 1);
                    await SpawnBotForGroup(BotCacheDataList, wildSpawnType, side, ibotCreator, botSpawnerClass, (Vector3)spawnPosition, cancellationTokenSource, botDifficulty, maxCount, hotspotTimer);
                }
            }
            else
            {
                Vector3? spawnPosition = await GetValidSpawnPosition(hotspotTimer.Hotspot, coordinate, maxSpawnAttempts);

                if (!spawnPosition.HasValue)
                {
                    // Failed to get a valid spawn position, move on to generating the next bot
                    #if DEBUG
                        Logger.LogDebug($"Actually Failed to get a valid spawn position for {hotspotTimer.Hotspot.Name} after {maxSpawnAttempts}, moving on to next bot anyways");
                    #endif
                }

                await SpawnBotFromCacheOrCreateNew(BotCacheDataList, wildSpawnType, side, ibotCreator, botSpawnerClass, (Vector3)spawnPosition, cancellationTokenSource, botDifficulty, hotspotTimer);
            }

        }

        #region botGroups

        private int getActualBotCount(string pluginGroupChance, int count)
        {
            if (pluginGroupChance == "None")
            {
                return 1;
            }
            else if (pluginGroupChance == "Max")
            {
                return count;
            }
            else if (pluginGroupChance == "Random")
            {
                string[] groupChances = { "None", "Low", "Default", "High", "Max" };
                pluginGroupChance = groupChances[UnityEngine.Random.Range(0, groupChances.Length)];
            }
            else
            {
                // Assuming getGroupChance is the actual implementation for non-random cases
                int actualGroupChance = getGroupChance(pluginGroupChance, count);
                return actualGroupChance;
            }

            // Recursively call the function with the updated pluginGroupChance
            return getActualBotCount(pluginGroupChance, count);
        }

        // i'm not sure how all this works, ChatGPT wrote this for me
        private int getGroupChance(string pmcGroupChance, int maxCount)
        {
            int actualMaxCount = maxCount;

            // Adjust probabilities based on maxCount
            double[] probabilities;
            try
            {
                probabilities = GetProbabilityArray(pmcGroupChance);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error getting probability array for group chance {pmcGroupChance}. Using default.");
                probabilities = GetDefaultProbabilityArray(pmcGroupChance);
            }

            System.Random random = new System.Random();

            // Determine actualMaxCount based on pmcGroupChance and probabilities
            actualMaxCount = getOutcomeWithProbability(random, probabilities, maxCount) + 1;

            return Math.Min(actualMaxCount, maxCount);
        }

        private double[] GetProbabilityArray(string pmcGroupChance)
        {
            if (DonutsPlugin.groupChanceWeights.TryGetValue(pmcGroupChance, out var relativeWeights))
            {
                double totalWeight = relativeWeights.Sum(); // Sum of all weights
                return relativeWeights.Select(weight => weight / totalWeight).ToArray();
            }

            throw new ArgumentException($"Invalid pmcGroupChance: {pmcGroupChance}");
        }

        private double[] GetDefaultProbabilityArray(string pmcGroupChance)
        {
            if (DonutsPlugin.groupChanceWeights.TryGetValue(pmcGroupChance, out var relativeWeights))
            {
                double totalWeight = relativeWeights.Sum(); // Sum of all weights
                return relativeWeights.Select(weight => weight / totalWeight).ToArray();
            }

            throw new ArgumentException($"Invalid pmcGroupChance: {pmcGroupChance}");
        }

        private int getOutcomeWithProbability(System.Random random, double[] probabilities, int maxCount)
        {
            double probabilitySum = 0.0;
            foreach (var probability in probabilities)
            {
                probabilitySum += probability;
            }

            if (Math.Abs(probabilitySum - 1.0) > 0.0001)
            {
                throw new InvalidOperationException("Probabilities should sum up to 1.");
            }

            double probabilityThreshold = random.NextDouble();
            double cumulative = 0.0;
            for (int i = 0; i < maxCount; i++)
            {
                cumulative += probabilities[i];
                if (probabilityThreshold < cumulative)
                {
                    return i;
                }
            }
            // Default outcome if probabilities are not well-defined
            return maxCount - 1;
        }

        #endregion

        #region botHelperMethods

        #region botDifficulty
        private BotDifficulty GetBotDifficulty(WildSpawnType wildSpawnType)
        {
            if (wildSpawnType == WildSpawnType.assault)
            {
                return grabSCAVDifficulty();
            }
            else if (wildSpawnType == sptUsec || wildSpawnType == sptBear || wildSpawnType == WildSpawnType.pmcBot)
            {
                return grabPMCDifficulty();
            }
            else
            {
                return grabOtherDifficulty();
            }
        }
        public static BotDifficulty grabPMCDifficulty()
        {
            switch (DonutsPlugin.botDifficultiesPMC.Value.ToLower())
            {
                case "asonline":
                    //return random difficulty from array of easy, normal, hard
                    BotDifficulty[] randomDifficulty = {
                        BotDifficulty.easy,
                        BotDifficulty.normal,
                        BotDifficulty.hard
                    };
                    var diff = UnityEngine.Random.Range(0, 3);
                    return randomDifficulty[diff];
                case "easy":
                    return BotDifficulty.easy;
                case "normal":
                    return BotDifficulty.normal;
                case "hard":
                    return BotDifficulty.hard;
                case "impossible":
                    return BotDifficulty.impossible;
                default:
                    return BotDifficulty.normal;
            }
        }
        public static BotDifficulty grabSCAVDifficulty()
        {
            switch (DonutsPlugin.botDifficultiesSCAV.Value.ToLower())
            {
                case "asonline":
                    //return random difficulty from array of easy, normal, hard
                    BotDifficulty[] randomDifficulty = {
                        BotDifficulty.easy,
                        BotDifficulty.normal,
                        BotDifficulty.hard
                    };
                    var diff = UnityEngine.Random.Range(0, 3);
                    return randomDifficulty[diff];
                case "easy":
                    return BotDifficulty.easy;
                case "normal":
                    return BotDifficulty.normal;
                case "hard":
                    return BotDifficulty.hard;
                case "impossible":
                    return BotDifficulty.impossible;
                default:
                    return BotDifficulty.normal;
            }
        }
        public static BotDifficulty grabOtherDifficulty()
        {
            switch (DonutsPlugin.botDifficultiesOther.Value.ToLower())
            {
                case "asonline":
                    //return random difficulty from array of easy, normal, hard
                    BotDifficulty[] randomDifficulty = {
                        BotDifficulty.easy,
                        BotDifficulty.normal,
                        BotDifficulty.hard
                    };
                    var diff = UnityEngine.Random.Range(0, 3);
                    return randomDifficulty[diff];
                case "easy":
                    return BotDifficulty.easy;
                case "normal":
                    return BotDifficulty.normal;
                case "hard":
                    return BotDifficulty.hard;
                case "impossible":
                    return BotDifficulty.impossible;
                default:
                    return BotDifficulty.normal;
            }
        }

        #endregion

        private async Task SpawnBotFromCacheOrCreateNew(List<BotCacheClass> botCacheList, WildSpawnType wildSpawnType, EPlayerSide side, IBotCreator ibotCreator,
            BotSpawner botSpawnerClass, Vector3 spawnPosition, CancellationTokenSource cancellationTokenSource, BotDifficulty botDifficulty, HotspotTimer hotspotTimer)
        {
            #if DEBUG
                DonutComponent.Logger.LogDebug("Bot Cache is not empty. Finding Cached Bot");
            #endif
            var botCacheElement = DonutsBotPrep.FindCachedBots(wildSpawnType, botDifficulty, 1);

            if (botCacheElement != null)
            {
                botCacheList.Remove(botCacheElement);

                var closestBotZone = botSpawnerClass.GetClosestZone(spawnPosition, out float dist);
                botCacheElement.AddPosition(spawnPosition);

            #if DEBUG
                DonutComponent.Logger.LogWarning($"Spawning bot at distance to player of: {Vector3.Distance(spawnPosition, DonutComponent.gameWorld.MainPlayer.Position)} " +
                    $"of side: {botCacheElement.Side} and difficulty: {botDifficulty} for hotspot {hotspotTimer.Hotspot.Name} ");
            #endif
                ActivateBot(closestBotZone, botCacheElement, cancellationTokenSource);
            }

            else
            {
            #if DEBUG
                DonutComponent.Logger.LogDebug("Bot Cache is empty for solo bot. Creating a new bot.");
            #endif
                CreateNewBot(wildSpawnType, side, ibotCreator, botSpawnerClass, spawnPosition, cancellationTokenSource);
            }
        }

        private async Task SpawnBotForGroup(List<BotCacheClass> botCacheList, WildSpawnType wildSpawnType, EPlayerSide side, IBotCreator ibotCreator,
            BotSpawner botSpawnerClass, Vector3 spawnPosition, CancellationTokenSource cancellationTokenSource, BotDifficulty botDifficulty, int maxCount, HotspotTimer hotspotTimer)
        {
            if (botCacheList != null && botCacheList.Count > 0)
            {
                //since last element was the group that was just added, remove it
                var botCacheElement = DonutsBotPrep.FindCachedBots(wildSpawnType, botDifficulty, maxCount);
                botCacheList.Remove(botCacheElement);

                var closestBotZone = botSpawnerClass.GetClosestZone(spawnPosition, out float dist);
                botCacheElement.AddPosition(spawnPosition);

                #if DEBUG
                    DonutComponent.Logger.LogWarning($"Spawning grouped bots at distance to player of: {Vector3.Distance(spawnPosition, DonutComponent.gameWorld.MainPlayer.Position)} " +
                        $"of side: {botCacheElement.Side} and difficulty: {botDifficulty} at hotspot: {hotspotTimer.Hotspot.Name}");
                #endif

                ActivateBot(closestBotZone, botCacheElement, cancellationTokenSource);
            }
        }
        public async Task CreateNewBot(WildSpawnType wildSpawnType, EPlayerSide side, IBotCreator ibotCreator, BotSpawner botSpawnerClass, Vector3 spawnPosition, CancellationTokenSource cancellationTokenSource)
        {
            BotDifficulty botdifficulty = GetBotDifficulty(wildSpawnType);

            IProfileData botData = new IProfileData(side, wildSpawnType, botdifficulty, 0f, null);
            BotCacheClass bot = await BotCacheClass.Create(botData, ibotCreator, 1, botSpawnerClass);
            bot.AddPosition((Vector3)spawnPosition);

            var closestBotZone = botSpawnerClass.GetClosestZone((Vector3)spawnPosition, out float dist);
            #if DEBUG
                DonutComponent.Logger.LogWarning($"Spawning bot at distance to player of: {Vector3.Distance((Vector3)spawnPosition, DonutComponent.gameWorld.MainPlayer.Position)} " +
                    $"of side: {bot.Side} and difficulty: {botdifficulty}");
            #endif

            ActivateBot(closestBotZone, bot, cancellationTokenSource);
        }

        public void ActivateBot(BotZone botZone, BotCacheClass botData, CancellationTokenSource cancellationTokenSource)
        {
            CreateBotCallbackWrapper createBotCallbackWrapper = new CreateBotCallbackWrapper();
            createBotCallbackWrapper.botData = botData;

            GetGroupWrapper getGroupWrapper = new GetGroupWrapper();

            // Call ActivateBot directly, using our own group callback and bot created callback
            // NOTE: Make sure to pass "false" for the third parameter to avoid "assaultGroup" conversion
            botCreator.ActivateBot(botData, botZone, false, new Func<BotOwner, BotZone, BotsGroup>(getGroupWrapper.GetGroupAndSetEnemies), new Action<BotOwner>(createBotCallbackWrapper.CreateBotCallback), cancellationTokenSource.Token);
        }

        // Custom GetGroupAndSetEnemies wrapper that handles grouping bots into multiple groups within the same botzone
        internal class GetGroupWrapper
        {
            private BotsGroup group = null;

            public BotsGroup GetGroupAndSetEnemies(BotOwner bot, BotZone zone)
            {
                // If we haven't found/created our BotsGroup yet, do so, and then lock it so nobody else can use it
                if (group == null)
                {
                    group = botSpawnerClass.GetGroupAndSetEnemies(bot, zone);
                    group.Lock();
                }

                return group;
            }
        }

        // Wrapper around method_10 called after bot creation, so we can pass it the BotCacheClass data
        internal class CreateBotCallbackWrapper
        {
            public BotCacheClass botData;
            public Stopwatch stopWatch = new Stopwatch();

            public void CreateBotCallback(BotOwner bot)
            {
                bool shallBeGroup = botData.SpawnParams?.ShallBeGroup != null;

                // I have no idea why BSG passes a stopwatch into this call...
                stopWatch.Start();
                methodCache["method_10"].Invoke(botSpawnerClass, new object[] { bot, botData, null, shallBeGroup, stopWatch });
            }
        }

        private WildSpawnType GetWildSpawnType(string spawnType)
        {
            switch (spawnType.ToLower())
            {
                case "arenafighterevent":
                    return WildSpawnType.arenaFighterEvent;
                case "assault":
                    return WildSpawnType.assault;
                case "assaultgroup":
                    return WildSpawnType.assaultGroup;
                case "bossboar":
                    return WildSpawnType.bossBoar;
                case "bossboarsniper":
                    return WildSpawnType.bossBoarSniper;
                case "bossbully":
                    return WildSpawnType.bossBully;
                case "bossgluhar":
                    return WildSpawnType.bossGluhar;
                case "bosskilla":
                    return WildSpawnType.bossKilla;
                case "bosskojaniy":
                    return WildSpawnType.bossKojaniy;
                case "bosssanitar":
                    return WildSpawnType.bossSanitar;
                case "bosstagilla":
                    return WildSpawnType.bossTagilla;
                case "bosszryachiy":
                    return WildSpawnType.bossZryachiy;
                case "crazyassaultevent":
                    return WildSpawnType.crazyAssaultEvent;
                case "cursedassault":
                    return WildSpawnType.cursedAssault;
                case "exusec-rogues":
                    return WildSpawnType.exUsec;
                case "followerboar":
                    return WildSpawnType.followerBoar;
                case "followerbully":
                    return WildSpawnType.followerBully;
                case "followergluharassault":
                    return WildSpawnType.followerGluharAssault;
                case "followergluharscout":
                    return WildSpawnType.followerGluharScout;
                case "followergluharsecurity":
                    return WildSpawnType.followerGluharSecurity;
                case "followergluharsnipe":
                    return WildSpawnType.followerGluharSnipe;
                case "followerkojaniy":
                    return WildSpawnType.followerKojaniy;
                case "followersanitar":
                    return WildSpawnType.followerSanitar;
                case "followertagilla":
                    return WildSpawnType.followerTagilla;
                case "followerzryachiy":
                    return WildSpawnType.followerZryachiy;
                case "gifter":
                    return WildSpawnType.gifter;
                case "marksman":
                    return WildSpawnType.marksman;
                case "raiders":
                    return WildSpawnType.pmcBot;
                case "sectantpriest":
                    return WildSpawnType.sectantPriest;
                case "sectantwarrior":
                    return WildSpawnType.sectantWarrior;
                case "usec":
                    return (WildSpawnType)AkiBotsPrePatcher.sptUsecValue;
                case "bear":
                    return (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
                case "sptusec":
                    return (WildSpawnType)AkiBotsPrePatcher.sptUsecValue;
                case "sptbear":
                    return (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
                case "followerbigpipe":
                    return WildSpawnType.followerBigPipe;
                case "followerbirdeye":
                    return WildSpawnType.followerBirdEye;
                case "bossknight":
                    return WildSpawnType.bossKnight;
                case "pmc":
                    //random wildspawntype is either assigned sptusec or sptbear at 50/50 chance
                    return (UnityEngine.Random.Range(0, 2) == 0) ? (WildSpawnType)AkiBotsPrePatcher.sptUsecValue : (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
                default:
                    return WildSpawnType.assault;
            }

        }
        private EPlayerSide GetSideForWildSpawnType(WildSpawnType spawnType)
        {
            //define spt wildspawn
            WildSpawnType sptUsec = (WildSpawnType)AkiBotsPrePatcher.sptUsecValue;
            WildSpawnType sptBear = (WildSpawnType)AkiBotsPrePatcher.sptBearValue;

            if (spawnType == WildSpawnType.pmcBot || spawnType == sptUsec)
            {
                return EPlayerSide.Usec;
            }
            else if (spawnType == sptBear)
            {
                return EPlayerSide.Bear;
            }
            else
            {
                return EPlayerSide.Savage;
            }
        }

        private void DespawnFurthestBot(string bottype)
        {
            var bots = gameWorld.RegisteredPlayers;
            float maxDistance = -1f;
            Player furthestBot = null;
            var tempBotCount = 0;

            if (bottype == "pmc")
            {
                if (Time.time - PMCdespawnCooldown < PMCdespawnCooldownDuration)
                {
                    return; // Exit the method without despawning
                }

                //don't know distances so have to loop through all bots
                foreach (Player bot in bots)
                {
                    // Ignore bots on the invalid despawn list, and the player
                    if (bot.IsYourPlayer || !validDespawnListPMC.Contains(bot.Profile.Info.Settings.Role) || bot.AIData.BotOwner.BotState != EBotState.Active)
                    {
                        continue;
                    }


                    // Don't include bots that have spawned within the last 10 seconds
                    if (Time.time - 10 < bot.AIData.BotOwner.ActivateTime)
                    {
                        continue;
                    }

                    float distance = (bot.Position - gameWorld.MainPlayer.Position).sqrMagnitude;
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        furthestBot = bot;
                    }

                    //add bots that match criteria but distance doesn't matter
                    tempBotCount++;
                }


            }
            else if (bottype == "scav")
            {
                if (Time.time - SCAVdespawnCooldown < SCAVdespawnCooldownDuration)
                {
                    return;
                }

                //don't know distances so have to loop through all bots
                foreach (Player bot in bots)
                {
                    // Ignore bots on the invalid despawn list, and the player
                    if (bot.IsYourPlayer || !validDespawnListScav.Contains(bot.Profile.Info.Settings.Role) || bot.AIData.BotOwner.BotState != EBotState.Active)
                    {
                        continue;
                    }

                    // Don't include bots that have spawned within the last 10 seconds
                    if (Time.time - 10 < bot.AIData.BotOwner.ActivateTime)
                    {
                        continue;
                    }

                    float distance = (bot.Position - gameWorld.MainPlayer.Position).sqrMagnitude;
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        furthestBot = bot;
                    }

                    //add bots that match criteria but distance doesn't matter
                    tempBotCount++;
                }
            }

            if (furthestBot != null)
            {
                if (bottype == "pmc" && tempBotCount <= PMCBotLimit)
                {
                    return;
                }
                else if (bottype == "scav" && tempBotCount <= SCAVBotLimit)
                {
                    return;
                }

                // Despawn the bot
                #if DEBUG
                    Logger.LogDebug($"Despawning bot: {furthestBot.Profile.Info.Nickname} ({furthestBot.name})");
                #endif
                BotOwner botOwner = furthestBot.AIData.BotOwner;

                var botgame = Singleton<IBotGame>.Instance;
                Singleton<Effects>.Instance.EffectsCommutator.StopBleedingForPlayer(botOwner.GetPlayer);
                botOwner.Deactivate();
                botOwner.Dispose();
                botgame.BotsController.BotDied(botOwner);
                botgame.BotsController.DestroyInfo(botOwner.GetPlayer);
                DestroyImmediate(botOwner.gameObject);
                Destroy(botOwner);

                if (bottype == "pmc")
                {
                    PMCdespawnCooldown = Time.time;
                }
                else if (bottype == "scav")
                {
                    SCAVdespawnCooldown = Time.time;
                }
            }

        }
        private async Task<Vector3?> GetValidSpawnPosition(Entry hotspot, Vector3 coordinate, int maxSpawnAttempts)
        {
            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                Vector3 spawnPosition = GenerateRandomSpawnPosition(hotspot, coordinate);

                if (NavMesh.SamplePosition(spawnPosition, out var navHit, 2f, NavMesh.AllAreas))
                {
                    spawnPosition = navHit.position;

                    if (IsValidSpawnPosition(spawnPosition, hotspot))
                    {
                        #if DEBUG
                            Logger.LogDebug("Found spawn position at: " + spawnPosition);
                        #endif
                        return spawnPosition;
                    }
                }

                await Task.Delay(1);
            }

            return null;
        }

        private Vector3 GenerateRandomSpawnPosition(Entry hotspot, Vector3 coordinate)
        {
            float randomX = UnityEngine.Random.Range(-hotspot.MaxDistance, hotspot.MaxDistance);
            float randomZ = UnityEngine.Random.Range(-hotspot.MaxDistance, hotspot.MaxDistance);

            return new Vector3(coordinate.x + randomX, coordinate.y, coordinate.z + randomZ);
        }
        #endregion

        #region spawnchecks
        private bool IsValidSpawnPosition(Vector3 spawnPosition, Entry hotspot)
        {
            if (spawnPosition != null && hotspot != null)
            {
                return !IsSpawnPositionInsideWall(spawnPosition) &&
                       !IsSpawnPositionInPlayerLineOfSight(spawnPosition) &&
                       !IsSpawnInAir(spawnPosition) &&
                       !IsMinSpawnDistanceFromPlayerTooShort(spawnPosition, hotspot);
            }
            return false;
        }
        private bool IsSpawnPositionInPlayerLineOfSight(Vector3 spawnPosition)
        {
            //add try catch for when player is null at end of raid
            try
            {
                Vector3 playerPosition = gameWorld.MainPlayer.MainParts[BodyPartType.head].Position;
                Vector3 direction = (playerPosition - spawnPosition).normalized;
                float distance = Vector3.Distance(spawnPosition, playerPosition);

                RaycastHit hit;
                if (!Physics.Raycast(spawnPosition, direction, out hit, distance, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    // No objects found between spawn point and player
                    return true;
                }
            }
            catch { }

            return false;
        }
        private bool IsSpawnPositionInsideWall(Vector3 position)
        {
            // Check if any game object parent has the name "WALLS" in it
            Vector3 boxSize = new Vector3(1f, 1f, 1f);
            Collider[] colliders = Physics.OverlapBox(position, boxSize, Quaternion.identity, LayerMaskClass.LowPolyColliderLayer);

            foreach (var collider in colliders)
            {
                Transform currentTransform = collider.transform;
                while (currentTransform != null)
                {
                    if (currentTransform.gameObject.name.ToUpper().Contains("WALLS"))
                    {
                        return true;
                    }
                    currentTransform = currentTransform.parent;
                }
            }

            return false;
        }

        /*private bool IsSpawnPositionObstructed(Vector3 position)
        {
            Ray ray = new Ray(position, Vector3.up);
            float distance = 5f;

            if (Physics.Raycast(ray, out RaycastHit hit, distance, LayerMaskClass.TerrainMask))
            {
                // If the raycast hits a collider, it means the position is obstructed
                return true;
            }

            return false;
        }*/
        private bool IsSpawnInAir(Vector3 position)
        {
            // Raycast down and determine if the position is in the air or not
            Ray ray = new Ray(position, Vector3.down);
            float distance = 10f;

            if (Physics.Raycast(ray, out RaycastHit hit, distance, LayerMaskClass.HighPolyWithTerrainMask))
            {
                // If the raycast hits a collider, it means the position is not in the air
                return false;
            }
            return true;
        }
        private bool IsMinSpawnDistanceFromPlayerTooShort(Vector3 position, Entry hotspot)
        {
            //if distance between player and spawn position is less than the hotspot min distance
            if (Vector3.Distance(gameWorld.MainPlayer.Position, position) < hotspot.MinSpawnDistanceFromPlayer)
            {
                return true;
            }

            return false;
        }
        #endregion

        #region displaymarkerstuff
        private StringBuilder DisplayedMarkerInfo = new StringBuilder();
        private StringBuilder PreviousMarkerInfo = new StringBuilder();
        private Coroutine resetMarkerInfoCoroutine;
        private void DisplayMarkerInformation()
        {
            if (gizmoSpheres.Count == 0)
            {
                return;
            }

            GameObject closestShape = null;
            float closestDistanceSq = float.MaxValue;

            // Find the closest primitive shape game object to the player
            foreach (var shape in gizmoSpheres)
            {
                Vector3 shapePosition = shape.transform.position;
                float distanceSq = (shapePosition - gameWorld.MainPlayer.Transform.position).sqrMagnitude;
                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    closestShape = shape;
                }
            }

            // Check if the closest shape is within 15m and directly visible to the player
            if (closestShape != null && closestDistanceSq <= 10f * 10f)
            {
                Vector3 direction = closestShape.transform.position - gameWorld.MainPlayer.Transform.position;
                float angle = Vector3.Angle(gameWorld.MainPlayer.Transform.forward, direction);

                if (angle < 20f)
                {
                    // Create a HashSet of positions for fast containment checks
                    var locationsSet = new HashSet<Vector3>();
                    foreach (var entry in fightLocations.Locations.Concat(sessionLocations.Locations))
                    {
                        locationsSet.Add(new Vector3(entry.Position.x, entry.Position.y, entry.Position.z));
                    }

                    // Check if the closest shape's position is contained in the HashSet
                    Vector3 closestShapePosition = closestShape.transform.position;
                    if (locationsSet.Contains(closestShapePosition))
                    {
                        if (displayMessageNotificationMethod != null)
                        {
                            Entry closestEntry = GetClosestEntry(closestShapePosition);
                            if (closestEntry != null)
                            {
                                PreviousMarkerInfo.Clear();
                                PreviousMarkerInfo.Append(DisplayedMarkerInfo);

                                DisplayedMarkerInfo.Clear();

                                DisplayedMarkerInfo.AppendLine("Donuts: Marker Info");
                                DisplayedMarkerInfo.AppendLine($"GroupNum: {closestEntry.GroupNum}");
                                DisplayedMarkerInfo.AppendLine($"Name: {closestEntry.Name}");
                                DisplayedMarkerInfo.AppendLine($"SpawnType: {closestEntry.WildSpawnType}");
                                DisplayedMarkerInfo.AppendLine($"Position: {closestEntry.Position.x}, {closestEntry.Position.y}, {closestEntry.Position.z}");
                                DisplayedMarkerInfo.AppendLine($"Bot Timer Trigger: {closestEntry.BotTimerTrigger}");
                                DisplayedMarkerInfo.AppendLine($"Spawn Chance: {closestEntry.SpawnChance}");
                                DisplayedMarkerInfo.AppendLine($"Max Random Number of Bots: {closestEntry.MaxRandomNumBots}");
                                DisplayedMarkerInfo.AppendLine($"Max Spawns Before Cooldown: {closestEntry.MaxSpawnsBeforeCoolDown}");
                                DisplayedMarkerInfo.AppendLine($"Ignore Timer for First Spawn: {closestEntry.IgnoreTimerFirstSpawn}");
                                DisplayedMarkerInfo.AppendLine($"Min Spawn Distance From Player: {closestEntry.MinSpawnDistanceFromPlayer}");
                                string txt = DisplayedMarkerInfo.ToString();

                                // Check if the marker info has changed since the last update
                                if (txt != PreviousMarkerInfo.ToString())
                                {
                                    MethodInfo displayMessageNotificationMethod;
                                    if (methodCache.TryGetValue("DisplayMessageNotification", out displayMessageNotificationMethod))
                                    {
                                        displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Default, Color.yellow });
                                    }

                                    // Stop the existing coroutine if it's running
                                    if (resetMarkerInfoCoroutine != null)
                                    {
                                        StopCoroutine(resetMarkerInfoCoroutine);
                                    }

                                    // Start a new coroutine to reset the marker info after a delay
                                    resetMarkerInfoCoroutine = StartCoroutine(ResetMarkerInfoAfterDelay());
                                }
                            }
                        }
                    }
                }
            }
        }
        private IEnumerator ResetMarkerInfoAfterDelay()
        {
            yield return new WaitForSeconds(5f);

            // Reset the marker info
            DisplayedMarkerInfo.Clear();
            resetMarkerInfoCoroutine = null;
        }
        private Entry GetClosestEntry(Vector3 position)
        {
            Entry closestEntry = null;
            float closestDistanceSq = float.MaxValue;

            foreach (var entry in fightLocations.Locations.Concat(sessionLocations.Locations))
            {
                Vector3 entryPosition = new Vector3(entry.Position.x, entry.Position.y, entry.Position.z);
                float distanceSq = (entryPosition - position).sqrMagnitude;
                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    closestEntry = entry;
                }
            }

            return closestEntry;
        }
        public static MethodInfo GetDisplayMessageNotificationMethod() => displayMessageNotificationMethod;
        #endregion

        #region gizmos
        private IEnumerator UpdateGizmoSpheresCoroutine()
        {
            while (isGizmoEnabled)
            {
                RefreshGizmoDisplay(); // Refresh the gizmo display periodically

                yield return new WaitForSeconds(3f);
            }
        }
        private void DrawMarkers(List<Entry> locations, Color color, PrimitiveType primitiveType)
        {
            foreach (var hotspot in locations)
            {
                var newCoordinate = new Vector3(hotspot.Position.x, hotspot.Position.y, hotspot.Position.z);

                if (maplocation == hotspot.MapName && !drawnCoordinates.Contains(newCoordinate))
                {
                    var marker = GameObject.CreatePrimitive(primitiveType);
                    var material = marker.GetComponent<Renderer>().material;
                    material.color = color;
                    marker.GetComponent<Collider>().enabled = false;
                    marker.transform.position = newCoordinate;

                    if (DonutsPlugin.gizmoRealSize.Value)
                    {
                        marker.transform.localScale = new Vector3(hotspot.MaxDistance, 3f, hotspot.MaxDistance);
                    }
                    else
                    {
                        marker.transform.localScale = new Vector3(1f, 1f, 1f);
                    }

                    gizmoSpheres.Add(marker);
                    drawnCoordinates.Add(newCoordinate);
                }
            }
        }

        public void ToggleGizmoDisplay(bool enableGizmos)
        {
            isGizmoEnabled = enableGizmos;

            if (isGizmoEnabled && gizmoUpdateCoroutine == null)
            {
                RefreshGizmoDisplay(); // Refresh the gizmo display initially
                gizmoUpdateCoroutine = StartCoroutine(UpdateGizmoSpheresCoroutine());
            }
            else if (!isGizmoEnabled && gizmoUpdateCoroutine != null)
            {
                StopCoroutine(gizmoUpdateCoroutine);
                gizmoUpdateCoroutine = null;

                ClearGizmoMarkers(); // Clear the drawn markers
            }
        }

        private void RefreshGizmoDisplay()
        {
            ClearGizmoMarkers(); // Clear existing markers

            // Check the values of DebugGizmos and gizmoRealSize and redraw the markers accordingly
            if (DonutsPlugin.DebugGizmos.Value)
            {
                if (fightLocations != null && fightLocations.Locations != null && fightLocations.Locations.Count > 0)
                {
                    DrawMarkers(fightLocations.Locations, Color.green, PrimitiveType.Sphere);
                }

                if (sessionLocations != null && sessionLocations.Locations != null && sessionLocations.Locations.Count > 0)
                {
                    DrawMarkers(sessionLocations.Locations, Color.red, PrimitiveType.Cube);
                }
            }
        }

        private void ClearGizmoMarkers()
        {
            foreach (var marker in gizmoSpheres)
            {
                Destroy(marker);
            }
            gizmoSpheres.Clear();
            drawnCoordinates.Clear();
        }

        private void OnGUI() => ToggleGizmoDisplay(DonutsPlugin.DebugGizmos.Value);
        #endregion
    }


    #region classes
    public class HotspotTimer
    {
        private Entry hotspot;
        private float timer;
        public bool inCooldown;
        public int timesSpawned;
        private float cooldownTimer;
        public Entry Hotspot => hotspot;

        public HotspotTimer(Entry hotspot)
        {
            this.hotspot = hotspot;
            this.timer = 0f;
            this.inCooldown = false;
            this.timesSpawned = 0;
            this.cooldownTimer = 0f;
        }

        public void UpdateTimer()
        {
            timer += Time.deltaTime;
            if (inCooldown)
            {
                cooldownTimer += Time.deltaTime;
                if (cooldownTimer >= DonutsPlugin.coolDownTimer.Value)
                {
                    inCooldown = false;
                    cooldownTimer = 0f;
                    timesSpawned = 0;
                }
            }
        }

        public float GetTimer() => timer;
        public bool ShouldSpawn()
        {
            if (hotspot.IgnoreTimerFirstSpawn == true)
            {
                return true;
            }
            return timer >= hotspot.BotTimerTrigger;
        }

        public void ResetTimer() => timer = 0f;
    }

    public class Entry
    {
        public string MapName
        {
            get; set;
        }
        public int GroupNum
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        public Position Position
        {
            get; set;
        }
        public string WildSpawnType
        {
            get; set;
        }
        public float MinDistance
        {
            get; set;
        }
        public float MaxDistance
        {
            get; set;
        }

        public float BotTriggerDistance
        {
            get; set;
        }

        public float BotTimerTrigger
        {
            get; set;
        }
        public int MaxRandomNumBots
        {
            get; set;
        }

        public int SpawnChance
        {
            get; set;
        }

        public int MaxSpawnsBeforeCoolDown
        {
            get; set;
        }

        public bool IgnoreTimerFirstSpawn
        {
            get; set;
        }

        public float MinSpawnDistanceFromPlayer
        {
            get; set;
        }
    }

    public class Position
    {
        public float x
        {
            get; set;
        }
        public float y
        {
            get; set;
        }
        public float z
        {
            get; set;
        }
    }

    public class FightLocations
    {
        public List<Entry> Locations
        {
            get; set;
        }
    }

    #endregion
}

