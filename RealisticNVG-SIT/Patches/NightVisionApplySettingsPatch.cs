using StayInTarkov;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using WindowsInput.Native;

namespace BorkelRNVG.Patches
{
    internal class NightVisionApplySettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.ApplySettings));
        }

        [PatchPrefix]
        private static void PatchPrefix(ref NightVision __instance, ref TextureMask ___TextureMask, ref Texture ___Mask)
        {
            ApplyModSettings(ref __instance);

            if (___TextureMask == null)
            {
                return;
            }

            int maskId = Shader.PropertyToID("_Mask");
            int invMaskSizeId = Shader.PropertyToID("_InvMaskSize");
            int invAspectId = Shader.PropertyToID("_InvAspect");
            int cameraAspectId = Shader.PropertyToID("_CameraAspect");

            var material = (Material)AccessTools.Property(__instance.GetType(), "Material_0").GetValue(__instance);

            var lensMask = Plugin.GetMatchingLensMask(___Mask);
            if (lensMask != null)
            {
                material.SetTexture(maskId, lensMask);
            }

            material.SetFloat(invMaskSizeId, 1f / __instance.MaskSize);

            float invAspectValue = ___Mask != null
                ? ___Mask.height / (float)___Mask.width
                : 1f;
            material.SetFloat(invAspectId, invAspectValue);

            var textureMaskCamera = (Camera)AccessTools.Field(___TextureMask.GetType(), "camera_0").GetValue(___TextureMask);
            float cameraAspectValue = textureMaskCamera != null
                ? textureMaskCamera.aspect
                : Screen.width / (float)Screen.height;
            material.SetFloat(cameraAspectId, cameraAspectValue);
        }

        private static void ApplyModSettings(ref NightVision nightVision)
        {
            nightVision.Color.a = (float)254 / 255; // i think it does nothing
            nightVision.MaskSize = 1; // does not affect the t-7 for some reason
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                return;
            }

            var player = gameWorld.MainPlayer;
            if (player == null)
            {
                return;
            }

            if (player.NightVisionObserver.Component == null
                || player.NightVisionObserver.Component.Item == null
                || player.NightVisionObserver.Component.Item.TemplateId == null)
            {
                return;
            }

            string nvgID = player.NightVisionObserver.Component.Item.TemplateId; // ID of the NVG
            // GPNVG-18
            if (nvgID == "5c0558060db834001b735271")
            {
                // vanilla intensity:2.27
                nightVision.Intensity = Plugin.quadGain.Value * Plugin.globalGain.Value + Plugin.quadGain.Value * Plugin.globalGain.Value * 0.3f * Plugin.gatingLevel.Value/2;
                // vanilla noiseintensity:0.02
                nightVision.NoiseIntensity = 2 * Plugin.quadNoiseIntensity.Value;
                // vanilla noisescale:5 bigger number means smaller noise
                //nightVision.NoiseScale = 0.95F; -> 0.05 in the bepinex menu, smaller number will mean smaller noise (easier for the user)
                nightVision.NoiseScale = 2f - 2 * Plugin.quadNoiseSize.Value;
                nightVision.MaskSize = Plugin.quadMaskSize.Value * Plugin.globalMaskSize.Value;
                nightVision.Color.r = Plugin.quadR.Value / 255;
                nightVision.Color.g = Plugin.quadG.Value / 255;
                nightVision.Color.b = Plugin.quadB.Value / 255;
                Plugin.nvgKey = VirtualKeyCode.NUMPAD9;
            }
            // PVS-14
            if (nvgID == "57235b6f24597759bf5a30f1")
            {
                //vanilla intensity:2.27
                nightVision.Intensity = Plugin.pvsGain.Value * Plugin.globalGain.Value + Plugin.pvsGain.Value * Plugin.globalGain.Value * 0.3f * Plugin.gatingLevel.Value/2;
                //vanilla noiseintensity:0.02
                nightVision.NoiseIntensity = 2 * Plugin.pvsNoiseIntensity.Value;
                //vanilla noisescale:5
                nightVision.NoiseScale = 2f - 2 * Plugin.pvsNoiseSize.Value;
                nightVision.MaskSize = Plugin.pvsMaskSize.Value * Plugin.globalMaskSize.Value;
                nightVision.Color.r = Plugin.pvsR.Value / 255;
                nightVision.Color.g = Plugin.pvsG.Value / 255;
                nightVision.Color.b = Plugin.pvsB.Value / 255;
                Plugin.nvgKey = VirtualKeyCode.NUMPAD8;
            }
            // N-15
            if (nvgID == "5c066e3a0db834001b7353f0")
            {
                //vanilla intensity:1.8
                nightVision.Intensity = Plugin.nGain.Value * Plugin.globalGain.Value + Plugin.nGain.Value * Plugin.globalGain.Value * 0.3f * Plugin.gatingLevel.Value/2;
                //vanilla noiseintensity:0.04
                nightVision.NoiseIntensity = 2 * Plugin.nNoiseIntensity.Value;
                //vanilla noisescale:2
                nightVision.NoiseScale = 2f - 2*Plugin.nNoiseSize.Value;
                nightVision.MaskSize = Plugin.nMaskSize.Value * Plugin.globalMaskSize.Value;
                nightVision.Color.r = Plugin.nR.Value / 255;
                nightVision.Color.g = Plugin.nG.Value / 255;
                nightVision.Color.b = Plugin.nB.Value / 255;
                Plugin.nvgKey = VirtualKeyCode.NUMPAD7;

            }
            // PNV-10T
            if (nvgID == "5c0696830db834001d23f5da")
            {
                //vanilla intensity:2
                nightVision.Intensity = Plugin.pnvGain.Value * Plugin.globalGain.Value + Plugin.pnvGain.Value * Plugin.globalGain.Value * 0.3f * Plugin.gatingLevel.Value/2;
                //vanilla noiseintensity:0.05
                nightVision.NoiseIntensity = 2*Plugin.pnvNoiseIntensity.Value;
                //vanilla noisescale:1
                nightVision.NoiseScale = 2f - 2*Plugin.pnvNoiseSize.Value;
                nightVision.MaskSize = Plugin.pnvMaskSize.Value * Plugin.globalMaskSize.Value;
                nightVision.Color.r = Plugin.pnvR.Value / 255;
                nightVision.Color.g = Plugin.pnvG.Value / 255;
                nightVision.Color.b = Plugin.pnvB.Value / 255;
                Plugin.nvgKey = VirtualKeyCode.NUMPAD6;
            }
        }
    }
}