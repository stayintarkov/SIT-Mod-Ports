using StayInTarkov;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static EFT.Player;
using ValueHandler = GClass701;
namespace CombatStances
{

    public class ClampSpeedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext).GetMethod("ClampSpeed", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, float speed, ref float __result)
        {

            Player player = (Player)AccessTools.Field(typeof(MovementContext), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                float stanceFactor = StanceController.IsPatrolStance ? 1.25f : StanceController.IsHighReady || StanceController.IsShortStock ? 0.95f : 1f;
                __result = Mathf.Clamp(speed, 0f, __instance.StateSpeedLimit * stanceFactor);
                return false;
            }
            return true;

        }
    }


    public class SetAimingSlowdownPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext).GetMethod("SetAimingSlowdown", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref MovementContext __instance, bool isAiming, float slow)
        {

            Player player = (Player)AccessTools.Field(typeof(MovementContext), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (isAiming)
                {
                    //slow is hard set to 0.33 when called, 0.4-0.43 feels best.
                    float baseSpeed = slow + 0.07f - Plugin.AimMoveSpeedInjuryReduction;
                    float totalSpeed = StanceController.IsActiveAiming ? baseSpeed * 1.45f : baseSpeed;
                    __instance.AddStateSpeedLimit(Math.Max(totalSpeed, 0.15f), Player.ESpeedLimit.Aiming);

                    return false;
                }
                __instance.RemoveStateSpeedLimit(Player.ESpeedLimit.Aiming);
                return false;
            }
            return true;
        }
    }

    public class SprintAccelerationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext).GetMethod("SprintAcceleration", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, float deltaTime)
        {
            Player player = (Player)AccessTools.Field(typeof(MovementContext), "_player").GetValue(__instance);

            if (player.IsYourPlayer == true)
            {
                ValueHandler rotationFrameSpan = (ValueHandler)AccessTools.Field(typeof(MovementContext), "_averageRotationX").GetValue(__instance);
                float stanceAccelBonus = StanceController.IsShortStock ? 0.9f : StanceController.IsLowReady ? 1.3f : StanceController.IsHighReady && Plugin.EnableTacSprint.Value ? 1.7f : StanceController.IsHighReady ? 1.3f : 1f;
                float stanceSpeedBonus = StanceController.IsPatrolStance ? 1.5f : StanceController.IsHighReady && Plugin.EnableTacSprint.Value ? 1.15f : 1f;

                float sprintAccel = player.Physical.SprintAcceleration * deltaTime * stanceAccelBonus;
                float speed = (player.Physical.SprintSpeed * __instance.SprintingSpeed + 1f) * __instance.StateSprintSpeedLimit * stanceSpeedBonus;
                float sprintInertia = Mathf.Max(EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(Mathf.Abs((float)rotationFrameSpan.Average)), EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(2.1474836E+09f) * (2f - player.Physical.Inertia));
                speed = Mathf.Clamp(speed * sprintInertia, 0.1f, speed);
                __instance.SprintSpeed = Mathf.Clamp(__instance.SprintSpeed + sprintAccel * Mathf.Sign(speed - __instance.SprintSpeed), 0.01f, speed);

                return false;
            }
            return true;
        }
    }
}
