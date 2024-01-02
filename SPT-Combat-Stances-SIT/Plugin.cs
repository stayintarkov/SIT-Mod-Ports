using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using HarmonyLib;
using EFT.Animations;
using EFT.InventoryLogic;
using static MineDirectional;
using BepInEx.Bootstrap;
using System;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.Networking;

namespace CombatStances
{
    [BepInPlugin("CombatStances", "Combat Stances", "1.4.2")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> EnableFSPatch { get; set; }
        public static ConfigEntry<bool> EnableNVGPatch { get; set; }

        public static ConfigEntry<KeyboardShortcut> ActiveAimKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> LowReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> HighReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> ShortStockKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> CycleStancesKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> MountKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> PatrolKeybind { get; set; }


        public static ConfigEntry<bool> ToggleActiveAim { get; set; }
        public static ConfigEntry<bool> StanceToggleDevice { get; set; }

        public static ConfigEntry<bool> EnableAltPistol { get; set; }
        public static ConfigEntry<bool> EnableIdleStamDrain { get; set; }
        public static ConfigEntry<bool> EnableStanceStamChanges { get; set; }
        public static ConfigEntry<bool> EnableTacSprint { get; set; }
        public static ConfigEntry<bool> EnableMountUI { get; set; }

        public static ConfigEntry<float> WeapOffsetX { get; set; }
        public static ConfigEntry<float> WeapOffsetY { get; set; }
        public static ConfigEntry<float> WeapOffsetZ { get; set; }

        public static ConfigEntry<float> StanceTransitionSpeedMulti { get; set; }
        public static ConfigEntry<float> StanceRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> ThirdPersonPositionSpeed { get; set; }
        public static ConfigEntry<float> ThirdPersonRotationSpeed { get; set; }
        public static ConfigEntry<float> ThirdPersonRotationMulti { get; set; }


        public static ConfigEntry<float> ActiveAimRotationX { get; set; }
        public static ConfigEntry<float> ActiveAimRotationY { get; set; }
        public static ConfigEntry<float> ActiveAimRotationZ { get; set; }

        public static ConfigEntry<float> PistolRotationX { get; set; }
        public static ConfigEntry<float> PistolRotationY { get; set; }
        public static ConfigEntry<float> PistolRotationZ { get; set; }

        public static ConfigEntry<float> ActiveAimSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveAimResetSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveAimRotationMulti { get; set; }
        public static ConfigEntry<float> PistolRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadyRotationMulti { get; set; }
        public static ConfigEntry<float> LowReadyRotationMulti { get; set; }
        public static ConfigEntry<float> ShortStockRotationMulti { get; set; }

        public static ConfigEntry<float> ShortStockAdditionalRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveAimAdditionalRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadyAdditionalRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> LowReadyAdditionalRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> PistolAdditionalRotationSpeedMulti { get; set; }

        public static ConfigEntry<float> ActiveAimResetRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> PistolResetRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationMulti { get; set; }
        public static ConfigEntry<float> LowReadyResetRotationMulti { get; set; }
        public static ConfigEntry<float> ShortStockResetRotationSpeedMulti { get; set; }

        public static ConfigEntry<float> HighReadySpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadyResetSpeedMulti { get; set; }
        public static ConfigEntry<float> LowReadySpeedMulti { get; set; }
        public static ConfigEntry<float> LowReadyResetSpeedMulti { get; set; }
        public static ConfigEntry<float> PistolPosSpeedMulti { get; set; }
        public static ConfigEntry<float> PistolPosResetSpeedMulti { get; set; }
        public static ConfigEntry<float> ShortStockSpeedMulti { get; set; }
        public static ConfigEntry<float> ShortStockResetSpeedMulti { get; set; }

        public static ConfigEntry<float> ActiveAimAdditionalRotationX { get; set; }
        public static ConfigEntry<float> ActiveAimAdditionalRotationY { get; set; }
        public static ConfigEntry<float> ActiveAimAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationX { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationY { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationZ { get; set; }

        public static ConfigEntry<float> HighReadyAdditionalRotationX { get; set; }
        public static ConfigEntry<float> HighReadyAdditionalRotationY { get; set; }
        public static ConfigEntry<float> HighReadyAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationX { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationY { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationZ { get; set; }

        public static ConfigEntry<float> LowReadyAdditionalRotationX { get; set; }
        public static ConfigEntry<float> LowReadyAdditionalRotationY { get; set; }
        public static ConfigEntry<float> LowReadyAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> LowReadyResetRotationX { get; set; }
        public static ConfigEntry<float> LowReadyResetRotationY { get; set; }
        public static ConfigEntry<float> LowReadyResetRotationZ { get; set; }

        public static ConfigEntry<float> PistolAdditionalRotationX { get; set; }
        public static ConfigEntry<float> PistolAdditionalRotationY { get; set; }
        public static ConfigEntry<float> PistolAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> PistolResetRotationX { get; set; }
        public static ConfigEntry<float> PistolResetRotationY { get; set; }
        public static ConfigEntry<float> PistolResetRotationZ { get; set; }

        public static ConfigEntry<float> ShortStockAdditionalRotationX { get; set; }
        public static ConfigEntry<float> ShortStockAdditionalRotationY { get; set; }
        public static ConfigEntry<float> ShortStockAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> ShortStockResetRotationX { get; set; }
        public static ConfigEntry<float> ShortStockResetRotationY { get; set; }
        public static ConfigEntry<float> ShortStockResetRotationZ { get; set; }

        public static ConfigEntry<float> PistolOffsetX { get; set; }
        public static ConfigEntry<float> PistolOffsetY { get; set; }
        public static ConfigEntry<float> PistolOffsetZ { get; set; }

        public static ConfigEntry<float> ActiveAimOffsetX { get; set; }
        public static ConfigEntry<float> ActiveAimOffsetY { get; set; }
        public static ConfigEntry<float> ActiveAimOffsetZ { get; set; }

        public static ConfigEntry<float> LowReadyOffsetX { get; set; }
        public static ConfigEntry<float> LowReadyOffsetY { get; set; }
        public static ConfigEntry<float> LowReadyOffsetZ { get; set; }

        public static ConfigEntry<float> LowReadyRotationX { get; set; }
        public static ConfigEntry<float> LowReadyRotationY { get; set; }
        public static ConfigEntry<float> LowReadyRotationZ { get; set; }

        public static ConfigEntry<float> HighReadyOffsetX { get; set; }
        public static ConfigEntry<float> HighReadyOffsetY { get; set; }
        public static ConfigEntry<float> HighReadyOffsetZ { get; set; }

        public static ConfigEntry<float> HighReadyRotationX { get; set; }
        public static ConfigEntry<float> HighReadyRotationY { get; set; }
        public static ConfigEntry<float> HighReadyRotationZ { get; set; }

        public static ConfigEntry<float> ShortStockOffsetX { get; set; }
        public static ConfigEntry<float> ShortStockOffsetY { get; set; }
        public static ConfigEntry<float> ShortStockOffsetZ { get; set; }

        public static ConfigEntry<float> ShortStockRotationX { get; set; }
        public static ConfigEntry<float> ShortStockRotationY { get; set; }
        public static ConfigEntry<float> ShortStockRotationZ { get; set; }

        public static ConfigEntry<float> ShortStockReadyOffsetX { get; set; }
        public static ConfigEntry<float> ShortStockReadyOffsetY { get; set; }
        public static ConfigEntry<float> ShortStockReadyOffsetZ { get; set; }

        public static ConfigEntry<float> ShortStockReadyRotationX { get; set; }
        public static ConfigEntry<float> ShortStockReadyRotationY { get; set; }
        public static ConfigEntry<float> ShortStockReadyRotationZ { get; set; }

        public static ConfigEntry<float> test1 { get; set; }
        public static ConfigEntry<float> test2 { get; set; }
        public static ConfigEntry<float> test3 { get; set; }
        public static ConfigEntry<float> test4 { get; set; }

        public static bool IsAiming;
        public static bool IsBlindFiring;
        public static bool HasOptic;
        public static bool IsAllowedADS;
        public static bool RightArmBlacked;
        public static bool LeftArmBlacked;

        public static bool IsFiring = false;
        public static bool IsFiringWiggle = false;
        public static float FiringTimer = 0.0f;
        public static float WiggleTimer = 0.0f;

        public static bool IsSprinting;
        public static bool DidWeaponSwap;
        public static bool IsInInventory = false;

        public static float BaseWeaponLength;
        public static float NewWeaponLength;

        public static float RemainingArmStamPercentage = 1f;
        public static float ADSInjuryMulti;

        public static float BaseHipfireInaccuracy;

        public static float ErgoDelta;

        public static float BreathIntensity = 1f;
        public static float HandsIntensity = 1f;
        public static float RecoilIntensity = 1f;
        public static float HandsDamping = 0.5f;

        public static float AimSpeed;
        public static float WeaponSkillErgo = 0f;
        public static float AimSkillADSBuff = 0f;
        public static float AimMoveSpeedInjuryReduction;


        public static int ShotCount = 0;
        public static int PrevShotCount = ShotCount;

        public static bool IsInThirdPerson = false;

        public static Vector3 TransformBaseStartPosition;
        public static Vector3 WeaponOffsetPosition;

        public static bool RecoilStandaloneIsPresent = false;
        private static bool checkedForOtherMods = false;
        private static bool warnedUser = false;

        public static float TotalConvergence;
        public static float TotalHRecoil;

        public static GameObject Hook;
        public static MountingUI MountingUIComponent;
        public static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();

        private void loadSprites()
        {
            string[] iconFilesDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\CombatStances\\icons\\", "*.png");

            foreach (string fileDir in iconFilesDir)
            {
                loadSprite(fileDir);
            }
        }

        private async void loadSprite(string path)
        {
            LoadedSprites[Path.GetFileName(path)] = await requestSprite(path);
        }

        private async Task<Sprite> requestSprite(string path)
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

        private void Awake()
        {
            try
            {
                loadSprites();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }

            Hook = new GameObject();
            MountingUIComponent = Hook.AddComponent<MountingUI>();
            DontDestroyOnLoad(Hook);

            intiConfigs();

            new ApplyComplexRotationPatch().Enable();
            new ApplySimpleRotationPatch().Enable();
            new InitTransformsPatch().Enable();
            new WeaponOverlappingPatch().Enable();
            new WeaponLengthPatch().Enable();
            new OnWeaponDrawPatch().Enable();
            new WeaponOverlapViewPatch().Enable();
            new ZeroAdjustmentsPatch().Enable();
            new PlayerLateUpdatePatch().Enable();
            new SprintAccelerationPatch().Enable();
            new SetAimingSlowdownPatch().Enable();
            new RegisterShotPatch().Enable();
            new SyncWithCharacterSkillsPatch().Enable();
            new PwaWeaponParamsPatch().Enable();
            new UpdateHipInaccuracyPatch().Enable();
            new SetAimingPatch().Enable();
            new ToggleAimPatch().Enable();
            new SetFireModePatch().Enable();
            new OperateStationaryWeaponPatch().Enable();
            new CollisionPatch().Enable();
            new RotatePatch().Enable();
            new SetTiltPatch().Enable();
            new ClampSpeedPatch().Enable();
            new UpdateWeaponVariablesPatch().Enable();
            new BattleUIScreenPatch().Enable();
            new PlayerInitPatch().Enable();

            Logger.LogInfo($"Plugin com.sit.combatstances is loaded!");
        }

        void Update()
        {
            if (!checkedForOtherMods)
            {
                checkedForOtherMods = true;

                RecoilStandaloneIsPresent = Chainloader.PluginInfos.ContainsKey("RecoilStandalone") && Chainloader.PluginInfos.ContainsKey("StanceRecoilBridge");
                Logger.LogWarning("Recoil Standalone Is Present = " + Chainloader.PluginInfos.ContainsKey("RecoilStandalone"));
                Logger.LogWarning("Realism Mod Is Present = " + Chainloader.PluginInfos.ContainsKey("RealismMod"));
            }
            if ((int)Time.time % 5 == 0 && !warnedUser)
            {
                warnedUser = true;
                if (Chainloader.PluginInfos.ContainsKey("RealismMod"))
                {
                    NotificationManagerClass.DisplayWarningNotification("ERROR: REALISM MOD IS ALSO INSTALLED! REALISM ALREADY CONTAINS COMBAT STANCES, UNINSTALL ONE OF THESE MODS!", EFT.Communications.ENotificationDurationType.Infinite);
                }
                if (Chainloader.PluginInfos.ContainsKey("StanceRecoilBridge") && !Chainloader.PluginInfos.ContainsKey("RecoilStandalone"))
                {
                    NotificationManagerClass.DisplayWarningNotification("ERROR: RECOIL OVERHAUL COMPATIBILITY BRIDGE IS INSTALLED BUT RECOIL STANDALONE IS NOT! REMOVE THE BRIDGE OR INSTALL RECOIL OVERHAUL!", EFT.Communications.ENotificationDurationType.Infinite);
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
                    Plugin.IsFiringWiggle = true;
                    StanceController.IsFiringFromStance = true;
                    Plugin.PrevShotCount = Plugin.ShotCount;
                }

                if (Plugin.ShotCount == Plugin.PrevShotCount)
                {
                    Plugin.FiringTimer += Time.deltaTime;
                    Plugin.WiggleTimer += Time.deltaTime;
                    if (Plugin.FiringTimer >= 0.14f)
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
                    StanceController.StanceShotTimer();
                }

                if (Utils.WeaponReady)
                {
                    Player player = Singleton<GameWorld>.Instance.MainPlayer;
                    StanceController.StanceState(player.HandsController.Item as Weapon);
                }
            }
        }

        private void intiConfigs() 
        {
            string testing = "0. Testing";
            string miscSettings = "1. Misc. Settings";
            string weapAimAndPos = "2. Weapon Stances And Position.";
            string activeAim = "3. Active Aim.";
            string highReady = "4. High Ready.";
            string lowReady = "5. Low Ready.";
            string pistol = "6. Pistol Position And Stance.";
            string shortStock = "7. Short-Stocking.";

            test1 = Config.Bind<float>(testing, "test 1", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 600, IsAdvanced = true }));
            test2 = Config.Bind<float>(testing, "test 2", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 500, IsAdvanced = true }));
            test3 = Config.Bind<float>(testing, "test 3", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 400, IsAdvanced = true }));
            test4 = Config.Bind<float>(testing, "test 4", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 300, IsAdvanced = true }));

            EnableNVGPatch = Config.Bind<bool>(miscSettings, "Enable NVG ADS Patch", true, new ConfigDescription("Magnified Optics Block ADS When Using NVGs.", null, new ConfigurationManagerAttributes { Order = 4 }));
            EnableFSPatch = Config.Bind<bool>(miscSettings, "Enable Faceshield Patch", true, new ConfigDescription("Faceshields Block ADS Unless The Specfic Stock/Weapon/Faceshield Allows It.", null, new ConfigurationManagerAttributes { Order = 3 }));

            EnableTacSprint = Config.Bind<bool>(weapAimAndPos, "Enable High Ready Sprint Animation", false, new ConfigDescription("Enables Usage Of High Ready Sprint Animation When Sprinting From High Ready Position.", null, new ConfigurationManagerAttributes { Order = 186 }));
            EnableAltPistol = Config.Bind<bool>(weapAimAndPos, "Enable Alternative Pistol Position And ADS", true, new ConfigDescription("Pistol Will Be Held Centered And In A Compressed Stance. ADS Will Be Animated.", null, new ConfigurationManagerAttributes { Order = 185 }));
            EnableIdleStamDrain = Config.Bind<bool>(weapAimAndPos, "Enable Idle Arm Stamina Drain", false, new ConfigDescription("Arm Stamina Will Drain When Not In A Stance (High And Low Ready, Short-Stocking).", null, new ConfigurationManagerAttributes { Order = 184 }));
            EnableStanceStamChanges = Config.Bind<bool>(weapAimAndPos, "Enable Stance Stamina And Movement Effects", true, new ConfigDescription("Enabled Stances To Affect Stamina And Movement Speed. High + Low Ready, Short-Stocking And Pistol Idle Will Regenerate Stamina Faster And Optionally Idle With Rifles Drains Stamina. High Ready Has Faster Sprint Speed And Sprint Accel, Low Ready Has Faster Sprint Accel. Arm Stamina Won't Drain Regular Stamina If It Reaches 0.", null, new ConfigurationManagerAttributes { Order = 183 }));
            ToggleActiveAim = Config.Bind<bool>(weapAimAndPos, "Use Toggle For Active Aim", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 181 }));
            StanceToggleDevice = Config.Bind<bool>(weapAimAndPos, "Stance Toggles Off Light/Laser", true, new ConfigDescription("Entering High/Low Ready Will Toggle Off Lights/Lasers.", null, new ConfigurationManagerAttributes { Order = 180 }));
            EnableMountUI = Config.Bind<bool>(weapAimAndPos, "Enable Mounting UI", true, new ConfigDescription("If Enabled, An Icon On Screen Will Indicate If Player Is Bracing, Mounting And What Side Of Cover They Are On.", null, new ConfigurationManagerAttributes { Order = 179 }));

            CycleStancesKeybind = Config.Bind(weapAimAndPos, "Cycle Stances Keybind", new KeyboardShortcut(KeyCode.J), new ConfigDescription("Cycles Between High, Low Ready and Short-Stocking. Double Click Returns To Idle.", null, new ConfigurationManagerAttributes { Order = 174 }));
            ActiveAimKeybind = Config.Bind(weapAimAndPos, "Active Aim Keybind", new KeyboardShortcut(KeyCode.LeftArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 173 }));
            HighReadyKeybind = Config.Bind(weapAimAndPos, "High Ready Keybind", new KeyboardShortcut(KeyCode.UpArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 172 }));
            LowReadyKeybind = Config.Bind(weapAimAndPos, "Low Ready Keybind", new KeyboardShortcut(KeyCode.DownArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 171 }));
            ShortStockKeybind = Config.Bind(weapAimAndPos, "Short-Stock Keybind", new KeyboardShortcut(KeyCode.RightArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));
            MountKeybind = Config.Bind(weapAimAndPos, "Mounting Keybind", new KeyboardShortcut(KeyCode.KeypadMultiply), new ConfigDescription("Snaps To Cover To Improve Weapon Stability And Recoil, Toggle Only.", null, new ConfigurationManagerAttributes { Order = 160 }));
            PatrolKeybind = Config.Bind(weapAimAndPos, "Patrol/Neutral Stance Keybind", new KeyboardShortcut(KeyCode.KeypadEnter), new ConfigDescription("Puts The Weapon In A Neutral Position, Improving Arm Stam Regen And Walk Speed. For Maximum Larping.", null, new ConfigurationManagerAttributes { Order = 155 }));

            WeapOffsetX = Config.Bind<float>(weapAimAndPos, "Weapon Position X-Axis", 0.0f, new ConfigDescription("Adjusts The Starting Position Of Weapon On Screen.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 152 }));
            WeapOffsetY = Config.Bind<float>(weapAimAndPos, "Weapon Position Y-Axis", 0.0f, new ConfigDescription("Adjusts The Starting Position Of Weapon On Screen.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 151 }));
            WeapOffsetZ = Config.Bind<float>(weapAimAndPos, "Weapon Position Z-Axis", 0.0f, new ConfigDescription("Adjusts The Starting Position Of Weapon On Screen.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 150 }));

            StanceTransitionSpeedMulti = Config.Bind<float>(weapAimAndPos, "Stance Transition Speed.", 15.0f, new ConfigDescription("Adjusts The Position Change Speed Between Stances.", new AcceptableValueRange<float>(1f, 30f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true }));
            StanceRotationSpeedMulti = Config.Bind<float>(weapAimAndPos, "Stance Rotation Speed Multi", 1f, new ConfigDescription("Adjusts The Speed Of Stance Rotation Changes.", new AcceptableValueRange<float>(1f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 146, IsAdvanced = true }));
            ThirdPersonRotationSpeed = Config.Bind<float>(weapAimAndPos, "Third Person Rotation Speed Multi", 1.0f, new ConfigDescription("Speed Of Stance Position Change In Third Person.", new AcceptableValueRange<float>(0.1f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 144, IsAdvanced = true }));
            ThirdPersonPositionSpeed = Config.Bind<float>(weapAimAndPos, "Third Person Position Speed Multi", 1.0f, new ConfigDescription("Speed Of Stance Rotation Change In Third Person.", new AcceptableValueRange<float>(0.1f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 143, IsAdvanced = true }));
            ThirdPersonRotationMulti = Config.Bind<float>(weapAimAndPos, "Third Person Rotation Multi", 2.0f, new ConfigDescription("Increases The Rotation Of High Ready And Low Ready Stances.", new AcceptableValueRange<float>(1f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 140, IsAdvanced = true }));

            ActiveAimAdditionalRotationSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Additonal Rotation Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true }));
            ActiveAimResetRotationSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Reset Rotation Speed Multi", 3.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true }));
            ActiveAimRotationMulti = Config.Bind<float>(activeAim, "Active Aim Rotation Speed Multi", 2.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 144, IsAdvanced = true }));
            ActiveAimSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Speed Multi", 10.0f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 143, IsAdvanced = true }));
            ActiveAimResetSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Reset Speed Multi", 9f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 142, IsAdvanced = true }));

            ActiveAimOffsetX = Config.Bind<float>(activeAim, "Active Aim Position X-Axis", -0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 135, IsAdvanced = true }));
            ActiveAimOffsetY = Config.Bind<float>(activeAim, "Active Aim Position Y-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 134, IsAdvanced = true }));
            ActiveAimOffsetZ = Config.Bind<float>(activeAim, "Active Aim Position Z-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 133, IsAdvanced = true }));

            ActiveAimRotationX = Config.Bind<float>(activeAim, "Active Aim Rotation X-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 122, IsAdvanced = true }));
            ActiveAimRotationY = Config.Bind<float>(activeAim, "Active Aim Rotation Y-Axis", -35.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 121, IsAdvanced = true }));
            ActiveAimRotationZ = Config.Bind<float>(activeAim, "Active Aim Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 120, IsAdvanced = true }));

            ActiveAimAdditionalRotationX = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation X-Axis", -1.5f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 111, IsAdvanced = true }));
            ActiveAimAdditionalRotationY = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation Y-Axis", -70f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true }));
            ActiveAimAdditionalRotationZ = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation Z-Axis", 2f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true }));

            ActiveAimResetRotationX = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation X-Axis", 5.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 102, IsAdvanced = true }));
            ActiveAimResetRotationY = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation Y-Axis", 50.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 101, IsAdvanced = true }));
            ActiveAimResetRotationZ = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation Z-Axis", -3.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 100, IsAdvanced = true }));

            HighReadyAdditionalRotationSpeedMulti = Config.Bind<float>(highReady, "High Ready Additonal Rotation Speed Multi", 2f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 94, IsAdvanced = true }));
            HighReadyResetRotationMulti = Config.Bind<float>(highReady, "High Ready Reset Rotation Speed Multi", 3.5f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 93, IsAdvanced = true }));
            HighReadyRotationMulti = Config.Bind<float>(highReady, "High Ready Rotation Speed Multi", 1.8f, new ConfigDescription("How Fast The Weapon Rotates Going Into Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 92, IsAdvanced = true }));
            HighReadyResetSpeedMulti = Config.Bind<float>(highReady, "High Ready Reset Speed Multi", 7.0f, new ConfigDescription("How Fast The Weapon Moves Going Out Of Stance", new AcceptableValueRange<float>(1f, 100.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 91, IsAdvanced = true }));
            HighReadySpeedMulti = Config.Bind<float>(highReady, "High Ready Speed Multi", 7.0f, new ConfigDescription("How Fast The Weapon Moves Going Into Stance", new AcceptableValueRange<float>(1f, 100.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 90, IsAdvanced = true }));

            HighReadyOffsetX = Config.Bind<float>(highReady, "High Ready Position X-Axis", 0.005f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 85, IsAdvanced = true }));
            HighReadyOffsetY = Config.Bind<float>(highReady, "High Ready Position Y-Axis", 0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 84, IsAdvanced = true }));
            HighReadyOffsetZ = Config.Bind<float>(highReady, "High Ready Position Z-Axis", -0.05f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 83, IsAdvanced = true }));

            HighReadyRotationX = Config.Bind<float>(highReady, "High Ready Rotation X-Axis", -10.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 72, IsAdvanced = true }));
            HighReadyRotationY = Config.Bind<float>(highReady, "High Ready Rotation Y-Axis", 3.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 71, IsAdvanced = true }));
            HighReadyRotationZ = Config.Bind<float>(highReady, "High Ready Rotation Z-Axis", 3.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 70, IsAdvanced = true }));

            HighReadyAdditionalRotationX = Config.Bind<float>(highReady, "High Ready Additional Rotation X-Axis", -10.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 69, IsAdvanced = true }));
            HighReadyAdditionalRotationY = Config.Bind<float>(highReady, "High Ready Additiona Rotation Y-Axis", 5.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 68, IsAdvanced = true }));
            HighReadyAdditionalRotationZ = Config.Bind<float>(highReady, "High Ready Additional Rotation Z-Axis", 1f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 67, IsAdvanced = true }));

            HighReadyResetRotationX = Config.Bind<float>(highReady, "High Ready Reset Rotation X-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 66, IsAdvanced = true }));
            HighReadyResetRotationY = Config.Bind<float>(highReady, "High Ready Reset Rotation Y-Axis", 3f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 65, IsAdvanced = true }));
            HighReadyResetRotationZ = Config.Bind<float>(highReady, "High Ready Reset Rotation Z-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true }));

            LowReadyAdditionalRotationSpeedMulti = Config.Bind<float>(lowReady, "Low Ready Additonal Rotation Speed Multi", 0.5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true }));
            LowReadyResetRotationMulti = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Speed Multi", 2.5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 63, IsAdvanced = true }));
            LowReadyRotationMulti = Config.Bind<float>(lowReady, "Low Ready Rotation Speed Multi", 2.5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 62, IsAdvanced = true }));
            LowReadySpeedMulti = Config.Bind<float>(lowReady, "Low Ready Speed Multi", 18f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 61, IsAdvanced = true }));
            LowReadyResetSpeedMulti = Config.Bind<float>(lowReady, "Low Ready Reset Speed Multi", 7.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 60, IsAdvanced = true }));

            LowReadyOffsetX = Config.Bind<float>(lowReady, "Low Ready Position X-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 55, IsAdvanced = true }));
            LowReadyOffsetY = Config.Bind<float>(lowReady, "Low Ready Position Y-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 54, IsAdvanced = true }));
            LowReadyOffsetZ = Config.Bind<float>(lowReady, "Low Ready Position Z-Axis", 0.0f, new ConfigDescription("Weapon Position When In Stance..", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 53, IsAdvanced = true }));

            LowReadyRotationX = Config.Bind<float>(lowReady, "Low Ready Rotation X-Axis", 8, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 42, IsAdvanced = true }));
            LowReadyRotationY = Config.Bind<float>(lowReady, "Low Ready Rotation Y-Axis", -5.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 41, IsAdvanced = true }));
            LowReadyRotationZ = Config.Bind<float>(lowReady, "Low Ready Rotation Z-Axis", -1.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 40, IsAdvanced = true }));

            LowReadyAdditionalRotationX = Config.Bind<float>(lowReady, "Low Ready Additional Rotation X-Axis", 12.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 39, IsAdvanced = true }));
            LowReadyAdditionalRotationY = Config.Bind<float>(lowReady, "Low Ready Additional Rotation Y-Axis", -50.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 38, IsAdvanced = true }));
            LowReadyAdditionalRotationZ = Config.Bind<float>(lowReady, "Low Ready Additional Rotation Z-Axis", 0.5f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 37, IsAdvanced = true }));

            LowReadyResetRotationX = Config.Bind<float>(lowReady, "Low Ready Reset Rotation X-Axis", -2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 36, IsAdvanced = true }));
            LowReadyResetRotationY = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Y-Axis", 2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true }));
            LowReadyResetRotationZ = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Z-Axis", -0.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true }));

            PistolAdditionalRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Additional Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true }));
            PistolResetRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Reset Rotation Speed Multi", 5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true }));
            PistolRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true }));
            PistolPosSpeedMulti = Config.Bind<float>(pistol, "Pistol Position Speed Multi", 10.0f, new ConfigDescription("", new AcceptableValueRange<float>(1.0f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true }));
            PistolPosResetSpeedMulti = Config.Bind<float>(pistol, "Pistol Position Reset Speed Multi", 7.0f, new ConfigDescription("", new AcceptableValueRange<float>(1.0f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true }));

            PistolOffsetX = Config.Bind<float>(pistol, "Pistol Position X-Axis.", 0.015f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 25, IsAdvanced = true }));
            PistolOffsetY = Config.Bind<float>(pistol, "Pistol Position Y-Axis.", 0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 24, IsAdvanced = true }));
            PistolOffsetZ = Config.Bind<float>(pistol, "Pistol Position Z-Axis.", -0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 23, IsAdvanced = true }));

            PistolRotationX = Config.Bind<float>(pistol, "Pistol Rotation X-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true }));
            PistolRotationY = Config.Bind<float>(pistol, "Pistol Rotation Y-Axis", -15f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true }));
            PistolRotationZ = Config.Bind<float>(pistol, "Pistol Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true }));

            PistolAdditionalRotationX = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation X-Axis.", 0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
            PistolAdditionalRotationY = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation Y-Axis.", -10.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
            PistolAdditionalRotationZ = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation Z-Axis.", 0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true }));

            PistolResetRotationX = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation X-Axis", 1.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
            PistolResetRotationY = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation Y-Axis", 2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true }));
            PistolResetRotationZ = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation Z-Axis", 0.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true }));

            ShortStockAdditionalRotationSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Additional Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true }));
            ShortStockResetRotationSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Reset Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true }));
            ShortStockRotationMulti = Config.Bind<float>(shortStock, "Short-Stock Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true }));
            ShortStockSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Position Speed Multi", 5.0f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true }));
            ShortStockResetSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Position Reset Speed Mult", 6.0f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true }));

            ShortStockOffsetX = Config.Bind<float>(shortStock, "Short-Stock Position X-Axis", 0.02f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 25, IsAdvanced = true }));
            ShortStockOffsetY = Config.Bind<float>(shortStock, "Short-Stock Position Y-Axis", 0.1f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 24, IsAdvanced = true }));
            ShortStockOffsetZ = Config.Bind<float>(shortStock, "Short-Stock Position Z-Axis", -0.025f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 23, IsAdvanced = true }));

            ShortStockRotationX = Config.Bind<float>(shortStock, "Short-Stock Rotation X-Axis", 0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true }));
            ShortStockRotationY = Config.Bind<float>(shortStock, "Short-Stock Rotation Y-Axis", -15.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true }));
            ShortStockRotationZ = Config.Bind<float>(shortStock, "Short-Stock Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true }));

            ShortStockAdditionalRotationX = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation X-Axis", -3.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
            ShortStockAdditionalRotationY = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation Y-Axis", -15.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
            ShortStockAdditionalRotationZ = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation Z-Axis", 1.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true }));

            ShortStockResetRotationX = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation X-Axis", -3.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
            ShortStockResetRotationY = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation Y-Axis", -2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true }));
            ShortStockResetRotationZ = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation Z-Axis", 1.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true }));
        }
    }
}
