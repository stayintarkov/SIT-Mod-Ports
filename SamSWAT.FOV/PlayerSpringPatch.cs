using Aki.Reflection.Patching;
using EFT.Animations;
using System.Reflection;
using UnityEngine;

namespace SamSWAT.FOV
{
    public class PlayerSpringPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayerSpring).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref Vector3 ___CameraOffset)
        {
            ___CameraOffset = new Vector3(0.04f, 0.04f, FovPlugin.HudFov.Value);
        }
    }
}
