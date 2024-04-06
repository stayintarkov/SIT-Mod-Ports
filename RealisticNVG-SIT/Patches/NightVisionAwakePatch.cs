using StayInTarkov;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    internal class NightVisionAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), "Awake");
        }

        [PatchPrefix]
        private static void PatchPrefix(NightVision __instance, ref Shader ___Shader) //___Shader is the same as __instance.Shader
        {
            //replaces the masks in the class NightVision and applies visual changes
            //Plugin.UltimateBloomInstance = __instance.GetComponent<UltimateBloom>(); //to disable it when NVG turns ON
            //Plugin.BloomAndFlaresInstance = __instance.GetComponent<BloomAndFlares>(); //to disable it when NVG turns ON
            __instance.AnvisMaskTexture = Plugin.maskAnvis;
            __instance.BinocularMaskTexture = Plugin.maskBino;
            __instance.OldMonocularMaskTexture = Plugin.maskMono;
            __instance.ThermalMaskTexture = Plugin.maskMono;
            __instance.Noise = Plugin.Noise;
            if(__instance.Color.g > 0.9f) //this prevents the vulcan nv scope from using the custom shader
                ___Shader = Plugin.nightVisionShader;
        }
    }
}
