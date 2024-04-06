using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using StayInTarkov;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;
using VersionChecker;

//use Comfort.Common for Singletons instead of RootMotion
using Singleton = Comfort.Common;

#pragma warning disable IDE0007

namespace dvize.FUInertia
{
    [BepInPlugin("com.dvize.FUInertia", "dvize.FUInertia", "2.2.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> tiltSpeed;
        public static ConfigEntry<float> tiltSensitivity;
        public static ConfigEntry<float> bodygravity;
        public static ConfigEntry<float> effectorlinkweight;
        public static ConfigEntry<float> weightCarryingTotalMultiplier;

        private void Awake()
        {
            //CheckEftVersion();

            tiltSpeed = Config.Bind(
               "Final IK",
               "tilt Speed (peek)",
               1000f,
               "FinalIK - Default Settings: 6");

            tiltSensitivity = Config.Bind(
                "Final IK",
                "tilt Sensitivity (peek)",
                10f,
                "FinalIK - Default Settings: 0.07");

            bodygravity = Config.Bind(
                "Final IK",
                "body gravity (body part gravity)",
                0f,
                "FinalIK - Default Settings: None");

            weightCarryingTotalMultiplier = Config.Bind(
                "Weight",
                "Weight Carry Capacity Multiplier",
                1f,
                "Modify from 1 (normal) to multiply the amount of weight you can carry");

            new inertiaOnWeightUpdatedPatch().Enable();
            new SprintAccelerationPatch().Enable();
            //new AnimatorTransitionSpeedPatch().Enable();
            new UpdateWeightLimitsPatch().Enable();

        }

        Player player;
        RootMotion.FinalIK.Inertia inertiaIK;
        RootMotion.FinalIK.BodyTilt bodytilt;
        private void Update()
        {
            try
            {
                if (Singleton<AbstractGame>.Instance.InRaid && Camera.main.transform.position != null)
                {
                    if (player == null)
                    {
                        player = Singleton<GameWorld>.Instance.MainPlayer;
                    }

                    if (inertiaIK == null)
                    {
                        inertiaIK = Singleton<Inertia>.Instance;
                    }

                    if (bodytilt == null)
                    {
                        bodytilt = Singleton<BodyTilt>.Instance;
                    }

                    bodytilt.tiltSpeed = Plugin.tiltSpeed.Value;
                    bodytilt.tiltSensitivity = Plugin.tiltSensitivity.Value;

                    foreach (Inertia.Body body in inertiaIK.bodies)
                    {
                        body.transform.position = Vector3.zero;
                        body.transform.localPosition = Vector3.zero;
                        body.gravity = Plugin.bodygravity.Value;

                        foreach (Inertia.Body.EffectorLink effectorlink in body.effectorLinks)
                        {
                            effectorlink.weight = Plugin.effectorlinkweight.Value;
                        }
                    }

                    //sidestep is that manual stepping out and aiming
                    Singleton<EFTHardSettings>.Instance.IdleStateMotionPreservation = 0.0f;
                    Singleton<EFTHardSettings>.Instance.DecelerationSpeed = 9999.0f;
                    Singleton<EFTHardSettings>.Instance.StrafeInertionCoefficient = 0f;
                    Singleton<EFTHardSettings>.Instance.StrafeInertionCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

                    Logger.LogInfo("CurrentAnimatorState: " + player.MovementContext.CurrentState.Name);
                    Logger.LogInfo("CurrentAnimatorStateIndex: " + player.MovementContext.CurrentAnimatorStateIndex);
                }
            }
            catch { }


        }

        private void CheckEftVersion()
        {
            // Make sure the version of EFT being run is the correct version
            int currentVersion = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart;
            int buildVersion = TarkovVersion.BuildVersion;
            if (currentVersion != buildVersion)
            {
                Logger.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                EFT.UI.ConsoleScreen.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                // We ignore this for now...
                //throw new Exception($"Invalid EFT Version ({currentVersion} != {buildVersion})");
            }
        }

    }


    public class inertiaOnWeightUpdatedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Physical), "OnWeightUpdated");
        }

        [PatchPrefix]
        private static bool Prefix(Physical __instance, float ___float_3)
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            if (player.InteractablePlayer.IsYourPlayer)
            {
                //use reflection to grab protected InventoryControllerClass _inventoryController from Player
                FieldInfo inventoryControllerField = AccessTools.Field(typeof(Player), "_inventoryController");
                InventoryController inventoryController = (InventoryController)inventoryControllerField.GetValue(player);

                //switch total weight from actual calc since interactable observer is locked down and too lazy
                
                float totalWeight = (player.InteractablePlayer.Skills.StrengthBuffElite ? (inventoryController.Inventory.TotalWeightEliteSkill * Plugin.weightCarryingTotalMultiplier.Value)
                    : (inventoryController.Inventory.TotalWeight * Plugin.weightCarryingTotalMultiplier.Value));

                BackendConfigSettingsClass.InertiaSettings inertia = Singleton<BackendConfigSettingsClass>.Instance.Inertia;
                //__instance.Inertia = __instance.CalculateValue(__instance.BaseInertiaLimits, totalWeight);
                __instance.Inertia = 0f;
                __instance.SprintAcceleration = inertia.SprintAccelerationLimits.InverseLerp(__instance.Inertia);
                __instance.PreSprintAcceleration = inertia.PreSprintAccelerationLimits.Evaluate(__instance.Inertia);
                float num = Mathf.Lerp(inertia.MinMovementAccelerationRangeRight.x, inertia.MaxMovementAccelerationRangeRight.x, __instance.Inertia);
                float num2 = Mathf.Lerp(inertia.MinMovementAccelerationRangeRight.y, inertia.MaxMovementAccelerationRangeRight.y, __instance.Inertia);
                EFTHardSettings.Instance.MovementAccelerationRange.MoveKey(1, new Keyframe(num, num2));
                __instance.Overweight = __instance.BaseOverweightLimits.InverseLerp(totalWeight);
                __instance.WalkOverweight = __instance.WalkOverweightLimits.InverseLerp(totalWeight);
                ___float_3 = __instance.SprintOverweightLimits.InverseLerp(totalWeight);
                __instance.WalkSpeedLimit = 1f - __instance.WalkSpeedOverweightLimits.InverseLerp(totalWeight);
                __instance.MoveSideInertia = 0f;
                __instance.MoveDiagonalInertia = inertia.DiagonalTime.Evaluate(__instance.Inertia);


                __instance.FallDamageMultiplier = Mathf.Lerp(1f, __instance.StaminaParameters.FallDamageMultiplier, __instance.Overweight);
                __instance.SoundRadius = __instance.StaminaParameters.SoundRadius.Evaluate(__instance.Overweight);
                __instance.MinStepSound.SetDirty();
                __instance.TransitionSpeed.SetDirty();

                //invoke method_3 and method_7 using reflection
                MethodInfo method_3 = AccessTools.Method(AccessTools.TypeByName("GClass602"), "method_3");
                MethodInfo method_7 = AccessTools.Method(AccessTools.TypeByName("GClass602"), "method_7");

                method_3.Invoke(__instance, null);
                method_7.Invoke(__instance, new object[] { totalWeight });
                
                /*__instance.method_3();
                __instance.method_7(totalWeight);*/

                return false;
            }

            return true;
        }
    }
    public class UpdateWeightLimitsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Physical), "UpdateWeightLimits");
        }

        [PatchPostfix]
        static void Postfix(Physical __instance)
        {
            // Set the Vector2 variables to zero. Something here causes strength to raise properly
            __instance.BaseInertiaLimits = Vector3.zero;
        }
    }

    public class EnableInertPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerAnimator), nameof(PlayerAnimator.EnableInert));
        }

        [PatchPrefix]
        static bool Prefix(PlayerAnimator __instance, int ___INERT_PARAM_HASH, int ___INERT_FLOAT_PARAM_HASH)
        {
            //removed ref bool enabled because always going to seet false
            __instance.Animator.SetBool(___INERT_PARAM_HASH, false);
            __instance.Animator.SetFloat(___INERT_FLOAT_PARAM_HASH, 0f);

            return false;
        }
    }

    /*public class AnimatorTransitionSpeedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerAnimator), nameof(PlayerAnimator.SetTransitionSpeed));
        }

        [PatchPrefix]
        static bool Prefix(PlayerAnimator __instance, ref float speed)
        {
            __instance.Animator.SetFloat(PlayerAnimator.TRANSITION_SPEED_HASH, Plugin.SetTransitionSpeedFloat.Value);

            return false;
        }
    }*/

    

    public class SprintAccelerationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), "SprintAcceleration");
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, float deltaTime, Player ____player, GClass709 ____averageRotationX)
        {

            bool inRaid = Singleton<AbstractGame>.Instance.InRaid;

            if (____player.IsYourPlayer && inRaid)
            {
                float num = ____player.Physical.SprintAcceleration * deltaTime;
                float num2 = (____player.Physical.SprintSpeed * __instance.SprintingSpeed + 1f) * __instance.StateSprintSpeedLimit;
                float num3 = Mathf.Max(EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(Mathf.Abs((float)____averageRotationX.Average)), EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(2.14748365E+09f) * (2f));
                num2 = Mathf.Clamp(num2 * num3, 0.1f, num2);
                __instance.SprintSpeed = Mathf.Clamp(__instance.SprintSpeed + num * Mathf.Sign(num2 - __instance.SprintSpeed), 0.01f, num2);

                return false;

            }

            return true;
        }
    }

    

}