using System.Collections.Concurrent;
using System.Collections.Generic;
using StayInTarkov;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.Components;
using SAIN.Helpers;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using DrakiaXYZ.BigBrain.Brains;
using UnityEngine.AI;
using SAIN.Layers;
using Comfort.Common;
using Mono.WebBrowser;

namespace SAIN.Patches.Generic
{
    internal class BotGroupAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotsGroup).GetMethod("AddEnemy");
        [PatchPrefix]
        public static bool PatchPrefix(IPlayer person)
        {
            if (person == null || (person.IsAI && person.AIData?.BotOwner?.GetPlayer == null))
            {
                return false;
            }

            return true;
        }
    }

    internal class BotMemoryAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotMemoryClass).GetMethod("AddEnemy");
        [PatchPrefix]
        public static bool PatchPrefix(IPlayer enemy)
        {
            if (enemy == null || (enemy.IsAI && enemy.AIData?.BotOwner?.GetPlayer == null))
            {
                return false;
            }

            return true;
        }
    }

    public class GrenadeThrownActionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "method_4");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotsController __instance, Grenade grenade, Vector3 position, Vector3 force, float mass)
        {
            Vector3 danger = Vector.DangerPoint(position, force, mass);
            foreach (BotOwner bot in __instance.Bots.BotOwners)
            {
                if (SAINPlugin.BotController.Bots.ContainsKey(bot.ProfileId))
                {
                    continue;
                }
                bot.BewareGrenade.AddGrenadeDanger(danger, grenade);
            }
            return false;
        }
    }

    public class GrenadeExplosionActionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "method_3");
        }

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            return false;
        }
    }

    public class GetBotController : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        public static void PatchPrefix(BotsController __instance)
        {
            SAINPlugin.BotController.DefaultController = __instance;
        }
    }

    public class GetBotSpawner : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), "AddPlayer");
        }

        [PatchPostfix]
        public static void PatchPostfix(BotSpawner __instance)
        {
            var controller = SAINPlugin.BotController;
            if (controller != null && controller.BotSpawner == null)
            {
                controller.BotSpawner = __instance;
            }
        }
    }
    
    // DO NOT ENABLE! For some reason this prevents BigBrain's layers from applying
    public class MultiThreadAICoreConTick : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AICoreControllerClass), "Update");
        }

        [PatchPrefix]
        public static bool PatchPrefix(AICoreControllerClass __instance, bool ___bool_0, HashSet<AbstractAiCoreAgentM> ___hashSet_0, HashSet<AbstractAiCoreAgentM> ___hashSet_1, HashSet<AbstractAiCoreAgentM> ___hashSet_2)
        {
            if (___bool_0)
                return false;
            if (___hashSet_1.Count > 0)
            {
                Parallel.ForEach(___hashSet_1, (abstractAiCoreAgentM) => ___hashSet_0.Remove(abstractAiCoreAgentM));
            }

            Parallel.ForEach(___hashSet_0, (abstractAiCoreAgentM) =>
            {
                try
                {
                    abstractAiCoreAgentM.Update();
                }
                catch (Exception ex)
                {
                    if (!___hashSet_2.Contains(abstractAiCoreAgentM))
                        ___hashSet_2.Add(abstractAiCoreAgentM);
                }
            });
            
            return false;
        }
    }

    public class BotMover3Fix : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover3), "CheckCornerIndexByReachDist");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotMover3 __instance, float distCur, Vector3 position, ref bool __result)
        {
            if (distCur == null || position == null)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
