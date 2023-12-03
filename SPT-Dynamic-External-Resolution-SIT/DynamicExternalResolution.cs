using BepInEx;
using Comfort.Common;
using DynamicExternalResolution.Configs;
using EFT;

namespace DynamicExternalResolution
{
    [BepInPlugin("com.DynamicExternalResolution", "Dynamic External Resolution", "1.3.0")]
    public class DynamicExternalResolution : BaseUnityPlugin
    {
        private static Player _localPlayer = null;

        public static Player getPlayerInstance()
        {
            if (_localPlayer != null)
            {
                return _localPlayer;
            }

            _localPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            return _localPlayer;
        }

        public static FPSCamera getCameraInstance()
        {
            return FPSCamera.Instance;
        }

        private void Awake()
        {
            DynamicExternalResolutionConfig.Init(Config);
            Patcher.PatchAll();
            Logger.LogInfo($"Plugin Dynamic External Resolution is loaded!");
        }

        private void OnDestroy()
        {
            Patcher.UnpatchAll();
            Logger.LogInfo($"Plugin DynamicExternalResolution is unloaded!");
        }
    }
}
