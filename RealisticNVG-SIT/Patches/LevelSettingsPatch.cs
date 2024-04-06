using StayInTarkov;
using BSG.CameraEffects;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BorkelRNVG.Patches
{
    internal class LevelSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LevelSettings), "method_0");
        }

        [PatchPrefix]
        private static void PatchPrefix(LevelSettings __instance)
        {
            Logger.LogMessage($"LevelSettings patch {__instance.AmbientType}");
            Logger.LogMessage($"nvcolor: {__instance.NightVisionSkyColor}");
            Logger.LogMessage($"scolor: {__instance.SkyColor}");
            __instance.NightVisionSkyColor = __instance.SkyColor;
        }
    }
}
