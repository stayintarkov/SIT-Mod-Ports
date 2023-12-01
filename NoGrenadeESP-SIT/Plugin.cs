using System.Diagnostics;
using System;
using BepInEx;
using BepInEx.Configuration;
using VersionChecker;

namespace NoGrenadeESP
{

    [BepInPlugin("com.dvize.NoGrenadeESP", "dvize.NoGrenadeESP", "1.6.1")]
    class NoGrenadeESPPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<int> PercentageNotRunFromGrenade
        {
            get;
            private set;
        }
        private void Awake()
        {
            //CheckEftVersion();

            PercentageNotRunFromGrenade = Config.Bind(
                "Main Settings",
                "Percentage Chance They Do Not Run Away from Grenade",
                35,
                "Set the percentage chance here");

            new GrenadePatch().Enable();
            new GrenadePatch2().Enable();

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
}
