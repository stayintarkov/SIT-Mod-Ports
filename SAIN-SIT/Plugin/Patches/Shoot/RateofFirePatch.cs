using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static SAIN.Helpers.Shoot;

namespace SAIN.Patches.Shoot
{
    public class AimTimePatch : ModulePatch
    {
        private static Type _aimingDataType;
        private static MethodInfo _aimingDataMethod7;
        private static PropertyInfo _PanicingProp;

        protected override MethodBase GetTargetMethod()
        {
            //return AccessTools.Method(typeof(GClass544), "method_7");
            _aimingDataType = PatchConstants.EftTypes.Single(x => x.GetProperty("LastSpreadCount") != null && x.GetProperty("LastAimTime") != null);
            _aimingDataMethod7 = AccessTools.Method(_aimingDataType, "method_7");
            _PanicingProp = AccessTools.Property(_aimingDataType, "Boolean_0");
            return _aimingDataMethod7;
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0, float dist, float ang, ref bool ___bool_1, ref float ___float_10, ref float __result)
        {
            float aimDelay = ___float_10;
            bool moving = ___bool_1;
            bool panicing = (bool)_PanicingProp.GetValue(___botOwner_0.AimingData);

            __result = CalculateAimTime(___botOwner_0, dist, ang, moving, panicing, aimDelay);

            if (FasterCQBReactionsGlobal
                && SAINPlugin.BotController.GetSAIN(___botOwner_0, out var component))
            {
                var settings = component.Info.FileSettings.Aiming;
                if (settings.FasterCQBReactions)
                {
                    float maxDist = settings.FasterCQBReactionsDistance;
                    if (dist <= maxDist)
                    {
                        float min = settings.FasterCQBReactionsMinimum;
                        float scale = dist / maxDist;
                        scale = Mathf.Clamp(scale, min, 1f);
                        float newResult = __result * scale;
                        __result = newResult;
                    }
                }
            }
            return false;
        }

        private static bool FasterCQBReactionsGlobal => SAINPlugin.LoadedPreset?.GlobalSettings.Aiming.FasterCQBReactionsGlobal == true;

        private static float CalculateAimTime(BotOwner botOwner, float distance, float angle, bool moving, bool panicing, float aimDelay)
        {
            var settings = botOwner.Settings;
            var fileSettings = settings.FileSettings;

            float baseAimTime = fileSettings.Aiming.BOTTOM_COEF;
            if (botOwner.Memory.IsInCover)
            {
                baseAimTime *= fileSettings.Aiming.COEF_FROM_COVER;
            }

            var curve = settings.Curv;
            float angleTime = curve.AimAngCoef.Evaluate(angle);
            float distanceTime = curve.AimTime2Dist.Evaluate(distance);

            float calculatedAimTime = angleTime * distanceTime * settings.Current.CurrentAccuratySpeed;
            if (panicing)
            {
                calculatedAimTime *= fileSettings.Aiming.PANIC_COEF;
            }

            float timeToAimResult = (baseAimTime + calculatedAimTime + aimDelay);
            if (moving)
            {
                timeToAimResult *= fileSettings.Aiming.COEF_IF_MOVE;
            }

            var shootController = botOwner.WeaponManager.ShootController;
            if (shootController != null && shootController.IsAiming == true)
            {
                timeToAimResult *= 0.8f;
            }

            float timeToAimResultClamped = Mathf.Clamp(timeToAimResult, 0f, fileSettings.Aiming.MAX_AIM_TIME);

            //StringBuilder debugString = new StringBuilder();
            //debugString.AppendLine($"Calculated Aim at Distance: [{distance}] Angle: [{angle}] for Bot: [{botOwner.name}]");
            //debugString.AppendLine($"Base Aim Time: [{baseAimTime}] InCover?: [{botOwner.Memory.IsInCover}] Cover Aim Modifier: [{fileSettings.Aiming.COEF_FROM_COVER}]");
            //debugString.AppendLine($"Calculated Aim Time: [{calculatedAimTime}] Angle Time: [{angleTime}] Distance Time: [{distanceTime}] Panicing? [{panicing}] PANIC_COEF: [{fileSettings.Aiming.PANIC_COEF}]");
            //debugString.AppendLine($"Time To Aim Result: [{timeToAimResult}] Moving?: [{moving}] COEF_IF_MOVE: [{fileSettings.Aiming.COEF_IF_MOVE}]");
            //debugString.AppendLine($"Time To Aim Clamped: [{timeToAimResultClamped}] MAX_AIM_TIME: [{fileSettings.Aiming.MAX_AIM_TIME}]");
            //
            //string debugResult = debugString.ToString();
            //Logger.LogWarning( debugResult );
            //NotificationManagerClass.DisplayWarningNotification(debugResult, EFT.Communications.ENotificationDurationType.Long);

            return timeToAimResultClamped;
        }
    }

    public class FullAutoPatch : ModulePatch
    {
        private static PropertyInfo _ShootData;

        protected override MethodBase GetTargetMethod()
        {
            _ShootData = AccessTools.Property(typeof(BotOwner), "ShootData");
            return AccessTools.Method(_ShootData.PropertyType, "method_6");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____owner, ref float ___nextFingerUpTime)
        {
            if (____owner.AimingData == null)
            {
                return true;
            }

            Weapon weapon = ____owner.WeaponManager.CurrentWeapon;

            if (weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
            {
                float distance = ____owner.AimingData.LastDist2Target;
                float scaledDistance = FullAutoBurstLength(____owner, distance);

                ___nextFingerUpTime = scaledDistance + Time.time;

                return false;
            }

            ___nextFingerUpTime = 0.001f + Time.time;

            return true;
        }
    }

    public class SemiAutoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass401), "method_1");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref BotOwner ___botOwner_0, ref float __result)
        {
            if (SAINPlugin.BotController.GetSAIN(___botOwner_0, out var component))
            {
                __result = component.Info.WeaponInfo.Firerate.SemiAutoROF();
            }
        }
    }
    public class SemiAutoPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass401), "method_6");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref BotOwner ___botOwner_0, ref float __result)
        {
            if (SAINPlugin.BotController.GetSAIN(___botOwner_0, out var component))
            {
                __result = component.Info.WeaponInfo.Firerate.SemiAutoROF();
            }
        }
    }
    public class SemiAutoPatch3 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass401), "method_0");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref BotOwner ___botOwner_0, ref float __result)
        {
            if (SAINPlugin.BotController.GetSAIN(___botOwner_0, out var component))
            {
                __result = component.Info.WeaponInfo.Firerate.SemiAutoROF();
            }
        }
    }
}