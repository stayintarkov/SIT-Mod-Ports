using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using Newtonsoft.Json;
using UnityEngine;

//disable the ide0007 warning for the entire file
#pragma warning disable IDE0007

namespace Donuts
{

    [BepInPlugin("com.dvize.Donuts", "dvize.Donuts", "1.4.3")]
    [BepInDependency("com.spt-aki.core", "3.8.0")]
    [BepInDependency("xyz.drakia.waypoints")]
    public class DonutsPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled;
        public static ConfigEntry<float> botTimerTrigger;
        public static ConfigEntry<float> coolDownTimer;
        public static ConfigEntry<bool> DespawnEnabledPMC;
        public static ConfigEntry<bool> DespawnEnabledSCAV;
        public static ConfigEntry<bool> HardCapEnabled;
        public static ConfigEntry<bool> hardStopOptionPMC;
        public static ConfigEntry<bool> hardStopOptionSCAV;
        public static ConfigEntry<bool> hotspotBoostPMC;
        public static ConfigEntry<bool> hotspotBoostSCAV;
        public static ConfigEntry<bool> hotspotIgnoreHardCapPMC;
        public static ConfigEntry<bool> hotspotIgnoreHardCapSCAV;
        public static ConfigEntry<int> hardStopTimePMC;
        public static ConfigEntry<int> hardStopTimeSCAV;
        public static ConfigEntry<string> forceAllBotType;

        // Global Min Distance From Player
        public static ConfigEntry<bool> globalMinSpawnDistanceFromPlayerBool;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerFactory;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerCustoms;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerGroundZero;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerInterchange;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerLaboratory;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerLighthouse;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerReserve;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerStreets;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerWoods;
        public static ConfigEntry<float> globalMinSpawnDistanceFromPlayerShoreline;

        public static ConfigEntry<bool> DebugGizmos;
        public static ConfigEntry<bool> gizmoRealSize;
        public static ConfigEntry<int> maxSpawnTriesPerBot;
        public static ConfigEntry<bool> ShowRandomFolderChoice;

        //Add folder scenarios
        internal static List<Folder> scenarios = new List<Folder>();
        internal static List<Folder> randomScenarios = new List<Folder>();
        public static ConfigEntry<string> scenarioSelection;
        public static ConfigEntry<string> scavScenarioSelection;
        public string[] scenarioValues = new string[] { };

        public static ConfigEntry<string> pmcGroupChance;
        public static ConfigEntry<string> scavGroupChance;

        public static ConfigEntry<string> pmcFaction;

        public static ConfigEntry<string> groupWeightDistroLow;
        public static ConfigEntry<string> groupWeightDistroDefault;
        public static ConfigEntry<string> groupWeightDistroHigh;

        //bot difficulty
        public static ConfigEntry<string> botDifficultiesPMC;
        public static ConfigEntry<string> botDifficultiesSCAV;
        public static ConfigEntry<string> botDifficultiesOther;
        public string[] botDiffList = new string[] { "AsOnline", "Easy", "Normal", "Hard", "Impossible" };

        // Bot Groups
        public string[] pmcGroupChanceList = new string[] { "None", "Default", "Low", "High", "Max", "Random" };
        public string[] scavGroupChanceList = new string[] { "None", "Default", "Low", "High", "Max", "Random" };

        public string[] pmcFactionList = new string[] { "Default", "USEC", "BEAR" };

        public string[] forceAllBotTypeList = new string[] { "Disabled", "SCAV", "PMC" };

        public static Dictionary<string, int[]> groupChanceWeights = new Dictionary<string, int[]>
        {
            { "Low", new int[] { 400, 90, 9, 0, 0 } },
            { "Default", new int[] { 210, 210, 45, 25, 10 } },
            { "High", new int[] { 0, 75, 175, 175, 75 } }
        };

        public string ConvertIntArrayToString(int[] array)
        {
            return string.Join(",", array);
        }

        //menu vars
        public static ConfigEntry<string> spawnName;
        public static ConfigEntry<int> groupNum;
        //make groupList of numbers 1-100
        public static int[] groupList = Enumerable.Range(1, 100).ToArray();
        public static ConfigEntry<string> wildSpawns;

