using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using EFT;
using Comfort.Common;
using TMPro;
using static EFT.Player;
using HarmonyLib;
using EFT.UI;

namespace AmandsHitmarker
{
    public class AmandsHitmarkerClass : MonoBehaviour
    {
        public static AudioSource HitmarkerAudioSource;
        public static bool CanDebugReloadFiles;
        public static GameObject killListGameObject;
        public static AmandsKillfeedText LastAmandsKillfeedText;
        public static bool hitmarker;
        public static DamageInfo damageInfo = new DamageInfo();
        public static EBodyPart bodyPart = EBodyPart.Chest;
        public static bool armorHitmarker;
        //public static float armorDamage;
        //public static DamageInfo armorDamageInfo;
        public static bool armorBreak;
        public static bool killHitmarker;
        public static DamageInfo killDamageInfo = new DamageInfo();
        public static EPlayerSide killPlayerSide;
        public static EBodyPart killBodyPart = EBodyPart.Chest;
        public static WildSpawnType killRole;
        public static string aggNickname;
        public static EPlayerSide aggPlayerSide = EPlayerSide.Usec;
        public static string killPlayerName;
        public static int killExperience;
        public static float killDistance;
        public static EDamageType killLethalDamageType;
        public static int killLevel;
        public static string killWeaponName;
        private static RectTransform rectTransform;
        private static VerticalLayoutGroup verticalLayoutGroup;

        public static List<AmandsAnimatedImage> amandsAnimatedImages = new List<AmandsAnimatedImage>();
        public static GameObject multiKillfeedGameObject;
        private static RectTransform multiKillfeedrectTransform;
        private static HorizontalLayoutGroup horizontalLayoutGroup;

        public static GameObject raidKillListGameObject;
        public static AmandsRaidKillfeedText LastAmandsRaidKillfeedText;
        private static RectTransform raidKillrectTransform;
        private static VerticalLayoutGroup raidKillverticalLayoutGroup;

        public static int Kills;
        public static int VictimLevelExp;
        public static int VictimBotLevelExp;
        public static float HeadShotMult;
        public static float LongShotDistance;
        public static List<int> Combo = new List<int>();

        public static GameObject ActiveUIScreen;
        public static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
        public static Dictionary<string, Sprite> LoadedRanks = new Dictionary<string, Sprite>();
        public static Dictionary<string, AudioClip> LoadedAudioClips = new Dictionary<string, AudioClip>();
        private static Sprite sprite;
        public static LocalPlayer localPlayer;
        public static string localPlayerNickname;
        public static GameObject PlayerSuperior;
        public static FirearmController firearmController;
        public static SSAA FPSCameraSSAA;
        private static float FPSCameraSSAARatio;
        private static GameObject TLH;
        private static GameObject TRH;
        private static GameObject BLH;
        private static GameObject BRH;
        private static RectTransform TLHRect;
        private static RectTransform TRHRect;
        private static RectTransform BLHRect;
        private static RectTransform BRHRect;
        private static Image TLHImage;
        private static Image TRHImage;
        private static Image BLHImage;
        private static Image BRHImage;
        private static Vector3 TLHOffset = new Vector3(-1, 1, 0);
        private static Vector3 TRHOffset = new Vector3(1, 1, 0);
        private static Vector3 BLHOffset = new Vector3(-1, -1, 0);
        private static Vector3 BRHOffset = new Vector3(1, -1, 0);
        private static Vector3 TLHOffsetAnim = Vector3.zero;
        private static Vector3 TRHOffsetAnim = Vector3.zero;
        private static Vector3 BLHOffsetAnim = Vector3.zero;
        private static Vector3 BRHOffsetAnim = Vector3.zero;
        private static GameObject BleedHitmarker;
        private static RectTransform BleedRect;
        private static Image BleedImage;
        private static GameObject StaticHitmarker;
        private static Image StaticHitmarkerImage;
        private static RectTransform StaticHitmarkerRect;
        private static GameObject ArmorHitmarker;
        private static Image ArmorHitmarkerImage;
        private static RectTransform ArmorHitmarkerRect;
        private static bool ForceHitmarkerPosition = false;
        private static Vector3 HitmarkerPosition = Vector3.zero;
        private static Vector3 HitmarkerPositionSnapshot = Vector3.zero;
        private static Vector3 position = Vector3.zero;
        private static Vector3 weaponDirection = Vector3.zero;

        private static bool UpdateHitmarker = false;
        private static float HitmarkerTime = 0.0f;
        private static float HitmarkerOpacity = 0.0f;
        private static float HitmarkerCenterOffset = 0.0f;
        private static Color HitmarkerColor = new Color(1.0f, 1.0f, 1.0f);
        private static Color KillfeedColor = new Color(1.0f, 1.0f, 1.0f);
        private static Color MultiKillfeedColor = new Color(1.0f, 1.0f, 1.0f);
        private static Color BleedColor = new Color(1.0f, 0.0f, 0.0f);
        private static AudioClip audioClip;
        private static Color ArmorHitmarkerColor = new Color(1.0f, 1.0f, 1.0f);

        public static bool UpdateDamageNumber = false;
        public static float DamageNumber = 0f;
        public static float ArmorDamageNumber = 0f;
        private static float damageNumberOpacity = 0.0f;
        private static GameObject damageNumberGameObject;
        public static RectTransform damageNumberRectTransform;
        public static TextMeshProUGUI damageNumberTextMeshPro;

        public static bool DebugMode = false;
        public static Vector3 DebugOffset = Vector3.zero;
        public static List<string> DebugWeapons = new List<string>() { "RD-704", "MGSL", "P90", "MCX .300 BLK", "G36 E", "FN 5-7", "AXMC", "SV-98" };
        public static List<string> DebugNames = new List<string>() { "Mellone", "Pin", "Big Pipe", "Birdeye", "Glukhar", "Killa", "Knight", "Reshala", "Sanitar", "Shturman", "Tagilla", "Zryachiy" };

        private static AnimationCurve animationCurve = new AnimationCurve();
        private static Keyframe[] keys;// = { new Keyframe(0f, 0f, 0f, 0f, 0.25f, 0.25f), new Keyframe(0.5f, 1f, 0f, 0f, 0.5f, 0.5f), new Keyframe(1f, 0f, 0f, 0f, 0.25f, 0.25f) };
        private static AnimationCurve AlphaAnimationCurve = new AnimationCurve();
        private static Keyframe[] AlphaKeys;// = { new Keyframe(1f, 1f, 0f, 0f, 0.25f, 0.25f), new Keyframe(1.5f, 0f, 0f, 0f, 0.25f, 0.25f) };

