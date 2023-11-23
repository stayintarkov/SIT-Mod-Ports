using StayInTarkov;
using Comfort.Common;
using EFT;
using System.Reflection;
using UnityEngine;

namespace FOVFix
{
    public class AimingSensitivityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("get_AimingSensitivity");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player.FirearmController __instance, ref float ____aimingSens)
        {

            float toggleZoomMulti = Plugin.CalledZoom && Plugin.IsAiming && Plugin.IsOptic ? Plugin.ToggleZoomOpticSensMulti.Value : Plugin.CalledZoom ? Plugin.ToggleZoomSensMulti.Value: 1f;

            if (Plugin.IsAiming)
            {
                /*  float baseSens = Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseAimingSensitivity;
                  float newSens = Mathf.Max(baseSens * (1f - ((Plugin.CurrentZoom - 1f) / Plugin.MouseSensFactor.Value)), Plugin.MouseSensLowerLimit.Value);
  */
                float newSens = 0.5f;

                if (Plugin.UseBasicSensCalc.Value)
                {
                    newSens = Singleton<SettingsManager>.Instance.Control.Settings.MouseAimingSensitivity * Utils.GetZoomSensValue(Plugin.CurrentZoom) * toggleZoomMulti;
                    Plugin.AimingSens = newSens;
                }
                else 
                {
                    Camera mainCamera = null;
                    Camera scopeCamera = null;
                    Camera[] cams = Camera.allCameras;
                    foreach (Camera cam in cams)
                    {
                        if (cam.name == "FPS Camera")
                        {
                            mainCamera = cam;
                            continue;
                        }
                        if (cam.name == "BaseOpticCamera(Clone)")
                        {
                            scopeCamera = cam;
                        }
                    }

                    float aimedFOV = !Plugin.IsOptic || scopeCamera == null ? Plugin.BaseScopeFOV.Value : scopeCamera.fieldOfView;
                    float hipFOV = Mathf.Deg2Rad * Camera.VerticalToHorizontalFieldOfView(mainCamera.fieldOfView, mainCamera.aspect);
                    float realAimedFOV = Mathf.Deg2Rad * Camera.VerticalToHorizontalFieldOfView(aimedFOV, mainCamera.aspect);
                    float exponent = 100f / Plugin.MouseSensFactor.Value;
                    float tanRatio = (float)(Mathf.Tan(realAimedFOV / 2) / Mathf.Tan(hipFOV / 2));
                    float inGameSens = Singleton<SettingsManager>.Instance.Control.Settings.MouseAimingSensitivity;
                    newSens = Mathf.Pow(tanRatio, exponent) * inGameSens * toggleZoomMulti;
                    Plugin.AimingSens = newSens;
                }

                ____aimingSens = newSens;
            }
        }
    }
}
