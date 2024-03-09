using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using DrakiaXYZ.VersionChecker;
using ThatsLit.Components;
using ThatsLit.Helpers;
using System;
using UnityEngine;
using static ThatsLit.AssemblyInfo;
using ThatsLit.Patches.Vision;

namespace ThatsLit
{
    public static class AssemblyInfo
    {
        public const string Title = ModName;
        public const string Description = "One step closer to fair gameplay, by giving AIs non-perfect vision and reactions. Because we too deserve grasses, bushes and nights.";
        public const string Configuration = SPTVersion;
        public const string Company = "";
        public const string Product = ModName;
        public const string Copyright = "Copyright © 2023 BA";
        public const string Trademark = "";
        public const string Culture = "";

        public const int TarkovVersion = 26535;
        public const string EscapeFromTarkov = "EscapeFromTarkov.exe";
        public const string ModName = "That's Lit";
        public const string ModVersion = "1.373.5";

        public const string SPTGUID = "com.spt-aki.core";
        public const string SPTVersion = "3.7.1";
    }

    [BepInPlugin("bastudio.thatslit", ModName, ModVersion)]
    [BepInDependency(SPTGUID, SPTVersion)]
    [BepInProcess(EscapeFromTarkov)]
    public class ThatsLitPlugin : BaseUnityPlugin
    {

        private void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, base.Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            BindConfigs();
            Patches();
        }

