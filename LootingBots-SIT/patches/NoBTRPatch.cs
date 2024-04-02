using System.Reflection;
using StayInTarkov;

/*
Since there is no BTR, we need to disable the beware BTR logic 
*/

namespace LootingBots.Patch
{
    internal class NoBTRPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass185).GetMethod(
                "Update",
                BindingFlags.Public | BindingFlags.Instance
            );
        }

        [PatchPrefix]
        private static bool PatchPrefix(GClass185 __instance)
        {
            __instance.method_0();
            return false;
        }
    }
}