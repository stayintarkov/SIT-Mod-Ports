using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        private static bool bool_0;

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
            else if ((double)Time.time - (double)simpleTask.StartTime >= (double)simpleTask.Delay)
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
            catch
            {
            }
        }

        public void AddFromList()
        {

        }

        public (bool, GClass536) LoadInternal()
        {
            return DoLoadInternal();
        }

        public static (bool, GClass536) DoLoadInternal()
        {
            string coreTxt = GClass537.LoadCoreByString();
            GClass536 core;
            if (coreTxt == null)
            {
                return (false, null);
            }

            core = GClass536.Create(coreTxt);
            if (!((UnityEngine.Object)GClass770.Load<TextAsset>(string.Format("Settings/{0}_{1}_BotGlobalSettings", (object)"", (object)"")) != (UnityEngine.Object)null))
                return (false, core);
            List<Task<bool>> tasks = new List<Task<bool>>();
            foreach (WildSpawnType wildSpawnType in Enum.GetValues(typeof(WildSpawnType)))
            {

                foreach (BotDifficulty botDifficulty in Enum.GetValues(typeof(BotDifficulty)))
                {
                    tasks.Add(DOTHING1(botDifficulty, wildSpawnType, false));
                }
            }

            Task.WaitAll(tasks.ToArray());
            if (tasks.Any(t => t.Result == false)) return (false, core);

            Debug.Log((object)"Internal bot settings load");
            return (true, core);
        }

        public bool LoadExternal()
        {
            return DoLoadExternal();
        }

        public static bool DoLoadExternal()
        {
            try
            {
                string path = string.Format("Settings/{0}_{1}_BotGlobalSettings.json", "", "");
                if (!File.Exists(path))
                    return false;
                GClass537.Core = GClass536.Create(File.ReadAllText(path));
                List<Task<bool>> tasks = new List<Task<bool>>();
                foreach (BotDifficulty botDifficulty in Enum.GetValues(typeof(BotDifficulty)))
                {

                    foreach (WildSpawnType wildSpawnType in GClass537.WildSpawnType_0)
                    {
                        tasks.Add(DOTHING1(botDifficulty, wildSpawnType, true));
                    }
                }

                Task.WaitAll(tasks.ToArray());
                if (tasks.Any(t => t.Result == false)) return false;

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Can't load external settings. ex:" + ex);
                return false;
            }
        }

        public static async Task<bool> DOTHING1(BotDifficulty botDifficulty, WildSpawnType wildSpawnType, bool external)
        {
            BotSettingsComponents val = GClass537.smethod_1(GClass537.CheckOnExclude(botDifficulty, wildSpawnType), wildSpawnType, external);
            if (val == null)
                return false;
            if (!GClass537.AllSettings.ContainsKey(botDifficulty, wildSpawnType))
                GClass537.AllSettings.Add(botDifficulty, wildSpawnType, val);
            return true;
        }

        public void Load()
        {
            //Console.WriteLine("Has Field: " + (typeof(GClass537).GetField("bool_0", BindingFlags.Static | BindingFlags.NonPublic) != null));
            FieldInfo f = typeof(GClass537).GetField("bool_0", BindingFlags.Static | BindingFlags.NonPublic);
            bool temp = (bool)f.GetValue(null);
            DoLoad(ref temp);
            f.SetValue(null, temp);
        }

        public static void DoLoad(ref bool bool_0)
        {
            if (bool_0)
                return;
            bool flag1 = false;
            bool flag2 = false;
            try
            {
                if (flag1 = DoLoadExternal())
                    Debug.Log((object)"External bot settings load");
            }
            catch (Exception ex)
            {
                Debug.LogError((object)("can't load external bots global settings ex:" + ex.StackTrace));
            }

            if (!flag1)
                (flag2, GClass537.Core) = DoLoadInternal();
            if (!flag2 && !flag1)
                Debug.Log((object)"Code bot settings load");
            bool_0 = true;
        }

        public void Save(bool codeSettings)
        {
            DoSave(codeSettings);
        }

        public static void DoSave(bool codeSettings)
        {
            string path = string.Format("Assets/CommonAssets/Scripts/AI/Resources/Settings/{0}_{1}_BotGlobalSettings.json", "", "");
            (_, GClass536 core) = DoLoadInternal();
            if (codeSettings)
                GClass537.smethod_4(path, GClass537.Core.ToPrettyJson<GClass536>());
            else
                GClass537.smethod_4(path, core.ToPrettyJson<GClass536>());
            foreach (BotDifficulty botDifficulty in Enum.GetValues(typeof (BotDifficulty)))
            {
                foreach (WildSpawnType role in GClass537.WildSpawnType_0)
                    GClass537.smethod_3(botDifficulty, role, codeSettings);
            }
        }
    }
}