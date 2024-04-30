using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.Components;
using SAIN.Helpers;
using System.Reflection;
using UnityEngine;
using DrakiaXYZ.BigBrain.Brains;
using UnityEngine.AI;
using SAIN.Layers;
using System;
using UnityEngine.UIElements;

namespace SAIN.Patches.Generic
{
    public class KickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.Interact));
            //return typeof(BotOwner)?.GetProperty("DoorOpener")?.PropertyType?.GetMethod("Interact", BindingFlags.Instance | BindingFlags.Public);
        }

        public static bool Enabled = true;

        [PatchPrefix]
        public static void PatchPrefix(ref BotOwner ____owner, Door door, ref EInteractionType Etype)
        {
            if (____owner == null || Enabled == false)
            {
                return;
            }

            EnemyInfo enemy = ____owner.Memory.GoalEnemy;
            if (enemy == null || enemy.Person?.Transform == null)
            {
                if (Etype == EInteractionType.Breach)
                {
                    Etype = EInteractionType.Open;
                }
                return;
            }

            if (Etype == EInteractionType.Open || Etype == EInteractionType.Breach)
            {
                bool enemyClose = Vector3.Distance(____owner.Position, enemy.CurrPosition) < 30f;

                if (enemyClose || ____owner.Memory.IsUnderFire)
                {
                    var breakInParameters = door.GetBreakInParameters(____owner.Position);

                    if (door.BreachSuccessRoll(breakInParameters.InteractionPosition))
                    {
                        Etype = EInteractionType.Breach;
                    }
                    else
                    {
                        Etype = EInteractionType.Open;
                    }
                }
                else
                {
                    Etype = EInteractionType.Open;
                }
            }
        }
    }
}
