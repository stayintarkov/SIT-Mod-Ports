using System.Reflection;
using StayInTarkov;
using HarmonyLib;

namespace NoGrenadeESP
{

    public class GrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GrenadeDangerPoint), "ShallRunAway");
        }

        [PatchPrefix]
        static bool Prefix(ref bool __result)
        {
            //Logger.LogInfo($"NoGrenadeESP: ShallRunAway from GrenadeAwarenessBehavior");
            if (UnityEngine.Random.Range(0, 100) < NoGrenadeESPPlugin.PercentageNotRunFromGrenade.Value)
            {
                __result = false;
                return false; // Skip the original method
            }

            return true; // Continue with the original method
        }
    }

    public class GrenadePatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotBewareGrenade), "ShallRunAway");
        }

        [PatchPrefix]
        static bool Prefix(ref bool __result)
        {
            //Logger.LogInfo($"NoGrenadeESP: ShallRunAway from GrenadeDangerPoint");
            if (UnityEngine.Random.Range(0, 100) < NoGrenadeESPPlugin.PercentageNotRunFromGrenade.Value)
            {
                __result = false;
                return false; // Skip the original method
            }

            return true; // Continue with the original method
        }
    }

}