        private void BindConfigs()
        {
            string category = "0. Readme";
            Config.Bind(category,
                        "Performance",
                        true,
                        new ConfigDescription("The mod takes away at least several fps. Actual overhead varies from machine to machine, some lose 5, some lose 20. You can try giving up the brightness module   if the performance is not acceptable.",
                                                         null,
                                                         new ConfigurationManagerAttributes() { ReadOnly = true }));
            Config.Bind(category,
                        "Balance",
                        true,
                        new ConfigDescription("The mod aims to make AIs reasonable without making it easy. However, SAIN or other mods can change bots, and everyone has different configurations, so you may have different experience than mine with default That's Lit configs. (Quite some people has bots too blind with That's Lit installed) If you have balance issue, consider raising SAIN difficulty (first) or tweaking scales below.",
                                                         null,
                                                         new ConfigurationManagerAttributes() { ReadOnly = true }));
            Config.Bind(category,
                        "Mechanics",
                        true,
                        new ConfigDescription("The mod tries to make everything as intuitive as possible so you can enjoy human-like AIs by just applying common sense. However, EFT's AIs are never designed to be human-like, the mod basically \"imagine up\" some new systems out of data here and there in the game, there are things can't be done, or can't be very accurate. It's best to read the mod description page if you want to make the most out of That's Lit.",
                                                         null,
                                                         new ConfigurationManagerAttributes() { ReadOnly = true }));

            category = "1. Main";
            EnabledMod = Config.Bind(category, "Enable", true, "Enable the mod. Most features can't be re-enabled in raids.");
            //ScoreOffset = Config.Bind(category, "Score Offset", 0f, "Modify the score ranging from -1 to 1, which reflect how much the player is lit. Starting from -0.4 a

            category = "2. Darkness / Brightness";
            EnabledLighting            = Config.Bind(category, "Enable", true, new ConfigDescription("Enable the module. With this turned off, AIs are not affected by your brightness.", null, new ConfigurationManagerAttributes() { Order                                                              = 100 }));
            DarknessImpactScale        = Config.Bind(category, "Darkness Impact Scale", 1f, new ConfigDescription("Scale how AI noticing players slower due to darkness.", new AcceptableValueRange<float>(0, 1f), new ConfigurationManagerAttributes() { Order                                           = 95 }));
            BrightnessImpactScale      = Config.Bind(category, "Brightness Impact Scale", 1f, new ConfigDescription("Scale how AI noticing players faster due to brightness.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes() { Order                                       = 94 }));
            LitVisionDistanceScale     = Config.Bind(category, "Lit Vision Distance Scale", 1f, new ConfigDescription("Scale how AI noticing players from further under some circumstances. This is designed to compensate low night vision distance from SAIN, you may want to set this to 0 if you don't run SAIN.", new AcceptableValueRange<float>(0, 1f), new ConfigurationManagerAttributes() { Order = 93 }));
            EnableFactoryNight         = Config.Bind(category, "Factory (Night)", true, "Enable darkness/brightness on the map.");
            EnableLighthouse           = Config.Bind(category, "Lighthouse", true, "Enable darkness/brightness on the map.");
            EnableShoreline            = Config.Bind(category, "Shoreline", true, "Enable darkness/brightness on the map.");
            EnableReserve              = Config.Bind(category, "Reserve", true, "Enable darkness/brightness on the map.");
            EnableWoods                = Config.Bind(category, "Woods", true, "Enable darkness/brightness on the map.");
            EnableInterchange          = Config.Bind(category, "Interchange", true, "Enable darkness/brightness on the map.");
            EnableCustoms              = Config.Bind(category, "Customs", true, "Enable darkness/brightness on the map.");
            EnableStreets              = Config.Bind(category, "Streets", true, "Enable darkness/brightness on the map.");

            category                   = "3. Encountering Patch";
            EnabledEncountering        = Config.Bind(category,
                                                     "Enable",
                                                     true,
                                                     new ConfigDescription("Enable the module. This randomly nerf AIs a bit at the moment they encounter you, especially when they are sprinting.", null, new ConfigurationManagerAttributes() { Order = 100 }));
            VagueHintChance            = Config.Bind(category,
                                                     "Vague Hint Chance",
                                                     0.6f,
                                                     new ConfigDescription("The chance to cancel a bot's visual confirmation on you and instead only tell it it's spotted from roughly your direction, when it's not facing your way.", new AcceptableValueRange<float>(0, 1f), new ConfigurationManagerAttributes() { Order = 99 }));

            category                   = "4. Grasses";
            EnabledGrasses             = Config.Bind(category, "Enable", true, new ConfigDescription("Enable the module. This enable grasses to block bot vision.", null, new ConfigurationManagerAttributes() { Order                                                                                    = 100 }));

            category                   = "5. Tweaks";
            GlobalRandomOverlookChance = Config.Bind(category,
                                                     "Global Random Overlook Chance",
                                                     0.01f,
                                                     new ConfigDescription("The chance for all AIs to simply overlook in 1 vision check.", new AcceptableValueRange<float>(0, 1f), new ConfigurationManagerAttributes() { Order = 100 }));
            FoliageImpactScale         = Config.Bind(category,
                                                     "Foliage Impact Scale",
                                                     1f,
                                                     new ConfigDescription("Scale the strength of extra chance to be overlooked from sneaking around foliages.", new AcceptableValueRange<float>(0, 1f), new ConfigurationManagerAttributes() { Order = 99 }));
            FinalImpactScale         = Config.Bind(category,
                                                     "Final Impact Scale",
                                                     1f,
                                                     new ConfigDescription("Scale the buff/nerf to bots from the mod. 0% = use the original value. Adjust this to balance your game to your liking. This is mainly provided for people whose game somehow becomes too easy with the mod.", new AcceptableValueRange<float>(0, 1f), new ConfigurationManagerAttributes() { Order = 98}));
            FinalOffset                = Config.Bind(category, "Final Offset", 0f, "(Not recommanded because it's easy to mess up the balance, try Final Impact Scale first) Modify the final 'time to be seen' seconds. Positive means AIs react slower and vice versa. Applied after Final Impact Scale.");
            IncludeBosses              = Config.Bind(category, "Include Bosses", false, "Should all features from this mod work for boss.");

            category                   = "6. Info";
            ScoreInfo                  = Config.Bind(category, "Lighting Info", true, "Shown at the upper left corner.");
            FoliageInfo                  = Config.Bind(category, "Foliage Info", true, "Gives a hint about surrounding foliage.");
            TerrainInfo                  = Config.Bind(category, "Terrain Info", true, "Gives a hint about surrounding grasses. Only grasses in direction to the bot doing vision check is applied and there are some more dynamic factors, so this only gives you the rough idea about how dense the surrounding grasses are.");
            HideMapTip                  = Config.Bind(category, "Hide Map Tip", false, "Hide the reminder about disabled lit detection.");

            category                   = "7. Performance";
            LessFoliageCheck           = Config.Bind(category, "Less Foliage Check", false, "Check surrounding foliage a bit less frequent. May or may not help with CPU usage but slower to update surrounding foliages.");
            LessEquipmentCheck         = Config.Bind(category, "Less Equipment Check", false, "Check equipment lights a bit less frequent. May or may not help with CPU usage but slower to update impact from turning on/off lights/lasers.");
            ResLevel                 = Config.Bind(category, "Resolustion Level", 2,
                                                   new ConfigDescription("Resolution of the observed image by the observer camera, higher level means somewhat higher accuracy. Has an impact on CPU time. Level1 -> 32x32, Level2 -> 64x64... This config is used on raid start.", new AcceptableValueRange<int>(1, 4)));

            category                   = "8. Debug";
            DebugInfo                  = Config.Bind(category, "Debug Info", false, "A lot of gibberish.");
            DebugTexture               = Config.Bind(category, "Debug Texture", false, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced                                                                        = true }));
            EnableHideout              = Config.Bind(category, "Hideout", false, "Enable darkness/brightness on the map.");
            EnableBenchmark              = Config.Bind(category, "Benchmark", false, "");
            // DevMode = Config.Bind(category, "Dev Mode", false, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // DevModeInvisible = Config.Bind(category, "Dev Mode Invisible", false, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // NoGPUReq = Config.Bind(category, "NoGPUReq", false, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideMinBaseAmbienceScore = Config.Bind(category, "MinBaseAmbienceScore", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideMaxBaseAmbienceScore = Config.Bind(category, "MaxBaseAmbienceScore", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideMinAmbienceLum = Config.Bind(category, "MinAmbienceLum", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideMaxAmbienceLum = Config.Bind(category, "MaxAmbienceLum", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverridePixelLumScoreScale = Config.Bind(category, "PixelLumScoreScale", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideMaxSunLightScore = Config.Bind(category, "MaxSunLightScore", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideMaxMoonLightScore = Config.Bind(category, "MaxMoonLightScore", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideScore0 = Config.Bind(category, "Score0", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideScore1 = Config.Bind(category, "Score1", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideScore2 = Config.Bind(category, "Score2", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideScore3 = Config.Bind(category, "Score3", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideScore4 = Config.Bind(category, "Score4", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideScore5 = Config.Bind(category, "Score5", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideScore6 = Config.Bind(category, "Score6", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideThreshold0 = Config.Bind(category, "Threshold0", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideThreshold1 = Config.Bind(category, "Threshold1", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideThreshold2 = Config.Bind(category, "Threshold2", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideThreshold3 = Config.Bind(category, "Threshold3", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideThreshold4 = Config.Bind(category, "Threshold4", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            // OverrideThreshold5 = Config.Bind(category, "Threshold5", 0f, new ConfigDescription("", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
        }