        public string[] wildDropValues = new string[]
        {
            "arenafighterevent",
            "assault",
            "assaultgroup",
            "bossboar",
            "bossboarsniper",
            "bossbully",
            "bossgluhar",
            "bosskilla",
            "bossknight",
            "bosskojaniy",
            "bosssanitar",
            "bosstagilla",
            "bosszryachiy",
            "crazyassaultevent",
            "cursedassault",
            "exusec",
            "followerbigpipe",
            "followerbirdeye",
            "followerboar",
            "followerbully",
            "followergluharassault",
            "followergluharscout",
            "followergluharsecurity",
            "followergluharsnipe",
            "followerkojaniy",
            "followersanitar",
            "followertagilla",
            "followerzryachiy",
            "gifter",
            "marksman",
            "pmc",
            "sectantpriest",
            "sectantwarrior",
            "sptusec",
            "sptbear"
        };
        public static ConfigEntry<float> minSpawnDist;
        public static ConfigEntry<float> maxSpawnDist;
        public static ConfigEntry<float> botTriggerDistance;
        public static ConfigEntry<int> maxRandNumBots;
        public static ConfigEntry<int> spawnChance;
        public static ConfigEntry<int> maxSpawnsBeforeCooldown;
        public static ConfigEntry<bool> ignoreTimerFirstSpawn;
        public static ConfigEntry<float> minSpawnDistanceFromPlayer;

        public static ConfigEntry<bool> saveNewFileOnly;
        public static ConfigEntry<BepInEx.Configuration.KeyboardShortcut> CreateSpawnMarkerKey;

        public static ConfigEntry<BepInEx.Configuration.KeyboardShortcut> WriteToFileKey;

        public static ConfigEntry<BepInEx.Configuration.KeyboardShortcut> DeleteSpawnMarkerKey;

        private void Awake()
        {
            //run dependency checker

            if (!DependencyChecker.ValidateDependencies(Logger, Info, this.GetType(), Config))
            {
                throw new Exception($"Missing Dependencies");
            }

            string defaultWeightsString = ConvertIntArrayToString(groupChanceWeights["Default"]);
            string lowWeightsString = ConvertIntArrayToString(groupChanceWeights["Low"]);
            string highWeightsString = ConvertIntArrayToString(groupChanceWeights["High"]);

            //Main Settings
            PluginEnabled = Config.Bind(
                "1. Main Settings",
                "Donuts On/Off",
                true,
                new ConfigDescription("Enable/Disable Spawning from Donuts Points",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 16 }));

