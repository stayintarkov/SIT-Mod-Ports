using System.Reflection;
using Aki.Reflection.Patching;
using EFT.HealthSystem;
using EFT.UI.Health;
using HarmonyLib;

namespace PlayerEncumbranceBar.Patches
{
    internal class HealthParametersShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HealthParametersPanel), nameof(HealthParametersPanel.Show));
        }

        [PatchPostfix]
        public static void PatchPostfix(HealthParametersPanel __instance, HealthParameterPanel ____weight, IHealthController ___ihealthController_0)
        {
            Plugin.Instance.OnHealthParametersPanelShow(__instance, ____weight, ___ihealthController_0);
        }
    }
}
