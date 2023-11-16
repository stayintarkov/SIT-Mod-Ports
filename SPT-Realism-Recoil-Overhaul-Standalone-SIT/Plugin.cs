using Aki.Common.Http;
using Aki.Common.Utils;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Comfort.Common;
using Diz.Jobs;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static System.Net.Mime.MediaTypeNames;
using SIT.Core;

namespace RecoilStandalone
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> ResetTime { get; set; }
        public static ConfigEntry<float> ConvergenceSpeedCurve { get; set; }
        public static ConfigEntry<float> SwayIntensity { get; set; }
        public static ConfigEntry<float> RecoilIntensity { get; set; }
        public static ConfigEntry<float> VertMulti { get; set; }
        public static ConfigEntry<float> HorzMulti { get; set; }
        public static ConfigEntry<float> DispMulti { get; set; }
        public static ConfigEntry<float> CamMulti { get; set; }
        public static ConfigEntry<float> ConvergenceMulti { get; set; }
        public static ConfigEntry<float> RecoilDamping { get; set; }
        public static ConfigEntry<float> HandsDamping { get; set; }
        public static ConfigEntry<bool> EnableCrank { get; set; }

        public static ConfigEntry<float> ResetSpeed { get; set; }
        public static ConfigEntry<float> RecoilClimbFactor { get; set; }
        public static ConfigEntry<float> RecoilDispersionFactor { get; set; }
        public static ConfigEntry<float> RecoilDispersionSpeed { get; set; }
        public static ConfigEntry<float> RecoilSmoothness { get; set; }
        public static ConfigEntry<float> ResetSensitivity { get; set; }
        public static ConfigEntry<bool> ResetVertical { get; set; }
        public static ConfigEntry<bool> ResetHorizontal { get; set; }
        public static ConfigEntry<float> RecoilClimbLimit { get; set; }
        public static ConfigEntry<float> PlayerControlMulti { get; set; }

        public static ConfigEntry<bool> EnableHybridRecoil { get; set; }
        public static ConfigEntry<bool> EnableHybridReset { get; set; }
        public static ConfigEntry<bool> HybridForAll { get; set; }

        public static ConfigEntry<float> test1 { get; set; }
        public static ConfigEntry<float> test2 { get; set; }
        public static ConfigEntry<float> test3 { get; set; }
        public static ConfigEntry<float> test4 { get; set; }

        public static bool IsFiring = false;
        public static float FiringTimer = 0.0f;
        public static bool IsFiringWiggle = false;
        public static float WiggleTimer = 0.0f;

        public static int ShotCount = 0;
        public static int PrevShotCount = ShotCount;
        public static bool StatsAreReset;

        public static float RecoilAngle;
        public static float TotalDispersion;
        public static float TotalDamping;
        public static float TotalHandDamping;
        public static float TotalConvergence;
        public static float TotalCameraRecoil;
        public static float TotalVRecoil;
        public static float TotalHRecoil;

        public static float BreathIntensity = 1f;
        public static float HandsIntensity = 1f;
        public static float SetRecoilIntensity = 1f;

        public static float MountingSwayBonus = 1f;
        public static float MountingRecoilBonus = 1f;
        public static float BracingSwayBonus = 1f;
        public static float BracingRecoilBonus = 1f;
        public static bool IsMounting = false;

        public static bool HasStock = false;


        public static bool IsAiming;

        public static bool LauncherIsActive = false;

        public static Weapon CurrentlyEquipedWeapon;

        public static bool RealismModIsPresent = false;
        public static bool CombatStancesIsPresent = false;
        private static bool checkedForOtherMods = false;
        private static bool warnedUser = false;

        public static bool IsVector = false;

        public static float PlayerControl = 0f;

        public static bool HasOptic = false;

        public static float RecoilDetla = 1f;

        void Awake()
        {
            string testing = "0. Testing";
            string RecoilClimbSettings = "1. Recoil Climb Settings";
            string RecoilSettings = "2. Recoil Settings";
            string AdvancedRecoilSettings = "3. Advanced Recoil Settings";
            string WeaponSettings = "4. Weapon Settings";

            test1 = Config.Bind<float>(testing, "test 1", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 600, IsAdvanced = true }));
            test2 = Config.Bind<float>(testing, "test 2", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 500, IsAdvanced = true }));
            test3 = Config.Bind<float>(testing, "test 3", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 400, IsAdvanced = true }));
            test4 = Config.Bind<float>(testing, "test 4", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 300, IsAdvanced = true }));

            EnableHybridRecoil = Config.Bind<bool>(RecoilClimbSettings, "Enable Hybrid Recoil System", true, new ConfigDescription("Combines Steady Recoil Climb With Auto-Compensation. If You Do Not Attempt To Control Recoil, Auto-Compensation Will Decrease Resulting In More Muzzle Flip. If You Control The Recoil, Auto-Comp Increases And Muzzle Flip Decreases.", null, new ConfigurationManagerAttributes { Order = 100 }));
            HybridForAll = Config.Bind<bool>(RecoilClimbSettings, "Enable Hybrid Recoil For All Weapons", false, new ConfigDescription("By Default This Hybrid System Is Only Enabled For Pistols And Stockless/Folded Stocked Weapons.", null, new ConfigurationManagerAttributes { Order = 90 }));
            EnableHybridReset = Config.Bind<bool>(RecoilClimbSettings, "Enable Recoil Reset For Hybrid Recoil", true, new ConfigDescription("Enables Recoil Reset For Pistols And Stockless/Folded Stocked Weapons That Are Using Hybrid Recoil, If The Other Reset Options Are Enabled.", null, new ConfigurationManagerAttributes { Order = 90 }));
            PlayerControlMulti = Config.Bind<float>(RecoilClimbSettings, "Player Control Strength", 80f, new ConfigDescription("How Quickly The Weapon Responds To Mouse Input If Using The Hybrid Recoil System.", new AcceptableValueRange<float>(0f, 200f), new ConfigurationManagerAttributes { Order = 85 }));

            ResetVertical = Config.Bind<bool>(RecoilClimbSettings, "Enable Vertical Reset", true, new ConfigDescription("Enables Weapon Reseting Back To Original Vertical Position.", null, new ConfigurationManagerAttributes { Order = 80 }));
            ResetHorizontal = Config.Bind<bool>(RecoilClimbSettings, "Enable Horizontal Reset", false, new ConfigDescription("Enables Weapon Reseting Back To Original Horizontal Position.", null, new ConfigurationManagerAttributes { Order = 70 }));
            ResetSpeed = Config.Bind<float>(RecoilClimbSettings, "Reset Speed", 0.003f, new ConfigDescription("How Fast The Weapon's Vertical Position Resets After Firing. Weapon's Convergence Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 60 }));
            ResetSensitivity = Config.Bind<float>(RecoilClimbSettings, "Reset Sensitvity", 0.15f, new ConfigDescription("The Amount Of Mouse Movement After Firing Needed To Cancel Reseting Back To Weapon's Original Position.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 50 }));
            RecoilSmoothness = Config.Bind<float>(RecoilClimbSettings, "Recoil Smoothness", 0.05f, new ConfigDescription("How Fast Recoil Moves Weapon While Firing, Higher Value Increases Smoothness.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 40 }));
            RecoilClimbFactor = Config.Bind<float>(RecoilClimbSettings, "Recoil Climb Multi", 0.12f, new ConfigDescription("Multiplier For How Much The Weapon Climbs Vertically Per Shot. Weapon's Vertical Recoil Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 30 }));
            RecoilClimbLimit = Config.Bind<float>(RecoilClimbSettings, "Recoil Climb Limit", 10f, new ConfigDescription("How Far Recoil Can Climb.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = 25 }));
            RecoilDispersionFactor = Config.Bind<float>(RecoilClimbSettings, "S-Pattern Multi.", 0.02f, new ConfigDescription("Increases The Size The Classic S Pattern. Weapon's Dispersion Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 20 }));
            RecoilDispersionSpeed = Config.Bind<float>(RecoilClimbSettings, "S-Pattern Speed Multi.", 2f, new ConfigDescription("Increases The Speed At Which Recoil Makes The Classic S Pattern.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 10 }));

            RecoilIntensity = Config.Bind<float>(RecoilSettings, "Recoil Intensity", 1f, new ConfigDescription("Changes The Overall Intenisty Of Recoil. This Will Increase/Decrease Horizontal Recoil, Dispersion, Vertical Recoil. Does Not Affect Recoil Climb Much, Mostly Spread And Visual.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 50 }));
            VertMulti = Config.Bind<float>(RecoilSettings, "Vertical Recoil Multi.", 0.55f, new ConfigDescription("Up/Down. Will Also Increase Recoil Climb.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 40 }));
            HorzMulti = Config.Bind<float>(RecoilSettings, "Horizontal Recoil Multi", 1.0f, new ConfigDescription("Forward/Back. Will Also Increase Weapon Shake While Firing.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 30 }));
            DispMulti = Config.Bind<float>(RecoilSettings, "Dispersion Recoil Multi", 1.0f, new ConfigDescription("Spread. Will Also Increase S-Pattern Size.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 20 }));
            CamMulti = Config.Bind<float>(RecoilSettings, "Camera Recoil Multi", 1f, new ConfigDescription("Visual Camera Recoil.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 10 }));
            ConvergenceMulti = Config.Bind<float>(RecoilSettings, "Convergence Multi", 15f, new ConfigDescription("AKA Auto-Compensation. Higher = Snappier Recoil, Faster Reset And Tighter Recoil Pattern.", new AcceptableValueRange<float>(0f, 40f), new ConfigurationManagerAttributes { Order = 1 }));
             
            ConvergenceSpeedCurve = Config.Bind<float>(AdvancedRecoilSettings, "Convergence Curve Multi", 1f, new ConfigDescription("The Convergence Curve. Lower Means More Recoil.", new AcceptableValueRange<float>(0.01f, 1.5f), new ConfigurationManagerAttributes { Order = 100 }));
            ResetTime = Config.Bind<float>(AdvancedRecoilSettings, "Time Before Reset", 0.14f, new ConfigDescription("The Time In Seconds That Has To Be Elapsed Before Firing Is Considered Over, Recoil Will Not Reset Until It Is Over.", new AcceptableValueRange<float>(0.1f, 0.5f), new ConfigurationManagerAttributes { Order = 10 }));
            EnableCrank = Config.Bind<bool>(AdvancedRecoilSettings, "Rearward Recoil", true, new ConfigDescription("Makes Recoil Go Towards Player's Shoulder Instead Of Forward.", null, new ConfigurationManagerAttributes { Order = 3 }));
            HandsDamping = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Wiggle", 0.7f, new ConfigDescription("The Amount Of Rearward Wiggle After Firing.", new AcceptableValueRange<float>(0.2f, 0.9f), new ConfigurationManagerAttributes { Order = 1 }));
            RecoilDamping = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Wiggle", 0.7f, new ConfigDescription("The Amount Of Vertical Wiggle After Firing.", new AcceptableValueRange<float>(0.2f, 0.9f), new ConfigurationManagerAttributes { Order = 2 }));
           
            SwayIntensity = Config.Bind<float>(WeaponSettings, "Sway Intensity", 1f, new ConfigDescription("Changes The Intensity Of Aim Sway And Inertia.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 1 }));

            new UpdateWeaponVariablesPatch().Enable();
            new UpdateSwayFactorsPatch().Enable();
            new GetAimingPatch().Enable();
            new ProcessPatch().Enable();
            new ShootPatch().Enable();
            new SetCurveParametersPatch().Enable();
            new PlayerLateUpdatePatch().Enable();
            new RotatePatch().Enable();
            new ApplyComplexRotationPatch().Enable();
            new BreathProcessPatch().Enable();  
        }

        void Update()
        {
            if (!checkedForOtherMods)
            {
                RealismModIsPresent = Chainloader.PluginInfos.ContainsKey("RealismMod");
                CombatStancesIsPresent = Chainloader.PluginInfos.ContainsKey("CombatStances") && Chainloader.PluginInfos.ContainsKey("StanceRecoilBridge");

                checkedForOtherMods = true;
                Logger.LogWarning("Combat Stances Is Present = " + Chainloader.PluginInfos.ContainsKey("CombatStances"));
                Logger.LogWarning("Realism Mod Is Present = " + RealismModIsPresent);
            }

            if ((int)Time.time % 5 == 0 && !warnedUser)
            {
                warnedUser = true;
                if (Chainloader.PluginInfos.ContainsKey("CombatStances") && !Chainloader.PluginInfos.ContainsKey("StanceRecoilBridge"))
                {
                    NotificationManagerClass.DisplayWarningNotification("ERROR: COMBAT STANCES IS INSTALLED BUT THE COMPATIBILITY BRIDGE IS NOT! INSTALL IT BEFORE USING THESE MODS TOGETHER!", EFT.Communications.ENotificationDurationType.Long);
                }
                if (Chainloader.PluginInfos.ContainsKey("RealismMod"))
                {
                    NotificationManagerClass.DisplayWarningNotification("ERROR: REALISM MOD IS ALSO INSTALLED! REALISM ALREADY CONTAINS RECOIL OVERHAUL, UNINSTALL ONE OF THESE MODS!", EFT.Communications.ENotificationDurationType.Long);
                }
                if (!Chainloader.PluginInfos.ContainsKey("CombatStances") && Chainloader.PluginInfos.ContainsKey("StanceRecoilBridge"))
                {
                    NotificationManagerClass.DisplayWarningNotification("ERROR: COMBAT STANCES COMPATIBILITY BRDIGE IS INSTALLED BUT COMBAT STANCES IS NOT! REMOVE THE BRIDGE OR INSTALL COMBAT STANCES!", EFT.Communications.ENotificationDurationType.Long);
                }
            }
            if ((int)Time.time % 5 != 0)
            {
                warnedUser = false;
            }
  
            if (Utils.CheckIsReady())
            {

                if (Plugin.ShotCount > Plugin.PrevShotCount)
                {
                    Plugin.IsFiring = true;
                    Plugin.IsFiringWiggle = true;
                    Plugin.PrevShotCount = Plugin.ShotCount;
                }

                if (Plugin.ShotCount == Plugin.PrevShotCount)
                {
                    Plugin.FiringTimer += Time.deltaTime;
                    Plugin.WiggleTimer += Time.deltaTime;
                    if (Plugin.FiringTimer >= Plugin.ResetTime.Value)
                    {
                        Plugin.IsFiring = false;
                        Plugin.ShotCount = 0;
                        Plugin.PrevShotCount = 0;
                        Plugin.FiringTimer = 0f;
                    }
                    if (Plugin.WiggleTimer >= 0.1f)
                    {
                        Plugin.IsFiringWiggle = false;
                        Plugin.WiggleTimer = 0f;
                    }
                }
            }
        }
    }
}

