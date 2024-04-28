using Aki.Reflection.Patching;
using EFT;
using System.Reflection;

namespace Donuts
{
    internal class BotProfilePreparationHook : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotsController).GetMethod(nameof(BotsController.AddActivePLayer));

        [PatchPrefix]
        public static void PatchPrefix() => DonutsBotPrep.Enable();
    }
}
