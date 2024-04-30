using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Weather;
using Interpolation;
using SAIN.Preset.GlobalSettings.Categories;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;

using BotEventHandler = GClass603;

namespace SAIN.Helpers
{
    public class AudioHelpers
    {
        public static void TryPlayShootSound(Player player)
        {
            if (player != null)
            {
                float range = 125f;
                AISoundType soundType = AISoundType.gun;

                FirearmController controller = player.HandsController as FirearmController;
                if (controller?.Item != null)
                {
                    PlayerWeaponInfoContainer info = SAINWeaponInfoHandler.GetPlayerWeaponInfo(player);
                    if (info != null)
                    {
                        var weaponInfo = info.GetWeaponInfo(controller.Item);
                        if (weaponInfo != null)
                        {
                            weaponInfo.TryCalculate();
                            range = weaponInfo.CalculatedAudibleRange;
                            soundType = weaponInfo.AISoundType;
                        }

                        info.PlayAISound(range * RainSoundModifier(), soundType);
                        return;
                    }
                }

                // If for some reason we can't get the weapon info on this player, just play the default sound
                if (nextShootTime < Time.time && Singleton<BotEventHandler>.Instantiated)
                {
                    nextShootTime = Time.time + 0.1f;
                    Singleton<BotEventHandler>.Instance.PlaySound(player, player.WeaponRoot.position, range * RainSoundModifier(), soundType);
                    Logger.LogWarning($"Could not find Weapon Info for [{player.Profile.Nickname}]!");
                }
            }
        }

        private static float nextShootTime;

        public static float RainSoundModifier()
        {
            if (WeatherController.Instance?.WeatherCurve == null)
                return 1f;

            if (RainCheckTimer < Time.time)
            {
                RainCheckTimer = Time.time + 10f;
                // Grabs the current rain Rounding
                float Rain = WeatherController.Instance.WeatherCurve.Rain;
                RainModifier = 1f;
                float max = 1f;
                float rainMin = 0.65f;

                Rain = InverseScaling(Rain, rainMin, max);

                // Combines ModifiersClass and returns
                RainModifier *= Rain;
            }
            return RainModifier;
        }

        public static float InverseScaling(float value, float min, float max)
        {
            // Inverse
            float InverseValue = 1f - value;

            // Scaling
            float ScaledValue = (InverseValue * (max - min)) + min;

            value = ScaledValue;

            return value;
        }

        private static float RainCheckTimer = 0f;
        private static float RainModifier = 1f;
    }
}