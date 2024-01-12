#define DEBUG_DETAILS
using Aki.Reflection.Patching;
using HarmonyLib;
using ThatsLit.Components;
using System.Reflection;
using UnityEngine;
using Comfort.Common;
using System.Collections.Generic;
using EFT.InventoryLogic;
using System;


namespace ThatsLit
{
    public class ExtraVisibleDistancePatch : ModulePatch
    {
        internal static System.Diagnostics.Stopwatch _benchmarkSW;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "CheckVisibility");
        }

        [PatchPrefix]
        public static bool PatchPrefix(EnemyInfo __instance, KeyValuePair<EnemyPart, EnemyPartData> part, ref float addVisibility)
        {
            if (__instance?.Owner == null
             || (part.Key?.Owner?.IsYourPlayer ?? false) == false
             || !ThatsLitPlugin.EnabledMod.Value
             || ThatsLitPlugin.LitVisionDistanceScale.Value == 0
             || !ThatsLitPlugin.EnabledLighting.Value
             || Time.frameCount % ((__instance.Owner.Id & 7) + 1) != 0) // Transform bot id to 1~7 and reduce and spread the workload to 1/7
                return true;

            ThatsLitMainPlayerComponent mainPlayer = Singleton<ThatsLitMainPlayerComponent>.Instance;
            if (mainPlayer?.scoreCalculator == null || __instance.Owner?.LookSensor == null) return true;

#region BENCHMARK
            if (ThatsLitPlugin.EnableBenchmark.Value)
            {
                if (_benchmarkSW == null) _benchmarkSW = new System.Diagnostics.Stopwatch();
                if (_benchmarkSW.IsRunning) throw new Exception("Wrong assumption");
                _benchmarkSW.Start();
            }
            else if (_benchmarkSW != null)
                _benchmarkSW = null;
#endregion

            bool thermalActive = false, nvgActive = false, scope = false;
            float scopeDis = 0;

            var botNVG = __instance.Owner?.NightVision;
            if (botNVG?.UsingNow == true) // goggles
            {
                NightVisionComponent.EMask? mask = botNVG.NightVisionItem?.Template?.Mask;
                thermalActive = mask == EFT.InventoryLogic.NightVisionComponent.EMask.Thermal;
                nvgActive = mask != null && mask != EFT.InventoryLogic.NightVisionComponent.EMask.Thermal;
            }
            else
            {
                EFT.InventoryLogic.SightComponent sightMod = __instance.Owner.GetPlayer?.ProceduralWeaponAnimation?.CurrentAimingMod;
                if (sightMod != null)
                {
                    scope = true;
                    if (Utility.IsThermalScope(sightMod.Item.TemplateId, out scopeDis))
                        thermalActive = true;
                    else if (Utility.IsNightVisionScope(sightMod.Item.TemplateId))
                        nvgActive = true;
                }
            }

            ScoreCalculator scoreCalculator = mainPlayer.scoreCalculator;
            if (thermalActive)
            {
                float compensation = (scope? scopeDis : 200) - __instance.Owner.LookSensor.VisibleDist;
                if (compensation > 0) addVisibility += UnityEngine.Random.Range(0.5f, 1f) * compensation * ThatsLitPlugin.LitVisionDistanceScale.Value;
            }
            else if (nvgActive && scoreCalculator.frame0.ambienceScore < 0)
            {
                float scale;
                if (scoreCalculator.irLight) scale = 4f;
                else if (scoreCalculator.irLaser) scale = 3.5f;
                else scale = 3f;
                scale = Mathf.Lerp(1, scale, Mathf.Clamp01(scoreCalculator.frame0.ambienceScore / -1f));

                float extra = __instance.Owner.LookSensor.VisibleDist * scoreCalculator.litScoreFactor * ThatsLitPlugin.LitVisionDistanceScale.Value * scale;
                addVisibility += UnityEngine.Random.Range(0.2f, 1f) * Mathf.Min(100, extra);
            }

#region BENCHMARK
            _benchmarkSW?.Stop();
#endregion

            return true;
        }
    }
}