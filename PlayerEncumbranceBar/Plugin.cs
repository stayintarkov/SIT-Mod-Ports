using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EFT.HealthSystem;
using EFT.UI.Health;
using PlayerEncumbranceBar.Config;
using PlayerEncumbranceBar.Patches;

namespace PlayerEncumbranceBar
{
    [BepInPlugin("com.mpstark.PlayerEncumbranceBar", "PlayerEncumbranceBar", "1.0.5")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;
        public static string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public PlayerEncumbranceBarComponent PlayerEncumbranceBar;

        internal void Awake()
        {
            Settings.Init(Config);
            Config.SettingChanged += (x, y) => PlayerEncumbranceBar.OnSettingChanged();

            Instance = this;
            DontDestroyOnLoad(this);

            // patches
            new HealthParametersShowPatch().Enable();
        }

        /// <summary>
        /// Try to attach to HealthParametersPanel if needed, and call our own show method
        /// </summary>
        public void OnHealthParametersPanelShow(HealthParametersPanel parametersPanel, HealthParameterPanel weightPanel, IHealthController healthController)
        {
            if (!PlayerEncumbranceBar)
            {
                PlayerEncumbranceBar = PlayerEncumbranceBarComponent.AttachToHealthParametersPanel(parametersPanel, weightPanel, healthController);
            }

            // check if bar actually exists after trying to attach it
            if (PlayerEncumbranceBar)
            {
                PlayerEncumbranceBar.Show(healthController);
            }
        }
    }
}
