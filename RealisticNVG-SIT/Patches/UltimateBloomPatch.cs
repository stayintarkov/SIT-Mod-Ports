using StayInTarkov;
using BSG.CameraEffects;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    internal class UltimateBloomPatch : ModulePatch //if Awake is prevented from running, the exaggerated bloom goes away
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UltimateBloom), "Awake");
        }

        [PatchPrefix]
        private static void PatchPrefix(UltimateBloom __instance)
        {
            //Plugin.UltimateBloomInstance = __instance; //to disable it when NVG turns ON
            //__instance.gameObject.SetActive(false);
            //return true;
            //__instance.m_FlareMask = Plugin.maskFlare;
            //Plugin.UltimateBloomInstance.gameObject.SetActive(false);
        }
    }
}
