using BepInEx;
using BepInEx.Configuration;
using EFT;
using EFT.InventoryLogic;
using System.Reflection;
using UnityEngine;
using TMPro;
using HarmonyLib;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using EFT.UI;
using SIT.Tarkov.Core;
using SIT.Core.Misc;
using SIT.Core.Coop;

namespace AmandsHitmarker
{
    [BepInPlugin("com.Amanda.Hitmarker", "Hitmarker", "2.5.4")]
    public class AHitmarkerPlugin : BaseUnityPlugin
    {
        public static GameObject Hook;
        public static AmandsHitmarkerClass AmandsHitmarkerClassComponent;
        public static Player PlayerProceedDamageThroughArmor;

        public static ConfigEntry<bool> EnableHitmarker { get; set; }
        public static ConfigEntry<EArmorHitmarker> EnableArmorHitmarker { get; set; }
        public static ConfigEntry<bool> EnableBleeding { get; set; }
        public static ConfigEntry<EHitmarkerPositionMode> HitmarkerPositionMode { get; set; }
        public static ConfigEntry<EHitmarkerPositionMode> ADSHitmarkerPositionMode { get; set; }
        public static ConfigEntry<Vector2> Thickness { get; set; }
        public static ConfigEntry<float> CenterOffset { get; set; }
        public static ConfigEntry<Vector3> ArmorOffset { get; set; }
        public static ConfigEntry<Vector2> ArmorSizeDelta { get; set; }
        public static ConfigEntry<float> AnimatedTime { get; set; }
        public static ConfigEntry<float> AnimatedAlphaTime { get; set; }
        public static ConfigEntry<float> AnimatedAmplitude { get; set; }
        public static ConfigEntry<string> Shape { get; set; }
        public static ConfigEntry<string> HeadshotShape { get; set; }
        public static ConfigEntry<string> ArmorShape { get; set; }
        public static ConfigEntry<string> ArmorBreakShape { get; set; }
        public static ConfigEntry<Vector2> BleedSize { get; set; }

        public static ConfigEntry<bool> EnableSounds { get; set; }
        public static ConfigEntry<float> SoundVolume { get; set; }
        public static ConfigEntry<string> HitmarkerSound { get; set; }
        public static ConfigEntry<string> HeadshotHitmarkerSound { get; set; }
        public static ConfigEntry<string> KillHitmarkerSound { get; set; }
        public static ConfigEntry<string> ArmorSound { get; set; }
        public static ConfigEntry<string> ArmorBreakSound { get; set; }
        public static ConfigEntry<float> StartDistance { get; set; }
        public static ConfigEntry<float> EndDistance { get; set; }

        public static ConfigEntry<Color> HitmarkerColor { get; set; }
        public static ConfigEntry<Color> ArmorColor { get; set; }
        public static ConfigEntry<Color> BearColor { get; set; }
        public static ConfigEntry<Color> UsecColor { get; set; }
        public static ConfigEntry<Color> ScavColor { get; set; }
        public static ConfigEntry<Color> ThrowWeaponColor { get; set; }
        public static ConfigEntry<Color> FollowerColor { get; set; }
        public static ConfigEntry<Color> BossColor { get; set; }
        public static ConfigEntry<Color> RaiderColor { get; set; }
        public static ConfigEntry<Color> BloodhoundColor { get; set; }
        public static ConfigEntry<Color> BleedColor { get; set; }
        public static ConfigEntry<Color> PoisonColor { get; set; }

        public static ConfigEntry<string> HitmarkerButton { get; set; }
        public static ConfigEntry<string> HeadshotHitmarkerButton { get; set; }
        public static ConfigEntry<string> BleedHitmarkerButton { get; set; }
        public static ConfigEntry<string> PoisonHitmarkerButton { get; set; }
        public static ConfigEntry<string> ArmorHitmarkerButton { get; set; }
        public static ConfigEntry<string> ArmorBreakHitmarkerButton { get; set; }
        public static ConfigEntry<string> UsecHitmarkerButton { get; set; }
        public static ConfigEntry<string> BearHitmarkerButton { get; set; }
        public static ConfigEntry<string> ScavHitmarkerButton { get; set; }
        public static ConfigEntry<string> ThrowWeaponHitmarkerButton { get; set; }
        public static ConfigEntry<string> FollowerHitmarkerButton { get; set; }
        public static ConfigEntry<string> BossHitmarkerButton { get; set; }
        public static ConfigEntry<string> RaiderHitmarkerButton { get; set; }
        public static ConfigEntry<string> DamageNumberButton { get; set; }
        public static ConfigEntry<string> HitmarkerSoundButton { get; set; }
        public static ConfigEntry<string> HeadshotHitmarkerSoundButton { get; set; }
        public static ConfigEntry<string> ArmorSoundButton { get; set; }
        public static ConfigEntry<string> ArmorBreakSoundButton { get; set; }
        public static ConfigEntry<string> KillHitmarkerSoundButton { get; set; }
        public static ConfigEntry<string> ReloadFilesButton { get; set; }
        public static ConfigEntry<string> ReloadUIButton { get; set; }

        public static ConfigEntry<bool> StaticHitmarkerOnly { get; set; }
        public static ConfigEntry<Vector2> StaticSizeDelta { get; set; }
        public static ConfigEntry<float> StaticSizeDeltaSpeed { get; set; }
        public static ConfigEntry<float> StaticOpacity { get; set; }

