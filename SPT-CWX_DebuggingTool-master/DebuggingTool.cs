using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using CWX_DebuggingTool.Helpers;
using CWX_DebuggingTool.Models;
using EFT;
using EFT.Console.Core;
using EFT.UI;
using System.Reflection;

namespace CWX_DebuggingTool
{
    [BepInPlugin("com.cwx.debuggingtool-dxyz", "cwx-debuggingtool-dxyz", "2.2.1")]
    public class DebuggingTool : BaseUnityPlugin
    {
        public static ConfigEntry<BotMonitorMode> DefaultMode;
        
        private void Awake()
        {
            DefaultMode = Config.Bind(
                "Main Settings",
                "DefaultMode",
                BotMonitorMode.None,
                "Default Mode on Startup");

            ConsoleScreen.Processor.RegisterCommandGroup<DebuggingTool>();
            new MatchStartPatch().Enable();
        }

        [ConsoleCommand("BotMonitor")]
        public static void BotMonitorConsoleCommand([ConsoleArgument("", "Options: 0 = off, 1 = Total bots, 2 = 1+Total bots per Zone, 3 = 2+Each bot")] BotMonitorMode mode )
        {
            if (mode == BotMonitorMode.None)
            {
                DisableBotMonitor();
                ConsoleScreen.Log("BotMonitor disabled");
            }
            else if (!mode.IsValid())
            {
                ConsoleScreen.LogError("Wrong Option used, please use 0, 1, 2 or 3");
            }
            else
            {
                ConsoleScreen.Log($"BotMonitor enabled with {mode.Description()}");
                EnableBotMonitor(mode);
            }
        }

        public static void DisableBotMonitor()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var btmInstance = gameWorld.GetComponent<BotmonClass>();
            if (btmInstance != null)
            {
                Destroy(btmInstance);
            }
        }

        public static void EnableBotMonitor(BotMonitorMode mode)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var btmInstance = gameWorld.GetOrAddComponent<BotmonClass>();
            btmInstance.Mode = mode;
        }

        // Add the component every time a match starts if enabled
        internal class MatchStartPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

            [PatchPrefix]
            public static void PatchPrefix()
            {
                if (DefaultMode.Value != BotMonitorMode.None)
                {
                    EnableBotMonitor(DefaultMode.Value);
                }
            }
        }
    }
}