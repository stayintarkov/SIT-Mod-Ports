using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EFT;
using UnityEngine;

namespace Replacements
{
    public class Replacements
    {
        private readonly HashSet<BotOwner> hashSet_0;

        private readonly List<AITaskManager.Data14> _simpleTasks;
        
        private readonly Dictionary<int, AITaskManager.Data14> _simpleTaskById;
        
        private readonly Dictionary<EAITaskGroupType, AITaskManager.Class250> _regularTasks;
        
        private readonly Queue<AITaskManager.Data14> _simpleTaskDataPool;
        
        private HashSet<int> hashSet_1;
        
        public void UpdateByUnity()
        {
            List<Task> tasks = new List<Task>();
            foreach (BotOwner botOwner in hashSet_0)
            {
                tasks.Add(DoBots(botOwner, hashSet_1));
            }

            Task.WaitAll(tasks.ToArray());
            /*foreach (BotOwner botOwner in this.hashSet_0)
            {
                DoBots(botOwner, hashSet_1);
            }*/

            this.AddFromList();
        }

        public static bool IsCanceled(AITaskManager.Data14 data)
        {
            return data.IsCancelRequested;
        }

        public void method_0()
        {
            for (int index = 0; index < _simpleTasks.Count; ++index)
            {
                DoMethod0(ref index, _simpleTasks, _simpleTaskById, _simpleTaskDataPool);
            }
            /*for (int index = 0; index < _simpleTasks.Count; ++index)
            {
                DoMethod0(ref index, _simpleTasks, _simpleTaskById, _simpleTaskDataPool);
            }*/
        }

        public static void DoMethod0(ref int index, List<AITaskManager.Data14> _simpleTasks, Dictionary<int, AITaskManager.Data14> _simpleTaskById, Queue<AITaskManager.Data14> _simpleTaskDataPool)
        {
            AITaskManager.Data14 simpleTask = _simpleTasks[index];
            if (IsCanceled(simpleTask))
            {
                _simpleTasks.RemoveAt(index);
                _simpleTaskById.Remove(simpleTask.Id);
                --index;
                method_3(simpleTask, _simpleTaskDataPool);
            }
            else if ((double) Time.time - (double) simpleTask.StartTime >= (double) simpleTask.Delay)
            {
                DoTasks0(simpleTask, index, _simpleTasks, _simpleTaskById, _simpleTaskDataPool);
            }
        }

        public void method_1()
        {
            DoMethod1(_regularTasks);
        }

        public static void DoMethod1(Dictionary<EAITaskGroupType, AITaskManager.Class250> _regularTasks)
        {
            Parallel.ForEach(_regularTasks, (regularTask) => regularTask.Value.UpdateGroup());
        }

        public static void method_3(AITaskManager.Data14 data, Queue<AITaskManager.Data14> _simpleTaskDataPool)
        {
            data.Dispose();
            _simpleTaskDataPool.Enqueue(data);
        }
        
        public static void DoTasks0(AITaskManager.Data14 simpleTask, int index, List<AITaskManager.Data14> _simpleTasks, Dictionary<int, AITaskManager.Data14> _simpleTaskById, Queue<AITaskManager.Data14> _simpleTaskDataPool)
        {
            bool flag = true;
            try
            {
                if (!((UnityEngine.Object)simpleTask.Bot == (UnityEngine.Object)null))
                {
                    if (simpleTask.Bot.BotState != EBotState.Active)
                        return;
                }

                simpleTask.Task();
            }
            catch (Exception ex)
            {
                GClass336.Instance.LogException(ex);
                flag = false;
            }
            finally
            {
                _simpleTasks.RemoveAt(index);
                _simpleTaskById.Remove(simpleTask.Id);
                --index;
                if (flag)
                    method_3(simpleTask, _simpleTaskDataPool);
            }
        }
        
        public static async Task DoBots(BotOwner botOwner, HashSet<int> hashSet_1)
        {
            try
            {
                try
                {
                    botOwner.UpdateManual();
                }
                catch
                {
                    if (!hashSet_1.Contains(botOwner.Id))
                        hashSet_1.Add(botOwner.Id);
                }
            }
            catch {}
        }
        
        public void AddFromList()
        {
            
        }
    }
}