        public static ConfigEntry<bool> EnableKillfeed { get; set; }
        public static ConfigEntry<Color> KillTextColor { get; set; }
        public static ConfigEntry<int> KillFontSize { get; set; }
        public static ConfigEntry<int> KillChildSpacing { get; set; }
        public static ConfigEntry<EKillPreset> KillPreset { get; set; }
        public static ConfigEntry<bool> KillChildDirection { get; set; }
        public static ConfigEntry<Vector2> KillPosition { get; set; }
        public static ConfigEntry<TextAlignmentOptions> KillTextAlignment { get; set; }
        public static ConfigEntry<float> KillFontOutline { get; set; }
        public static ConfigEntry<bool> KillFontUpperCase { get; set; }
        public static ConfigEntry<float> KillTime { get; set; }
        public static ConfigEntry<float> KillOpacitySpeed { get; set; }
        public static ConfigEntry<bool> KillUpperText { get; set; }
        public static ConfigEntry<EHeadshotXP> KillHeadshotXP { get; set; }
        public static ConfigEntry<EKillStart> KillStart { get; set; }
        public static ConfigEntry<EKillNameColor> KillNameColor { get; set; }
        public static ConfigEntry<Color> KillNameSingleColor { get; set; }
        public static ConfigEntry<EKillEnd> KillEnd { get; set; }
        public static ConfigEntry<bool> KillStreakXP { get; set; }
        public static ConfigEntry<int> KillDistanceThreshold { get; set; }

        public static ConfigEntry<bool> EnableMultiKillfeed { get; set; }
        public static ConfigEntry<EMultiKillfeedPMCMode> MultiKillfeedPMCIconMode { get; set; }
        public static ConfigEntry<EMultiKillfeedColorMode> MultiKillfeedColorMode { get; set; }
        public static ConfigEntry<Color> MultiKillfeedColor { get; set; }
        public static ConfigEntry<Color> MultiKillfeedHeadshotColor { get; set; }
        public static ConfigEntry<Vector2> MultiKillfeedSize { get; set; }
        public static ConfigEntry<string> MultiKillfeedUsecShape { get; set; }
        public static ConfigEntry<string> MultiKillfeedBearShape { get; set; }
        public static ConfigEntry<string> MultiKillfeedGenericShape { get; set; }
        public static ConfigEntry<int> MultiKillfeedChildSpacing { get; set; }
        public static ConfigEntry<Vector2> MultiKillfeedRectPosition { get; set; }
        public static ConfigEntry<Vector2> MultiKillfeedRectPivot { get; set; }

        public static ConfigEntry<bool> EnableRaidKillfeed { get; set; }
        public static ConfigEntry<bool> RaidKillRole { get; set; }
        public static ConfigEntry<EKillNameColor> RaidKillNameColor { get; set; }
        public static ConfigEntry<float> RaidKillTime { get; set; }
        public static ConfigEntry<int> RaidKillDistance { get; set; }
        public static ConfigEntry<int> RaidKillFontSize { get; set; }
        public static ConfigEntry<float> RaidKillFontOutline { get; set; }
        public static ConfigEntry<int> RaidKillChildSpacing { get; set; }
        public static ConfigEntry<ERaidKillPreset> RaidKillPreset { get; set; }
        public static ConfigEntry<bool> RaidKillChildDirection { get; set; }
        public static ConfigEntry<Vector2> RaidKillPosition { get; set; }
        public static ConfigEntry<TextAlignmentOptions> RaidKillTextAlignment { get; set; }