        public static ConfigEntry<bool> ScoreInfo { get; private set; }
        public static ConfigEntry<bool> TerrainInfo { get; private set; }
        public static ConfigEntry<bool> FoliageInfo { get; private set; }
        public static ConfigEntry<bool> DebugInfo { get; private set; }
        public static ConfigEntry<bool> HideMapTip { get; private set; }
        public static ConfigEntry<bool> DebugTexture { get; private set; }
        public static ConfigEntry<bool> EnabledMod { get; private set; }
        public static ConfigEntry<bool> EnabledLighting { get; private set; }
        public static ConfigEntry<bool> EnabledEncountering { get; private set; }
        public static ConfigEntry<bool> EnabledGrasses { get; private set; }
        public static ConfigEntry<float> ScoreOffset { get; private set; }
        public static ConfigEntry<float> DarknessImpactScale { get; private set; }
        public static ConfigEntry<float> BrightnessImpactScale { get; private set; }
        public static ConfigEntry<float> LitVisionDistanceScale { get; private set; }
        public static ConfigEntry<float> FinalOffset { get; private set; }
        public static ConfigEntry<float> FinalImpactScale { get; private set; }
        public static ConfigEntry<float> VagueHintChance { get; private set; }
        public static ConfigEntry<float> GlobalRandomOverlookChance { get; private set; }
        public static ConfigEntry<float> FoliageImpactScale { get; private set; }
        public static ConfigEntry<bool> IncludeBosses { get; private set; }
        public static ConfigEntry<bool> LessFoliageCheck { get; private set; }
        public static ConfigEntry<bool> LessEquipmentCheck { get; private set; }
        public static ConfigEntry<bool> EnableLighthouse { get; private set; }
        public static ConfigEntry<bool> EnableFactoryNight { get; private set; }
        public static ConfigEntry<bool> EnableReserve { get; private set; }
        public static ConfigEntry<bool> EnableCustoms { get; private set; }
        public static ConfigEntry<bool> EnableShoreline { get; private set; }
        public static ConfigEntry<bool> EnableInterchange { get; private set; }
        public static ConfigEntry<bool> EnableStreets { get; private set; }
        public static ConfigEntry<bool> EnableWoods { get; private set; }
        public static ConfigEntry<bool> EnableHideout { get; private set; }
        public static ConfigEntry<bool> EnableBenchmark { get; private set; }
        public static ConfigEntry<int> ResLevel { get; private set; }
        // public static ConfigEntry<bool> DevMode { get; private set; }
        // public static ConfigEntry<bool> DevModeInvisible { get; private set; }
        // public static ConfigEntry<bool> NoGPUReq { get; private set; }
        // public static ConfigEntry<float> OverrideMinBaseAmbienceScore { get; private set; }
        // public static ConfigEntry<float> OverrideMaxBaseAmbienceScore { get; private set; }
        // public static ConfigEntry<float> OverrideMinAmbienceLum { get; private set; }
        // public static ConfigEntry<float> OverrideMaxAmbienceLum { get; private set; }
        // public static ConfigEntry<float> OverridePixelLumScoreScale { get; private set; }
        // public static ConfigEntry<float> OverrideMaxSunLightScore { get; private set; }
        // public static ConfigEntry<float> OverrideMaxMoonLightScore { get; private set; }
        // public static ConfigEntry<float> OverrideScore0 { get; private set; }
        // public static ConfigEntry<float> OverrideScore1 { get; private set; }
        // public static ConfigEntry<float> OverrideScore2 { get; private set; }
        // public static ConfigEntry<float> OverrideScore3 { get; private set; }
        // public static ConfigEntry<float> OverrideScore4 { get; private set; }
        // public static ConfigEntry<float> OverrideScore5 { get; private set; }
        // public static ConfigEntry<float> OverrideScore6 { get; private set; }
        // public static ConfigEntry<float> OverrideThreshold0 { get; private set; }
        // public static ConfigEntry<float> OverrideThreshold1 { get; private set; }
        // public static ConfigEntry<float> OverrideThreshold2 { get; private set; }
        // public static ConfigEntry<float> OverrideThreshold3 { get; private set; }
        // public static ConfigEntry<float> OverrideThreshold4 { get; private set; }
        // public static ConfigEntry<float> OverrideThreshold5 { get; private set; }

        private void Patches()
        {
            new SeenCoefPatch().Enable();
            new EncounteringPatch().Enable();
            new ExtraVisibleDistancePatch().Enable();
            // new SoundOverlapPatch().Enable();
        }

        private void Update()
        {
            GameWorldHandler.Update();
        }
    }
}