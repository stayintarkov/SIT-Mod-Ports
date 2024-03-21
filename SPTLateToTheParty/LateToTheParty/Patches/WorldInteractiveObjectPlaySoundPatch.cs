using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.Interactive;

namespace LateToTheParty.Patches
{
    public class WorldInteractiveObjectPlaySoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WorldInteractiveObject).GetMethod("PlaySound", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(WorldInteractiveObject __instance)
        {
            if (!Controllers.SwitchController.HasToggledInitialSwitches)
            {
                Controllers.LoggingController.LogWarning("Suppressing sound for " + __instance.Id + " until all initial switches have been toggled");

                return false;
            }

            return true;
        }
    }
}