        public static ConfigEntry<bool> EnableDamageNumber { get; set; }
        public static ConfigEntry<bool> EnableArmorDamageNumber { get; set; }
        public static ConfigEntry<float> DamageAnimationTime { get; set; }
        public static ConfigEntry<EDamageNumberPositionMode> DamagePositionMode { get; set; }
        public static ConfigEntry<int> DamageFontSize { get; set; }
        public static ConfigEntry<float> DamageFontOutline { get; set; }
        public static ConfigEntry<Vector2> DamageRectPosition { get; set; }
        public static ConfigEntry<Vector2> DamageRectPivot { get; set; }
        private void Awake()
        {
            Debug.LogError("AmandsHitmarker Awake()");
            Hook = new GameObject();
            AmandsHitmarkerClassComponent = Hook.AddComponent<AmandsHitmarkerClass>();
            AmandsHitmarkerClass.HitmarkerAudioSource = Hook.AddComponent<AudioSource>();
            DontDestroyOnLoad(Hook);
        }
        private void Start()
        {
            EnableHitmarker = Config.Bind<bool>("AmandsHitmarker", "EnableHitmarker", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 420 }));
            EnableArmorHitmarker = Config.Bind<EArmorHitmarker>("AmandsHitmarker", "EnableArmorHitmarker", EArmorHitmarker.BreakingOnly, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 414 }));
            EnableBleeding = Config.Bind<bool>("AmandsHitmarker", "EnableBleeding", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 410 }));

            HitmarkerPositionMode = Config.Bind<EHitmarkerPositionMode>("AmandsHitmarker", "Position Mode", EHitmarkerPositionMode.ImpactPoint, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 320 }));
            ADSHitmarkerPositionMode = Config.Bind<EHitmarkerPositionMode>("AmandsHitmarker", "ADS Position Mode", EHitmarkerPositionMode.GunDirection, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 310 }));

            Thickness = Config.Bind<Vector2>("AmandsHitmarker", "Thickness", new Vector2(40.0f, 40.0f), new ConfigDescription("Individual image size", null, new ConfigurationManagerAttributes { Order = 210 }));
            CenterOffset = Config.Bind<float>("AmandsHitmarker", "CenterOffset", 15.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 200 }));
            ArmorOffset = Config.Bind<Vector3>("AmandsHitmarker", "ArmorOffset", new Vector3(70.0f, 0.0f, 0.0f), new ConfigDescription("Armor shape offset", null, new ConfigurationManagerAttributes { Order = 198, IsAdvanced = true }));
            ArmorSizeDelta = Config.Bind<Vector2>("AmandsHitmarker", "ArmorSizeDelta", new Vector2(64.0f, 64.0f), new ConfigDescription("Armor shape size delta", null, new ConfigurationManagerAttributes { Order = 196, IsAdvanced = true }));
            AnimatedTime = Config.Bind<float>("AmandsHitmarker", "AnimatedTime", 0.25f, new ConfigDescription("Animation time duration", new AcceptableValueRange<float>(0.01f, 1.0f), new ConfigurationManagerAttributes { Order = 190 }));
            AnimatedAlphaTime = Config.Bind<float>("AmandsHitmarker", "AnimatedAlphaTime", 0.25f, new ConfigDescription("Alpha animation time duration", new AcceptableValueRange<float>(0.01f, 1.0f), new ConfigurationManagerAttributes { Order = 180 }));
            AnimatedAmplitude = Config.Bind<float>("AmandsHitmarker", "AnimatedAmplitude", 2.5f, new ConfigDescription("Animation size amplitude", new AcceptableValueRange<float>(1.0f, 10.0f), new ConfigurationManagerAttributes { Order = 178 }));
            Shape = Config.Bind<string>("AmandsHitmarker", "Shape", "Hitmarker.png", new ConfigDescription("Supported File PNG", null, new ConfigurationManagerAttributes { Order = 170 }));
            HeadshotShape = Config.Bind<string>("AmandsHitmarker", "HeadshotShape", "HeadshotHitmarker.png", new ConfigDescription("Supported File PNG", null, new ConfigurationManagerAttributes { Order = 160 }));
            ArmorShape = Config.Bind<string>("AmandsHitmarker", "ArmorShape", "Armor.png", new ConfigDescription("Supported File PNG", null, new ConfigurationManagerAttributes { Order = 158 }));
            ArmorBreakShape = Config.Bind<string>("AmandsHitmarker", "ArmorBreakShape", "ArmorBreak.png", new ConfigDescription("Supported File PNG", null, new ConfigurationManagerAttributes { Order = 154 }));
            BleedSize = Config.Bind<Vector2>("AmandsHitmarker", "BleedSize", new Vector2(128.0f, 128.0f), new ConfigDescription("Bleed kill glow image size", null, new ConfigurationManagerAttributes { Order = 150 }));
            EnableSounds = Config.Bind<bool>("AmandsHitmarker", "EnableSounds", true, new ConfigDescription("Supported Files WAV OGG", null, new ConfigurationManagerAttributes { Order = 140 }));
            SoundVolume = Config.Bind<float>("AmandsHitmarker", "SoundVolume", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f,4.0f), new ConfigurationManagerAttributes { Order = 136 }));
            HitmarkerSound = Config.Bind<string>("AmandsHitmarker", "HitmarkerSound", "Hitmarker.wav", new ConfigDescription("Supported Files WAV OGG", null, new ConfigurationManagerAttributes { Order = 130 }));
            HeadshotHitmarkerSound = Config.Bind<string>("AmandsHitmarker", "HeadshotHitmarkerSound", "HeadshotHitmarker.wav", new ConfigDescription("Supported Files WAV OGG", null, new ConfigurationManagerAttributes { Order = 120 }));
            KillHitmarkerSound = Config.Bind<string>("AmandsHitmarker", "KillHitmarkerSound", "KillHitmarker.wav", new ConfigDescription("Supported Files WAV OGG", null, new ConfigurationManagerAttributes { Order = 110 }));
            ArmorSound = Config.Bind<string>("AmandsHitmarker", "ArmorSound", "Armor.wav", new ConfigDescription("Supported Files WAV OGG", null, new ConfigurationManagerAttributes { Order = 106 }));
            ArmorBreakSound = Config.Bind<string>("AmandsHitmarker", "ArmorBreakSound", "ArmorBreak.wav", new ConfigDescription("Supported Files WAV OGG", null, new ConfigurationManagerAttributes { Order = 102 }));
            StartDistance = Config.Bind<float>("AmandsHitmarker", "StartDistance", -1.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));
            EndDistance = Config.Bind<float>("AmandsHitmarker", "EndDIstance", 10000000.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 98, IsAdvanced = true }));

            HitmarkerColor = Config.Bind<Color>("Colors", "HitmarkerColor", new Color(0.84f, 0.88f, 0.95f, 1f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180 }));
            ArmorColor = Config.Bind<Color>("Colors", "ArmorColor", new Color(0.2826087f, 0.6086956f, 1.0f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));
            BearColor = Config.Bind<Color>("Colors", "BearColor", new Color(1.0f, 0.0f, 0.0f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160 }));
            UsecColor = Config.Bind<Color>("Colors", "UsecColor", new Color(1.0f, 0.0f, 0.1f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));
            ScavColor = Config.Bind<Color>("Colors", "ScavColor", new Color(1.0f, 0.3043478f, 0.0f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140 }));
            ThrowWeaponColor = Config.Bind<Color>("Colors", "ThrowWeaponColor", new Color(0.4130435f, 0.0f, 1.0f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130 }));
            FollowerColor = Config.Bind<Color>("Colors", "FollowerColor", new Color(1.0f, 0.8043478f, 0.0f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 124 }));
            BossColor = Config.Bind<Color>("Colors", "BossColor", new Color(1.0f, 0.8043478f, 0.0f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120 }));
            RaiderColor = Config.Bind<Color>("Colors", "RaiderColor", new Color(1.0f, 0.8043478f, 0.0f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 118 }));
            BloodhoundColor = Config.Bind<Color>("Colors", "BloodhoundColor", new Color(1.0f, 0.0f, 0.0f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 116 }));
            BleedColor = Config.Bind<Color>("Colors", "BleedColor", new Color(1.0f, 0.0f, 0.0f, 0.69f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));
            PoisonColor = Config.Bind<Color>("Colors", "PoisonColor", new Color(0.0f, 1.0f, 0.0f, 0.69f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 106, IsAdvanced = true }));

            HitmarkerButton= Config.Bind<string>("Debug", "HitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 280, HideSettingName = true, CustomDrawer = HitmarkerButtonDrawer }));
            HeadshotHitmarkerButton = Config.Bind<string>("Debug", "HeadshotHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 270, HideSettingName = true, CustomDrawer = HeadshotHitmarkerButtonDrawer }));
            BleedHitmarkerButton = Config.Bind<string>("Debug", "BleedHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 260, HideSettingName = true, CustomDrawer = BleedHitmarkerButtonDrawer }));
            PoisonHitmarkerButton = Config.Bind<string>("Debug", "PoisonHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 250, HideSettingName = true, CustomDrawer = PoisonHitmarkerButtonDrawer }));
            ArmorHitmarkerButton = Config.Bind<string>("Debug", "ArmorHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 240, HideSettingName = true, CustomDrawer = ArmorHitmarkerButtonDrawer }));
            ArmorBreakHitmarkerButton = Config.Bind<string>("Debug", "ArmorBreakHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 230, HideSettingName = true, CustomDrawer = ArmorBreakHitmarkerButtonDrawer }));
            BearHitmarkerButton = Config.Bind<string>("Debug", "BearHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 220, HideSettingName = true, CustomDrawer = BearHitmarkerButtonDrawer }));
            UsecHitmarkerButton = Config.Bind<string>("Debug", "UsecHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 210, HideSettingName = true, CustomDrawer = UsecHitmarkerButtonDrawer }));
            ScavHitmarkerButton = Config.Bind<string>("Debug", "ScavHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 200, HideSettingName = true, CustomDrawer = ScavHitmarkerButtonDrawer }));
            ThrowWeaponHitmarkerButton = Config.Bind<string>("Debug", "ThrowWeaponHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 190, HideSettingName = true, CustomDrawer = ThrowWeaponHitmarkerButtonDrawer }));
            FollowerHitmarkerButton = Config.Bind<string>("Debug", "FollowerHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180, HideSettingName = true, CustomDrawer = FollowerHitmarkerButtonDrawer }));
            BossHitmarkerButton = Config.Bind<string>("Debug", "BossHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170, HideSettingName = true, CustomDrawer = BossHitmarkerButtonDrawer }));
            RaiderHitmarkerButton = Config.Bind<string>("Debug", "RaiderHitmarkerButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170, HideSettingName = true, CustomDrawer = RaiderHitmarkerButtonDrawer }));
            DamageNumberButton = Config.Bind<string>("Debug", "DamageNumberButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 168, HideSettingName = true, CustomDrawer = DamageNumberDrawer }));
            HitmarkerSoundButton = Config.Bind<string>("Debug", "HitmarkerSoundButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160, HideSettingName = true, CustomDrawer = HitmarkerSoundButtonDrawer }));
            HeadshotHitmarkerSoundButton = Config.Bind<string>("Debug", "HeadshotHitmarkerSoundButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150, HideSettingName = true, CustomDrawer = HeadshotHitmarkerSoundButtonDrawer }));
            ArmorSoundButton = Config.Bind<string>("Debug", "ArmorSoundButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140, HideSettingName = true, CustomDrawer = ArmorSoundButtonDrawer }));
            ArmorBreakSoundButton = Config.Bind<string>("Debug", "ArmorBreakSoundButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130, HideSettingName = true, CustomDrawer = ArmorBreakSoundButtonDrawer }));
            KillHitmarkerSoundButton = Config.Bind<string>("Debug", "KillHitmarkerSoundButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, HideSettingName = true, CustomDrawer = KillHitmarkerSoundButtonDrawer }));
            ReloadFilesButton = Config.Bind<string>("Debug", "ReloadFilesButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, HideSettingName = true, CustomDrawer = ReloadFilesButtonDrawer }));
            ReloadUIButton = Config.Bind<string>("Debug", "ReloadUIButton", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, HideSettingName = true, CustomDrawer = ReloadUIButtonDrawer }));

            StaticHitmarkerOnly = Config.Bind<bool>("Static", "StaticHitmarkerOnly", false, new ConfigDescription("Use only the static hitmarker image", null, new ConfigurationManagerAttributes { Order = 140, IsAdvanced = true }));
            StaticSizeDelta = Config.Bind<Vector2>("Static", "StaticSizeDelta", new Vector2(200.0f, 200.0f), new ConfigDescription("Static hitmarker size", null, new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true }));
            StaticSizeDeltaSpeed = Config.Bind<float>("Static", "StaticSizeDeltaSpeed", 0.01f, new ConfigDescription("Animation size increase", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));
            StaticOpacity = Config.Bind<float>("Static", "StaticOpacity", 0.25f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true }));

            EnableKillfeed = Config.Bind<bool>("AmandsKillfeed", "EnableKillfeed", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 290 }));
            KillTextColor = Config.Bind<Color>("AmandsKillfeed", "TextColor", new Color(0.84f, 0.88f, 0.95f, 1f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 280, IsAdvanced = true }));
            KillFontSize = Config.Bind<int>("AmandsKillfeed", "FontSize", 26, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 270 }));
            KillFontOutline = Config.Bind<float>("AmandsKillfeed", "FontOutline", 0.01f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 260, IsAdvanced = true }));
            KillFontUpperCase = Config.Bind<bool>("AmandsKillfeed", "FontUpperCase", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150, IsAdvanced = true }));
            KillChildSpacing = Config.Bind<int>("AmandsKillfeed", "TextSpacing", -26, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 240 }));
            KillPreset = Config.Bind<EKillPreset>("AmandsKillfeed", "Preset", EKillPreset.Center, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 230 }));
            KillPosition = Config.Bind<Vector2>("AmandsKillfeed", "Position", new Vector2(0f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 220 }));
            KillChildDirection = Config.Bind<bool>("AmandsKillfeed", "Invert TextDirection", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 200, IsAdvanced = true }));
            KillTextAlignment = Config.Bind<TextAlignmentOptions>("AmandsKillfeed", "TextAlignment", TextAlignmentOptions.Right, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 190, IsAdvanced = true }));
            KillTime = Config.Bind<float>("AmandsKillfeed", "Time", 3f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 20.0f), new ConfigurationManagerAttributes { Order = 180 }));
            KillOpacitySpeed = Config.Bind<float>("AmandsKillfeed", "OpacitySpeed", 0.08f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f,1.0f), new ConfigurationManagerAttributes { Order = 170 }));
            KillUpperText = Config.Bind<bool>("AmandsKillfeed", "EnableUpperText", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160, IsAdvanced = true }));
            KillHeadshotXP = Config.Bind<EHeadshotXP>("AmandsKillfeed", "Headshot XP", EHeadshotXP.On, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 158 }));
            KillStart = Config.Bind<EKillStart>("AmandsKillfeed", "Start", EKillStart.Weapon, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));
            KillNameColor = Config.Bind<EKillNameColor>("AmandsKillfeed", "Name", EKillNameColor.Colored, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140}));
            KillNameSingleColor = Config.Bind<Color>("AmandsKillfeed", "SingleColor", new Color(1.0f, 0.0f, 0.0f, 1.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true }));
            KillEnd = Config.Bind<EKillEnd>("AmandsKillfeed", "End", EKillEnd.Experience, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120 }));
            KillStreakXP = Config.Bind<bool>("AmandsKillfeed", "Streak XP", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 118 }));
            KillDistanceThreshold = Config.Bind<int>("AmandsKillfeed", "DistanceThreshold", 50, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));

            EnableMultiKillfeed = Config.Bind<bool>("AmandsMultiKillfeed", "EnableMultiKillfeed", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 220 }));
            MultiKillfeedPMCIconMode = Config.Bind<EMultiKillfeedPMCMode>("AmandsMultiKillfeed", "PMC Icon Mode", EMultiKillfeedPMCMode.Ranks, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 210 }));
            MultiKillfeedColorMode = Config.Bind<EMultiKillfeedColorMode>("AmandsMultiKillfeed", "IconColor Mode", EMultiKillfeedColorMode.HeadshotColorOnly, new ConfigDescription("Icon Color Mode", null, new ConfigurationManagerAttributes { Order = 200 }));
            MultiKillfeedColor = Config.Bind<Color>("AmandsMultiKillfeed", "IconColor", new Color(0.84f, 0.88f, 0.95f, 1f), new ConfigDescription("Icon Color", null, new ConfigurationManagerAttributes { Order = 190 }));
            MultiKillfeedHeadshotColor = Config.Bind<Color>("AmandsMultiKillfeed", "IconHeadshotColor", new Color(0.9f, 0.0f, 0.0f, 1f), new ConfigDescription("Icon Headshot Color", null, new ConfigurationManagerAttributes { Order = 180 }));
            MultiKillfeedUsecShape = Config.Bind<string>("AmandsMultiKillfeed", "UsecShape", "Usec.png", new ConfigDescription("Supported File PNG", null, new ConfigurationManagerAttributes { Order = 170, IsAdvanced = true }));
            MultiKillfeedBearShape = Config.Bind<string>("AmandsMultiKillfeed", "BearShape", "Bear.png", new ConfigDescription("Supported File PNG", null, new ConfigurationManagerAttributes { Order = 160, IsAdvanced = true }));
            MultiKillfeedGenericShape = Config.Bind<string>("AmandsMultiKillfeed", "GenericShape", "Generic.png", new ConfigDescription("Supported File PNG. Default Icon: Skull By Andy Horvath", null, new ConfigurationManagerAttributes { Order = 150, IsAdvanced = true }));
            MultiKillfeedSize = Config.Bind<Vector2>("AmandsMultiKillfeed", "IconSize", new Vector2(150.0f, 150.0f), new ConfigDescription("Icon size", null, new ConfigurationManagerAttributes { Order = 140 }));
            MultiKillfeedChildSpacing = Config.Bind<int>("AmandsMultiKillfeed", "IconSpacing", -75, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130 }));
            MultiKillfeedRectPosition = Config.Bind<Vector2>("AmandsMultiKillfeed", "RectPosition", new Vector2(0f, -220f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120 }));
            MultiKillfeedRectPivot = Config.Bind<Vector2>("AmandsMultiKillfeed", "RectPivot", new Vector2(0f, 0.5f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true }));

            EnableRaidKillfeed = Config.Bind<bool>("AmandsRaidKillfeed", "EnableRaidKillfeed", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 230 }));
            RaidKillRole = Config.Bind<bool>("AmandsRaidKillfeed", "RaidKillRole", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 220 }));
            RaidKillNameColor = Config.Bind<EKillNameColor>("AmandsRaidKillfeed", "Name", EKillNameColor.Colored, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 210 }));
            RaidKillTime = Config.Bind<float>("AmandsRaidKillfeed", "Time", 5f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 20.0f), new ConfigurationManagerAttributes { Order = 200 }));
            RaidKillDistance = Config.Bind<int>("AmandsRaidKillfeed", "Distance", 10000, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 190 }));
            RaidKillFontSize = Config.Bind<int>("AmandsRaidKillfeed", "FontSize", 20, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180 }));
            RaidKillFontOutline = Config.Bind<float>("AmandsRaidKillfeed", "FontOutline", 0.01f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170, IsAdvanced = true }));
            RaidKillChildSpacing = Config.Bind<int>("AmandsRaidKillfeed", "TextSpacing", -28, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160 }));
            RaidKillPreset = Config.Bind<ERaidKillPreset>("AmandsRaidKillfeed", "Preset", ERaidKillPreset.BottomLeft, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));
            RaidKillChildDirection = Config.Bind<bool>("AmandsRaidKillfeed", "Invert TextDirection", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140, IsAdvanced = true }));
            RaidKillPosition = Config.Bind<Vector2>("AmandsRaidKillfeed", "Position", new Vector2(0f, 0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130 }));
            RaidKillTextAlignment = Config.Bind<TextAlignmentOptions>("AmandsRaidKillfeed", "TextAlignment", TextAlignmentOptions.Left, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true }));

            EnableDamageNumber = Config.Bind<bool>("AmandsHitmarker DamageNumber", "EnableDamageNumber", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 190 }));
            EnableArmorDamageNumber = Config.Bind<bool>("AmandsHitmarker DamageNumber", "EnableArmorDamageNumber", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180 }));
            DamageAnimationTime = Config.Bind<float>("AmandsHitmarker DamageNumber", "AnimationTime", 0.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 1.0f), new ConfigurationManagerAttributes { Order = 170 }));
            DamagePositionMode = Config.Bind<EDamageNumberPositionMode>("AmandsHitmarker DamageNumber", "Position Mode", EDamageNumberPositionMode.Hitmarker, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160 }));
            DamageFontSize = Config.Bind<int>("AmandsHitmarker DamageNumber", "FontSize", 20, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));
            DamageFontOutline = Config.Bind<float>("AmandsHitmarker DamageNumber", "FontOutline", 0.01f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140, IsAdvanced = true }));
            DamageRectPosition = Config.Bind<Vector2>("AmandsHitmarker DamageNumber", "RectPosition", new Vector2(0f, -100f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130 }));
            DamageRectPivot = Config.Bind<Vector2>("AmandsHitmarker DamageNumber", "RectPivot", new Vector2(0.5f, 0.5f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));

            new AmandsDamagePatch().Enable();
            new AmandsArmorDamagePatch().Enable();
            new AmandsProceedArmorDamagePatch().Enable();
            new AmandsKillPatch().Enable();
            new AmandsLocalPlayerPatchHM().Enable();
            new AmandsMenuUIPatch().Enable();
            new AmandsBattleUIScreenPatch().Enable();
            new AmandsSSAAPatch().Enable();

            AmandsHitmarkerHelper.Init();
        }
        private static void HitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("Hitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.HitmarkerDebug(null, null);
        }
        private static void HeadshotHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("HeadshotHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.HeadshotHitmarkerDebug(null, null);
        }
        private static void BleedHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("BleedHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.BleedHitmarkerDebug(null, null);
        }
        private static void PoisonHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("PoisonHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.PoisonHitmarkerDebug(null, null);
        }
        private static void ArmorHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("ArmorHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.ArmorHitmarkerDebug(null, null);
        }
        private static void ArmorBreakHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("ArmorBreakHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.ArmorBreakHitmarkerDebug(null, null);
        }
        private static void UsecHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("UsecHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.UsecHitmarkerDebug(null, null);
        }
        private static void BearHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("BearHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.BearHitmarkerDebug(null, null);
        }
        private static void ScavHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("ScavHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.ScavHitmarkerDebug(null, null);
        }
        private static void ThrowWeaponHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("ThrowWeaponHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.ThrowWeaponHitmarkerDebug(null, null);
        }
        private static void FollowerHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("FollowerHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.FollowerHitmarkerDebug(null, null);
        }
        private static void BossHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("BossHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.BossHitmarkerDebug(null, null);
        }
        private static void RaiderHitmarkerButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("RaiderHitmarker", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.RaiderHitmarkerDebug(null, null);
        }
        private static void DamageNumberDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("DamageNumber", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.DamageNumberDebug(null, null);
        }
        private static void HitmarkerSoundButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("HitmarkerSound", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.HitmarkerSoundDebug(null, null);
        }
        private static void HeadshotHitmarkerSoundButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("HeadshotHitmarkerSound", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.HeadshotHitmarkerSoundDebug(null, null);
        }
        private static void ArmorSoundButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("ArmorSound", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.ArmorSoundDebug(null, null);
        }
        private static void ArmorBreakSoundButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("ArmorBreakSound", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.ArmorBreakSoundDebug(null, null);
        }
        private static void KillHitmarkerSoundButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("KillHitmarkerSound", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.KillHitmarkerSoundDebug(null, null);
        }
        private static void ReloadFilesButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("ReloadFiles", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.ReloadFiles();
        }
        private static void ReloadUIButtonDrawer(ConfigEntryBase entry)
        {
            bool button = GUILayout.Button("ReloadUI", GUILayout.ExpandWidth(true));
            if (button) AmandsHitmarkerClass.ReloadUI();
        }
    }
    public class AmandsLocalPlayerPatchHM : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            //return typeof(LocalPlayer).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            return typeof(CoopGame).GetMethod("vmethod_2", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Task<LocalPlayer> __result)
        {
            LocalPlayer localPlayer = __result.Result;
            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                AmandsHitmarkerClass.localPlayer = localPlayer;
                AmandsHitmarkerClass.localPlayerNickname = localPlayer.Profile.Nickname;
                AmandsHitmarkerClass.PlayerSuperior = localPlayer.gameObject;
                AmandsHitmarkerClass.Kills = 0;
                AmandsHitmarkerClass.ReloadFiles();
                AmandsHitmarkerClass.CanDebugReloadFiles = true;
            }
        }
    }
    public class AmandsMenuUIPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.UI.MenuUI).GetMethod("Awake");
        }
        [PatchPostfix]
        private static void PatchPostFix(ref EFT.UI.MenuUI __instance)
        {
            if (AmandsHitmarkerClass.ActiveUIScreen == __instance.transform.GetChild(0).gameObject) return;
            AmandsHitmarkerClass.ActiveUIScreen = __instance.transform.GetChild(0).gameObject;
            AmandsHitmarkerClass.DestroyGameObjects();
            AmandsHitmarkerClass.CreateGameObjects(__instance.transform.GetChild(0));
            AmandsHitmarkerClass.XPFormula();
        }
    }
    public class AmandsBattleUIScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.UI.BattleUIScreen).GetMethod("Show");
        }
        [PatchPostfix]
        private static void PatchPostFix(ref EFT.UI.BattleUIScreen __instance)
        {
            if (AmandsHitmarkerClass.ActiveUIScreen == __instance.gameObject) return;
            AmandsHitmarkerClass.ActiveUIScreen = __instance.gameObject;
            AmandsHitmarkerClass.DestroyGameObjects();
            AmandsHitmarkerClass.CreateGameObjects(__instance.transform);
            AmandsHitmarkerClass.XPFormula();
            AmandsHitmarkerClass.DebugMode = false;
            AmandsHitmarkerClass.DebugOffset = Vector3.zero;
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
            AmandsHitmarkerClass.FPSCameraSSAA = __instance;
        }
    }
    public class AmandsDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("ApplyDamageInfo");
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Player __instance, DamageInfo damageInfo, EBodyPart bodyPartType)
        {
            // Temporary old version support code
            bool IsYourPlayerAgresssor = false;
            Player player = Traverse.Create(damageInfo).Field("Player").GetValue<object>() as Player;
            if (player != null)
            {
                IsYourPlayerAgresssor = AmandsHitmarkerClass.localPlayer != null && player == AmandsHitmarkerClass.localPlayer;
            }
            else
            {
                object playerObject = Traverse.Create(damageInfo).Field("Player").GetValue<object>();
                if (playerObject != null)
                {
                    string AggressorNickname = Traverse.Create(playerObject).Property("Nickname").GetValue<string>();
                    IsYourPlayerAgresssor = AggressorNickname == AmandsHitmarkerClass.localPlayerNickname;
                }
            }
            if (IsYourPlayerAgresssor)
            {
                if (AmandsHitmarkerClass.localPlayer != null)
                {
                    float distance = Vector3.Distance(AmandsHitmarkerClass.localPlayer.Position, __instance.Position);
                    if (distance < AHitmarkerPlugin.StartDistance.Value || distance > AHitmarkerPlugin.EndDistance.Value)
                    {
                        AmandsHitmarkerClass.armorHitmarker = false;
                        AmandsHitmarkerClass.armorBreak = false;
                        return;
                    }
                }
                AmandsHitmarkerClass.hitmarker = true;
                AmandsHitmarkerClass.damageInfo = damageInfo;
                AmandsHitmarkerClass.bodyPart = bodyPartType;
                if (AmandsHitmarkerClass.damageNumberTextMeshPro == null) return;
                if ((AHitmarkerPlugin.EnableDamageNumber.Value && damageInfo.DidBodyDamage > 0.01f) || (AHitmarkerPlugin.EnableArmorDamageNumber.Value && AmandsHitmarkerClass.ArmorDamageNumber > 0.01f))
                {
                    string text = "";
                    AmandsHitmarkerClass.DamageNumber += damageInfo.DidBodyDamage;
                    if (AHitmarkerPlugin.EnableDamageNumber.Value && AmandsHitmarkerClass.DamageNumber > 0.01f)
                    {
                        text = ((int)AmandsHitmarkerClass.DamageNumber).ToString() + " ";
                    }
                    if (AHitmarkerPlugin.EnableArmorDamageNumber.Value && AmandsHitmarkerClass.ArmorDamageNumber > 0.01f)
                    {
                        text = text + "<color=#" + ColorUtility.ToHtmlStringRGB(AHitmarkerPlugin.ArmorColor.Value) + ">" + (Math.Round(AmandsHitmarkerClass.ArmorDamageNumber, 1)).ToString("F1") + "</color> ";
                    }
                    AmandsHitmarkerClass.damageNumberTextMeshPro.text = text;
                    AmandsHitmarkerClass.damageNumberTextMeshPro.color = AHitmarkerPlugin.HitmarkerColor.Value;
                    AmandsHitmarkerClass.damageNumberTextMeshPro.alpha = 1f;
                    AmandsHitmarkerClass.UpdateDamageNumber = false;
                }
            }
        }
    }
    public class AmandsProceedArmorDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("ProceedDamageThroughArmor");
        }
        [PatchPrefix]
        private static void PatchPrefix(ref Player __instance, DamageInfo damageInfo)
        {
            // Temporary old version support code
            Player player = Traverse.Create(damageInfo).Field("Player").GetValue<object>() as Player;
            if (player != null)
            {
                if (AmandsHitmarkerClass.localPlayer != null && player == AmandsHitmarkerClass.localPlayer && !__instance.IsYourPlayer)
                {
                    AHitmarkerPlugin.PlayerProceedDamageThroughArmor = __instance;
                }
            }
            else
            {
                object playerObject = Traverse.Create(damageInfo).Field("Player").GetValue<object>();
                if (playerObject != null)
                {
                    string Nickname = Traverse.Create(playerObject).Property("Nickname").GetValue<string>();
                    if (Nickname == AmandsHitmarkerClass.localPlayerNickname)
                    {
                        AHitmarkerPlugin.PlayerProceedDamageThroughArmor = __instance;
                    }
                }
            }
        }
    }
    public class AmandsArmorDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ArmorComponent).GetMethod("ApplyDurabilityDamage");
        }
        [PatchPrefix]
        private static void PatchPrefix(ref ArmorComponent __instance, float armorDamage)
        {
            if (AHitmarkerPlugin.PlayerProceedDamageThroughArmor != null && armorDamage > 0)
            {
                List<ArmorComponent> list = Traverse.Create(AHitmarkerPlugin.PlayerProceedDamageThroughArmor).Field("_preAllocatedArmorComponents").GetValue<List<ArmorComponent>>();
                if (list.Contains(__instance))
                {
                    AmandsHitmarkerClass.ArmorDamageNumber += Mathf.Min(__instance.Repairable.Durability, armorDamage);
                    AmandsHitmarkerClass.armorHitmarker = true;
                    if (__instance.Repairable.Durability - armorDamage < 0)
                    {
                        AmandsHitmarkerClass.armorBreak = true;
                    }
                }
            }
        }
    }
    public class AmandsKillPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnBeenKilledByAggressor");
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Player __instance, Player aggressor, DamageInfo damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            if (AmandsHitmarkerClass.localPlayer != null && aggressor == AmandsHitmarkerClass.localPlayer && __instance != AmandsHitmarkerClass.localPlayer)
            {
                float distance = Vector3.Distance(aggressor.Position, __instance.Position);
                if (distance < AHitmarkerPlugin.StartDistance.Value || distance > AHitmarkerPlugin.EndDistance.Value) return;
                AmandsHitmarkerClass.killHitmarker = true;
                AmandsHitmarkerClass.killDamageInfo = damageInfo;
                AmandsHitmarkerClass.killBodyPart = bodyPart;
                AmandsHitmarkerClass.killRole = Traverse.Create(Traverse.Create(__instance.Profile.Info).Field("Settings").GetValue<object>()).Field("Role").GetValue<WildSpawnType>();
                AmandsHitmarkerClass.killExperience = Traverse.Create(Traverse.Create(__instance.Profile.Info).Field("Settings").GetValue<object>()).Field("Experience").GetValue<int>();
                AmandsHitmarkerClass.killPlayerName = __instance.Profile.Nickname;
                AmandsHitmarkerClass.killPlayerSide = __instance.Side;
                AmandsHitmarkerClass.killDistance = distance;
                AmandsHitmarkerClass.killLethalDamageType = lethalDamageType;
                AmandsHitmarkerClass.killLevel = __instance.Profile.Info.Level;
                AmandsHitmarkerClass.killWeaponName = damageInfo.Weapon == null ? "?" : AmandsHitmarkerHelper.Localized(damageInfo.Weapon.ShortName,0);
                AmandsHitmarkerClass.Kills += 1;
                AmandsHitmarkerClass.Killfeed();
                AmandsHitmarkerClass.MultiKillfeed();
            }
            if (AHitmarkerPlugin.EnableRaidKillfeed.Value && aggressor != null)
            {
                if (AmandsHitmarkerClass.localPlayer != null && AmandsHitmarkerClass.localPlayer != aggressor && Vector3.Distance(AmandsHitmarkerClass.localPlayer.Position, __instance.Position) > AHitmarkerPlugin.RaidKillDistance.Value) return;
                AmandsHitmarkerClass.RaidKillfeed(aggressor.Side, Traverse.Create(Traverse.Create(aggressor.Profile.Info).Field("Settings").GetValue<object>()).Field("Role").GetValue<WildSpawnType>(), (aggressor.Side == EPlayerSide.Savage ? AmandsHitmarkerHelper.Transliterate(aggressor.Profile.Nickname) : aggressor.Profile.Nickname), damageInfo.Weapon == null ? "?" : AmandsHitmarkerHelper.Localized(damageInfo.Weapon.ShortName, 0), lethalDamageType, __instance.Side, Traverse.Create(Traverse.Create(__instance.Profile.Info).Field("Settings").GetValue<object>()).Field("Role").GetValue<WildSpawnType>(), (__instance.Side == EPlayerSide.Savage ? AmandsHitmarkerHelper.Transliterate(__instance.Profile.Nickname) : __instance.Profile.Nickname));
            }
        }
    }
}