            DespawnEnabledPMC = Config.Bind(
                "1. Main Settings",
                "Despawn PMCs",
                true,
                new ConfigDescription("When enabled, removes furthest PMC bots from player for each new dynamic spawn bot that is over your Donuts bot caps (ScenarioConfig.json).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 15 }));

            DespawnEnabledSCAV = Config.Bind(
                "1. Main Settings",
                "Despawn SCAVs",
                true,
                new ConfigDescription("When enabled, removes furthest SCAV bots from player for each new dynamic spawn bot that is over your Donuts bot caps (ScenarioConfig.json).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 15 }));

            HardCapEnabled = Config.Bind(
                "1. Main Settings",
                "Bot Hard Cap Option",
                false,
                new ConfigDescription("When enabled, all bot spawns will be hard capped by your preset caps. In other words, if your bot count is at the total Donuts cap then no more bots will spawn until one dies (vanilla SPT behavior).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 14 }));

            coolDownTimer = Config.Bind(
                "1. Main Settings",
                "Cool Down Timer",
                300f,
                new ConfigDescription("Cool Down Timer for after a spawn has successfully spawned a bot the spawn marker's MaxSpawnsBeforeCoolDown",
                new AcceptableValueRange<float>(0f, 1000f),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 13 }));

            pmcGroupChance = Config.Bind(
                "1. Main Settings",
                "Donuts PMC Group Chance",
                "Default",
                new ConfigDescription("Setting to determine the odds of PMC groups and group size. All odds are configurable, check Advanced Settings above. See mod page for more details.",
                new AcceptableValueList<string>(pmcGroupChanceList),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 12 }));

            scavGroupChance = Config.Bind(
                "1. Main Settings",
                "Donuts SCAV Group Chance",
                "Default",
                new ConfigDescription("Setting to determine the odds of SCAV groups and group size. All odds are configurable, check Advanced Settings above. See mod page for more details. See mod page for more details.",
                new AcceptableValueList<string>(scavGroupChanceList),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 11 }));

            botDifficultiesPMC = Config.Bind(
                "1. Main Settings",
                "Donuts PMC Spawn Difficulty",
                "Normal",
                new ConfigDescription("Difficulty Setting for All PMC Donuts Related Spawns",
                new AcceptableValueList<string>(botDiffList),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 10 }));

            botDifficultiesSCAV = Config.Bind(
                "1. Main Settings",
                "Donuts SCAV Spawn Difficulty",
                "Normal",
                new ConfigDescription("Difficulty Setting for All SCAV Donuts Related Spawns",
                new AcceptableValueList<string>(botDiffList),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 9 }));

            botDifficultiesOther = Config.Bind(
                "1. Main Settings",
                "Other Bot Type Spawn Difficulty",
                "Normal",
                new ConfigDescription("Difficulty Setting for all other bot types spawned with Donuts, such as bosses, Rogues, Raiders, etc.",
                new AcceptableValueList<string>(botDiffList),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 8 }));

            ShowRandomFolderChoice = Config.Bind(
                "1. Main Settings",
                "Show Random Scenario Selection",
                true,
                new ConfigDescription("Shows the Random Scenario Selected on Raid Start in bottom right",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            pmcFaction = Config.Bind(
                "2. Additional Spawn Settings",
                "Force PMC Faction",
                "Default",
                new ConfigDescription("Force a specific faction for all PMC spawns or use the default specified faction in the Donuts spawn files. Default is a random faction.",
                new AcceptableValueList<string>(pmcFactionList),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 12 }));

            forceAllBotType = Config.Bind(
                "2. Additional Spawn Settings",
                "Force Bot Type for All Spawns",
                "Disabled",
                new ConfigDescription("Force a specific faction for all PMC spawns or use the default specified faction in the Donuts spawn files. Default is a random faction.",
                new AcceptableValueList<string>(forceAllBotTypeList),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 11 }));

