using BSG.CameraEffects;
using EFT.CameraControl;
using System;
using UnityEngine;

namespace BorkelRNVG
{
    public class BorkelRNVGController : MonoBehaviour
    {
        private FPSCamera fpsCamera;
        private NightVision nightVision;

        private void Start()
        {
            SubscribeSettings();
        }

        private void SubscribeSettings()
        {
            PlayerCameraController.OnPlayerCameraControllerCreated += PlayerCameraCreated;
            PlayerCameraController.OnPlayerCameraControllerDestroyed += PlayerCameraDestroyed;
            //Gating
            Plugin.gatingLevel.SettingChanged += SettingUpdated;
            // Globals
            Plugin.globalMaskSize.SettingChanged += SettingUpdated;
            Plugin.globalGain.SettingChanged += SettingUpdated;

            // GPNVG-18
            Plugin.quadGain.SettingChanged += SettingUpdated;
            Plugin.quadNoiseIntensity.SettingChanged += SettingUpdated;
            Plugin.quadNoiseSize.SettingChanged += SettingUpdated;
            Plugin.quadMaskSize.SettingChanged += SettingUpdated;
            Plugin.quadR.SettingChanged += SettingUpdated;
            Plugin.quadG.SettingChanged += SettingUpdated;
            Plugin.quadB.SettingChanged += SettingUpdated;

            // PVS-14
            Plugin.pvsGain.SettingChanged += SettingUpdated;
            Plugin.pvsNoiseIntensity.SettingChanged += SettingUpdated;
            Plugin.pvsNoiseSize.SettingChanged += SettingUpdated;
            Plugin.pvsMaskSize.SettingChanged += SettingUpdated;
            Plugin.pvsR.SettingChanged += SettingUpdated;
            Plugin.pvsG.SettingChanged += SettingUpdated;
            Plugin.pvsB.SettingChanged += SettingUpdated;

            // N-15
            Plugin.nGain.SettingChanged += SettingUpdated;
            Plugin.nNoiseIntensity.SettingChanged += SettingUpdated;
            Plugin.nNoiseSize.SettingChanged += SettingUpdated;
            Plugin.nMaskSize.SettingChanged += SettingUpdated;
            Plugin.nR.SettingChanged += SettingUpdated;
            Plugin.nG.SettingChanged += SettingUpdated;
            Plugin.nB.SettingChanged += SettingUpdated;

            // PNV-10T
            Plugin.pnvGain.SettingChanged += SettingUpdated;
            Plugin.pnvNoiseIntensity.SettingChanged += SettingUpdated;
            Plugin.pnvNoiseSize.SettingChanged += SettingUpdated;
            Plugin.pnvMaskSize.SettingChanged += SettingUpdated;
            Plugin.pnvR.SettingChanged += SettingUpdated;
            Plugin.pnvG.SettingChanged += SettingUpdated;
            Plugin.pnvB.SettingChanged += SettingUpdated;
        }

        private void PlayerCameraCreated(PlayerCameraController controller, Camera cam)
        {
            if (!FPSCamera.Exist)
            {
                return;
            }

            fpsCamera = FPSCamera.Instance;
            if (fpsCamera.NightVision != null)
            {
                nightVision = fpsCamera.NightVision;
            }
        }

        private void PlayerCameraDestroyed()
        {
            fpsCamera = null;
            nightVision = null;
        }

        private bool CheckFpsCameraExist()
        {
            if (fpsCamera != null)
            {
                return true;
            }
            return false;
        }

        private void SettingUpdated(object sender, EventArgs e)
        {
            if (nightVision == null)
            {
                if (!CheckFpsCameraExist())
                {
                    return;
                }
                nightVision = fpsCamera.NightVision;
            }
            nightVision.ApplySettings();
        }
    }
}
