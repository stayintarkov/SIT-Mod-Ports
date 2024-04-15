using System;
using System.Diagnostics;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using UnityEngine;
using VersionChecker;

namespace DoorBreach
{
    [BepInPlugin("com.nektonick.ShootTheDoor", "nektonick.ShootTheDoor", DoorBreachPlugin.PluginVarsion)]
    // [BepInDependency("com.spt-aki.core", "3.7.1")]
    public class DoorBreachPlugin : BaseUnityPlugin
    {
        public const String PluginVarsion = "1.3.0";
        public const String LOCKPICK_AMMO_TAG = "#Lockpick";
        public const String TIER1_LOCKPICK_TAG = "#TIER1";
        public const String TIER2_LOCKPICK_TAG = "#TIER2";
        public const String TIER3_LOCKPICK_TAG = "#TIER3";    

        public static ConfigEntry<float> ObjectHP;

        public static ConfigEntry<float> NonLockHitDmgMult;
        public static ConfigEntry<float> LockHitDmgMult;

        public static ConfigEntry<float> ThinWoodProtectionMult;
        public static ConfigEntry<float> PlasticProtectionMult;
        public static ConfigEntry<float> ThickWoodProtectionMult;
        public static ConfigEntry<float> ThinMetalProtectionMult;
        public static ConfigEntry<float> ThickMetalProtectionMult;

        public static ConfigEntry<float> MeeleWeaponDamageMult;

        public static ConfigEntry<float> Tier0LockpickAmmoBaseDamage;
        public static ConfigEntry<float> Tier0LockpickAmmoDamageMult;
        public static ConfigEntry<float> Tier1LockpickAmmoBaseDamage;
        public static ConfigEntry<float> Tier1LockpickAmmoDamageMult;
        public static ConfigEntry<float> Tier2LockpickAmmoBaseDamage;
        public static ConfigEntry<float> Tier2LockpickAmmoDamageMult;
        public static ConfigEntry<float> Tier3LockpickAmmoBaseDamage;
        public static ConfigEntry<float> Tier3LockpickAmmoDamageMult;

        public static int interactiveLayer;
        private void Awake()
        {
            // CheckEftVersion();

            ObjectHP = Config.Bind("1. HP", "ObjectHP", 200F);

            NonLockHitDmgMult = Config.Bind("2. Lock Hits", "NonLockHitDmgMult", 0.5F);
            LockHitDmgMult = Config.Bind("2. Lock Hits", "LockHitDmgMult", 2F);

            ThinWoodProtectionMult = Config.Bind("3. Material Protection", "ThinWoodProtectionMult", 3F);
            PlasticProtectionMult = Config.Bind("3. Material Protection", "PlasticProtectionMult", 3F);
            ThickWoodProtectionMult = Config.Bind("3. Material Protection", "ThickWoodProtectionMult", 5F);
            ThinMetalProtectionMult = Config.Bind("3. Material Protection", "ThinMetalProtectionMult", 10F);
            ThickMetalProtectionMult = Config.Bind("3. Material Protection", "ThickMetalProtectionMult", 15F);

            MeeleWeaponDamageMult = Config.Bind("4. Specific Weapon", "MeeleWeaponDamageMult", 5F);

            Tier0LockpickAmmoBaseDamage = Config.Bind("5. Lockpick ammo", "Tier0LockpickAmmoBaseDamage", 100F);
            Tier0LockpickAmmoDamageMult = Config.Bind("5. Lockpick ammo", "Tier0LockpickAmmoDamageMult", 1F);
            Tier1LockpickAmmoBaseDamage = Config.Bind("5. Lockpick ammo", "Tier1LockpickAmmoBaseDamage", 500F);
            Tier1LockpickAmmoDamageMult = Config.Bind("5. Lockpick ammo", "Tier1LockpickAmmoDamageMult", 1F);
            Tier2LockpickAmmoBaseDamage = Config.Bind("5. Lockpick ammo", "Tier2LockpickAmmoBaseDamage", 2500F);
            Tier2LockpickAmmoDamageMult = Config.Bind("5. Lockpick ammo", "Tier2LockpickAmmoDamageMult", 1F);
            Tier3LockpickAmmoBaseDamage = Config.Bind("5. Lockpick ammo", "Tier3LockpickAmmoBaseDamage", 10000F);
            Tier3LockpickAmmoDamageMult = Config.Bind("5. Lockpick ammo", "Tier3LockpickAmmoDamageMult", 1F);

            new NewGamePatch().Enable();
            new ShootTheDoor.ApplyHit().Enable();
        }

        private void CheckEftVersion()
        {
            // Make sure the version of EFT being run is the correct version
            int currentVersion = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart;
            int buildVersion = TarkovVersion.BuildVersion;
            if (currentVersion != buildVersion)
            {
                Logger.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                EFT.UI.ConsoleScreen.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                throw new Exception($"Invalid EFT Version ({currentVersion} != {buildVersion})");
            }
        }
    }

    //re-initializes each new game
    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix()
        {
            //stolen from drakiaxyz - thanks
            DoorBreachPlugin.interactiveLayer = LayerMask.NameToLayer("Interactive");

            ShootTheDoor.DoorBreachComponent.Enable();
        }
    }
}
