using Aki.Reflection.Patching;
using StayInTarkov.AkiSupport.Airdrops;
using System.Reflection;
using UnityEngine;

namespace TechHappy.MinimapSender.Patches
{
    public class AirdropOnBoxLandPatch : ModulePatch
    {
        // Getting a method that is called when an airdrop box lands. Harmony uses this method.
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AirdropBox).GetMethod("OnBoxLand", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// Adds the airdrop's position vector to the airdrops array after the airdrop box lands.
        /// </summary>
        /// <param name="__instance">The MonoBehaviour instance.</param>
        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            Vector3 airdropBoxPos = __instance.transform.position;
            MinimapSenderPlugin.MinimapSenderLogger.LogDebug($"AirdropBox OnBoxLand() was called.");
            MinimapSenderPlugin.MinimapSenderLogger.LogDebug($"Position {airdropBoxPos.x}, {airdropBoxPos.z}");

            MinimapSenderPlugin.airdrops.Add(airdropBoxPos);
        }
    }
}