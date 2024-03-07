using System.Reflection;
using Comfort.Common;
using EFT;
using StayInTarkov;

namespace SAIN.Plugin.Patches
{
    public class BotsControllerStopPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            // Stop updating debug overlays
            if (Singleton<GameWorld>.Instance.gameObject.TryGetComponent(out Components.DebugData debugController))
            {
                debugController.enabled = false;
            }
        }
    }
}