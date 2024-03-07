using System.Reflection;
using Comfort.Common;
using EFT;
using StayInTarkov;

namespace SAIN.Plugin.Patches
{
    public class BotOwnerBrainActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod("method_10", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(BotOwner __instance)
        {
            registerBot(__instance);
        }

        private static void registerBot(BotOwner __instance)
        {
            Singleton<GameWorld>.Instance.GetComponent<Components.DebugData>().RegisterBot(__instance);
        }
    }
}