        public static void XPFormula()
        {
            /*BackendConfigSettingsClass backendConfigSettingsClass = Singleton<BackendConfigSettingsClass>.Instance;
            if (backendConfigSettingsClass != null)
            {
                object Experience = Traverse.Create(backendConfigSettingsClass).Field("Experience").GetValue<object>();
                if (Experience != null)
                {
                    object Kill = Traverse.Create(Experience).Field("Kill").GetValue<object>();
                    if (Kill != null)
                    {
                        VictimLevelExp = Traverse.Create(Kill).Field("VictimLevelExp").GetValue<int>();
                        VictimBotLevelExp = Traverse.Create(Kill).Field("VictimBotLevelExp").GetValue<int>();
                        HeadShotMult = Traverse.Create(Kill).Field("HeadShotMult").GetValue<float>();
                        LongShotDistance = Traverse.Create(Kill).Field("LongShotDistance").GetValue<float>();
                        object[] combo = Traverse.Create(Kill).Field("Combo").GetValue<object[]>();
                        Combo.Clear();
                        foreach (object c in combo)
                        {
                            Combo.Add(Traverse.Create(c).Field("Percent").GetValue<int>());
                        }
                    }
                }
            }*/
            VictimLevelExp = 175;
            VictimBotLevelExp = 100;
            HeadShotMult = 1.2f;
            LongShotDistance = 100;
            Combo = new List<int>() { 0, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        }
        public static int GetKillingBonusPercent(int killed)
        {
            int num = Mathf.Clamp(killed - 1, 0, Combo.Count - 1);
            return Combo[num];
        }
        public void Start()
        {
            animationCurve.keys = keys;
            AlphaAnimationCurve.keys = AlphaKeys;

            AHitmarkerPlugin.EnableHitmarker.SettingChanged += HitmarkerDebug;
            AHitmarkerPlugin.EnableArmorHitmarker.SettingChanged += ArmorHitmarkerDebug;
            AHitmarkerPlugin.EnableBleeding.SettingChanged += BleedHitmarkerDebug;
            AHitmarkerPlugin.Thickness.SettingChanged += HitmarkerDebug;
            AHitmarkerPlugin.CenterOffset.SettingChanged += HitmarkerDebug;
            AHitmarkerPlugin.ArmorOffset.SettingChanged += ArmorHitmarkerDebug;
            AHitmarkerPlugin.ArmorSizeDelta.SettingChanged += ArmorHitmarkerDebug;
            AHitmarkerPlugin.AnimatedTime.SettingChanged += UpdateHitmarkerAnimation;
            AHitmarkerPlugin.AnimatedAlphaTime.SettingChanged += UpdateHitmarkerAnimation;
            AHitmarkerPlugin.AnimatedAmplitude.SettingChanged += UpdateHitmarkerAnimation;
            AHitmarkerPlugin.BleedSize.SettingChanged += BleedHitmarkerDebug;

            AHitmarkerPlugin.HitmarkerColor.SettingChanged += HitmarkerDebug;
            AHitmarkerPlugin.ArmorColor.SettingChanged += ArmorHitmarkerDebug;
            AHitmarkerPlugin.BearColor.SettingChanged += BearHitmarkerDebug;
            AHitmarkerPlugin.UsecColor.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.ScavColor.SettingChanged += ScavHitmarkerDebug;
            AHitmarkerPlugin.ThrowWeaponColor.SettingChanged += ThrowWeaponHitmarkerDebug;
            AHitmarkerPlugin.FollowerColor.SettingChanged += FollowerHitmarkerDebug;
            AHitmarkerPlugin.BossColor.SettingChanged += BossHitmarkerDebug;
            AHitmarkerPlugin.RaiderColor.SettingChanged += BossHitmarkerDebug;
            AHitmarkerPlugin.BleedColor.SettingChanged += BleedHitmarkerDebug;
            AHitmarkerPlugin.PoisonColor.SettingChanged += PoisonHitmarkerDebug;

            AHitmarkerPlugin.EnableKillfeed.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillTextColor.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillFontSize.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillFontOutline.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillFontUpperCase.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillChildSpacing.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillPreset.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillChildDirection.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillTextAlignment.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillOpacitySpeed.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillUpperText.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillStart.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillNameColor.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillNameSingleColor.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillEnd.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.KillDistanceThreshold.SettingChanged += UsecHitmarkerDebug;

            AHitmarkerPlugin.KillChildSpacing.SettingChanged += UpdateKillfeed;
            AHitmarkerPlugin.KillPreset.SettingChanged += UpdateKillPreset;
            AHitmarkerPlugin.KillPosition.SettingChanged += UpdateKillfeed;

            AHitmarkerPlugin.EnableMultiKillfeed.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.MultiKillfeedPMCIconMode.SettingChanged += UsecHitmarkerDebug;

            AHitmarkerPlugin.MultiKillfeedChildSpacing.SettingChanged += UpdateMultiKillfeed;
            AHitmarkerPlugin.MultiKillfeedRectPosition.SettingChanged += UpdateMultiKillfeed;
            AHitmarkerPlugin.MultiKillfeedRectPivot.SettingChanged += UpdateMultiKillfeed;

            AHitmarkerPlugin.EnableRaidKillfeed.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.RaidKillNameColor.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.RaidKillRole.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.RaidKillFontSize.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.RaidKillFontOutline.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.RaidKillChildDirection.SettingChanged += UsecHitmarkerDebug;
            AHitmarkerPlugin.RaidKillTextAlignment.SettingChanged += UsecHitmarkerDebug;

            AHitmarkerPlugin.RaidKillChildSpacing.SettingChanged += UpdateRaidKillfeed;
            AHitmarkerPlugin.RaidKillPreset.SettingChanged += UpdateRaidKillPreset;
            AHitmarkerPlugin.RaidKillPosition.SettingChanged += UpdateRaidKillfeed;

            AHitmarkerPlugin.DamageFontSize.SettingChanged += UpdateDamageNumbers;
            AHitmarkerPlugin.DamageFontOutline.SettingChanged += UpdateDamageNumbers;
            AHitmarkerPlugin.DamageRectPosition.SettingChanged += UpdateDamageNumbers;
            AHitmarkerPlugin.DamageRectPivot.SettingChanged += UpdateDamageNumbers;

            AHitmarkerPlugin.EnableDamageNumber.SettingChanged += DamageNumberDebug;
            AHitmarkerPlugin.EnableArmorDamageNumber.SettingChanged += DamageNumberDebug;
            AHitmarkerPlugin.DamageAnimationTime.SettingChanged += DamageNumberDebug;

            keys = new Keyframe[] { new Keyframe(0f, 0f, 0f, 0f, 0.25f, 0.25f), new Keyframe(AHitmarkerPlugin.AnimatedTime.Value / 2, AHitmarkerPlugin.AnimatedAmplitude.Value, 0f, 0f, 0.5f, 0.5f), new Keyframe(AHitmarkerPlugin.AnimatedTime.Value, 0f, 0f, 0f, 0.25f, 0.25f) };
            AlphaKeys = new Keyframe[] { new Keyframe(AHitmarkerPlugin.AnimatedTime.Value, 1f, 0f, 0f, 0.25f, 0.25f), new Keyframe(AHitmarkerPlugin.AnimatedTime.Value + AHitmarkerPlugin.AnimatedAlphaTime.Value, 0f, 0f, 0f, 0.25f, 0.25f) };
            animationCurve.keys = keys;
            AlphaAnimationCurve.keys = AlphaKeys;

            ReloadFiles();
        }
        public void UpdateDamageNumbers(object sender, EventArgs e)
        {
            damageNumberRectTransform.localPosition = new Vector3(AHitmarkerPlugin.DamageRectPosition.Value.x, AHitmarkerPlugin.DamageRectPosition.Value.y, 0f);
            damageNumberRectTransform.pivot = AHitmarkerPlugin.DamageRectPivot.Value;
            damageNumberTextMeshPro.fontSize = AHitmarkerPlugin.DamageFontSize.Value;
            damageNumberTextMeshPro.outlineWidth = AHitmarkerPlugin.DamageFontOutline.Value;
            DamageNumberDebug(null, null);
        }
        public void UpdateHitmarkerAnimation(object sender, EventArgs e)
        {
            keys = new Keyframe[] { new Keyframe(0f, 0f, 0f, 0f, 0.25f, 0.25f), new Keyframe(AHitmarkerPlugin.AnimatedTime.Value / 2, AHitmarkerPlugin.AnimatedAmplitude.Value, 0f, 0f, 0.5f, 0.5f), new Keyframe(AHitmarkerPlugin.AnimatedTime.Value, 0f, 0f, 0f, 0.25f, 0.25f) };
            AlphaKeys = new Keyframe[] { new Keyframe(AHitmarkerPlugin.AnimatedTime.Value, 1f, 0f, 0f, 0.25f, 0.25f), new Keyframe(AHitmarkerPlugin.AnimatedTime.Value + AHitmarkerPlugin.AnimatedAlphaTime.Value, 0f, 0f, 0f, 0.25f, 0.25f) };
            animationCurve.keys = keys;
            AlphaAnimationCurve.keys = AlphaKeys;
            HitmarkerDebug(null,null);
        }
        public void Update()
        {
            if ((hitmarker || killHitmarker) && ActiveUIScreen != null)
            {
                ForceHitmarkerPosition = false;
                bool tmpHitmarker = hitmarker;
                hitmarker = false;
                bool tmpArmorHitmarker = armorHitmarker;
                armorHitmarker = false;
                bool tmpArmorBreak = armorBreak;
                armorBreak = false;
                bool tmpKillHitmarker = killHitmarker;
                killHitmarker = false;

                if (!AHitmarkerPlugin.EnableBleeding.Value && (tmpKillHitmarker && !tmpHitmarker)) return;

                if (AHitmarkerPlugin.EnableSounds.Value && !DebugMode)
                {
                    if (LoadedAudioClips.ContainsKey(AHitmarkerPlugin.HitmarkerSound.Value))
                    {
                        audioClip = LoadedAudioClips[AHitmarkerPlugin.HitmarkerSound.Value];
                    }
                }
                HitmarkerColor = AHitmarkerPlugin.HitmarkerColor.Value;
                if (bodyPart == EBodyPart.Head)
                {
                    if (LoadedSprites.ContainsKey(AHitmarkerPlugin.HeadshotShape.Value))
                    {
                        sprite = LoadedSprites[AHitmarkerPlugin.HeadshotShape.Value];
                    }
                    StaticHitmarkerImage.sprite = LoadedSprites["StaticHeadshotHitmarker.png"];
                }
                else
                {
                    if (LoadedSprites.ContainsKey(AHitmarkerPlugin.Shape.Value))
                    {
                        sprite = LoadedSprites[AHitmarkerPlugin.Shape.Value];
                    }
                }
                if (tmpHitmarker)
                {
                    switch (damageInfo.DamageType)
                    {
                        case EDamageType.GrenadeFragment:
                        case EDamageType.Explosion:
                            ForceHitmarkerPosition = true;
                            break;
                    }
                }
                ArmorHitmarkerColor = Color.clear;
                if (tmpArmorHitmarker && !tmpKillHitmarker)
                {
                    HitmarkerColor = AHitmarkerPlugin.ArmorColor.Value;
                    if (AHitmarkerPlugin.EnableArmorHitmarker.Value != EArmorHitmarker.Disabled)
                    {
                        if (tmpArmorBreak)
                        {
                            ArmorHitmarkerColor = AHitmarkerPlugin.ArmorColor.Value;
                            ArmorHitmarkerImage.sprite = LoadedSprites[AHitmarkerPlugin.ArmorBreakShape.Value];
                        }
                        else if (AHitmarkerPlugin.EnableArmorHitmarker.Value != EArmorHitmarker.BreakingOnly)
                        {
                            ArmorHitmarkerColor = AHitmarkerPlugin.HitmarkerColor.Value;
                            ArmorHitmarkerImage.sprite = LoadedSprites[AHitmarkerPlugin.ArmorShape.Value];
                        }
                        ArmorHitmarkerRect.sizeDelta = AHitmarkerPlugin.ArmorSizeDelta.Value;
                    }
                    if (tmpArmorBreak && AHitmarkerPlugin.EnableSounds.Value && !DebugMode)
                    {
                        audioClip = LoadedAudioClips[AHitmarkerPlugin.ArmorBreakSound.Value];
                    }
                    else if (AHitmarkerPlugin.EnableSounds.Value && !DebugMode)
                    {
                        audioClip = LoadedAudioClips[AHitmarkerPlugin.ArmorSound.Value];
                    }
                }
                BleedColor = Color.clear;
                if (tmpKillHitmarker)
                {
                    switch (killPlayerSide)
                    {
                        case EPlayerSide.Usec:
                            HitmarkerColor = AHitmarkerPlugin.UsecColor.Value;
                            break;
                        case EPlayerSide.Bear:
                            HitmarkerColor = AHitmarkerPlugin.BearColor.Value;
                            break;
                        case EPlayerSide.Savage:
                            HitmarkerColor = AHitmarkerPlugin.ScavColor.Value;
                            break;
                    }
                    switch (killLethalDamageType)
                    {
                        case EDamageType.GrenadeFragment:
                        case EDamageType.Explosion:
                            HitmarkerColor = AHitmarkerPlugin.ThrowWeaponColor.Value;
                            ForceHitmarkerPosition = true;
                            break;
                    }
                    if (killPlayerSide == EPlayerSide.Savage)
                    {
                        string RoleName = AmandsHitmarkerHelper.Localized(AmandsHitmarkerHelper.GetScavRoleKey(killRole), EStringCase.Upper);
                        switch (RoleName)
                        {
                            case "BLOODHOUND":
                                HitmarkerColor = AHitmarkerPlugin.BloodhoundColor.Value;
                                break;
                            case "RAIDER":
                                HitmarkerColor = AHitmarkerPlugin.RaiderColor.Value;
                                break;
                            default:
                                if (AmandsHitmarkerHelper.IsFollower(killRole)) HitmarkerColor = AHitmarkerPlugin.FollowerColor.Value;
                                if (AmandsHitmarkerHelper.IsBoss(killRole) || AmandsHitmarkerHelper.CountAsBoss(killRole)) HitmarkerColor = AHitmarkerPlugin.BossColor.Value;
                                break;
                        }
                    }
                    switch (killLethalDamageType)
                    {
                        case EDamageType.LightBleeding:
                        case EDamageType.HeavyBleeding:
                            BleedColor = AHitmarkerPlugin.BleedColor.Value;
                            BleedRect.sizeDelta = AHitmarkerPlugin.BleedSize.Value;
                            BleedRect.localPosition = DebugOffset;
                            ForceHitmarkerPosition = true;
                            break;
                        case EDamageType.Poison:
                            BleedColor = AHitmarkerPlugin.PoisonColor.Value;
                            BleedRect.sizeDelta = AHitmarkerPlugin.BleedSize.Value;
                            BleedRect.localPosition = DebugOffset;
                            ForceHitmarkerPosition = true;
                            break;
                        default:
                            if (killLethalDamageType.ToString() == "LethalToxin")
                            {
                                BleedColor = AHitmarkerPlugin.PoisonColor.Value;
                                BleedRect.sizeDelta = AHitmarkerPlugin.BleedSize.Value;
                                BleedRect.localPosition = DebugOffset;
                                ForceHitmarkerPosition = true;
                            }
                            break;
                    }
                    if (AHitmarkerPlugin.EnableSounds.Value && !DebugMode)
                    {
                        if (killBodyPart == EBodyPart.Head && LoadedAudioClips.ContainsKey(AHitmarkerPlugin.HeadshotHitmarkerSound.Value))
                        {
                            audioClip = LoadedAudioClips[AHitmarkerPlugin.HeadshotHitmarkerSound.Value];
                        }
                        else if (LoadedAudioClips.ContainsKey(AHitmarkerPlugin.KillHitmarkerSound.Value))
                        {
                            audioClip = LoadedAudioClips[AHitmarkerPlugin.KillHitmarkerSound.Value];
                        }
                    }
                }
                if (AHitmarkerPlugin.EnableHitmarker.Value)
                {
                    if (firearmController == null && PlayerSuperior != null)
                    {
                        firearmController = PlayerSuperior.GetComponent<FirearmController>();
                    }
                    if (Camera.main != null)
                    {
                        FPSCameraSSAARatio = (float)FPSCameraSSAA.GetOutputHeight() / (float)FPSCameraSSAA.GetInputHeight();
                        /*if (FPSCameraSSAA != null && (FPSCameraSSAA.UsesDLSSUpscaler() || FPSCameraSSAA.UsesFSRUpscaler()) || AmandsHitmarkerHelper.UsesFSR2Upscaler())
                        {
                            FPSCameraSSAARatio = (float)FPSCameraSSAA.GetOutputHeight() / (float)FPSCameraSSAA.GetInputHeight();
                        }*/
                        Vector2 ScreenPointSnapshot = Camera.main.WorldToScreenPoint(damageInfo.HitPoint) * FPSCameraSSAARatio;
                        HitmarkerPositionSnapshot = new Vector2(ScreenPointSnapshot.x - (Screen.width / 2), ScreenPointSnapshot.y - (Screen.height / 2));
                    }
                    TLHImage.sprite = sprite; TRHImage.sprite = sprite;
                    BLHImage.sprite = sprite; BRHImage.sprite = sprite;
                    TLHRect.sizeDelta = AHitmarkerPlugin.Thickness.Value; TRHRect.sizeDelta = AHitmarkerPlugin.Thickness.Value;
                    BLHRect.sizeDelta = AHitmarkerPlugin.Thickness.Value; BRHRect.sizeDelta = AHitmarkerPlugin.Thickness.Value;
                    StaticHitmarkerRect.sizeDelta = AHitmarkerPlugin.StaticSizeDelta.Value;

                    HitmarkerCenterOffset = AHitmarkerPlugin.CenterOffset.Value;

                    HitmarkerPosition = Vector3.zero;
                    HitmarkerOpacity = 1.0f;
                    HitmarkerTime = 0.0f;
                    UpdateHitmarker = true;
                }
                if (AHitmarkerPlugin.EnableSounds.Value && !DebugMode)// Singleton<BetterAudio>.Instance != null && !DebugMode)
                {
                    //Singleton<BetterAudio>.Instance.PlayNonspatial(audioClip, BetterAudio.AudioSourceGroupType.Nonspatial, 0.0f, AHitmarkerPlugin.SoundVolume.Value);
                    HitmarkerAudioSource.PlayOneShot(audioClip, AHitmarkerPlugin.SoundVolume.Value);
                }
            }
            if (UpdateHitmarker)
            {
                HitmarkerTime += Time.deltaTime;
                HitmarkerOpacity = AlphaAnimationCurve.Evaluate(HitmarkerTime);
                HitmarkerCenterOffset = AHitmarkerPlugin.CenterOffset.Value + animationCurve.Evaluate(HitmarkerTime);
                if (firearmController != null && !DebugMode)
                {
                    EHitmarkerPositionMode HitmarkerPositionMode = firearmController.IsAiming ? AHitmarkerPlugin.ADSHitmarkerPositionMode.Value : AHitmarkerPlugin.HitmarkerPositionMode.Value;
                    if (ForceHitmarkerPosition) HitmarkerPositionMode = EHitmarkerPositionMode.Center;
                    switch (HitmarkerPositionMode)
                    {
                        case EHitmarkerPositionMode.Center:
                            HitmarkerPosition = Vector3.zero;
                            break;
                        case EHitmarkerPositionMode.GunDirection:
                            position = firearmController.CurrentFireport.position;
                            weaponDirection = firearmController.WeaponDirection;
                            firearmController.AdjustShotVectors(ref position, ref weaponDirection);
                            Vector2 ScreenPoint = Camera.main.WorldToScreenPoint(position + (weaponDirection * 100)) * FPSCameraSSAARatio;
                            HitmarkerPosition = new Vector2(ScreenPoint.x - (Screen.width / 2), ScreenPoint.y - (Screen.height / 2));
                            break;
                        case EHitmarkerPositionMode.ImpactPoint:
                            Vector2 ScreenPoint2 = Camera.main.WorldToScreenPoint(damageInfo.HitPoint) * FPSCameraSSAARatio;
                            HitmarkerPosition = new Vector2(ScreenPoint2.x - (Screen.width / 2), ScreenPoint2.y - (Screen.height / 2));
                            break;
                        case EHitmarkerPositionMode.ImpactPointStatic:
                            HitmarkerPosition = HitmarkerPositionSnapshot;
                            break;
                    }
                }
                if (AHitmarkerPlugin.StaticHitmarkerOnly.Value)
                {
                    StaticHitmarkerImage.color = new Color(HitmarkerColor.r, HitmarkerColor.g, HitmarkerColor.b, HitmarkerColor.a * HitmarkerOpacity * AHitmarkerPlugin.StaticOpacity.Value);
                    StaticHitmarkerRect.sizeDelta += Vector2.one * AHitmarkerPlugin.StaticSizeDeltaSpeed.Value;
                    StaticHitmarkerRect.localPosition = DebugOffset + HitmarkerPosition;
                    TLHImage.color = Color.clear; TRHImage.color = Color.clear; 
                    BLHImage.color = Color.clear; BRHImage.color = Color.clear;
                    BleedImage.color = new Color(BleedColor.r, BleedColor.g, BleedColor.b, BleedColor.a * HitmarkerOpacity);
                    ArmorHitmarkerRect.localPosition = DebugOffset + HitmarkerPosition + AHitmarkerPlugin.ArmorOffset.Value;
                    ArmorHitmarkerImage.color = new Color(ArmorHitmarkerColor.r, ArmorHitmarkerColor.g, ArmorHitmarkerColor.b, ArmorHitmarkerColor.a * HitmarkerOpacity);
                }
                else
                {
                    StaticHitmarkerRect.sizeDelta += Vector2.one * AHitmarkerPlugin.StaticSizeDeltaSpeed.Value;
                    TLHOffsetAnim = TLHOffset * HitmarkerCenterOffset;
                    TRHOffsetAnim = TRHOffset * HitmarkerCenterOffset;
                    BLHOffsetAnim = BLHOffset * HitmarkerCenterOffset;
                    BRHOffsetAnim = BRHOffset * HitmarkerCenterOffset;
                    TLHRect.localPosition = DebugOffset + HitmarkerPosition + TLHOffsetAnim;
                    TRHRect.localPosition = DebugOffset + HitmarkerPosition + TRHOffsetAnim;
                    BLHRect.localPosition = DebugOffset + HitmarkerPosition + BLHOffsetAnim;
                    BRHRect.localPosition = DebugOffset + HitmarkerPosition + BRHOffsetAnim;
                    BleedRect.localPosition = DebugOffset + HitmarkerPosition;
                    StaticHitmarkerRect.localPosition = DebugOffset + HitmarkerPosition;
                    Color ImageColor = new Color(HitmarkerColor.r, HitmarkerColor.g, HitmarkerColor.b, HitmarkerColor.a * HitmarkerOpacity);
                    TLHImage.color = ImageColor; TRHImage.color = ImageColor; 
                    BLHImage.color = ImageColor; BRHImage.color = ImageColor;
                    BleedImage.color = new Color(BleedColor.r, BleedColor.g, BleedColor.b, BleedColor.a * HitmarkerOpacity);
                    ArmorHitmarkerRect.localPosition = DebugOffset + HitmarkerPosition + AHitmarkerPlugin.ArmorOffset.Value;
                    ArmorHitmarkerImage.color = new Color(ArmorHitmarkerColor.r, ArmorHitmarkerColor.g, ArmorHitmarkerColor.b, ArmorHitmarkerColor.a * HitmarkerOpacity);
                    StaticHitmarkerImage.color = new Color(HitmarkerColor.r, HitmarkerColor.g, HitmarkerColor.b, HitmarkerColor.a * HitmarkerOpacity * AHitmarkerPlugin.StaticOpacity.Value);
                    if ((AHitmarkerPlugin.EnableDamageNumber.Value && DamageNumber > 0.01f) || (AHitmarkerPlugin.EnableArmorDamageNumber.Value && ArmorDamageNumber > 0.01f))
                    {
                        damageNumberRectTransform.localPosition = DebugOffset + new Vector3(AHitmarkerPlugin.DamageRectPosition.Value.x, AHitmarkerPlugin.DamageRectPosition.Value.y, 0f);
                        damageNumberOpacity = 1f;
                        UpdateDamageNumber = true;
                    }
                }
                if (((AHitmarkerPlugin.EnableDamageNumber.Value && DamageNumber > 0.01f) || (AHitmarkerPlugin.EnableArmorDamageNumber.Value && ArmorDamageNumber > 0.01f)) && damageNumberRectTransform != null)
                {
                    switch (AHitmarkerPlugin.DamagePositionMode.Value)
                    {
                        case EDamageNumberPositionMode.Hitmarker:
                            damageNumberRectTransform.localPosition = DebugOffset + (Vector3)AHitmarkerPlugin.DamageRectPosition.Value + HitmarkerPosition;
                            break;
                    }
                }
                if (HitmarkerTime > (AHitmarkerPlugin.AnimatedTime.Value + AHitmarkerPlugin.AnimatedAlphaTime.Value))
                {
                    UpdateHitmarker = false;
                    DebugMode = false;
                    DebugOffset = Vector3.zero;
                    if ((AHitmarkerPlugin.EnableDamageNumber.Value && DamageNumber > 0.01f) || (AHitmarkerPlugin.EnableArmorDamageNumber.Value && ArmorDamageNumber > 0.01f))
                    {
                        damageNumberOpacity = 1f;
                        UpdateDamageNumber = true;
                    }
                }
            }
            if (UpdateDamageNumber)
            {
                if (firearmController != null && !DebugMode)
                {
                    EHitmarkerPositionMode HitmarkerPositionMode = firearmController.IsAiming ? AHitmarkerPlugin.ADSHitmarkerPositionMode.Value : AHitmarkerPlugin.HitmarkerPositionMode.Value;
                    if (ForceHitmarkerPosition) HitmarkerPositionMode = EHitmarkerPositionMode.Center;
                    switch (HitmarkerPositionMode)
                    {
                        case EHitmarkerPositionMode.Center:
                            HitmarkerPosition = Vector3.zero;
                            break;
                        case EHitmarkerPositionMode.GunDirection:
                            position = firearmController.CurrentFireport.position;
                            weaponDirection = firearmController.WeaponDirection;
                            firearmController.AdjustShotVectors(ref position, ref weaponDirection);
                            Vector2 ScreenPoint = Camera.main.WorldToScreenPoint(position + (weaponDirection * 100)) * FPSCameraSSAARatio;
                            HitmarkerPosition = new Vector2(ScreenPoint.x - (Screen.width / 2), ScreenPoint.y - (Screen.height / 2));
                            break;
                        case EHitmarkerPositionMode.ImpactPoint:
                            Vector2 ScreenPoint2 = Camera.main.WorldToScreenPoint(damageInfo.HitPoint) * FPSCameraSSAARatio;
                            HitmarkerPosition = new Vector2(ScreenPoint2.x - (Screen.width / 2), ScreenPoint2.y - (Screen.height / 2));
                            break;
                        case EHitmarkerPositionMode.ImpactPointStatic:
                            HitmarkerPosition = HitmarkerPositionSnapshot;
                            break;
                    }
                    if (((AHitmarkerPlugin.EnableDamageNumber.Value && DamageNumber > 0.01f) || (AHitmarkerPlugin.EnableArmorDamageNumber.Value && ArmorDamageNumber > 0.01f)) && damageNumberRectTransform != null)
                    {
                        switch (AHitmarkerPlugin.DamagePositionMode.Value)
                        {
                            case EDamageNumberPositionMode.Hitmarker:
                                damageNumberRectTransform.localPosition = DebugOffset + (Vector3)AHitmarkerPlugin.DamageRectPosition.Value + HitmarkerPosition;
                                break;
                        }
                    }
                }
                damageNumberOpacity -= Time.deltaTime / AHitmarkerPlugin.DamageAnimationTime.Value;
                if (damageNumberTextMeshPro != null) damageNumberTextMeshPro.alpha = damageNumberOpacity;
                if (damageNumberOpacity < 0)
                {
                    UpdateDamageNumber = false;
                    DamageNumber = 0f;
                    ArmorDamageNumber = 0f;
                }
            }
        }
        public static void Killfeed()
        {
            if (ActiveUIScreen == null) return;

            if (AHitmarkerPlugin.KillChildDirection.Value)
            {
                CreateKillText();
            }
            if (AHitmarkerPlugin.KillUpperText.Value)
            {
                string UpperText = "";
                switch (killLethalDamageType)
                {
                    case EDamageType.LightBleeding:
                    case EDamageType.HeavyBleeding:
                        UpperText = "<color=#" + ColorUtility.ToHtmlStringRGB(AHitmarkerPlugin.BleedColor.Value) + ">" + "BLEEDING" + "</color> ";
                        break;
                    case EDamageType.Poison:
                        UpperText = "<color=#" + ColorUtility.ToHtmlStringRGB(AHitmarkerPlugin.PoisonColor.Value) + ">" + "POISON" + "</color> ";
                        break;
                    default:
                        if (killLethalDamageType.ToString() == "LethalToxin")
                        {
                            UpperText = "<color=#" + ColorUtility.ToHtmlStringRGB(AHitmarkerPlugin.PoisonColor.Value) + ">" + "POISON" + "</color> ";
                        }
                        break;
                }
                if (killBodyPart == EBodyPart.Head)
                {
                    if (AHitmarkerPlugin.KillHeadshotXP.Value == EHeadshotXP.On)
                    {
                        float BaseExp = 0;
                        switch (killPlayerSide)
                        {
                            case EPlayerSide.Usec:
                                BaseExp = VictimLevelExp;
                                break;
                            case EPlayerSide.Bear:
                                BaseExp = VictimLevelExp;
                                break;
                            case EPlayerSide.Savage:
                                BaseExp = killExperience;
                                if (BaseExp < 0)
                                {
                                    BaseExp = VictimBotLevelExp;
                                }
                                break;
                        }
                        UpperText = UpperText + "HEADSHOT " + (int)(BaseExp * Mathf.Max(HeadShotMult - 1f, 0)) + "XP";
                    }
                    else
                    {
                        UpperText = UpperText + "HEADSHOT";
                    }
                }
                if (UpperText != "")
                {
                    CreateUpperText(UpperText, (int)(AHitmarkerPlugin.KillFontSize.Value * 0.75), AHitmarkerPlugin.KillTime.Value, AHitmarkerPlugin.KillOpacitySpeed.Value);
                }
                if (killBodyPart == EBodyPart.Head && killDistance >= LongShotDistance)
                {
                    CreateUpperText("LONGSHOT", (int)(AHitmarkerPlugin.KillFontSize.Value * 0.75), AHitmarkerPlugin.KillTime.Value, AHitmarkerPlugin.KillOpacitySpeed.Value);
                }
            }
            if (!AHitmarkerPlugin.KillChildDirection.Value)
            {
                CreateKillText();
            }
        }
        public static void MultiKillfeed()
        {
            if (!AHitmarkerPlugin.EnableMultiKillfeed.Value) return;
            Sprite sprite = null;
            MultiKillfeedColor = AHitmarkerPlugin.MultiKillfeedColor.Value;
            if (killBodyPart == EBodyPart.Head && AHitmarkerPlugin.MultiKillfeedColorMode.Value != EMultiKillfeedColorMode.SingleColor)
            {
                MultiKillfeedColor = AHitmarkerPlugin.MultiKillfeedHeadshotColor.Value;
            }
            if (killPlayerSide == EPlayerSide.Usec || killPlayerSide == EPlayerSide.Bear)
            {
                switch (AHitmarkerPlugin.MultiKillfeedPMCIconMode.Value)
                {
                    case EMultiKillfeedPMCMode.Generic:
                        sprite = LoadedSprites[AHitmarkerPlugin.MultiKillfeedGenericShape.Value];
                        break;
                    case EMultiKillfeedPMCMode.Custom:
                        switch (killPlayerSide)
                        {
                            case EPlayerSide.Usec:
                                sprite = LoadedSprites[AHitmarkerPlugin.MultiKillfeedUsecShape.Value];
                                break;
                            case EPlayerSide.Bear:
                                sprite = LoadedSprites[AHitmarkerPlugin.MultiKillfeedBearShape.Value];
                                break;
                        }
                        break;
                    case EMultiKillfeedPMCMode.Ranks:
                        int num = (killLevel > 80) ? 80 : (((int)((float)killLevel / 5f) + 1) * 5);
                        if (LoadedRanks.ContainsKey("Rank" + num + ".png")) sprite = LoadedRanks["Rank" + num + ".png"];
                        break;
                }
            }
            if (killPlayerSide == EPlayerSide.Savage)
            {
                sprite = LoadedSprites[AHitmarkerPlugin.MultiKillfeedGenericShape.Value];
            }
            switch (killLethalDamageType)
            {
                case EDamageType.GrenadeFragment:
                case EDamageType.Explosion:
                    if (AHitmarkerPlugin.MultiKillfeedColorMode.Value == EMultiKillfeedColorMode.Colored) MultiKillfeedColor = AHitmarkerPlugin.ThrowWeaponColor.Value;
                    break;
            }
            if (killPlayerSide == EPlayerSide.Savage)
            {
                string RoleName = AmandsHitmarkerHelper.Localized(AmandsHitmarkerHelper.GetScavRoleKey(killRole), EStringCase.Upper);
                switch (RoleName)
                {
                    case "BLOODHOUND":
                        if (AHitmarkerPlugin.MultiKillfeedColorMode.Value == EMultiKillfeedColorMode.Colored) MultiKillfeedColor = AHitmarkerPlugin.BloodhoundColor.Value;
                        break;
                    case "RAIDER":
                        if (AHitmarkerPlugin.MultiKillfeedColorMode.Value == EMultiKillfeedColorMode.Colored) MultiKillfeedColor = AHitmarkerPlugin.RaiderColor.Value;
                        break;
                    default:
                        if (AmandsHitmarkerHelper.IsFollower(killRole) && AHitmarkerPlugin.MultiKillfeedColorMode.Value == EMultiKillfeedColorMode.Colored) MultiKillfeedColor = AHitmarkerPlugin.FollowerColor.Value;
                        if (AmandsHitmarkerHelper.IsBoss(killRole) || AmandsHitmarkerHelper.CountAsBoss(killRole))
                        {
                            if (AHitmarkerPlugin.MultiKillfeedColorMode.Value == EMultiKillfeedColorMode.Colored) MultiKillfeedColor = AHitmarkerPlugin.BossColor.Value;
                            sprite = LoadedSprites[AHitmarkerPlugin.MultiKillfeedGenericShape.Value];
                        }
                        break;
                }
            }
            GameObject amandsAnimatedImageGameObject = new GameObject("Multikill");
            amandsAnimatedImageGameObject.transform.SetParent(multiKillfeedGameObject.transform);
            amandsAnimatedImageGameObject.transform.SetSiblingIndex(0);
            AmandsAnimatedImage amandsAnimatedImage = amandsAnimatedImageGameObject.AddComponent<AmandsAnimatedImage>();
            amandsAnimatedImage.color = MultiKillfeedColor;
            amandsAnimatedImage.sprite = sprite;
            amandsAnimatedImages.Add(amandsAnimatedImage);
        }
        public static void CreateGameObjects(Transform parent)
        {
            damageNumberGameObject = new GameObject("damageNumber");
            damageNumberRectTransform = damageNumberGameObject.AddComponent<RectTransform>();
            damageNumberGameObject.transform.SetParent(parent);
            damageNumberRectTransform.anchorMin = Vector2.zero;
            damageNumberRectTransform.anchorMax = Vector2.zero;
            damageNumberRectTransform.sizeDelta = new Vector2(0f, 0f);
            damageNumberRectTransform.localPosition = new Vector3(AHitmarkerPlugin.DamageRectPosition.Value.x, AHitmarkerPlugin.DamageRectPosition.Value.y, 0f);
            damageNumberRectTransform.pivot = AHitmarkerPlugin.DamageRectPivot.Value;
            damageNumberTextMeshPro = damageNumberGameObject.AddComponent<TextMeshProUGUI>();
            damageNumberTextMeshPro.fontSize = AHitmarkerPlugin.DamageFontSize.Value;
            damageNumberTextMeshPro.outlineWidth = AHitmarkerPlugin.DamageFontOutline.Value;
            damageNumberTextMeshPro.fontStyle = FontStyles.UpperCase;
            damageNumberTextMeshPro.alignment = TextAlignmentOptions.Center;
            damageNumberTextMeshPro.alpha = 0f;

            killListGameObject = new GameObject("killList");
            rectTransform = killListGameObject.AddComponent<RectTransform>();
            killListGameObject.transform.SetParent(parent);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, 0f);
            verticalLayoutGroup = killListGameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childControlHeight = false;
            ContentSizeFitter contentSizeFitter = killListGameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UpdateKillfeed(null,null);

            multiKillfeedGameObject = new GameObject("multiKillList");
            multiKillfeedrectTransform = multiKillfeedGameObject.AddComponent<RectTransform>();
            multiKillfeedGameObject.transform.SetParent(parent);
            multiKillfeedrectTransform.anchorMin = Vector2.zero;
            multiKillfeedrectTransform.anchorMax = Vector2.zero;
            multiKillfeedrectTransform.sizeDelta = new Vector2(0f, 0f);
            horizontalLayoutGroup = multiKillfeedGameObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.spacing = AHitmarkerPlugin.MultiKillfeedChildSpacing.Value;
            horizontalLayoutGroup.childForceExpandHeight = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childControlHeight = false;
            horizontalLayoutGroup.childControlWidth = false;
            ContentSizeFitter contentSizeFitter2 = multiKillfeedGameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter2.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter2.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            multiKillfeedrectTransform.localPosition = new Vector3(AHitmarkerPlugin.MultiKillfeedRectPosition.Value.x - (AHitmarkerPlugin.MultiKillfeedSize.Value.x / (2f * (Screen.height / 1080))), AHitmarkerPlugin.MultiKillfeedRectPosition.Value.y, 0f);
            multiKillfeedrectTransform.pivot = AHitmarkerPlugin.MultiKillfeedRectPivot.Value;

            raidKillListGameObject = new GameObject("killList");
            raidKillrectTransform = raidKillListGameObject.AddComponent<RectTransform>();
            raidKillListGameObject.transform.SetParent(parent);
            raidKillrectTransform.anchorMin = Vector2.zero;
            raidKillrectTransform.anchorMax = Vector2.zero;
            raidKillrectTransform.sizeDelta = new Vector2(0f, 0f);
            raidKillverticalLayoutGroup = raidKillListGameObject.AddComponent<VerticalLayoutGroup>();
            raidKillverticalLayoutGroup.childControlHeight = false;
            ContentSizeFitter contentSizeFitter3 = raidKillListGameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter3.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter3.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UpdateRaidKillfeed(null, null);

            ArmorHitmarker = new GameObject("ArmorHitmarker");
            ArmorHitmarkerRect = ArmorHitmarker.AddComponent<RectTransform>();
            ArmorHitmarkerImage = ArmorHitmarker.AddComponent<Image>();
            ArmorHitmarker.transform.SetParent(parent);
            ArmorHitmarkerImage.sprite = LoadedSprites[AHitmarkerPlugin.ArmorShape.Value];
            ArmorHitmarkerImage.raycastTarget = false;
            ArmorHitmarkerImage.color = Color.clear;

            BleedHitmarker = new GameObject("BleedHitmarker");
            BleedRect = BleedHitmarker.AddComponent<RectTransform>();
            BleedImage = BleedHitmarker.AddComponent<Image>();
            BleedHitmarker.transform.SetParent(parent);
            BleedImage.sprite = LoadedSprites["BleedHitmarker.png"];
            BleedImage.raycastTarget = false;
            BleedImage.color = Color.clear;

            StaticHitmarker = new GameObject("StaticHitmarker");
            StaticHitmarkerRect = StaticHitmarker.AddComponent<RectTransform>();
            StaticHitmarkerImage = StaticHitmarker.AddComponent<Image>();
            StaticHitmarker.transform.SetParent(parent);
            StaticHitmarkerImage.sprite = LoadedSprites["StaticHitmarker.png"];
            StaticHitmarkerImage.raycastTarget = false;
            StaticHitmarkerImage.color = Color.clear;

            TLH = new GameObject("TLH");
            TLHRect = TLH.AddComponent<RectTransform>();
            TLHImage = TLH.AddComponent<Image>();
            TLH.transform.SetParent(parent);
            TLHImage.sprite = LoadedSprites[AHitmarkerPlugin.Shape.Value];
            TLHImage.raycastTarget = false;
            TLHImage.color = Color.clear;
            TLHRect.localRotation = Quaternion.Euler(0, 0, 45);

            TRH = new GameObject("TRH");
            TRHRect = TRH.AddComponent<RectTransform>();
            TRHImage = TRH.AddComponent<Image>();
            TRH.transform.SetParent(parent);
            TRHImage.sprite = LoadedSprites[AHitmarkerPlugin.Shape.Value];
            TRHImage.raycastTarget = false;
            TRHImage.color = Color.clear;
            TRHRect.localRotation = Quaternion.Euler(0, 0, -45);

            BLH = new GameObject("BLH");
            BLHRect = BLH.AddComponent<RectTransform>();
            BLHImage = BLH.AddComponent<Image>();
            BLH.transform.SetParent(parent);
            BLHImage.sprite = LoadedSprites[AHitmarkerPlugin.Shape.Value];
            BLHImage.raycastTarget = false;
            BLHImage.color = Color.clear;
            BLHRect.localRotation = Quaternion.Euler(0, 0, -45);

            BRH = new GameObject("BRH");
            BRHRect = BRH.AddComponent<RectTransform>();
            BRHImage = BRH.AddComponent<Image>();
            BRH.transform.SetParent(parent);
            BRHImage.sprite = LoadedSprites[AHitmarkerPlugin.Shape.Value];
            BRHImage.raycastTarget = false;
            BRHImage.color = Color.clear;
            BRHRect.localRotation = Quaternion.Euler(0, 0, 45);
        }
        public static void DestroyGameObjects()
        {
            if (damageNumberGameObject != null) Destroy(damageNumberGameObject);
            if (killListGameObject != null) Destroy(killListGameObject);
            if (multiKillfeedGameObject != null) Destroy(multiKillfeedGameObject);
            if (raidKillListGameObject != null) Destroy(raidKillListGameObject);
            if (ArmorHitmarker != null) Destroy(ArmorHitmarker);
            if (BleedHitmarker != null) Destroy(BleedHitmarker);
            if (StaticHitmarker != null) Destroy(StaticHitmarker);
            if (TLH != null) Destroy(TLH);
            if (TRH != null) Destroy(TRH);
            if (BLH != null) Destroy(BLH);
            if (BRH != null) Destroy(BRH);
        }
        public static void CreateUpperText(string text, int fontSize, float time, float OpacitySpeed)
        {
            if (!AHitmarkerPlugin.EnableKillfeed.Value) return;
            GameObject TextGameObject = new GameObject("TextGameObject");
            TextGameObject.transform.SetParent(killListGameObject.transform);
            if (AHitmarkerPlugin.KillChildDirection.Value)
            {
                TextGameObject.transform.SetSiblingIndex(0);
            }
            AmandsKillfeedText TempAmandsAnimatedText = TextGameObject.AddComponent<AmandsKillfeedText>();
            TempAmandsAnimatedText.text = text;
            TempAmandsAnimatedText.color = AHitmarkerPlugin.KillTextColor.Value;
            TempAmandsAnimatedText.fontSize = fontSize;
            TempAmandsAnimatedText.outlineWidth = AHitmarkerPlugin.KillFontOutline.Value;
            TempAmandsAnimatedText.time = time;
            TempAmandsAnimatedText.OpacitySpeed = OpacitySpeed;
            TempAmandsAnimatedText.textAlignmentOptions = AHitmarkerPlugin.KillTextAlignment.Value;
            LastAmandsKillfeedText = TempAmandsAnimatedText;
            //Destroy(TextGameObject, time + 10);
        }
        public static void CreateKillText()
        {
            if (!AHitmarkerPlugin.EnableKillfeed.Value) return;

            KillfeedColor = AHitmarkerPlugin.KillTextColor.Value;
            switch (killPlayerSide)
            {
                case EPlayerSide.Usec:
                    KillfeedColor = AHitmarkerPlugin.UsecColor.Value;
                    break;
                case EPlayerSide.Bear:
                    KillfeedColor = AHitmarkerPlugin.BearColor.Value;
                    break;
                case EPlayerSide.Savage:
                    KillfeedColor = AHitmarkerPlugin.ScavColor.Value;
                    break;
            }
            switch (killLethalDamageType)
            {
                case EDamageType.GrenadeFragment:
                case EDamageType.Explosion:
                    KillfeedColor = AHitmarkerPlugin.ThrowWeaponColor.Value;
                    break;
            }
            /*if (killPlayerSide == EPlayerSide.Savage)
            {
                if (AmandsHitmarkerHelper.IsFollower(killRole)) KillfeedColor = AHitmarkerPlugin.FollowerColor.Value;
                if (AmandsHitmarkerHelper.IsBoss(killRole) || AmandsHitmarkerHelper.CountAsBoss(killRole)) KillfeedColor = AHitmarkerPlugin.BossColor.Value;
            }*/
            string Start = "";
            string RoleName = "";
            Color RoleColor = Color.white;
            string Name = "";
            string End = "";
            switch (killPlayerSide)
            {
                case EPlayerSide.Usec:
                    RoleName = "USEC";
                    RoleColor = AHitmarkerPlugin.UsecColor.Value;
                    break;
                case EPlayerSide.Bear:
                    RoleName = "BEAR";
                    RoleColor = AHitmarkerPlugin.BearColor.Value;
                    break;
                case EPlayerSide.Savage:
                    RoleColor = AHitmarkerPlugin.ScavColor.Value;
                    break;
            }
            if (killPlayerSide == EPlayerSide.Savage)
            {
                RoleName = AmandsHitmarkerHelper.Localized(AmandsHitmarkerHelper.GetScavRoleKey(killRole), EStringCase.Upper);
                switch (RoleName)
                {
                    case "BLOODHOUND":
                        RoleColor = AHitmarkerPlugin.BloodhoundColor.Value;
                        KillfeedColor = RoleColor;
                        break;
                    case "RAIDER":
                        RoleColor = AHitmarkerPlugin.RaiderColor.Value;
                        KillfeedColor = RoleColor;
                        break;
                    default:
                        if (AmandsHitmarkerHelper.IsFollower(killRole)) RoleColor = AHitmarkerPlugin.FollowerColor.Value;
                        if (AmandsHitmarkerHelper.IsBoss(killRole) || AmandsHitmarkerHelper.CountAsBoss(killRole)) RoleColor = AHitmarkerPlugin.BossColor.Value;
                        KillfeedColor = RoleColor;
                        break;
                }
            }
            switch (AHitmarkerPlugin.KillNameColor.Value)
            {
                case EKillNameColor.None:
                    RoleColor = AHitmarkerPlugin.KillTextColor.Value;
                    break;
                case EKillNameColor.SingleColor:
                    RoleColor = AHitmarkerPlugin.KillNameSingleColor.Value;
                    break;
            }
            switch (AHitmarkerPlugin.KillStart.Value)
            {
                case EKillStart.PlayerWeapon:
                    if (localPlayer != null)
                    {
                        aggNickname = localPlayer.Profile.Nickname;
                        aggPlayerSide = localPlayer.Profile.Side;
                    }
                    Color playerRoleColor = AHitmarkerPlugin.KillTextColor.Value;
                    switch (AHitmarkerPlugin.KillNameColor.Value)
                    {
                        case EKillNameColor.SingleColor:
                            playerRoleColor = AHitmarkerPlugin.KillNameSingleColor.Value;
                            break;
                        case EKillNameColor.Colored:
                            switch (aggPlayerSide)
                            {
                                case EPlayerSide.Usec:
                                    playerRoleColor = AHitmarkerPlugin.UsecColor.Value;
                                    break;
                                case EPlayerSide.Bear:
                                    playerRoleColor = AHitmarkerPlugin.BearColor.Value;
                                    break;
                                case EPlayerSide.Savage:
                                    playerRoleColor = AHitmarkerPlugin.ScavColor.Value;
                                    break;
                            }
                            break;
                    }
                    Start = "<b><color=#" + ColorUtility.ToHtmlStringRGB(playerRoleColor) + ">" + aggNickname + "</color> " + killWeaponName + " </b> ";
                    break;
                case EKillStart.Weapon:
                    Start = "<b>" + killWeaponName + "</b> ";
                    break;
                case EKillStart.WeaponRole:
                    Start = "<b>" + killWeaponName + " <color=#" + ColorUtility.ToHtmlStringRGB(RoleColor) + ">" + RoleName + "</color></b> ";
                    break;
            }
            switch (AHitmarkerPlugin.KillNameColor.Value)
            {
                case EKillNameColor.None:
                    Name = (killPlayerSide == EPlayerSide.Savage ? AmandsHitmarkerHelper.Transliterate(killPlayerName) : killPlayerName) + " ";
                    break;
                case EKillNameColor.SingleColor:
                    Name = "<color=#" + ColorUtility.ToHtmlStringRGB(AHitmarkerPlugin.KillNameSingleColor.Value) + ">" + (killPlayerSide == EPlayerSide.Savage ? AmandsHitmarkerHelper.Transliterate(killPlayerName) : killPlayerName) + "</color> ";
                    break;
                case EKillNameColor.Colored:
                    Name = "<color=#" + ColorUtility.ToHtmlStringRGB(KillfeedColor) + ">" + (killPlayerSide == EPlayerSide.Savage ? AmandsHitmarkerHelper.Transliterate(killPlayerName) : killPlayerName) + "</color> ";
                    break;
            }
            if (killDistance > AHitmarkerPlugin.KillDistanceThreshold.Value && !(AHitmarkerPlugin.KillEnd.Value == EKillEnd.Distance || AHitmarkerPlugin.KillEnd.Value == EKillEnd.None))
            {
                End = "<b>" + ((int)killDistance) + "M</b>";
            }
            else
            {
                EKillEnd killEnd = AHitmarkerPlugin.KillEnd.Value;
                if (killEnd == EKillEnd.Level && killPlayerSide == EPlayerSide.Savage) killEnd = EKillEnd.Experience;
                switch (killEnd)
                {
                    case EKillEnd.Bodypart:
                        End = "<b>" + killBodyPart + "</b>";
                        break;
                    case EKillEnd.Role:
                        End = "<b>" + "<color=#" + ColorUtility.ToHtmlStringRGB(RoleColor) + ">" + RoleName + "</color></b>";
                        break;
                    case EKillEnd.Experience:
                        float BaseExp = 0;
                        float HeadshotExp = 0;
                        float StreakExp = 0;
                        switch (killPlayerSide)
                        {
                            case EPlayerSide.Usec:
                                BaseExp = VictimLevelExp;
                                break;
                            case EPlayerSide.Bear:
                                BaseExp = VictimLevelExp;
                                break;
                            case EPlayerSide.Savage:
                                BaseExp = killExperience;
                                if (BaseExp < 0)
                                {
                                    BaseExp = VictimBotLevelExp;
                                }
                                break;
                        }
                        if (killBodyPart == EBodyPart.Head && AHitmarkerPlugin.KillHeadshotXP.Value == EHeadshotXP.OnFormula)
                        {
                            HeadshotExp = (int)((float)BaseExp * Mathf.Max(HeadShotMult - 1f,0));
                        }
                        if (AHitmarkerPlugin.KillStreakXP.Value)
                        {
                            if (Combo.Count != 0)
                            {
                                StreakExp = (int)((float)BaseExp * ((float)GetKillingBonusPercent(Kills) / 100f));
                            }
                        }
                        End = "<b>" + (int)(BaseExp + HeadshotExp + StreakExp) + "XP</b>";
                        break;
                    case EKillEnd.Distance:
                        End = "<b>" + ((int)killDistance) + "M</b>";
                        break;
                    case EKillEnd.DamageType:
                        End = "<b>" + killLethalDamageType + "</b>";
                        break;
                    case EKillEnd.Level:
                        End = "<b>Level " + killLevel + "</b>";
                        break;
                }
            }
            GameObject TextGameObject = new GameObject("KillTextGameObject");
            TextGameObject.transform.SetParent(killListGameObject.transform);
            if (AHitmarkerPlugin.KillChildDirection.Value)
            {
                TextGameObject.transform.SetSiblingIndex(0);
            }
            AmandsKillfeedText TempAmandsAnimatedText = TextGameObject.AddComponent<AmandsKillfeedText>();
            TempAmandsAnimatedText.text = Start + Name + End;
            TempAmandsAnimatedText.color = AHitmarkerPlugin.KillTextColor.Value;
            TempAmandsAnimatedText.fontSize = AHitmarkerPlugin.KillFontSize.Value;
            TempAmandsAnimatedText.outlineWidth = AHitmarkerPlugin.KillFontOutline.Value;
            if (AHitmarkerPlugin.KillFontUpperCase.Value)
            {
                TempAmandsAnimatedText.fontStyles = FontStyles.UpperCase;
            }
            TempAmandsAnimatedText.time = AHitmarkerPlugin.KillTime.Value;
            TempAmandsAnimatedText.OpacitySpeed = AHitmarkerPlugin.KillOpacitySpeed.Value;
            TempAmandsAnimatedText.textAlignmentOptions = AHitmarkerPlugin.KillTextAlignment.Value;
            LastAmandsKillfeedText = TempAmandsAnimatedText;
            //Destroy(TextGameObject, AHitmarkerPlugin.KillTime.Value + 10);
        }
        public static void RaidKillfeed(EPlayerSide aggressorSide, WildSpawnType aggressorRole, string aggressorNickname, string weaponName, EDamageType lethalDamageType, EPlayerSide victimSide, WildSpawnType victimRole, string victimNickname)
        {
            if (!AHitmarkerPlugin.EnableRaidKillfeed.Value) return;

            string Start = "";
            string aggressorRoleName = "";
            Color aggressorColor = AHitmarkerPlugin.KillTextColor.Value;
            switch (aggressorSide)
            {
                case EPlayerSide.Usec:
                    aggressorRoleName = "USEC";
                    aggressorColor = AHitmarkerPlugin.UsecColor.Value;
                    break;
                case EPlayerSide.Bear:
                    aggressorRoleName = "BEAR";
                    aggressorColor = AHitmarkerPlugin.BearColor.Value;
                    break;
                case EPlayerSide.Savage:
                    aggressorColor = AHitmarkerPlugin.ScavColor.Value;
                    break;
            }
            if (aggressorSide == EPlayerSide.Savage)
            {
                aggressorRoleName = AmandsHitmarkerHelper.Localized(AmandsHitmarkerHelper.GetScavRoleKey(aggressorRole), EStringCase.Upper);
                switch (aggressorRoleName)
                {
                    case "BLOODHOUND":
                        aggressorColor = AHitmarkerPlugin.BloodhoundColor.Value;
                        break;
                    case "RAIDER":
                        aggressorColor = AHitmarkerPlugin.RaiderColor.Value;
                        break;
                    default:
                        if (AmandsHitmarkerHelper.IsFollower(aggressorRole)) aggressorColor = AHitmarkerPlugin.FollowerColor.Value;
                        if (AmandsHitmarkerHelper.IsBoss(aggressorRole) || AmandsHitmarkerHelper.CountAsBoss(aggressorRole)) aggressorColor = AHitmarkerPlugin.BossColor.Value;
                        break;
                }
            }
            string End;
            string victimRoleName = "";
            Color victimColor = AHitmarkerPlugin.KillTextColor.Value;
            switch (victimSide)
            {
                case EPlayerSide.Usec:
                    victimRoleName = "USEC";
                    victimColor = AHitmarkerPlugin.UsecColor.Value;
                    break;
                case EPlayerSide.Bear:
                    victimRoleName = "BEAR";
                    victimColor = AHitmarkerPlugin.BearColor.Value;
                    break;
                case EPlayerSide.Savage:
                    victimColor = AHitmarkerPlugin.ScavColor.Value;
                    break;
            }
            if (victimSide == EPlayerSide.Savage)
            {
                victimRoleName = AmandsHitmarkerHelper.Localized(AmandsHitmarkerHelper.GetScavRoleKey(victimRole), EStringCase.Upper);
                switch (victimRoleName)
                {
                    case "BLOODHOUND":
                        victimColor = AHitmarkerPlugin.BloodhoundColor.Value;
                        break;
                    case "RAIDER":
                        victimColor = AHitmarkerPlugin.RaiderColor.Value;
                        break;
                    default:
                        if (AmandsHitmarkerHelper.IsFollower(victimRole)) victimColor = AHitmarkerPlugin.FollowerColor.Value;
                        if (AmandsHitmarkerHelper.IsBoss(victimRole) || AmandsHitmarkerHelper.CountAsBoss(victimRole)) victimColor = AHitmarkerPlugin.BossColor.Value;
                        break;
                }
            }

            switch (AHitmarkerPlugin.RaidKillNameColor.Value)
            {
                case EKillNameColor.None:
                    aggressorColor = AHitmarkerPlugin.KillTextColor.Value;
                    victimColor = AHitmarkerPlugin.KillTextColor.Value;
                    break;
                case EKillNameColor.SingleColor:
                    aggressorColor = AHitmarkerPlugin.KillNameSingleColor.Value;
                    victimColor = AHitmarkerPlugin.KillNameSingleColor.Value;
                    break;
            }
            if (AHitmarkerPlugin.RaidKillRole.Value)
            {
                Start = "<b><color=#" + ColorUtility.ToHtmlStringRGB(aggressorColor) + ">" + aggressorRoleName + "</b> " + aggressorNickname + "</color> " + "<b>" + weaponName + "</b> ";
                if (AHitmarkerPlugin.RaidKillNameColor.Value == EKillNameColor.Colored && (lethalDamageType == EDamageType.GrenadeFragment || lethalDamageType == EDamageType.Explosion))
                {
                    End = "<b><color=#" + ColorUtility.ToHtmlStringRGB(victimColor) + ">" + victimRoleName + "</b></color><color=#" + ColorUtility.ToHtmlStringRGB(AHitmarkerPlugin.ThrowWeaponColor.Value) + "> " + victimNickname;
                }
                else
                {
                    End = "<b><color=#" + ColorUtility.ToHtmlStringRGB(victimColor) + ">" + victimRoleName + "</b> " + victimNickname + "</color>";
                }
            }
            else
            {
                Start = "<color=#" + ColorUtility.ToHtmlStringRGB(aggressorColor) + ">" + aggressorNickname + "</color> " + "<b>" + weaponName + "</b> ";
                End = "<color=#" + ColorUtility.ToHtmlStringRGB(victimColor) + ">" + victimNickname + "</color>";
            }

            GameObject TextGameObject = new GameObject("RaidKillTextGameObject");
            TextGameObject.transform.SetParent(raidKillListGameObject.transform);
            if (AHitmarkerPlugin.RaidKillChildDirection.Value)
            {
                TextGameObject.transform.SetSiblingIndex(0);
            }
            AmandsRaidKillfeedText tempAmandsRaidKillfeedText = TextGameObject.AddComponent<AmandsRaidKillfeedText>();
            tempAmandsRaidKillfeedText.text = Start + End;
            tempAmandsRaidKillfeedText.color = AHitmarkerPlugin.KillTextColor.Value;
            tempAmandsRaidKillfeedText.fontSize = AHitmarkerPlugin.RaidKillFontSize.Value;
            tempAmandsRaidKillfeedText.outlineWidth = AHitmarkerPlugin.RaidKillFontOutline.Value;
            if (AHitmarkerPlugin.KillFontUpperCase.Value)
            {
                tempAmandsRaidKillfeedText.fontStyles = FontStyles.UpperCase;
            }
            tempAmandsRaidKillfeedText.time = AHitmarkerPlugin.RaidKillTime.Value;
            tempAmandsRaidKillfeedText.OpacitySpeed = AHitmarkerPlugin.KillOpacitySpeed.Value;
            tempAmandsRaidKillfeedText.textAlignmentOptions = AHitmarkerPlugin.RaidKillTextAlignment.Value;
            LastAmandsRaidKillfeedText = tempAmandsRaidKillfeedText;
            //Destroy(TextGameObject, AHitmarkerPlugin.RaidKillTime.Value + 10);
        }
        public static void ReloadUI()
        {
            DestroyGameObjects();
            CreateGameObjects(ActiveUIScreen.transform);
        }
        public static void ReloadFiles()
        {
            string[] Files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Hitmarker/images/", "*.png");
            foreach (string File in Files)
            {
                LoadSprite(File);
            }
            string[] RankFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Hitmarker/ranks/", "*.png");
            foreach (string File in RankFiles)
            {
                LoadRanks(File);
            }
            string[] AudioFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Hitmarker/sounds/");
            foreach (string File in AudioFiles)
            {
                LoadAudioClip(File);
            }
        }
        async static void LoadSprite(string path)
        {
            LoadedSprites[Path.GetFileName(path)] = await RequestSprite(path);
        }
        async static void LoadRanks(string path)
        {
            LoadedRanks[Path.GetFileName(path)] = await RequestSprite(path);
        }
        async static Task<Sprite> RequestSprite(string path)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
            var SendWeb = www.SendWebRequest();

