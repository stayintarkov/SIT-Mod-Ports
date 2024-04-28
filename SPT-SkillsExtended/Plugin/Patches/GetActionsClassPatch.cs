using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using SkillsExtended.Helpers;
using System.Reflection;

namespace SkillsExtended.Patches
{
    internal class GetActionsClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GetActionsClass).GetMethod("smethod_3", BindingFlags.Public | BindingFlags.Static);

        [PatchPostfix]
        private static void Postfix(ref InteractionStates __result, GamePlayerOwner owner, WorldInteractiveObject worldInteractiveObject)
        {
            if (WorldInteractionUtils.IsBotInteraction(owner))
            {
                return;
            }

            worldInteractiveObject.AddLockpickingInteraction(__result, owner);
            worldInteractiveObject.AddInspectInteraction(__result, owner);
        }
    }
}