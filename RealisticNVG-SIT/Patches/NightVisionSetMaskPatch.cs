using StayInTarkov;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityStandardAssets.ImageEffects;


namespace BorkelRNVG.Patches
{
    internal class NightVisionSetMaskPatch : ModulePatch
    {
        // This will patch the instance of the NightVision class
        // Thanks Fontaine, Mirni, Cj, GrooveypenguinX, Choccster, kiobu-kouhai, GrakiaXYZ, kiki, Props (sorry if i forget someone)
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.SetMask));
        }

        [PatchPrefix]
        private static void PatchPrefix(ref NightVision __instance)
        {
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
            string nvgID = player.NightVisionObserver.Component.Item.TemplateId; //ID of the nvg
            //n15 id: 5c066e3a0db834001b7353f0
            if (nvgID == "5c066e3a0db834001b7353f0")
            {
                __instance.BinocularMaskTexture = Plugin.maskBino; //makes sure the N-15 is binocular after patching the PNV-10T

            }
            //pnv10t id: 5c0696830db834001d23f5da
            else if (nvgID == "5c0696830db834001d23f5da")
            {
                __instance.BinocularMaskTexture = Plugin.maskPnv; //forces the PNV-10T to use its own unique mask

            }
        }
    }
}