            while (!SendWeb.isDone)
                await Task.Yield();

            if (www.isNetworkError || www.isHttpError)
            {
                return null;
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                return sprite;
            }
        }
        async static void LoadAudioClip(string path)
        {
            LoadedAudioClips[Path.GetFileName(path)] = await RequestAudioClip(path);
        }
        async static Task<AudioClip> RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            AudioType audioType = AudioType.WAV;
            switch (extension)
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
            }
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            var SendWeb = www.SendWebRequest();

            while (!SendWeb.isDone)
                await Task.Yield();

            if (www.isNetworkError || www.isHttpError)
            {
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(www);
                return audioclip;
            }
        }
        public static void HitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            System.Random rnd = new System.Random();
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = ActiveUIScreen != null;
        }
        public static void HeadshotHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            System.Random rnd = new System.Random();
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Head;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = ActiveUIScreen != null;
        }
        public static void BleedHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            killRole = WildSpawnType.assault;
            killPlayerSide = EPlayerSide.Savage;
            killDistance = 25;
            System.Random rnd = new System.Random();
            killWeaponName = DebugWeapons[rnd.Next(DebugWeapons.Count)];
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            aggNickname = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killBodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.HeavyBleeding;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            killHitmarker = ActiveUIScreen != null;
            Killfeed();
            MultiKillfeed();
            RaidKillfeed(EPlayerSide.Usec, WildSpawnType.bossKnight, aggNickname, killWeaponName, EDamageType.HeavyBleeding, EPlayerSide.Savage, WildSpawnType.assault, killPlayerName);

        }
        public static void PoisonHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            killRole = WildSpawnType.assault;
            killPlayerSide = EPlayerSide.Savage;
            killDistance = 25;
            System.Random rnd = new System.Random();
            killWeaponName = DebugWeapons[rnd.Next(DebugWeapons.Count)];
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            aggNickname = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killBodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Poison;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            killHitmarker = ActiveUIScreen != null;
            Killfeed();
            MultiKillfeed();
            RaidKillfeed(EPlayerSide.Usec, WildSpawnType.bossKnight, aggNickname, killWeaponName, EDamageType.Poison, EPlayerSide.Savage, WildSpawnType.assault, killPlayerName);
        }
        public static void ArmorHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            System.Random rnd = new System.Random();
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = true;
            armorHitmarker = ActiveUIScreen != null;
        }
        public static void ArmorBreakHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            System.Random rnd = new System.Random();
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = true;
            armorHitmarker = ActiveUIScreen != null;
            armorBreak = ActiveUIScreen != null;
        }
        public static void UsecHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            killRole = WildSpawnType.bossKnight;
            killPlayerSide = EPlayerSide.Usec;
            killDistance = 25;
            System.Random rnd = new System.Random();
            killWeaponName = DebugWeapons[rnd.Next(DebugWeapons.Count)];
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            aggNickname = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killBodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = ActiveUIScreen != null;
            killHitmarker = ActiveUIScreen != null;
            Killfeed();
            MultiKillfeed();
            RaidKillfeed(EPlayerSide.Usec, WildSpawnType.bossKnight, aggNickname, killWeaponName, EDamageType.Bullet, EPlayerSide.Usec, WildSpawnType.bossKnight, killPlayerName);
        }
        public static void BearHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            killRole = WildSpawnType.followerBigPipe;
            killPlayerSide = EPlayerSide.Bear;
            killDistance = 25;
            System.Random rnd = new System.Random();
            killWeaponName = DebugWeapons[rnd.Next(DebugWeapons.Count)];
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            aggNickname = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killBodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = ActiveUIScreen != null;
            killHitmarker = ActiveUIScreen != null;
            Killfeed();
            MultiKillfeed();
            RaidKillfeed(EPlayerSide.Usec, WildSpawnType.bossKnight, aggNickname, killWeaponName, EDamageType.Bullet, EPlayerSide.Bear, WildSpawnType.followerBigPipe, killPlayerName);
        }
        public static void ScavHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            killRole = WildSpawnType.assault;
            killPlayerSide = EPlayerSide.Savage;
            killDistance = 25;
            System.Random rnd = new System.Random();
            killWeaponName = DebugWeapons[rnd.Next(DebugWeapons.Count)];
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            aggNickname = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killBodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = ActiveUIScreen != null;
            killHitmarker = ActiveUIScreen != null;
            Killfeed();
            MultiKillfeed();
            RaidKillfeed(EPlayerSide.Usec, WildSpawnType.bossKnight, aggNickname, killWeaponName, EDamageType.Bullet, EPlayerSide.Savage, WildSpawnType.assault, killPlayerName);
        }
        public static void ThrowWeaponHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            killRole = WildSpawnType.assault;
            killPlayerSide = EPlayerSide.Savage;
            killDistance = 25;
            System.Random rnd = new System.Random();
            killWeaponName = DebugWeapons[rnd.Next(DebugWeapons.Count)];
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            aggNickname = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killBodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.GrenadeFragment;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = ActiveUIScreen != null;
            killHitmarker = ActiveUIScreen != null;
            Killfeed();
            MultiKillfeed();
            RaidKillfeed(EPlayerSide.Usec, WildSpawnType.bossKnight, aggNickname, killWeaponName, EDamageType.GrenadeFragment, EPlayerSide.Savage, WildSpawnType.assault, killPlayerName);
        }
        public static void FollowerHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            killRole = WildSpawnType.followerBully;
            killPlayerSide = EPlayerSide.Savage;
            killExperience = 100;
            killDistance = 25;
            System.Random rnd = new System.Random();
            killWeaponName = DebugWeapons[rnd.Next(DebugWeapons.Count)];
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            aggNickname = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killBodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = ActiveUIScreen != null;
            killHitmarker = ActiveUIScreen != null;
            Killfeed();
            MultiKillfeed();
            RaidKillfeed(EPlayerSide.Usec, WildSpawnType.bossKnight, aggNickname, killWeaponName, EDamageType.Bullet, EPlayerSide.Savage, WildSpawnType.followerBully, killPlayerName);
        }
        public static void BossHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            killRole = WildSpawnType.bossKnight;
            killPlayerSide = EPlayerSide.Savage;
            killExperience = 100;
            killDistance = 25;
            System.Random rnd = new System.Random();
            killWeaponName = DebugWeapons[rnd.Next(DebugWeapons.Count)];
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            aggNickname = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killBodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = ActiveUIScreen != null;
            killHitmarker = ActiveUIScreen != null;
            Killfeed();
            MultiKillfeed();
            RaidKillfeed(EPlayerSide.Usec, WildSpawnType.bossKnight, aggNickname, killWeaponName, EDamageType.Bullet, EPlayerSide.Savage, WildSpawnType.bossKnight, killPlayerName);
        }
        public static void RaiderHitmarkerDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            killRole = WildSpawnType.pmcBot;
            killPlayerSide = EPlayerSide.Savage;
            killExperience = 100;
            killDistance = 25;
            System.Random rnd = new System.Random();
            killWeaponName = DebugWeapons[rnd.Next(DebugWeapons.Count)];
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            aggNickname = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killBodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1, 80);
            hitmarker = ActiveUIScreen != null;
            killHitmarker = ActiveUIScreen != null;
            Killfeed();
            MultiKillfeed();
            RaidKillfeed(EPlayerSide.Usec, WildSpawnType.bossKnight, aggNickname, killWeaponName, EDamageType.Bullet, EPlayerSide.Savage, WildSpawnType.pmcBot, killPlayerName);
        }
        public static void DamageNumberDebug(object sender, EventArgs e)
        {
            DebugOffset = new Vector3(600, 0, 0);
            DebugMode = true;
            System.Random rnd = new System.Random();
            killPlayerName = DebugNames[rnd.Next(DebugNames.Count)];
            bodyPart = EBodyPart.Chest;
            killLethalDamageType = EDamageType.Bullet;
            killLevel = (int)UnityEngine.Random.Range(1,80);
            hitmarker = ActiveUIScreen != null;
            if (damageNumberTextMeshPro == null) return;
            if (AHitmarkerPlugin.EnableDamageNumber.Value || AHitmarkerPlugin.EnableArmorDamageNumber.Value)
            {
                string text = "";
                DamageNumber += UnityEngine.Random.Range(1f, 100f);
                ArmorDamageNumber += UnityEngine.Random.Range(1f, 100f);
                if (AHitmarkerPlugin.EnableDamageNumber.Value)
                {
                    text = ((int)DamageNumber).ToString() + " ";
                }
                if (AHitmarkerPlugin.EnableArmorDamageNumber.Value)
                {
                    text = text + "<color=#" + ColorUtility.ToHtmlStringRGB(AHitmarkerPlugin.ArmorColor.Value) + ">" + (Math.Round(ArmorDamageNumber, 1)).ToString("F1") + "</color> ";
                }
                damageNumberTextMeshPro.text = text;
                damageNumberTextMeshPro.color = AHitmarkerPlugin.HitmarkerColor.Value;
                damageNumberTextMeshPro.alpha = 1f;
                UpdateDamageNumber = false;
            }
        }
        public static void HitmarkerSoundDebug(object sender, EventArgs e)
        {
            if (LoadedAudioClips.ContainsKey(AHitmarkerPlugin.HitmarkerSound.Value))// && Singleton<BetterAudio>.Instance != null)
            {
                //Singleton<BetterAudio>.Instance.PlayNonspatial(LoadedAudioClips[AHitmarkerPlugin.HitmarkerSound.Value], BetterAudio.AudioSourceGroupType.Nonspatial, 0.0f, AHitmarkerPlugin.SoundVolume.Value);
                if (CanDebugReloadFiles && localPlayer == null)
                {
                    CanDebugReloadFiles = false;
                    ReloadFiles();
                }
                HitmarkerAudioSource.PlayOneShot(LoadedAudioClips[AHitmarkerPlugin.HitmarkerSound.Value], AHitmarkerPlugin.SoundVolume.Value);
            }
        }
        public static void HeadshotHitmarkerSoundDebug(object sender, EventArgs e)
        {
            if (LoadedAudioClips.ContainsKey(AHitmarkerPlugin.HeadshotHitmarkerSound.Value))// && Singleton<BetterAudio>.Instance != null)
            {
                //Singleton<BetterAudio>.Instance.PlayNonspatial(LoadedAudioClips[AHitmarkerPlugin.HeadshotHitmarkerSound.Value], BetterAudio.AudioSourceGroupType.Nonspatial, 0.0f, AHitmarkerPlugin.SoundVolume.Value);
                if (CanDebugReloadFiles && localPlayer == null)
                {
                    CanDebugReloadFiles = false;
                    ReloadFiles();
                }
                HitmarkerAudioSource.PlayOneShot(LoadedAudioClips[AHitmarkerPlugin.HeadshotHitmarkerSound.Value], AHitmarkerPlugin.SoundVolume.Value);
            }
        }
        public static void KillHitmarkerSoundDebug(object sender, EventArgs e)
        {
            if (LoadedAudioClips.ContainsKey(AHitmarkerPlugin.KillHitmarkerSound.Value))// && Singleton<BetterAudio>.Instance != null)
            {
                //Singleton<BetterAudio>.Instance.PlayNonspatial(LoadedAudioClips[AHitmarkerPlugin.KillHitmarkerSound.Value], BetterAudio.AudioSourceGroupType.Nonspatial, 0.0f, AHitmarkerPlugin.SoundVolume.Value);
                if (CanDebugReloadFiles && localPlayer == null)
                {
                    CanDebugReloadFiles = false;
                    ReloadFiles();
                }
                HitmarkerAudioSource.PlayOneShot(LoadedAudioClips[AHitmarkerPlugin.KillHitmarkerSound.Value], AHitmarkerPlugin.SoundVolume.Value);
            }
        }
        public static void ArmorSoundDebug(object sender, EventArgs e)
        {
            if (LoadedAudioClips.ContainsKey(AHitmarkerPlugin.KillHitmarkerSound.Value))// && Singleton<BetterAudio>.Instance != null)
            {
                //Singleton<BetterAudio>.Instance.PlayNonspatial(LoadedAudioClips[AHitmarkerPlugin.ArmorSound.Value], BetterAudio.AudioSourceGroupType.Nonspatial, 0.0f, AHitmarkerPlugin.SoundVolume.Value);
                if (CanDebugReloadFiles && localPlayer == null)
                {
                    CanDebugReloadFiles = false;
                    ReloadFiles();
                }
                HitmarkerAudioSource.PlayOneShot(LoadedAudioClips[AHitmarkerPlugin.ArmorSound.Value], AHitmarkerPlugin.SoundVolume.Value);
            }
        }
        public static void ArmorBreakSoundDebug(object sender, EventArgs e)
        {
            if (LoadedAudioClips.ContainsKey(AHitmarkerPlugin.KillHitmarkerSound.Value))// && Singleton<BetterAudio>.Instance != null)
            {
                //Singleton<BetterAudio>.Instance.PlayNonspatial(LoadedAudioClips[AHitmarkerPlugin.ArmorBreakSound.Value], BetterAudio.AudioSourceGroupType.Nonspatial, 0.0f, AHitmarkerPlugin.SoundVolume.Value);
                if (CanDebugReloadFiles && localPlayer == null)
                {
                    CanDebugReloadFiles = false;
                    ReloadFiles();
                }
                HitmarkerAudioSource.PlayOneShot(LoadedAudioClips[AHitmarkerPlugin.ArmorBreakSound.Value], AHitmarkerPlugin.SoundVolume.Value);
            }
        }
        public static void ReloadFilesDebug(object sender, EventArgs e)
        {
            ReloadFiles();
        }
        public static void UpdateKillPreset(object sender, EventArgs e)
        {
            if (rectTransform != null && verticalLayoutGroup != null)
            {
                verticalLayoutGroup.spacing = AHitmarkerPlugin.KillChildSpacing.Value;
                switch (AHitmarkerPlugin.KillPreset.Value)
                {
                    case EKillPreset.Center:
                        rectTransform.localPosition = new Vector2(0f, -250f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(0.5f, 1f);
                        AHitmarkerPlugin.KillChildDirection.Value = true;
                        AHitmarkerPlugin.KillTextAlignment.Value = TextAlignmentOptions.Right;
                        break;
                    case EKillPreset.TopLeft:
                        rectTransform.localPosition = new Vector2(-((Screen.width / 2) - 30), 530f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(0.0f, 1f);
                        AHitmarkerPlugin.KillChildDirection.Value = true;
                        AHitmarkerPlugin.KillTextAlignment.Value = TextAlignmentOptions.Left;
                        break;
                    case EKillPreset.TopRight:
                        rectTransform.localPosition = new Vector2((Screen.width / 2) - 30, 530f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(1f, 1f);
                        AHitmarkerPlugin.KillChildDirection.Value = true;
                        AHitmarkerPlugin.KillTextAlignment.Value = TextAlignmentOptions.Right;
                        break;
                    case EKillPreset.BottomLeft:
                        rectTransform.localPosition = new Vector2(-((Screen.width / 2) - 30), -280f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(0f, 0f);
                        AHitmarkerPlugin.KillChildDirection.Value = false;
                        AHitmarkerPlugin.KillTextAlignment.Value = TextAlignmentOptions.Left;
                        break;
                    case EKillPreset.BottomRight:
                        rectTransform.localPosition = new Vector2((Screen.width / 2) - 30, -420.0f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(1f, 0f);
                        AHitmarkerPlugin.KillChildDirection.Value = false;
                        AHitmarkerPlugin.KillTextAlignment.Value = TextAlignmentOptions.Right;
                        break;
                }
            }
        }
        public static void UpdateKillfeed(object sender, EventArgs e)
        {
            if (rectTransform != null && verticalLayoutGroup != null)
            {
                verticalLayoutGroup.spacing = AHitmarkerPlugin.KillChildSpacing.Value;
                switch (AHitmarkerPlugin.KillPreset.Value)
                {
                    case EKillPreset.Center:
                        rectTransform.localPosition = new Vector2(0f, -250f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(0.5f, 1f);
                        break;
                    case EKillPreset.TopLeft:
                        rectTransform.localPosition = new Vector2(-((Screen.width / 2) - 30), 530f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(0.0f, 1f);
                        break;
                    case EKillPreset.TopRight:
                        rectTransform.localPosition = new Vector2((Screen.width / 2) - 30, 530f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(1f, 1f);
                        break;
                    case EKillPreset.BottomLeft:
                        rectTransform.localPosition = new Vector2(-((Screen.width / 2) - 30), -280f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(0f, 0f);
                        break;
                    case EKillPreset.BottomRight:
                        rectTransform.localPosition = new Vector2((Screen.width / 2) - 30, -420.0f) + AHitmarkerPlugin.KillPosition.Value;
                        rectTransform.pivot = new Vector2(1f, 0f);
                        break;
                }
            }
        }
        public static void UpdateRaidKillPreset(object sender, EventArgs e)
        {
            if (raidKillrectTransform != null && raidKillverticalLayoutGroup != null)
            {
                raidKillverticalLayoutGroup.spacing = AHitmarkerPlugin.RaidKillChildSpacing.Value;
                switch (AHitmarkerPlugin.RaidKillPreset.Value)
                {
                    case ERaidKillPreset.TopLeft:
                        AHitmarkerPlugin.RaidKillChildDirection.Value = true;
                        raidKillrectTransform.localPosition = new Vector2(-((Screen.width / 2) - 30), 530f) + AHitmarkerPlugin.RaidKillPosition.Value;
                        raidKillrectTransform.pivot = new Vector2(0.0f, 1f);
                        AHitmarkerPlugin.RaidKillTextAlignment.Value = TextAlignmentOptions.Left;
                        break;
                    case ERaidKillPreset.TopRight:
                        AHitmarkerPlugin.RaidKillChildDirection.Value = true;
                        raidKillrectTransform.localPosition = new Vector2((Screen.width / 2) - 30, 530f) + AHitmarkerPlugin.RaidKillPosition.Value;
                        raidKillrectTransform.pivot = new Vector2(1f, 1f);
                        AHitmarkerPlugin.RaidKillTextAlignment.Value = TextAlignmentOptions.Right;
                        break;
                    case ERaidKillPreset.BottomLeft:
                        AHitmarkerPlugin.RaidKillChildDirection.Value = false;
                        raidKillrectTransform.localPosition = new Vector2(-((Screen.width / 2) - 30), -280f) + AHitmarkerPlugin.RaidKillPosition.Value;
                        raidKillrectTransform.pivot = new Vector2(0f, 0f);
                        AHitmarkerPlugin.RaidKillTextAlignment.Value = TextAlignmentOptions.Left;
                        break;
                    case ERaidKillPreset.BottomRight:
                        AHitmarkerPlugin.RaidKillChildDirection.Value = false;
                        raidKillrectTransform.localPosition = new Vector2((Screen.width / 2) - 30, -420.0f) + AHitmarkerPlugin.RaidKillPosition.Value;
                        raidKillrectTransform.pivot = new Vector2(1f, 0f);
                        AHitmarkerPlugin.RaidKillTextAlignment.Value = TextAlignmentOptions.Right;
                        break;
                }
            }
        }
        public static void UpdateRaidKillfeed(object sender, EventArgs e)
        {
            if (raidKillrectTransform != null && raidKillverticalLayoutGroup != null)
            {
                raidKillverticalLayoutGroup.spacing = AHitmarkerPlugin.RaidKillChildSpacing.Value;
                switch (AHitmarkerPlugin.RaidKillPreset.Value)
                {
                    case ERaidKillPreset.TopLeft:
                        raidKillrectTransform.localPosition = new Vector2(-((Screen.width / 2) - 30), 530f) + AHitmarkerPlugin.RaidKillPosition.Value;
                        raidKillrectTransform.pivot = new Vector2(0.0f, 1f);
                        break;
                    case ERaidKillPreset.TopRight:
                        raidKillrectTransform.localPosition = new Vector2((Screen.width / 2) - 30, 530f) + AHitmarkerPlugin.RaidKillPosition.Value;
                        raidKillrectTransform.pivot = new Vector2(1f, 1f);
                        break;
                    case ERaidKillPreset.BottomLeft:
                        raidKillrectTransform.localPosition = new Vector2(-((Screen.width / 2) - 30), -280f) + AHitmarkerPlugin.RaidKillPosition.Value;
                        raidKillrectTransform.pivot = new Vector2(0f, 0f);
                        break;
                    case ERaidKillPreset.BottomRight:
                        raidKillrectTransform.localPosition = new Vector2((Screen.width / 2) - 30, -420.0f) + AHitmarkerPlugin.RaidKillPosition.Value;
                        raidKillrectTransform.pivot = new Vector2(1f, 0f);
                        break;
                }
            }
        }
        public static void UpdateMultiKillfeed(object sender, EventArgs e)
        {
            if (multiKillfeedrectTransform != null && horizontalLayoutGroup != null)
            {
                multiKillfeedrectTransform.localPosition = new Vector3(AHitmarkerPlugin.MultiKillfeedRectPosition.Value.x - (AHitmarkerPlugin.MultiKillfeedSize.Value.x / (2f * (Screen.height / 1080))), AHitmarkerPlugin.MultiKillfeedRectPosition.Value.y, 0f);
                multiKillfeedrectTransform.pivot = AHitmarkerPlugin.MultiKillfeedRectPivot.Value;
                horizontalLayoutGroup.spacing = AHitmarkerPlugin.MultiKillfeedChildSpacing.Value;
            }
        }
    }
    public class AmandsKillfeedText : MonoBehaviour
    {
        public TMP_Text tMP_Text;
        public string text;
        public Color color = new Color(0.84f, 0.88f, 0.95f, 1f);
        public int fontSize = 26;
        public float outlineWidth = 0.01f;
        public FontStyles fontStyles = FontStyles.SmallCaps;
        public TextAlignmentOptions textAlignmentOptions = TextAlignmentOptions.Right;
        public float time = 2f;
        public float OpacitySpeed = 0.08f;
        public bool EnableWaitAndStart = true;
        private float Opacity = 1f;
        private float StartOpacity = 0f;
        private bool UpdateOpacity = false;
        private bool UpdateStartOpacity = false;

        public void Start()
        {
            tMP_Text = gameObject.AddComponent<TextMeshProUGUI>();
            if (tMP_Text != null)
            {
                tMP_Text.text = text;
                tMP_Text.color = color;
                tMP_Text.fontSize = fontSize;
                tMP_Text.outlineWidth = outlineWidth;
                tMP_Text.fontStyle = fontStyles;
                tMP_Text.alignment = textAlignmentOptions;
                tMP_Text.alpha = 0f;
                if (EnableWaitAndStart)
                {
                    WaitAndStart();
                    UpdateStartOpacity = true;
                }
            }
            else
            {
                foreach (AmandsAnimatedImage amandsAnimatedImage in AmandsHitmarkerClass.amandsAnimatedImages)
                {
                    amandsAnimatedImage.Opacity = 1f;
                    amandsAnimatedImage.UpdateOpacity = true;
                }
                Destroy(gameObject);
            }
        }
        private async void WaitAndStart()
        {
            await Task.Delay((int)(Math.Min(20f, time) * 1000));
            UpdateOpacity = true;
            if (this == AmandsHitmarkerClass.LastAmandsKillfeedText)
            {
                foreach (AmandsAnimatedImage amandsAnimatedImage in AmandsHitmarkerClass.amandsAnimatedImages)
                {
                    amandsAnimatedImage.Opacity = 1f;
                    amandsAnimatedImage.UpdateOpacity = true;
                }
            }
        }
        public void Update()
        {
            if (UpdateOpacity)
            {
                Opacity -= Math.Max(0.01f, OpacitySpeed);
                tMP_Text.alpha = Opacity;
                if (Opacity < 0)
                {
                    UpdateOpacity = false;
                    UpdateStartOpacity = false;
                    if (this == AmandsHitmarkerClass.LastAmandsKillfeedText)
                    {
                        AmandsHitmarkerClass.killListGameObject.DestroyAllChildren();
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
            else if (UpdateStartOpacity && StartOpacity < 1f)
            {
                StartOpacity += OpacitySpeed*2f;
                tMP_Text.alpha = StartOpacity;
            }
        }
    }
    public class AmandsRaidKillfeedText : MonoBehaviour
    {
        public TMP_Text tMP_Text;
        public string text;
        public Color color = new Color(0.84f, 0.88f, 0.95f, 1f);
        public int fontSize = 26;
        public float outlineWidth = 0.01f;
        public FontStyles fontStyles = FontStyles.SmallCaps;
        public TextAlignmentOptions textAlignmentOptions = TextAlignmentOptions.Right;
        public float time = 2f;
        public float OpacitySpeed = 0.08f;
        public bool EnableWaitAndStart = true;
        private float Opacity = 1f;
        private float StartOpacity = 0f;
        private bool UpdateOpacity = false;
        private bool UpdateStartOpacity = false;

        public void Start()
        {
            tMP_Text = gameObject.AddComponent<TextMeshProUGUI>();
            if (tMP_Text != null)
            {
                tMP_Text.text = text;
                tMP_Text.color = color;
                tMP_Text.fontSize = fontSize;
                tMP_Text.outlineWidth = outlineWidth;
                tMP_Text.fontStyle = fontStyles;
                tMP_Text.alignment = textAlignmentOptions;
                tMP_Text.alpha = 0f;
                if (EnableWaitAndStart)
                {
                    WaitAndStart();
                    UpdateStartOpacity = true;
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private async void WaitAndStart()
        {
            await Task.Delay((int)(Math.Min(20f, time) * 1000));
            UpdateOpacity = true;
        }
        public void Update()
        {
            if (UpdateOpacity)
            {
                Opacity -= Math.Max(0.01f, OpacitySpeed);
                tMP_Text.alpha = Opacity;
                if (Opacity < 0)
                {
                    UpdateOpacity = false;
                    UpdateStartOpacity = false;
                    if (this == AmandsHitmarkerClass.LastAmandsRaidKillfeedText)
                    {
                        AmandsHitmarkerClass.raidKillListGameObject.DestroyAllChildren();
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
            else if (UpdateStartOpacity && StartOpacity < 1f)
            {
                StartOpacity += OpacitySpeed * 2f;
                tMP_Text.alpha = StartOpacity;
            }
        }
    }
    public class AmandsAnimatedImage : MonoBehaviour
    {
        public RectTransform imageRectTransform;
        public Image image;
        public Sprite sprite;
        public Color color = new Color(0.84f, 0.88f, 0.95f, 1f);
        public float OpacitySpeed = 0.08f;
        public float Opacity = 1f;
        public bool UpdateOpacity = false;

        public void Start()
        {
            imageRectTransform = gameObject.AddComponent<RectTransform>();
            if (imageRectTransform != null)
            {
                imageRectTransform.sizeDelta = AHitmarkerPlugin.MultiKillfeedSize.Value;
                image = gameObject.AddComponent<Image>();
                if (image != null)
                {
                    image.sprite = sprite;
                    image.raycastTarget = false;
                    image.color = color;
                    if (!AHitmarkerPlugin.EnableKillfeed.Value) WaitAndStart();
                    WaitAndStart2();
                }
                else
                {
                    AmandsHitmarkerClass.amandsAnimatedImages.Remove(this);
                    Destroy(gameObject);
                }
            }
            else
            {
                AmandsHitmarkerClass.amandsAnimatedImages.Remove(this);
                Destroy(gameObject);
            }
        }
        private async void WaitAndStart()
        {
            await Task.Delay((int)(Math.Min(20f, AHitmarkerPlugin.KillTime.Value) * 1000));
            UpdateOpacity = true;
        }
        private async void WaitAndStart2()
        {
            await Task.Delay(60000);
            UpdateOpacity = true;
        }
        public void Update()
        {
            if (UpdateOpacity)
            {
                Opacity -= Math.Max(0.01f, OpacitySpeed);
                image.color = new Color(color.r, color.g, color.b, Opacity);
                if (Opacity < 0)
                {
                    UpdateOpacity = false;
                    AmandsHitmarkerClass.amandsAnimatedImages.Remove(this);
                    Destroy(gameObject);
                }
            }
        }
    }
    public enum EArmorHitmarker
    {
        Disabled,
        Enabled,
        BreakingOnly
    }
    public enum EKillStart
    {
        None,
        PlayerWeapon,
        Weapon,
        WeaponRole
    }
    public enum EKillNameColor
    {
        None,
        SingleColor,
        Colored
    }
    public enum EKillEnd
    {
        None,
        Bodypart,
        Role,
        Experience,
        Distance,
        DamageType,
        Level
    }
    public enum EKillPreset
    {
        Center,
        TopRight,
        BottomRight,
        TopLeft,
        BottomLeft,
    }
    public enum ERaidKillPreset
    {
        TopRight,
        BottomRight,
        TopLeft,
        BottomLeft,
    }
    public enum EMultiKillfeedPMCMode
    {
        Generic,
        Custom,
        Ranks
    }
    public enum EMultiKillfeedColorMode
    {
        SingleColor,
        HeadshotColorOnly,
        Colored
    }
    public enum EHitmarkerPositionMode
    {
        Center,
        GunDirection,
        ImpactPoint,
        ImpactPointStatic
    }
    public enum EDamageNumberPositionMode
    {
        Screen,
        Hitmarker
    }
    public enum EHeadshotXP
    {
        Off,
        On,
        OnFormula
    }
}
