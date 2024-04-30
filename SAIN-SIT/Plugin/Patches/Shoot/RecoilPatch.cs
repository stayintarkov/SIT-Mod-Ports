using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using System;
using Aki.Reflection.Utils;
using System.Linq;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using EFTAimingClass = GBotAiming;

namespace SAIN.Patches.Shoot
{
    public class AimOffsetPatch : ModulePatch
    {
        private static Type _aimingDataType;
        protected override MethodBase GetTargetMethod()
        {
            _aimingDataType = PatchConstants.EftTypes.Single(x => x.GetProperty("LastSpreadCount") != null && x.GetProperty("LastAimTime") != null);
            return AccessTools.Method(_aimingDataType, "method_13");
        }

        private static float DebugTimer;

        [PatchPrefix]
        public static bool PatchPrefix(ref EFTAimingClass __instance, ref BotOwner ___botOwner_0, ref Vector3 ___vector3_5, ref Vector3 ___vector3_4, ref float ___float_13)
        {
            Vector3 badShootOffset = ___vector3_5;
            float aimUpgradeByTime = ___float_13;
            Vector3 aimOffset = ___vector3_4;
            Vector3 recoilOffset = ___botOwner_0.RecoilData.RecoilOffset;
            Vector3 realTargetPoint = __instance.RealTargetPoint;

            // Applies aiming offset, recoil offset, and scatter offsets
            // Default Setup :: Vector3 finalTarget = __instance.RealTargetPoint + badShootOffset + (AimUpgradeByTime * (AimOffset + ___botOwner_0.RecoilData.RecoilOffset));
            Vector3 finalOffset = badShootOffset + (aimUpgradeByTime * (aimOffset + recoilOffset));

            IPlayer person = ___botOwner_0?.Memory?.GoalEnemy?.Person;

            if (person != null)
            {
                if (SAINPlugin.LoadedPreset.GlobalSettings.General.HeadShotProtection)
                {
                    realTargetPoint = FindCenterMass(person);
                    finalOffset += CheckHeadShotOffset(finalOffset, realTargetPoint, ___botOwner_0, person);
                }

                if (SAINPlugin.LoadedPreset.GlobalSettings.General.NotLookingToggle)
                {
                    finalOffset += NotLookingOffset(person, ___botOwner_0);
                }
            }

            __instance.EndTargetPoint = realTargetPoint + finalOffset;
            return false;
        }

        private static Vector3 FindCenterMass(IPlayer person)
        {
            Vector3 headPos = person.MainParts[BodyPartType.head].Position;
            Vector3 floorPos = person.Position;
            Vector3 centerMass = Vector3.Lerp(headPos, floorPos, 0.35f);

            if (person.IsYourPlayer && SAINPlugin.DebugMode && _debugCenterMassTimer < Time.time)
            {
                _debugCenterMassTimer = Time.time + 1f;
                DebugGizmos.Sphere(centerMass, 0.1f, 5f);
            }

            return centerMass;
        }

        private static float _debugCenterMassTimer;

        private static Vector3 NotLookingOffset(IPlayer person, BotOwner botOwner)
        {
            if (person.IsAI == false)
            {
                float ExtraSpread = SAINNotLooking.GetSpreadIncrease(botOwner);
                if (ExtraSpread > 0)
                {
                    Vector3 vectorSpread = UnityEngine.Random.insideUnitSphere * ExtraSpread;
                    if (SAINPlugin.DebugMode && DebugTimer < Time.time)
                    {
                        DebugTimer = Time.time + 1f;
                        Logger.LogDebug($"Increasing Spread because Player isn't looking. Magnitude: [{vectorSpread.magnitude}]");
                    }
                    return vectorSpread;
                }
            }
            return Vector3.zero;
        }