            hardStopOptionPMC = Config.Bind(
                "2. Additional Spawn Settings",
                "PMC Spawn Hard Stop",
                false,
                new ConfigDescription("If enabled, all PMC spawns stop completely once there is n time left in your raid. This is configurable in seconds (see below).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 10 }));

            hardStopTimePMC = Config.Bind(
                "2. Additional Spawn Settings",
                "PMC Spawn Hard Stop: Time Left in Raid",
                300,
                new ConfigDescription("The time (in seconds) left in your raid that will stop any further PMC spawns (if option is enabled). Default is 300 (5 minutes).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 9 }));

            hardStopOptionSCAV = Config.Bind(
                "2. Additional Spawn Settings",
                "SCAV Spawn Hard Stop",
                false,
                new ConfigDescription("If enabled, all SCAV spawns stop completely once there is n time left in your raid. This is configurable in seconds (see below).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 8 }));

            hardStopTimeSCAV = Config.Bind(
                "2. Additional Spawn Settings",
                "SCAV Spawn Hard Stop: Time Left in Raid",
                300,
                new ConfigDescription("The time (in seconds) left in your raid that will stop any further SCAV spawns (if option is enabled). Default is 300 (5 minutes).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 7 }));

            hotspotBoostPMC = Config.Bind(
                "2. Additional Spawn Settings",
                "PMC Hot Spot Spawn Boost",
                false,
                new ConfigDescription("If enabled, all hotspot points have a much higher chance of spawning more PMCs. (CAN BE TOGGLED MID-RAID)",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 6 }));

            hotspotBoostSCAV = Config.Bind(
                "2. Additional Spawn Settings",
                "SCAV Hot Spot Spawn Boost",
                false,
                new ConfigDescription("If enabled, all hotspot points have a much higher chance of spawning more SCAVs. (CAN BE TOGGLED MID-RAID)",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 5 }));

            hotspotIgnoreHardCapPMC = Config.Bind(
                "2. Additional Spawn Settings",
                "PMC Hot Spot Ignore Hard Cap",
                false,
                new ConfigDescription("If enabled, all hotspot spawn points will ignore the hard cap (if enabled). This applies to any spawn points labeled with 'Hotspot'. Strongly recommended to use this option + Despawn + Hardcap.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 4 }));

            hotspotIgnoreHardCapSCAV = Config.Bind(
                "2. Additional Spawn Settings",
                "SCAV Hot Spot Ignore Hard Cap",
                false,
                new ConfigDescription("If enabled, all hotspot spawn points will ignore the hard cap (if enabled). This applies to any spawn points labeled with 'Hotspot'. I recommended using this option with Despawn + Hardcap + Boost for the best experience with more action in hot spot areas.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 }));

            globalMinSpawnDistanceFromPlayerBool = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Use Global Min Distance From Player",
                false,
                new ConfigDescription("If enabled, all spawns on all presets will use the global minimum spawn distance from player for each map defined below.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            globalMinSpawnDistanceFromPlayerFactory = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Factory",
                35f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            globalMinSpawnDistanceFromPlayerCustoms = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Customs",
                85f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            globalMinSpawnDistanceFromPlayerReserve = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Reserve",
                80f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            globalMinSpawnDistanceFromPlayerStreets = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Streets",
                80f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            globalMinSpawnDistanceFromPlayerWoods = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Woods",
                150f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            globalMinSpawnDistanceFromPlayerLaboratory = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Laboratory",
                40f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            globalMinSpawnDistanceFromPlayerShoreline = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Shoreline",
                100f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            globalMinSpawnDistanceFromPlayerGroundZero = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Ground Zero",
                65f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            globalMinSpawnDistanceFromPlayerInterchange = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Interchange",
                80f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            globalMinSpawnDistanceFromPlayerLighthouse = Config.Bind(
                "3. Global Minimum Spawn Distance From Player",
                "Lighthouse",
                80f,
                new ConfigDescription("Distance (in meters) that bots should spawn away from the player (you).",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            // advanced settings
            maxSpawnTriesPerBot = Config.Bind(
                "4. Advanced Spawn Settings",
                "Max Spawn Tries Per Bot",
                20,
                new ConfigDescription("It will stop trying to spawn one of the bots after this many attempts to find a good spawn point",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 4 }));

            groupWeightDistroLow = Config.Bind(
                "5. Group Chance Weight Distribution",
                "Low",
                lowWeightsString,
                new ConfigDescription("Weight Distribution for Group Chance 'Low'. Use relative weights for group sizes 1/2/3/4/5, respectively. Use this formula: group weight / total weight = % chance.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));

            groupWeightDistroDefault = Config.Bind(
                "5. Group Chance Weight Distribution",
                "Default",
                defaultWeightsString,
                new ConfigDescription("Weight Distribution for Group Chance 'Default'. Use relative weights for group sizes 1/2/3/4/5, respectively. Use this formula: group weight / total weight = % chance.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));

            groupWeightDistroHigh = Config.Bind(
                "5. Group Chance Weight Distribution",
                "High",
                highWeightsString,
                new ConfigDescription("Weight Distribution for Group Chance 'High'. Use relative weights for group sizes 1/2/3/4/5, respectively. Use this formula: group weight / total weight = % chance.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 1 }));

            //Debugging
            DebugGizmos = Config.Bind(
                "6. Debugging",
                "Enable Debug Markers",
                false,
                new ConfigDescription("When enabled, draws debug spheres on set spawn from json",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));

            gizmoRealSize = Config.Bind(
                "6. Debugging",
                "Debug Sphere Real Size",
                false,
                new ConfigDescription("When enabled, debug spheres will be the real size of the spawn radius",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 1 }));

            // Spawn Point Maker
            spawnName = Config.Bind(
                "7. Spawn Point Maker",
                "Name",
                "Spawn Name Here",
                new ConfigDescription("Name used to identify the spawn marker",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 14 }));

            groupNum = Config.Bind(
                "7. Spawn Point Maker",
                "Group Number",
                1,
                new ConfigDescription("Group Number used to identify the spawn marker",
                new AcceptableValueList<int>(groupList),
                new ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false, Order = 13 }));

            wildSpawns = Config.Bind(
                "7. Spawn Point Maker",
                "Wild Spawn Type",
                "pmc",
                new ConfigDescription("Select an option.",
                new AcceptableValueList<string>(wildDropValues),
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 12 }));

            minSpawnDist = Config.Bind(
                "7. Spawn Point Maker",
                "Min Spawn Distance",
                1f,
                new ConfigDescription("Min Distance Bots will Spawn From Marker You Set.",
                new AcceptableValueRange<float>(0f, 500f),
                new ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false, Order = 11 }));

            maxSpawnDist = Config.Bind(
                "7. Spawn Point Maker",
                "Max Spawn Distance",
                20f,
                new ConfigDescription("Max Distance Bots will Spawn From Marker You Set.",
                new AcceptableValueRange<float>(1f, 1000f),
                new ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false, Order = 10 }));

            botTriggerDistance = Config.Bind(
                "7. Spawn Point Maker",
                "Bot Spawn Trigger Distance",
                100f,
                new ConfigDescription("Distance in which the player is away from the fight location point that it triggers bot spawn",
                new AcceptableValueRange<float>(0.1f, 1000f),
                new ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false, Order = 9 }));

            botTimerTrigger = Config.Bind(
                "7. Spawn Point Maker",
                "Bot Spawn Timer Trigger",
                180f,
                new ConfigDescription("In seconds before it spawns next wave while player in the fight zone area",
                new AcceptableValueRange<float>(0f, 10000f),
                new ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false, Order = 8 }));

            maxRandNumBots = Config.Bind(
                "7. Spawn Point Maker",
                "Max Random Bots",
                2,
                new ConfigDescription("Maximum number of bots of Wild Spawn Type that can spawn on this marker",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 7 }));

            spawnChance = Config.Bind(
                "7. Spawn Point Maker",
                "Spawn Chance for Marker",
                50,
                new ConfigDescription("Chance bot will be spawn here after timer is reached",
                new AcceptableValueRange<int>(0, 100),
                new ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false, Order = 6 }));

            maxSpawnsBeforeCooldown = Config.Bind(
                "7. Spawn Point Maker",
                "Max Spawns Before Cooldown",
                5,
                new ConfigDescription("Number of successful spawns before this marker goes in cooldown",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 5 }));

            ignoreTimerFirstSpawn = Config.Bind(
                "7. Spawn Point Maker",
                "Ignore Timer for First Spawn",
                false,
                new ConfigDescription("When enabled for this point, it will still spawn even if timer is not ready for first spawn only",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 4 }));

            minSpawnDistanceFromPlayer = Config.Bind(
                "7. Spawn Point Maker",
                "Min Spawn Distance From Player",
                40f,
                new ConfigDescription("How far the random selected spawn near the spawn marker needs to be from player",
                new AcceptableValueRange<float>(0f, 500f),
                new ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false, Order = 3 }));

            CreateSpawnMarkerKey = Config.Bind(
                "7. Spawn Point Maker",
                "Create Spawn Marker Key",
                new BepInEx.Configuration.KeyboardShortcut(),
                new ConfigDescription("Press this key to create a spawn marker at your current location",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));

            DeleteSpawnMarkerKey = Config.Bind(
                "7. Spawn Point Maker",
                "Delete Spawn Marker Key",
                new BepInEx.Configuration.KeyboardShortcut(),
                new ConfigDescription("Press this key to delete closest spawn marker within 5m of your player location",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 1 }));

            //Save Settings
            saveNewFileOnly = Config.Bind(
                "8. Save Settings",
                "Save New Locations Only",
                false,
                new ConfigDescription("If enabled saves the raid session changes to a new file. Disabled saves all locations you can see to a new file.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));

            WriteToFileKey = Config.Bind(
                "8. Save Settings",
                "Create Temp Json File",
                new BepInEx.Configuration.KeyboardShortcut(UnityEngine.KeyCode.KeypadMinus),
                new ConfigDescription("Press this key to write the json file with all entries so far",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 1 }));

            //Patches
            new NewGameDonutsPatch().Enable();
            new BotGroupAddEnemyPatch().Enable();
            new BotMemoryAddEnemyPatch().Enable();
            new MatchEndPlayerDisposePatch().Enable();
            new PatchStandbyTeleport().Enable();
            new BotProfilePreparationHook().Enable();
            new AddEnemyPatch().Enable();

            SetupScenariosUI();
        }

        private void SetupScenariosUI()
        {
            // populate the list of scenarios
            LoadDonutsFolders();

            List<string> scenarioValuesList = new List<string>(scenarioValues);
            // scenarioValuesList.Add("Random");

            // Add folder.Name to the scenarioValuesList
            foreach (Folder folder in scenarios)
            {
                Logger.LogWarning("Adding scenario: " + folder.Name);
                scenarioValuesList.Add(folder.Name);
            }

            foreach (Folder folder in randomScenarios)
            {
                Logger.LogWarning("Adding random scenario: " + folder.RandomScenarioConfig);
                scenarioValuesList.Add(folder.RandomScenarioConfig);
            }

            scenarioValues = scenarioValuesList.ToArray();

            scenarioSelection = Config.Bind(
                "1. Main Settings",
                "PMC Raid Preset Selection",
                "Live Like (Random)",
                new ConfigDescription("Select a preset to use when spawning as PMC",
                new AcceptableValueList<string>(scenarioValues),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            scavScenarioSelection = Config.Bind(
                "1. Main Settings",
                "SCAV Raid Preset Selection",
                "scav-raids",
                new ConfigDescription("Select a preset to use when spawning as SCAV",
                new AcceptableValueList<string>(scenarioValues),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));
        }

        private void Update()
        {
            if (IsKeyPressed(CreateSpawnMarkerKey.Value))
            {
                EditorFunctions.CreateSpawnMarker();
            }
            if (IsKeyPressed(WriteToFileKey.Value))
            {
                EditorFunctions.WriteToJsonFile();
            }
            if (IsKeyPressed(DeleteSpawnMarkerKey.Value))
            {
                EditorFunctions.DeleteSpawnMarker();
            }
        }

        internal void LoadDonutsFolders()
        {
            string dllPath = Assembly.GetExecutingAssembly().Location;
            string directoryPath = Path.GetDirectoryName(dllPath);

            string filePath = Path.Combine(directoryPath, "ScenarioConfig.json");

            Logger.LogWarning("Found file at: " + filePath);

            string file = File.ReadAllText(filePath);
            scenarios = JsonConvert.DeserializeObject<List<Folder>>(file);

            if (scenarios.Count == 0)
            {
                Logger.LogError("No Donuts Folders found in Scenario Config file, disabling plugin");
                Debug.Break();
            }

            Logger.LogDebug("Loaded " + scenarios.Count + " Donuts Scenario Folders");

            string randFilePath = Path.Combine(directoryPath, "RandomScenarioConfig.json");

            Logger.LogWarning("Found file at: " + randFilePath);

            string randFile = File.ReadAllText(randFilePath);
            randomScenarios = JsonConvert.DeserializeObject<List<Folder>>(randFile);
        }

        internal static Folder GrabDonutsFolder(string folderName)
        {
            return scenarios.FirstOrDefault(temp => temp.Name == folderName);
        }
        bool IsKeyPressed(KeyboardShortcut key)
        {
            if (!UnityInput.Current.GetKeyDown(key.MainKey)) return false;

            return key.Modifiers.All(modifier => UnityInput.Current.GetKey(modifier));
        }

    }

    //re-initializes each new game
    internal class NewGameDonutsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix() => DonutComponent.Enable();
    }











}
