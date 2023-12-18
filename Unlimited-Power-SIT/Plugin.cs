using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using EFT.Communications;

namespace Power
{
    [BepInPlugin("DJ.UnlimitedPower", "DJs Unlimited Power", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static GameObject Hook;
        internal static UnlimitedPower Script;
        internal static ManualLogSource logger;
        
        internal static ConfigEntry<bool> Enablemod;
        internal static ConfigEntry<bool> ShowNotification;
        internal static ConfigEntry<int> RandomRangeMin;
        internal static ConfigEntry<int> RandomRangeMax;

        void Awake()
        {
            logger = Logger;
            Logger.LogInfo("Loading Unlimited...Powaaaaaa");
            Hook = new GameObject("Power Object");
            Script = Hook.AddComponent<UnlimitedPower>();
            DontDestroyOnLoad(Hook);

            Enablemod = Config.Bind(
                "Power",
                "Enable mod",
                true,
                new ConfigDescription("If enabled, allows power switches to be dynamically turned on at random points throughout your raids.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 1 }));

            ShowNotification = Config.Bind(
                "Power",
                "Enable Notifications",
                true,
                new ConfigDescription("If enabled, shows a notification when a switch is flipped.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 1 }));

            RandomRangeMax = Config.Bind(
               "Power",
               "Random timer range maximum",
               30,
               new ConfigDescription("The time is in minutes, cannot be lower than the minimum", new AcceptableValueRange<int>(1, 30)));

            RandomRangeMin = Config.Bind(
               "Power",
               "Random timer range minimum",
               5,
               new ConfigDescription("The time is in minutes, cannot be higher than the maximum", new AcceptableValueRange<int>(1, 30)));
        }

        // Validate the users config input
        // Set them to default if they are invalid
        void Update()
        {
            if (RandomRangeMin.Value > RandomRangeMax.Value)
            {
                RandomRangeMin.Value = 5;
                RandomRangeMax.Value = 30;
                NotificationManagerClass.DisplayMessageNotification("Unlimited Power: Config values reset minimum cannot be above maximum.", ENotificationDurationType.Default);
            }
        }
    }
}