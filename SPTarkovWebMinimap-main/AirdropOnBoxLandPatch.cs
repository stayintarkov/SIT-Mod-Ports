using System;
using System.Linq;
using System.Reflection;
using SIT.Tarkov.Core;
using SIT.Core.AkiSupport.Airdrops;
using Comfort.Common;
using EFT;
using EFT.Airdrop;
using EFT.SynchronizableObjects;
using UnityEngine;

namespace TechHappy.MinimapSender
{
    public class AirdropOnBoxLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            //return typeof(AirdropPlane).GetMethod("Init", BindingFlags.Public | BindingFlags.Static);

            MethodInfo onBoxLandMethod = typeof(AirdropBox).GetMethod("OnBoxLand", BindingFlags.NonPublic | BindingFlags.Instance);

            return typeof(AirdropBox).GetMethod("OnBoxLand", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance) //ref Vector3 airdropPoint
        {
            Vector3 airdropBoxPos = __instance.transform.position;
            MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"AirdropBox OnBoxLand() was called!");
            MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Position {airdropBoxPos.x}, {airdropBoxPos.z}");

            MinimapSenderPlugin.airdrops.Add(airdropBoxPos);
        }
    }
}