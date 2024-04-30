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

    //DO NOT ENABLE! Enabling causes the game to crash, needs more investigation
    public class MultiThreadBotTick : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsClass), "UpdateByUnity");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotsClass __instance, HashSet<BotOwner> ___hashSet_0, HashSet<int> ___hashSet_1)
        {
            Parallel.ForEach(___hashSet_0, (BotOwner botOwner) =>
            {
                try
                {
                        botOwner.UpdateManual();
                }
                catch (Exception ex)
                {
                    if (!___hashSet_1.Contains(botOwner.Id))
                        ___hashSet_1.Add(botOwner.Id);
                }
            });

            __instance.AddFromList();
            return false;
        }
    }

    public class SuppressExceptionUpdateManual : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "UpdateManual");
        }
        
        [PatchFinalizer]
        static Exception Finalizer()
        {
            return null;
        }
    }
    
    public class MultiThreadAITaskManTickM0 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AITaskManager), "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(AITaskManager __instance, List<AITaskManager.Data14> ____simpleTasks, Dictionary<int, AITaskManager.Data14> ____simpleTaskById)
        {
            if (____simpleTasks.Count <= 0) return false;

            ConcurrentBag<AITaskManager.Data14> runMethod3 = new ConcurrentBag<AITaskManager.Data14>();
            
            Parallel.ForEach(____simpleTasks, (simpleTask) =>
            {
                if (simpleTask.IsCancelRequested)
                {
                    ____simpleTaskById.Remove(simpleTask.Id);
                    runMethod3.Add(simpleTask);
                }
                else if ((double)Time.time - (double)simpleTask.StartTime >= (double)simpleTask.Delay)
                {
                    bool flag = true;
                    try
                    {
                        if (!((UnityEngine.Object)simpleTask.Bot == (UnityEngine.Object)null))
                        {
                            if (simpleTask.Bot.BotState != EBotState.Active)
                            {
                                return;
                            }
                        }

                        simpleTask.Task();
                    }
                    catch (Exception ex)
                    {
                        GClass336.Instance.LogException(ex);
                        SAIN.Logger.LogError(ex.Message);
                        flag = false;
                    }
                    ____simpleTaskById.Remove(simpleTask.Id);
                    SAIN.Logger.LogInfo("FLAG: " + flag);
                    if (flag)
                        runMethod3.Add(simpleTask);
                }
            });

            for (int i = 0; i < ____simpleTasks.Count; i++)
            {
                if (____simpleTasks[i].Bot != null && ____simpleTasks[i].Bot.BotState == EBotState.Active)
                {
                    ____simpleTasks.RemoveAt(i);
                    --i;
                }
            }

            foreach (AITaskManager.Data14 simpleTask in runMethod3)
            {
                __instance.method_3(simpleTask);
            }
            
            return false;
        }
    }
    
    public class MultiThreadAITaskManTickM1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AITaskManager), "method_1");
        }

        [PatchPrefix]
        public static bool PatchPrefix(AITaskManager __instance, Dictionary<EAITaskGroupType, AITaskManager.Class250> ____regularTasks)
        {
            Parallel.ForEach(____regularTasks, (regularTask) => regularTask.Value.UpdateGroup());
            return false;
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