        private static Vector3 CheckHeadShotOffset(Vector3 finalOffset, Vector3 realTargetPoint, BotOwner botOwner, IPlayer person)
        {
            if (person.IsAI == true)
            {
                return Vector3.zero;
            }

            // Get the head position of a bot's current enemy if it exists
            Vector3 headPos = person.MainParts[BodyPartType.head].Position;
            // Check the Distance to the bot's aiming target, and see if its really close or on the player's head
            Vector3 headDirection = headPos - realTargetPoint;
            Vector3 offsetDirection = finalOffset;

            // Is the aim offset in the same direction as the player's head?
            float dot = Vector3.Dot(headDirection.normalized, offsetDirection.normalized);
            if (dot > 0.9f 
                && headDirection.sqrMagnitude * 0.8f < offsetDirection.sqrMagnitude)
            {
                // Shift the aim target if it was going to be a headshot
                Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
                Vector3 direction = headPos - botOwner.WeaponRoot.position;
                Vector3 offsetResult = rotation * direction.normalized * 0.25f;

                if (EFTMath.RandomBool())
                {
                    offsetResult = -offsetResult;
                }

                if (person.IsYourPlayer)
                {
                    if (SAINPlugin.DebugMode)
                    {
                        string debugString = $"Head Protection Active: Dot Product: [{dot}]";
                        SAIN.Logger.NotifyDebug(debugString);
                        SAIN.Logger.LogDebug(debugString);

                        if (debugHeadShotOffsetObject == null)
                        {
                            Color color = DebugGizmos.RandomColor;
                            debugHeadShotOffsetObject = DebugGizmos.Sphere(offsetResult + headPos, 0.05f, color, false);
                            debugHeadObject = DebugGizmos.Sphere(headPos, 0.05f, Color.red, false);
                            debugHeadLineObject = DebugGizmos.Line(headPos, offsetResult + headPos, color, 0.025f, false);
                        }
                        else
                        {
                            DebugGizmos.UpdatePositionLine(headPos, offsetResult + headPos, debugHeadLineObject);
                            debugHeadObject.transform.position = headPos;
                            debugHeadShotOffsetObject.transform.position = offsetResult + headPos;
                        }
                    }
                    else if (debugHeadShotOffsetObject != null)
                    {
                        GameObject.Destroy(debugHeadShotOffsetObject);
                        GameObject.Destroy(debugHeadObject);
                        GameObject.Destroy(debugHeadLineObject);
                    }
                }

                return offsetResult;
            }
            return Vector3.zero;
        }

        private static GameObject debugHeadShotOffsetObject;
        private static GameObject debugHeadObject;
        private static GameObject debugHeadLineObject;
    }

    public class RecoilPatch : ModulePatch
    {
        private static PropertyInfo _RecoilDataPI;

        protected override MethodBase GetTargetMethod()
        {
            _RecoilDataPI = AccessTools.Property(typeof(BotOwner), "RecoilData");
            return AccessTools.Method(_RecoilDataPI.PropertyType, "Recoil");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref Vector3 ____recoilOffset, ref BotOwner ____owner)
        {
            if (SAINPlugin.BotController == null)
            {
                Logger.LogError($"Bot Controller Null in [{nameof(RecoilPatch)}]");
                return true;
            }
            if (SAINPlugin.BotController.GetSAIN(____owner, out SAINComponentClass sain))
            {
                return false;
            }
            return true;
        }
    }

    public class LoseRecoilPatch : ModulePatch
    {
        private static PropertyInfo _RecoilDataPI;

        protected override MethodBase GetTargetMethod()
        {
            _RecoilDataPI = AccessTools.Property(typeof(BotOwner), "RecoilData");
            return AccessTools.Method(_RecoilDataPI.PropertyType, "LosingRecoil");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref Vector3 ____recoilOffset, ref BotOwner ____owner)
        {
            if (SAINPlugin.BotController == null)
            {
                Logger.LogError($"Bot Controller Null in [{nameof(LoseRecoilPatch)}]");
                return true;
            }
            if (SAINPlugin.BotController.GetSAIN(____owner, out SAINComponentClass sain))
            {
                var recoil = sain?.Info?.WeaponInfo?.Recoil;
                if (recoil != null)
                {
                    ____recoilOffset = recoil.CurrentRecoilOffset;
                    return false;
                }
            }
            return true;
        }
    }

    public class EndRecoilPatch : ModulePatch
    {
        private static PropertyInfo _RecoilDataPI;

        protected override MethodBase GetTargetMethod()
        {
            _RecoilDataPI = AccessTools.Property(typeof(BotOwner), "RecoilData");
            return AccessTools.Method(_RecoilDataPI.PropertyType, "CheckEndRecoil");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____owner)
        {
            if (SAINPlugin.BotController == null)
            {
                Logger.LogError($"Bot Controller Null in [{nameof(EndRecoilPatch)}]");
                return true;
            }
            if (SAINPlugin.BotController.GetSAIN(____owner, out SAINComponentClass sain))
            {
                return false;
            }
            return true;
        }
    }
}