using StayInTarkov;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    internal class ThermalVisionSetMaskPatch : ModulePatch
    {
        // This will patch the instance of the ThermalVision class to edit the T-7

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ThermalVision), nameof(ThermalVision.SetMask));
        }

        [PatchPrefix]
        private static void PatchPrefix(ref ThermalVision __instance)
        {
            if (__instance.IsPixelated)
            {
                return;
            }

            //this is all for the T7
            //__instance.TextureMask.Size = 1f;
            //__instance.ThermalVisionUtilities.MaskDescription.MaskSize = 1f; //for some reason changing mask size does not work
            __instance.ThermalVisionUtilities.MaskDescription.Mask = Plugin.maskThermal;
            __instance.ThermalVisionUtilities.MaskDescription.Mask.wrapMode = TextureWrapMode.Clamp;
            __instance.ThermalVisionUtilities.MaskDescription.OldMonocularMaskTexture = Plugin.maskThermal;
            __instance.ThermalVisionUtilities.MaskDescription.OldMonocularMaskTexture.wrapMode = TextureWrapMode.Clamp;
            __instance.ThermalVisionUtilities.MaskDescription.ThermalMaskTexture = Plugin.maskThermal;
            __instance.ThermalVisionUtilities.MaskDescription.ThermalMaskTexture.wrapMode = TextureWrapMode.Clamp;
            __instance.IsPixelated = true;
            __instance.IsNoisy = false;
            __instance.IsMotionBlurred = true;
            __instance.PixelationUtilities = new PixelationUtilities();
            __instance.PixelationUtilities.Mode = 0;
            __instance.PixelationUtilities.BlockCount = 320; //doesn't do anything really
            __instance.PixelationUtilities.PixelationMask = Plugin.maskPixel;
            __instance.PixelationUtilities.PixelationShader = Plugin.pixelationShader;
            __instance.StuckFpsUtilities = new StuckFPSUtilities();
            __instance.IsFpsStuck = true;
            __instance.StuckFpsUtilities.MinFramerate = 60;
            __instance.StuckFpsUtilities.MaxFramerate = 60;
        }
    }
}
