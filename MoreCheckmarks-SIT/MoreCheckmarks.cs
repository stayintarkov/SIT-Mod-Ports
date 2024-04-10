using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using System;
using EFT;
using EFT.UI.DragAndDrop;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.Interactive;
using EFT.Quests;
using System.Linq;
using TMPro;
using BepInEx;
using Aki.Common.Http;
using Comfort.Common;

using InteractionController = GetActionsClass;
using InteractionInstance = InteractionStates;
using Action = Action1;
using QuestTemplate = RawQuestClass;
using EFT.Hideout;
using Newtonsoft.Json;

namespace MoreCheckmarks
{
    public struct NeededStruct
    {
        public bool foundNeeded;
        public bool foundFulfilled;
        public int possessedCount;
        public int requiredCount;
    }

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class MoreCheckmarksMod : BaseUnityPlugin
    {
        // BepinEx
        public const string pluginGuid = "VIP.TommySoucy.MoreCheckmarks";
        public const string pluginName = "MoreCheckmarks";
        public const string pluginVersion = "1.5.13";

        // Config settings
        public static bool fulfilledAnyCanBeUpgraded = false;
        public static int questPriority = 2;
        public static int hideoutPriority = 3;
        public static int wishlistPriority = 4;
        public static int barterPriority = 0;
        public static int craftPriority = 1;
        public static bool showFutureModulesLevels = false;
        public static bool showBarter = true;
        public static bool showCraft = true;
        public static bool showFutureCraft = true;
        public static Color needMoreColor = new Color(1, 0.37255f, 0.37255f);
        public static Color fulfilledColor = new Color(0.30588f, 1, 0.27843f);
        public static Color wishlistColor = new Color(0, 0, 1);
        public static Color barterColor = new Color(1, 0, 1);
        public static Color craftColor = new Color(0, 1, 1);
        public static bool includeFutureQuests = true;

        // Assets
        public static JObject config;
        public static Sprite whiteCheckmark;
        private static TMP_FontAsset benderBold;
        public static string modPath;

        // Live
        public static MoreCheckmarksMod modInstance;
        // Quest IDs and Names by items in their requirements
        public static Dictionary<string, QuestPair> questDataStartByItemTemplateID = new Dictionary<string, QuestPair>();
        public static Dictionary<string, Dictionary<string, int>> neededStartItemsByQuest = new Dictionary<string, Dictionary<string, int>>();
        public static Dictionary<string, QuestPair> questDataCompleteByItemTemplateID = new Dictionary<string, QuestPair>();
        public static Dictionary<string, Dictionary<string, int>> neededCompleteItemsByQuest = new Dictionary<string, Dictionary<string, int>>();
        public class QuestPair
        {
            public Dictionary<string, string> questData = new Dictionary<string, string>();
            public int count = 0;
        }
        public static JObject itemData;
        public static JObject locales;
        public static Dictionary<string, string> productionEndProductByID = new Dictionary<string, string>();
        // Barter item name and amount of price by items in price
        public static List<Dictionary<string, List<KeyValuePair<string, int>>>> bartersByItemByTrader = new List<Dictionary<string, List<KeyValuePair<string, int>>>>();
        public static string[] traders = new string[] { "Prapor", "Therapist", "Fence", "Skier", "Peacekeeper", "Mechanic", "Ragman", "Jaeger", "Lighthouse keeper" };
        public static int[] priorities = new int[] { 0, 1, 2, 3, 4 };
        public static bool[] neededFor = new bool[5];
        public static Color[] colors = new Color[] { Color.yellow, needMoreColor, wishlistColor, barterColor, craftColor };

        private void Start()
        {
            Logger.LogInfo("MoreCheckmarks Started");

            modInstance = this;

            Init();
        }

        private void Init()
        {
            modPath = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(MoreCheckmarksMod)).Location);
            modPath.Replace('\\', '/');

            LoadConfig();

            LoadAssets();

            LoadData();

            DoPatching();
        }

        private static string GetBackendUrl()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args == null)
                return null;

            var beUrl = string.Empty;

            foreach (string arg in args)
            {
                if (arg.Contains("BackendUrl"))
                {
                    string json = arg.Replace("-config=", string.Empty);
                    dynamic result = JsonConvert.DeserializeObject(json);
                    if (result != null)
                        beUrl = result.BackendUrl;
                    break;
                }
            }

            return beUrl;
        }

        public void LoadData()
        {
            LogInfo("Loading data");
            LogInfo("\tQuests");
            string backendUrl = GetBackendUrl();
            bool hasTrailing = backendUrl.EndsWith(@"/");
            string path = "/MoreCheckmarksRoutes/quests";
            if (hasTrailing)
                path = path.Substring(1);

            JArray questData = JArray.Parse(RequestHandler.GetJson(path, false));
            questDataStartByItemTemplateID.Clear();
            neededStartItemsByQuest.Clear();
            questDataCompleteByItemTemplateID.Clear();
            neededCompleteItemsByQuest.Clear();

            for (int i = 0; i < questData.Count; ++i)
            {
                if (questData[i]["conditions"] != null && questData[i]["conditions"]["AvailableForFinish"] != null)
                {
                    JArray availableForFinishConditions = questData[i]["conditions"]["AvailableForFinish"] as JArray;
                    for (int j = 0; j < availableForFinishConditions.Count; ++j)
                    {
                        if (availableForFinishConditions[j]["conditionType"] != null)
                        {
                            if (availableForFinishConditions[j]["conditionType"].ToString().Equals("HandoverItem"))
                            {
                                if (availableForFinishConditions[j]["target"] != null)
                                {
                                    JArray targets = availableForFinishConditions[j]["target"] as JArray;
                                    for (int k = 0; k < targets.Count; ++k)
                                    {
                                        if (questDataCompleteByItemTemplateID.TryGetValue(targets[k].ToString(), out QuestPair quests))
                                        {
                                            if (!quests.questData.ContainsKey(questData[i]["name"].ToString()))
                                            {
                                                quests.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            }
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            quests.count += parsedValue;
                                        }
                                        else
                                        {
                                            QuestPair newPair = new QuestPair();
                                            newPair.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            newPair.count = parsedValue;
                                            questDataCompleteByItemTemplateID.Add(targets[k].ToString(), newPair);
                                        }

                                        if (neededCompleteItemsByQuest.TryGetValue(questData[i]["_id"].ToString(), out Dictionary<string, int> items))
                                        {
                                            if (!items.ContainsKey(targets[k].ToString()))
                                            {
                                                items.Add(targets[k].ToString(), 0);
                                            }
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            items[targets[k].ToString()] += parsedValue;
                                        }
                                        else
                                        {
                                            Dictionary<string, int> newDict = new Dictionary<string, int>();
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            newDict.Add(targets[k].ToString(), parsedValue);
                                            neededCompleteItemsByQuest.Add(questData[i]["_id"].ToString(), newDict);
                                        }
                                    }
                                }
                                else
                                {
                                    LogError("Quest " + questData[i]["_id"].ToString() + " finish condition " + j + " of type HandoverItem missing target");
                                }
                            }

                            if (availableForFinishConditions[j]["conditionType"].ToString().Equals("FindItem"))
                            {
                                if (availableForFinishConditions[j]["target"] != null)
                                {
                                    JArray targets = availableForFinishConditions[j]["target"] as JArray;
                                    for (int k = 0; k < targets.Count; ++k)
                                    {
                                        // Check if there is a hand in item condition for the same item and at least the same count
                                        // If so skip this, we will count the hand in instead
                                        bool foundInHandin = false;
                                        for (int l = 0; l < availableForFinishConditions.Count; ++l)
                                        {
                                            if (availableForFinishConditions[l]["conditionType"].ToString().Equals("HandoverItem"))
                                            {
                                                JArray handInTargets = availableForFinishConditions[l]["target"] as JArray;
                                                if (handInTargets != null && StringJArrayContainsString(handInTargets, targets[k].ToString()) &&
                                                    (!int.TryParse(availableForFinishConditions[l]["value"].ToString(), out int parsedValue) ||
                                                     !int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int currentParsedValue) ||
                                                     parsedValue == currentParsedValue))
                                                {
                                                    foundInHandin = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (foundInHandin)
                                        {
                                            continue;
                                        }

                                        if (questDataCompleteByItemTemplateID.TryGetValue(targets[k].ToString(), out QuestPair quests))
                                        {
                                            if (!quests.questData.ContainsKey(questData[i]["name"].ToString()))
                                            {
                                                quests.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            }
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            quests.count += parsedValue;
                                        }
                                        else
                                        {
                                            QuestPair newPair = new QuestPair();
                                            newPair.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            newPair.count = parsedValue;
                                            questDataCompleteByItemTemplateID.Add(targets[k].ToString(), newPair);
                                        }

                                        if (neededCompleteItemsByQuest.TryGetValue(questData[i]["_id"].ToString(), out Dictionary<string, int> items))
                                        {
                                            if (!items.ContainsKey(targets[k].ToString()))
                                            {
                                                items.Add(targets[k].ToString(), 0);
                                            }
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            items[targets[k].ToString()] += parsedValue;
                                        }
                                        else
                                        {
                                            Dictionary<string, int> newDict = new Dictionary<string, int>();
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            newDict.Add(targets[k].ToString(), parsedValue);
                                            neededCompleteItemsByQuest.Add(questData[i]["_id"].ToString(), newDict);
                                        }
                                    }
                                }
                                else
                                {
                                    LogError("Quest " + questData[i]["_id"].ToString() + " finish condition " + j + " of type FindItem missing target");
                                }
                            }

                            if (availableForFinishConditions[j]["conditionType"].ToString().Equals("LeaveItemAtLocation"))
                            {
                                if (availableForFinishConditions[j]["target"] != null)
                                {
                                    JArray targets = availableForFinishConditions[j]["target"] as JArray;
                                    for (int k = 0; k < targets.Count; ++k)
                                    {
                                        if (questDataCompleteByItemTemplateID.TryGetValue(targets[k].ToString(), out QuestPair quests))
                                        {
                                            if (!quests.questData.ContainsKey(questData[i]["name"].ToString()))
                                            {
                                                quests.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            }
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            quests.count += parsedValue;
                                        }
                                        else
                                        {
                                            QuestPair newPair = new QuestPair();
                                            newPair.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            newPair.count = parsedValue;
                                            questDataCompleteByItemTemplateID.Add(targets[k].ToString(), newPair);
                                        }

                                        if (neededCompleteItemsByQuest.TryGetValue(questData[i]["_id"].ToString(), out Dictionary<string, int> items))
                                        {
                                            if (!items.ContainsKey(targets[k].ToString()))
                                            {
                                                items.Add(targets[k].ToString(), 0);
                                            }
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            items[targets[k].ToString()] += parsedValue;
                                        }
                                        else
                                        {
                                            Dictionary<string, int> newDict = new Dictionary<string, int>();
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            newDict.Add(targets[k].ToString(), parsedValue);
                                            neededCompleteItemsByQuest.Add(questData[i]["_id"].ToString(), newDict);
                                        }
                                    }
                                }
                                else
                                {
                                    LogError("Quest " + questData[i]["_id"].ToString() + " finish condition " + j + " of type LeaveItemAtLocation missing target");
                                }
                            }

                            if (availableForFinishConditions[j]["conditionType"].ToString().Equals("PlaceBeacon"))
                            {
                                if (availableForFinishConditions[j]["target"] != null)
                                {
                                    JArray targets = availableForFinishConditions[j]["target"] as JArray;
                                    for (int k = 0; k < targets.Count; ++k)
                                    {
                                        if (questDataCompleteByItemTemplateID.TryGetValue(targets[k].ToString(), out QuestPair quests))
                                        {
                                            if (!quests.questData.ContainsKey(questData[i]["name"].ToString()))
                                            {
                                                quests.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            }
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            quests.count += parsedValue;
                                        }
                                        else
                                        {
                                            QuestPair newPair = new QuestPair();
                                            newPair.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            newPair.count = parsedValue;
                                            questDataCompleteByItemTemplateID.Add(targets[k].ToString(), newPair);
                                        }

                                        if (neededCompleteItemsByQuest.TryGetValue(questData[i]["_id"].ToString(), out Dictionary<string, int> items))
                                        {
                                            if (!items.ContainsKey(targets[k].ToString()))
                                            {
                                                items.Add(targets[k].ToString(), 0);
                                            }
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            items[targets[k].ToString()] += parsedValue;
                                        }
                                        else
                                        {
                                            Dictionary<string, int> newDict = new Dictionary<string, int>();
                                            int.TryParse(availableForFinishConditions[j]["value"].ToString(), out int parsedValue);
                                            newDict.Add(targets[k].ToString(), parsedValue);
                                            neededCompleteItemsByQuest.Add(questData[i]["_id"].ToString(), newDict);
                                        }
                                    }
                                }
                                else
                                {
                                    LogError("Quest " + questData[i]["_id"].ToString() + " finish condition " + j + " of type PlaceBeacon missing target");
                                }
                            }
                        }
                        else
                        {
                            LogError("Quest " + questData[i]["_id"].ToString() + " finish condition " + j + " missing condition type");
                        }
                    }
                }
                else
                {
                    LogError("Quest " + questData[i]["_id"].ToString() + " missing finish conditions");
                }

                if (questData[i]["conditions"] != null && questData[i]["conditions"]["AvailableForFinish"] != null)
                {
                    JArray availableForStartConditions = questData[i]["conditions"]["AvailableForStart"] as JArray;
                    for (int j = 0; j < availableForStartConditions.Count; ++j)
                    {
                        if (availableForStartConditions[j]["conditionType"] != null)
                        {
                            if (availableForStartConditions[j]["conditionType"].ToString().Equals("HandoverItem"))
                            {
                                if (availableForStartConditions[j]["target"] != null)
                                {
                                    JArray targets = availableForStartConditions[j]["target"] as JArray;
                                    for (int k = 0; k < targets.Count; ++k)
                                    {
                                        if (questDataStartByItemTemplateID.TryGetValue(targets[k].ToString(), out QuestPair quests))
                                        {
                                            if (!quests.questData.ContainsKey(questData[i]["name"].ToString()))
                                            {
                                                quests.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            }
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            quests.count += parsedValue;
                                        }
                                        else
                                        {
                                            QuestPair newPair = new QuestPair();
                                            newPair.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            newPair.count = parsedValue;
                                            questDataStartByItemTemplateID.Add(targets[k].ToString(), newPair);
                                        }

                                        if (neededStartItemsByQuest.TryGetValue(questData[i]["_id"].ToString(), out Dictionary<string, int> items))
                                        {
                                            if (!items.ContainsKey(targets[k].ToString()))
                                            {
                                                items.Add(targets[k].ToString(), 0);
                                            }
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            items[targets[k].ToString()] += parsedValue;
                                        }
                                        else
                                        {
                                            Dictionary<string, int> newDict = new Dictionary<string, int>();
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            newDict.Add(targets[k].ToString(), parsedValue);
                                            neededStartItemsByQuest.Add(questData[i]["_id"].ToString(), newDict);
                                        }
                                    }
                                }
                                else
                                {
                                    LogError("Quest " + questData[i]["_id"].ToString() + " start condition " + j + " of type HandoverItem missing target");
                                }
                            }

                            if (availableForStartConditions[j]["conditionType"].ToString().Equals("FindItem"))
                            {
                                if (availableForStartConditions[j]["target"] != null)
                                {
                                    JArray targets = availableForStartConditions[j]["target"] as JArray;
                                    for (int k = 0; k < targets.Count; ++k)
                                    {
                                        // Check if there is a hand in item condition for the same item and at least the same count
                                        // If so skip this, we will count the hand in instead
                                        bool foundInHandin = false;
                                        for (int l = 0; l < availableForStartConditions.Count; ++l)
                                        {
                                            if (availableForStartConditions[l]["conditionType"].ToString().Equals("HandoverItem"))
                                            {
                                                JArray handInTargets = availableForStartConditions[l]["target"] as JArray;
                                                if (handInTargets != null && StringJArrayContainsString(handInTargets, targets[k].ToString()) &&
                                                    (!int.TryParse(availableForStartConditions[l]["value"].ToString(), out int parsedValue) ||
                                                     !int.TryParse(availableForStartConditions[j]["value"].ToString(), out int currentParsedValue) ||
                                                     parsedValue == currentParsedValue))
                                                {
                                                    foundInHandin = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (foundInHandin)
                                        {
                                            continue;
                                        }

                                        if (questDataStartByItemTemplateID.TryGetValue(targets[k].ToString(), out QuestPair quests))
                                        {
                                            if (!quests.questData.ContainsKey(questData[i]["name"].ToString()))
                                            {
                                                quests.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            }
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            quests.count += parsedValue;
                                        }
                                        else
                                        {
                                            QuestPair newPair = new QuestPair();
                                            newPair.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            newPair.count = parsedValue;
                                            questDataStartByItemTemplateID.Add(targets[k].ToString(), newPair);
                                        }

                                        if (neededStartItemsByQuest.TryGetValue(questData[i]["_id"].ToString(), out Dictionary<string, int> items))
                                        {
                                            if (!items.ContainsKey(targets[k].ToString()))
                                            {
                                                items.Add(targets[k].ToString(), 0);
                                            }
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            items[targets[k].ToString()] += parsedValue;
                                        }
                                        else
                                        {
                                            Dictionary<string, int> newDict = new Dictionary<string, int>();
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            newDict.Add(targets[k].ToString(), parsedValue);
                                            neededStartItemsByQuest.Add(questData[i]["_id"].ToString(), newDict);
                                        }
                                    }
                                }
                                else
                                {
                                    LogError("Quest " + questData[i]["_id"].ToString() + " start condition " + j + " of type FindItem missing target");
                                }
                            }

                            if (availableForStartConditions[j]["conditionType"].ToString().Equals("LeaveItemAtLocation"))
                            {
                                if (availableForStartConditions[j]["target"] != null)
                                {
                                    JArray targets = availableForStartConditions[j]["target"] as JArray;
                                    for (int k = 0; k < targets.Count; ++k)
                                    {
                                        if (questDataStartByItemTemplateID.TryGetValue(targets[k].ToString(), out QuestPair quests))
                                        {
                                            if (!quests.questData.ContainsKey(questData[i]["name"].ToString()))
                                            {
                                                quests.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            }
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            quests.count += parsedValue;
                                        }
                                        else
                                        {
                                            QuestPair newPair = new QuestPair();
                                            newPair.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            newPair.count = parsedValue;
                                            questDataStartByItemTemplateID.Add(targets[k].ToString(), newPair);
                                        }

                                        if (neededStartItemsByQuest.TryGetValue(questData[i]["_id"].ToString(), out Dictionary<string, int> items))
                                        {
                                            if (!items.ContainsKey(targets[k].ToString()))
                                            {
                                                items.Add(targets[k].ToString(), 0);
                                            }
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            items[targets[k].ToString()] += parsedValue;
                                        }
                                        else
                                        {
                                            Dictionary<string, int> newDict = new Dictionary<string, int>();
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            newDict.Add(targets[k].ToString(), parsedValue);
                                            neededStartItemsByQuest.Add(questData[i]["_id"].ToString(), newDict);
                                        }
                                    }
                                }
                                else
                                {
                                    LogError("Quest " + questData[i]["_id"].ToString() + " start condition " + j + " of type LeaveItemAtLocation missing target");
                                }
                            }

                            if (availableForStartConditions[j]["conditionType"].ToString().Equals("PlaceBeacon"))
                            {
                                if (availableForStartConditions[j]["target"] != null)
                                {
                                    JArray targets = availableForStartConditions[j]["target"] as JArray;
                                    for (int k = 0; k < targets.Count; ++k)
                                    {
                                        if (questDataStartByItemTemplateID.TryGetValue(targets[k].ToString(), out QuestPair quests))
                                        {
                                            if (!quests.questData.ContainsKey(questData[i]["name"].ToString()))
                                            {
                                                quests.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            }
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            quests.count += parsedValue;
                                        }
                                        else
                                        {
                                            QuestPair newPair = new QuestPair();
                                            newPair.questData.Add(questData[i]["name"].ToString(), questData[i]["QuestName"].ToString());
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            newPair.count = parsedValue;
                                            questDataStartByItemTemplateID.Add(targets[k].ToString(), newPair);
                                        }

                                        if (neededStartItemsByQuest.TryGetValue(questData[i]["_id"].ToString(), out Dictionary<string, int> items))
                                        {
                                            if (!items.ContainsKey(targets[k].ToString()))
                                            {
                                                items.Add(targets[k].ToString(), 0);
                                            }
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            items[targets[k].ToString()] += parsedValue;
                                        }
                                        else
                                        {
                                            Dictionary<string, int> newDict = new Dictionary<string, int>();
                                            int.TryParse(availableForStartConditions[j]["value"].ToString(), out int parsedValue);
                                            newDict.Add(targets[k].ToString(), parsedValue);
                                            neededStartItemsByQuest.Add(questData[i]["_id"].ToString(), newDict);
                                        }
                                    }
                                }
                                else
                                {
                                    LogError("Quest " + questData[i]["_id"].ToString() + " start condition " + j + " of type PlaceBeacon missing target");
                                }
                            }
                        }
                        else
                        {
                            LogError("Quest " + questData[i]["_id"].ToString() + " start condition " + j + " missing condition type");
                        }
                    }
                }
                else
                {
                    LogError("Quest " + questData[i]["_id"].ToString() + " missing start conditions");
                }
            }

            LogInfo("\tItems");
            string euro = "569668774bdc2da2298b4568";
            string rouble = "5449016a4bdc2d6f028b456f";
            string dollar = "5696686a4bdc2da3298b456a";
            if (itemData == null)
            {
                itemData = JObject.Parse(RequestHandler.GetJson("/MoreCheckmarksRoutes/items"));
            }

            LogInfo("\tAssorts");
            string jsonAssorts = RequestHandler.GetJson("/MoreCheckmarksRoutes/assorts");

            // Check if the received JSON string is not null or empty
            if (!string.IsNullOrEmpty(jsonAssorts))
            {
                // Attempt to parse the JSON string to determine its format
                try
                {
                    JToken token = JToken.Parse(jsonAssorts);

                    if (token is JArray)
                    {
                        // If the JSON is an array (expected for assorts data)
                        JArray assortData = (JArray)token;
                        bartersByItemByTrader.Clear();

                        for (int i = 0; i < assortData.Count; ++i)
                        {
                            bartersByItemByTrader.Add(new Dictionary<string, List<KeyValuePair<string, int>>>());
                            JArray items = assortData[i]["items"] as JArray;

                            for (int j = 0; j < items.Count; ++j)
                            {
                                if (items[j]["parentId"] != null && items[j]["parentId"].ToString().Equals("hideout"))
                                {
                                    JToken bartersToken = assortData[i]["barter_scheme"][items[j]["_id"].ToString()];

                                    if (bartersToken is JArray)
                                    {
                                        JArray barters = (JArray)bartersToken;

                                        for (int k = 0; k < barters.Count; ++k)
                                        {
                                            JArray barter = barters[k] as JArray;

                                            for (int l = 0; l < barter.Count; ++l)
                                            {
                                                string priceTPL = barter[l]["_tpl"].ToString();

                                                // Check if priceTPL is not a standard currency (euro, rouble, dollar)
                                                if (!priceTPL.Equals(euro) && !priceTPL.Equals(rouble) && !priceTPL.Equals(dollar))
                                                {
                                                    if (bartersByItemByTrader[i].TryGetValue(priceTPL, out List<KeyValuePair<string, int>> barterList))
                                                    {
                                                        barterList.Add(new KeyValuePair<string, int>(items[j]["_tpl"].ToString(), (int)(barter[l]["count"])));
                                                    }
                                                    else
                                                    {
                                                        bartersByItemByTrader[i].Add(priceTPL, new List<KeyValuePair<string, int>>() { new KeyValuePair<string, int>(items[j]["_tpl"].ToString(), (int)(barter[l]["count"])) });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (token is JObject)
                    {
                        // Handle the case where the JSON is an object instead of an array
                        LogError("Unexpected JSON format for Assorts: Received an object instead of an array");
                    }
                    else
                    {
                        // Handle other unexpected JSON formats
                        LogError("Unexpected JSON format for Assorts: Unknown format");
                    }
                }
                catch (JsonReaderException ex)
                {
                    LogError("Error parsing Assorts JSON: " + ex.Message);
                }
            }
            else
            {
                // Handle case where JSON string is empty or null
                LogError("No JSON data received for Assorts");
            }


            LogInfo("\tProductions");
            string jsonProductions = RequestHandler.GetJson("/MoreCheckmarksRoutes/productions");

            // Check if the received JSON string is not null or empty
            if (!string.IsNullOrEmpty(jsonProductions))
            {
                // Attempt to parse the JSON string to determine its format
                try
                {
                    JToken token = JToken.Parse(jsonProductions);

                    if (token is JArray)
                    {
                        // If the JSON is an array (expected for productions data)
                        JArray productionData = (JArray)token;
                        productionEndProductByID.Clear();

                        for (int i = 0; i < productionData.Count; ++i)
                        {
                            productionEndProductByID.Add(productionData[i]["_id"].ToString(), productionData[i]["endProduct"].ToString());
                        }
                    }
                    else if (token is JObject)
                    {
                        // Handle the case where the JSON is an object instead of an array
                        LogError("Unexpected JSON format for Productions: Received an object instead of an array");
                    }
                    else
                    {
                        // Handle other unexpected JSON formats
                        LogError("Unexpected JSON format for Productions: Unknown format");
                    }
                }
                catch (JsonReaderException ex)
                {
                    LogError("Error parsing Productions JSON: " + ex.Message);
                }
            }
            else
            {
                // Handle case where JSON string is empty or null
                LogError("No JSON data received for Productions");
            }

        }

        private bool StringJArrayContainsString(JArray arr, string s)
        {
            for (int i = 0; i < arr.Count; ++i)
            {
                if (arr[i].ToString().Equals(s))
                {
                    return true;
                }
            }
            return false;
        }

        private void LoadConfig()
        {
            try
            {
                config = JObject.Parse(File.ReadAllText(modPath + "/Config.json"));

                if (config["fulfilledAnyCanBeUpgraded"] != null)
                {
                    fulfilledAnyCanBeUpgraded = (bool)config["fulfilledAnyCanBeUpgraded"];
                }
                if (config["questPriority"] != null)
                {
                    questPriority = (int)config["questPriority"];
                    priorities[0] = questPriority;
                }
                if (config["hideoutPriority"] != null)
                {
                    hideoutPriority = (int)config["hideoutPriority"];
                    priorities[1] = hideoutPriority;
                }
                if (config["wishlistPriority"] != null)
                {
                    wishlistPriority = (int)config["wishlistPriority"];
                    priorities[2] = wishlistPriority;
                }
                if (config["barterPriority"] != null)
                {
                    barterPriority = (int)config["barterPriority"];
                    priorities[3] = barterPriority;
                }
                if (config["craftPriority"] != null)
                {
                    craftPriority = (int)config["craftPriority"];
                    priorities[4] = craftPriority;
                }
                if (config["showFutureModulesLevels"] != null)
                {
                    showFutureModulesLevels = (bool)config["showFutureModulesLevels"];
                }
                if (config["showBarter"] != null)
                {
                    showBarter = (bool)config["showBarter"];
                }
                if (config["needMoreColor"] != null)
                {
                    needMoreColor = new Color((float)config["needMoreColor"][0], (float)config["needMoreColor"][1], (float)config["needMoreColor"][2]);
                }
                if (config["fulfilledColor"] != null)
                {
                    fulfilledColor = new Color((float)config["fulfilledColor"][0], (float)config["fulfilledColor"][1], (float)config["fulfilledColor"][2]);
                }
                if (config["wishlistColor"] != null)
                {
                    wishlistColor = new Color((float)config["wishlistColor"][0], (float)config["wishlistColor"][1], (float)config["wishlistColor"][2]);
                    colors[2] = wishlistColor;
                }
                if (config["barterColor"] != null)
                {
                    barterColor = new Color((float)config["barterColor"][0], (float)config["barterColor"][1], (float)config["barterColor"][2]);
                    colors[3] = barterColor;
                }
                if (config["craftColor"] != null)
                {
                    craftColor = new Color((float)config["craftColor"][0], (float)config["craftColor"][1], (float)config["craftColor"][2]);
                    colors[4] = craftColor;
                }
                if (config["includeFutureQuests"] != null)
                {
                    includeFutureQuests = (bool)config["includeFutureQuests"];
                }
                if (config["showCraft"] != null)
                {
                    showCraft = (bool)config["showCraft"];
                }
                if (config["showFutureCraft"] != null)
                {
                    showFutureCraft = (bool)config["showFutureCraft"];
                }

                Logger.LogInfo("Configs loaded");
            }
            catch (FileNotFoundException) { /* In case of file not found, we don't want to do anything, user prob deleted it for a reason */ }
            catch (Exception ex) { MoreCheckmarksMod.LogError("Couldn't read MoreCheckmarksConfig.txt, using default settings instead. Error: " + ex.Message); }
        }

        private void LoadAssets()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(modPath + "/MoreCheckmarksAssets");

            if (assetBundle == null)
            {
                MoreCheckmarksMod.LogError("Failed to load assets, inspect window checkmark may be miscolored");
            }
            else
            {
                whiteCheckmark = assetBundle.LoadAsset<Sprite>("WhiteCheckmark");

                benderBold = assetBundle.LoadAsset<TMP_FontAsset>("BenderBold");
                TMP_Text.OnFontAssetRequest += TMP_Text_onFontAssetRequest;

                MoreCheckmarksMod.LogInfo("Assets loaded");
            }
        }

        public static TMP_FontAsset TMP_Text_onFontAssetRequest(int hash, string name)
        {
            if (name.Equals("BENDERBOLD"))
            {
                return benderBold;
            }
            else
            {
                return null;
            }
        }

        public static void DoPatching()
        {
            // Get assemblies
            Type ProfileSelector = null;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; ++i)
            {
                if (assemblies[i].GetName().Name.Equals("Assembly-CSharp"))
                {
                    // UPDATE: This is to know when a new profile is selected so we can load up to date data
                    // We want to do this when client makes request "/client/game/profile/select"
                    // Look for that string in dnspy, this creates a callback with a method_0, that is the method we want to postfix
                    ProfileSelector = assemblies[i].GetType("TradingBackend1+Class1300");
                }
            }

            var harmony = new HarmonyLib.Harmony("VIP.TommySoucy.MoreCheckmarks");

            // Auto patch
            harmony.PatchAll();

            // Manual patch
            MethodInfo profileSelectorOriginal = ProfileSelector.GetMethod("method_0", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo profileSelectorPostfix = typeof(ProfileSelectionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(profileSelectorOriginal, null, new HarmonyMethod(profileSelectorPostfix));
        }

        public static NeededStruct GetNeeded(string itemTemplateID, ref List<string> areaNames)
        {
            NeededStruct neededStruct = new NeededStruct();
            neededStruct.possessedCount = 0;
            neededStruct.requiredCount = 0;

            try
            {
                HideoutClass hideoutInstance = Comfort.Common.Singleton<HideoutClass>.Instance;
                foreach (EFT.Hideout.AreaData ad in hideoutInstance.AreaDatas)
                {
                    // Skip if don't have area data
                    if (ad == null || ad.Template == null || ad.Template.Name == null || ad.NextStage == null)
                    {
                        continue;
                    }

                    // Skip if the area has no future upgrade
                    if (ad.Status == EFT.Hideout.EAreaStatus.NoFutureUpgrades)
                    {
                        continue;
                    }

                    // Collect all future stages
                    List<EFT.Hideout.Stage> futureStages = new List<EFT.Hideout.Stage>();
                    EFT.Hideout.Stage lastStage = ad.CurrentStage;
                    while ((lastStage = ad.StageAt(lastStage.Level + 1)) != null && lastStage.Level != 0)
                    {
                        // Don't want to check requirements for an area we are currently constructing/upgrading
                        if (ad.Status == EFT.Hideout.EAreaStatus.Constructing || ad.Status == EFT.Hideout.EAreaStatus.Upgrading)
                        {
                            continue;
                        }
                        futureStages.Add(lastStage);

                        // If only want next level requirements, skip the rest
                        if (!MoreCheckmarksMod.showFutureModulesLevels)
                        {
                            break;
                        }
                    }

                    // Skip are if no stages were found to check requirements for
                    if (futureStages.Count == 0)
                    {
                        continue;
                    }

                    // Check requirements
                    foreach (EFT.Hideout.Stage stage in futureStages)
                    {
                        EFT.Hideout.RelatedRequirements requirements = stage.Requirements;

                        try
                        {
                            foreach (var requirement in requirements)
                            {
                                if (requirement != null)
                                {
                                    EFT.Hideout.ItemRequirement itemRequirement = requirement as EFT.Hideout.ItemRequirement;
                                    if (itemRequirement != null)
                                    {
                                        string requirementTemplate = itemRequirement.TemplateId;
                                        if (itemTemplateID == requirementTemplate)
                                        {
                                            // Sum up the total amount of this item required in entire hideout and update possessed amount
                                            neededStruct.requiredCount += itemRequirement.IntCount;
                                            neededStruct.possessedCount = itemRequirement.UserItemsCount;

                                            // A requirement but already have the amount we need
                                            if (requirement.Fulfilled)
                                            {
                                                // Even if we have enough of this item to fulfill a requirement in one area
                                                // we might still need it, and if thats the case we want to show that color, not fulfilled color, so you know you still need more of it
                                                // So only set color to fulfilled if not needed
                                                if (!neededStruct.foundNeeded && !neededStruct.foundFulfilled)
                                                {
                                                    neededStruct.foundFulfilled = true;
                                                }

                                                if (areaNames != null)
                                                {
                                                    areaNames.Add("<color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.fulfilledColor) + ">" + ad.Template.Name + " lvl" + stage.Level + "</color>");
                                                }
                                            }
                                            else
                                            {
                                                if (!neededStruct.foundNeeded)
                                                {
                                                    neededStruct.foundNeeded = true;
                                                }

                                                if (areaNames != null)
                                                {
                                                    areaNames.Add("<color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.needMoreColor) + ">" + ad.Template.Name + " lvl" + stage.Level + "</color>");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            MoreCheckmarksMod.LogError("Failed to get whether item " + itemTemplateID + " was needed for hideout area: " + ad.Template.Name);
                        }
                    }
                }
            }
            catch (Exception)
            {
                MoreCheckmarksMod.LogError("Failed to get whether item " + itemTemplateID + " was needed for hideout upgrades.");
            }

            return neededStruct;
        }

        public static bool GetNeededCraft(string itemTemplateID, ref string tooltip, bool needTooltip = true)
        {
            bool required = false;
            bool gotTooltip = false;
            try
            {
                HideoutClass hideoutInstance = Comfort.Common.Singleton<HideoutClass>.Instance;
                foreach (EFT.Hideout.AreaData ad in hideoutInstance.AreaDatas)
                {
                    // Skip if don't have area data
                    if (ad == null || ad.Template == null || ad.Template.Name == null)
                    {
                        continue;
                    }

                    // Get stage to check productions of
                    // Productions are cumulative, a stage will have productions of all previous stages
                    Stage currentStage = ad.CurrentStage;
                    if (currentStage == null)
                    {
                        int level = 0;
                        while (currentStage == null)
                        {
                            currentStage = ad.StageAt(level++);
                        }
                    }
                    if (currentStage != null)
                    {
                        Stage newStage = ad.StageAt(currentStage.Level + 1);
                        while (newStage != null && newStage.Level != 0)
                        {
                            if (newStage.Level > ad.CurrentLevel && !showFutureCraft)
                            {
                                break;
                            }
                            currentStage = newStage;
                            newStage = ad.StageAt(currentStage.Level + 1);
                        }
                    }
                    if (currentStage == null)
                    {
                        continue;
                    }

                    // UPDATE: Class here is class used in AreaData.Stage.Production.Data array
                    if (currentStage.Production != null && currentStage.Production.Data != null)
                    {
                        bool areaNameAdded = false;
                        foreach (AbstractScheme productionData in currentStage.Production.Data)
                        {
                            Requirement[] requirements = productionData.requirements;

                            foreach (Requirement baseReq in requirements)
                            {
                                if (baseReq.Type == ERequirementType.Item)
                                {
                                    ItemRequirement itemRequirement = baseReq as ItemRequirement;

                                    if (itemTemplateID == itemRequirement.TemplateId)
                                    {
                                        required = true;

                                        if (needTooltip)
                                        {
                                            if (productionEndProductByID.TryGetValue(productionData._id, out string product))
                                            {
                                                gotTooltip = true;
                                                if (!areaNameAdded)
                                                {
                                                    tooltip += "\n  " + ad.Template.Name.Localized();
                                                    areaNameAdded = true;
                                                }
                                                tooltip += "\n    <color=#" + ColorUtility.ToHtmlStringRGB(craftColor) + ">" + (product + " Name").Localized() + " lvl" + productionData.Level + "</color> (" + itemRequirement.IntCount + ")";
                                            }
                                        }
                                        else
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MoreCheckmarksMod.LogError("Failed to get whether item " + itemTemplateID + " was needed for crafting: " + ex.Message);
            }

            return required && gotTooltip;
        }

        public static bool IsQuestItem(IEnumerable<QuestDataClass> quests, string templateID)
        {
            //QuestControllerClass.GetItemsForCondition
            try
            {
                if (includeFutureQuests)
                {
                    return questDataCompleteByItemTemplateID.TryGetValue(templateID, out QuestPair questPair) && questPair.questData.Count > 0;
                }
                else
                {
                    foreach (QuestDataClass quest in quests)
                    {
                        if (quest != null &&
                            quest.Status == EQuestStatus.Started &&
                            quest.Template != null && quest.Template.Conditions != null && quest.Template.Conditions.ContainsKey(EQuestStatus.AvailableForFinish))
                        {
                            if (quest.Template.Conditions != null)
                            {
                                foreach (KeyValuePair<EQuestStatus, Conditions> keyValuePair in quest.Template.Conditions)
                                {
                                    if (keyValuePair.Key == EQuestStatus.AvailableForFinish)
                                    {
                                        foreach (Condition condition in keyValuePair.Value)
                                        {
                                            if (condition is ConditionItem conditionItem)
                                            {
                                                if (conditionItem.target != null && conditionItem.target.Contains(templateID))
                                                {
                                                    return true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MoreCheckmarksMod.LogError("Failed to get whether item " + templateID + " is quest item: " + ex.Message + "\n" + ex.StackTrace);
            }

            return false;
        }

        public static List<List<KeyValuePair<string, int>>> GetBarters(string ID)
        {
            List<List<KeyValuePair<string, int>>> bartersByTrader = new List<List<KeyValuePair<string, int>>>();

            if (showBarter)
            {
                for (int i = 0; i < bartersByItemByTrader.Count; ++i)
                {
                    List<KeyValuePair<string, int>> current = null;

                    if (bartersByItemByTrader[i] != null)
                    {
                        bartersByItemByTrader[i].TryGetValue(ID, out current);
                    }

                    if (current == null)
                    {
                        current = new List<KeyValuePair<string, int>>();
                    }

                    bartersByTrader.Add(current);
                }
            }

            return bartersByTrader;
        }

        public static void LogInfo(string msg)
        {
            modInstance.Logger.LogInfo(msg);
        }

        public static void LogError(string msg)
        {
            modInstance.Logger.LogError(msg);
        }
    }

    [HarmonyPatch]
    class QuestItemViewPanelShowPatch
    {
        // Replaces the original QuestItemViewPanel.Show() to use custom checkmark colors and tooltips
        [HarmonyPatch(typeof(EFT.UI.DragAndDrop.QuestItemViewPanel), nameof(EFT.UI.DragAndDrop.QuestItemViewPanel.Show))]
        static bool Prefix(EFT.Profile profile, EFT.InventoryLogic.Item item, EFT.UI.SimpleTooltip tooltip, EFT.UI.DragAndDrop.QuestItemViewPanel __instance,
                            ref Image ____questIconImage, ref Sprite ____foundInRaidSprite, ref string ___string_5, ref EFT.UI.SimpleTooltip ___simpleTooltip_0,
                            TextMeshProUGUI ____questItemLabel)
        {
            try
            {
                // Hide by default
                __instance.HideGameObject();

                int possessedCount = 0;
                int possessedQuestCount = 0;
                if (profile != null)
                {
                    IEnumerable<Item> inventoryItems = Singleton<HideoutClass>.Instance.AllStashItems.Where(x => x.TemplateId == item.TemplateId);
                    if (inventoryItems != null)
                    {
                        foreach (Item currentItem in inventoryItems)
                        {
                            if (currentItem.MarkedAsSpawnedInSession)
                            {
                                possessedQuestCount += currentItem.StackObjectsCount;
                            }
                            possessedCount += currentItem.StackObjectsCount;
                        }
                    }
                }
                else
                {
                    MoreCheckmarksMod.LogError("Profile null for item " + item.Template.Name);
                }

                // Get requirements
                List<string> areaNames = new List<string>();
                NeededStruct neededStruct = MoreCheckmarksMod.GetNeeded(item.TemplateId, ref areaNames);
                string craftTooltip = "";
                bool craftRequired = MoreCheckmarksMod.showCraft && MoreCheckmarksMod.GetNeededCraft(item.TemplateId, ref craftTooltip);
                MoreCheckmarksMod.questDataStartByItemTemplateID.TryGetValue(item.TemplateId, out MoreCheckmarksMod.QuestPair startQuests);
                MoreCheckmarksMod.questDataCompleteByItemTemplateID.TryGetValue(item.TemplateId, out MoreCheckmarksMod.QuestPair completeQuests);
                bool questItem = item.MarkedAsSpawnedInSession && (item.QuestItem || MoreCheckmarksMod.includeFutureQuests ? (startQuests != null && startQuests.questData.Count > 0) || (completeQuests != null && completeQuests.questData.Count > 0) : (___string_5 != null && ___string_5.Contains("quest")));
                bool wishlist = ItemUiContext.Instance.IsInWishList(item.TemplateId);
                List<List<KeyValuePair<string, int>>> bartersByTrader = MoreCheckmarksMod.GetBarters(item.TemplateId);
                bool gotBarters = false;
                if (bartersByTrader != null)
                {
                    for (int i = 0; i < bartersByTrader.Count; ++i)
                    {
                        if (bartersByTrader[i] != null && bartersByTrader[i].Count > 0)
                        {
                            gotBarters = true;
                            break;
                        }
                    }
                }

                // Setup label for inspect view
                if (____questItemLabel != null)
                {
                    // Since being quest item could be set by future quests, need to make sure we have "QUEST ITEM" label
                    if (questItem)
                    {
                        ____questItemLabel.text = "QUEST ITEM";
                    }
                    ____questItemLabel.gameObject.SetActive(questItem);
                }

                MoreCheckmarksMod.neededFor[0] = questItem;
                MoreCheckmarksMod.neededFor[1] = neededStruct.foundNeeded || neededStruct.foundFulfilled;
                MoreCheckmarksMod.neededFor[2] = wishlist;
                MoreCheckmarksMod.neededFor[3] = gotBarters;
                MoreCheckmarksMod.neededFor[4] = craftRequired;

                // Find needed with highest priority
                int currentNeeded = -1;
                int currentHighest = -1;
                for (int i = 0; i < 5; ++i)
                {
                    if (MoreCheckmarksMod.neededFor[i] && MoreCheckmarksMod.priorities[i] > currentHighest)
                    {
                        currentNeeded = i;
                        currentHighest = MoreCheckmarksMod.priorities[i];
                    }
                }

                // Set checkmark if necessary
                if (currentNeeded > -1)
                {
                    // Handle special case of areas
                    if (currentNeeded == 1)
                    {
                        if (neededStruct.foundNeeded) // Need more
                        {
                            SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, MoreCheckmarksMod.needMoreColor);
                        }
                        else if (neededStruct.foundFulfilled) // We have enough for at least one upgrade
                        {
                            if (MoreCheckmarksMod.fulfilledAnyCanBeUpgraded) // We want to know when have enough for at least one upgrade
                            {
                                SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, MoreCheckmarksMod.fulfilledColor);
                            }
                            else // We only want fulfilled checkmark when ALL requiring this item can be upgraded
                            {
                                // Check if we trully do not need more of this item for now
                                if (neededStruct.possessedCount >= neededStruct.requiredCount)
                                {
                                    SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, MoreCheckmarksMod.fulfilledColor);
                                }
                                else // Still need more
                                {
                                    SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, MoreCheckmarksMod.needMoreColor);
                                }
                            }
                        }
                    }
                    else // Not area, just set color
                    {
                        SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, MoreCheckmarksMod.colors[currentNeeded]);
                    }
                }
                else if (item.MarkedAsSpawnedInSession) // Item not needed for anything but found in raid
                {
                    SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, Color.white);
                }

                // Set tooltip based on requirements
                SetTooltip(profile, areaNames, ref ___string_5, ref ___simpleTooltip_0, ref tooltip, item, startQuests, completeQuests, possessedCount, possessedQuestCount, neededStruct.requiredCount, wishlist, bartersByTrader, gotBarters, craftRequired, craftTooltip);

                return false;
            }
            catch (Exception ex)
            {
                if (item != null)
                {
                    MoreCheckmarksMod.LogError("QuestItemViewPanelShowPatch failed on item: " + item.TemplateId + " named " + item.LocalizedName() + ":\n" + ex.Message + ":\n" + ex.StackTrace);
                }
                else
                {
                    MoreCheckmarksMod.LogError("QuestItemViewPanelShowPatch failed, item null:\n" + ex.Message + ":\n" + ex.StackTrace);
                }
            }

            return true;
        }

        private static void SetCheckmark(EFT.UI.DragAndDrop.QuestItemViewPanel __instance, Image ____questIconImage, Sprite sprite, Color color)
        {
            try
            {
                // Following calls base class method ShowGameObject()
                __instance.ShowGameObject();
                ____questIconImage.sprite = sprite;
                ____questIconImage.color = color;
            }
            catch
            {
                MoreCheckmarksMod.LogError("SetCheckmark failed");
            }
        }

        private static void SetTooltip(EFT.Profile profile, List<string> areaNames, ref string ___string_5, ref EFT.UI.SimpleTooltip ___simpleTooltip_0, ref EFT.UI.SimpleTooltip tooltip,
                                       EFT.InventoryLogic.Item item, MoreCheckmarksMod.QuestPair startQuests, MoreCheckmarksMod.QuestPair completeQuests,
                                       int possessedCount, int possessedQuestCount, int requiredCount, bool wishlist, List<List<KeyValuePair<string, int>>> bartersByTrader, bool gotBarters,
                                       bool craftRequired, string craftTooltip)
        {
            try
            {
                // Reset string
                ___string_5 = "STASH".Localized(null) + ": <color=#dd831a>" + possessedQuestCount + "</color>/" + possessedCount;

                // Show found in raid if found in raid
                if (item.MarkedAsSpawnedInSession)
                {
                    ___string_5 += "\n" + "Item found in raid".Localized(null);
                }

                // Add quests
                bool gotQuest = false;
                if (item.MarkedAsSpawnedInSession)
                {
                    if (MoreCheckmarksMod.includeFutureQuests)
                    {
                        string questStartString = "<color=#dd831a>";
                        bool gotStartQuests = false;
                        bool gotMoreThanOneStartQuest = false;
                        int totalItemCount = 0;
                        if (startQuests != null)
                        {
                            if (startQuests.questData.Count > 0)
                            {
                                gotStartQuests = true;
                                totalItemCount = startQuests.count;
                            }
                            if (startQuests.questData.Count > 1)
                            {
                                gotMoreThanOneStartQuest = true;
                            }
                            int count = startQuests.questData.Count;
                            int index = 0;
                            foreach (KeyValuePair<string, string> questEntry in startQuests.questData)
                            {
                                string localizedName = questEntry.Key.Localized(null);
                                if (questEntry.Key.Equals(localizedName))
                                {
                                    // Could not localize name, just use default name
                                    if (questEntry.Value.IsNullOrEmpty())
                                    {
                                        questStartString += "Unknown Quest";
                                    }
                                    else
                                    {
                                        questStartString += questEntry.Value;
                                    }
                                }
                                else
                                {
                                    questStartString += localizedName;
                                }
                                if (index != count - 1)
                                {
                                    questStartString += ",\n  ";
                                }
                                else
                                {
                                    questStartString += "</color>";
                                }

                                ++index;
                            }
                        }
                        if (gotStartQuests)
                        {
                            gotQuest = true;
                            ___string_5 = "\nNeeded (" + possessedQuestCount + "/" + totalItemCount + ") to start quest" + (gotMoreThanOneStartQuest ? "s" : "") + ":\n  " + questStartString;
                        }
                        string questCompleteString = "<color=#dd831a>";
                        bool gotCompleteQuests = false;
                        bool gotMoreThanOneCompleteQuest = false;
                        if (completeQuests != null)
                        {
                            if (completeQuests.questData.Count > 0)
                            {
                                gotCompleteQuests = true;
                                totalItemCount = completeQuests.count;
                            }
                            if (completeQuests.questData.Count > 1)
                            {
                                gotMoreThanOneCompleteQuest = true;
                            }
                            int count = completeQuests.questData.Count;
                            int index = 0;
                            foreach (KeyValuePair<string, string> questEntry in completeQuests.questData)
                            {
                                string localizedName = questEntry.Key.Localized(null);
                                if (questEntry.Key.Equals(localizedName))
                                {
                                    // Could not localize name, just use default name
                                    if (questEntry.Value.IsNullOrEmpty())
                                    {
                                        questCompleteString += "Unknown Quest";
                                    }
                                    else
                                    {
                                        questCompleteString += questEntry.Value;
                                    }
                                }
                                else
                                {
                                    questCompleteString += localizedName;
                                }
                                if (index != count - 1)
                                {
                                    questCompleteString += ",\n  ";
                                }
                                else
                                {
                                    questCompleteString += "</color>";
                                }

                                ++index;
                            }
                        }
                        if (gotCompleteQuests)
                        {
                            gotQuest = true;
                            ___string_5 += "\nNeeded (" + possessedQuestCount + "/" + totalItemCount + ") to complete quest" + (gotMoreThanOneCompleteQuest ? "s" : "") + ":\n  " + questCompleteString;
                        }
                    }
                    else // Don't include future quests, do as vanilla
                    {
                        QuestTemplate rawQuest0 = null;
                        ConditionItem conditionItem = null;
                        foreach (QuestDataClass questDataClass in profile.QuestsData)
                        {
                            if (questDataClass.Status == EQuestStatus.Started && questDataClass.Template != null)
                            {
                                // UPDATE: Look for the type used in StatusData's Template var of type RawQuestClass
                                // with QuestConditionsList, for the value
                                foreach (KeyValuePair<EQuestStatus, Conditions> kvp in questDataClass.Template.Conditions)
                                {
                                    EQuestStatus equestStatus;
                                    Conditions gclass;
                                    kvp.Deconstruct(out equestStatus, out gclass);
                                    foreach (Condition condition in gclass)
                                    {
                                        ConditionItem conditionItem2;
                                        if (!questDataClass.CompletedConditions.Contains(condition.id) && (conditionItem2 = (condition as ConditionItem)) != null && conditionItem2.target.Contains(item.TemplateId))
                                        {
                                            rawQuest0 = questDataClass.Template;
                                            conditionItem = conditionItem2;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (rawQuest0 != null)
                        {
                            string arg = "<color=#dd831a>" + rawQuest0.Name + "</color>";
                            if (item.QuestItem)
                            {
                                gotQuest = true;
                                ___string_5 += string.Format("\nItem is related to an active {0} quest".Localized(null), arg);
                            }
                            Weapon weapon;
                            ConditionWeaponAssembly condition;
                            if (!gotQuest && (weapon = (item as Weapon)) != null && (condition = (conditionItem as ConditionWeaponAssembly)) != null && Inventory.IsWeaponFitsCondition(weapon, condition, false))
                            {
                                gotQuest = true;
                                ___string_5 += string.Format("\nItem fits the active {0} quest requirements".Localized(null), arg);
                            }
                            if (!gotQuest && item.MarkedAsSpawnedInSession)
                            {
                                gotQuest = true;
                                ___string_5 += string.Format("\nItem that has been found in raid for the {0} quest".Localized(null), arg);
                            }
                        }
                    }
                }

                // Add areas
                bool gotAreas = areaNames.Count > 0;
                string areaNamesString = "";
                for (int i = 0; i < areaNames.Count; ++i)
                {
                    areaNamesString += "\n  " + areaNames[i];
                }
                if (!areaNamesString.Equals(""))
                {
                    ___string_5 += string.Format("\nNeeded ({1}/{2}) for area" + (areaNames.Count == 1 ? "" : "s") + ":{0}", areaNamesString, possessedCount, requiredCount);
                }

                // Add wishlist
                if (wishlist)
                {
                    ___string_5 += string.Format("\nOn {0}", "<color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.wishlistColor) + ">Wish List</color>");
                }

                // Add craft
                if (craftRequired)
                {
                    ___string_5 += string.Format("\nNeeded for crafting:{0}", craftTooltip);
                }

                // Add barters
                if (gotBarters)
                {
                    bool firstBarter = false;
                    if (bartersByTrader != null)
                    {
                        for (int i = 0; i < bartersByTrader.Count; ++i)
                        {
                            if (bartersByTrader[i] != null && bartersByTrader[i].Count > 0)
                            {
                                if (!firstBarter)
                                {
                                    ___string_5 += "\n" + "Barter".Localized(null) + ":";
                                    firstBarter = true;
                                }
                                string bartersString = "\n With " + (MoreCheckmarksMod.traders.Length > i ? MoreCheckmarksMod.traders[i] : "Custom Trader " + i) + ":";
                                for (int j = 0; j < bartersByTrader[i].Count; ++j)
                                {
                                    bartersString += "\n  <color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.barterColor) + ">" + bartersByTrader[i][j].Key.LocalizedName() + "</color> (" + bartersByTrader[i][j].Value + ")";
                                }
                                ___string_5 += bartersString;
                            }
                        }
                    }
                }

                if (gotQuest || gotAreas || wishlist || gotBarters || craftRequired || item.MarkedAsSpawnedInSession)
                {
                    // If this is not a quest item or found in raid, the original returns and the tooltip never gets set, so we need to set it ourselves
                    ___simpleTooltip_0 = tooltip;
                }
            }
            catch
            {
                MoreCheckmarksMod.LogError("SetToolTip failed");
            }
        }
    }

    [HarmonyPatch]
    class ItemSpecificationPanelShowPatch
    {
        // This postfix will run after the inspect window sets its checkmark if there is one
        // If there is one, the postfix for the QuestItemViewPanel will always have run before
        // This patch just changes the sprite to a default white one so we can set its color to whatever we need
        [HarmonyPatch(typeof(EFT.UI.ItemSpecificationPanel), "method_2")]
        static void Postfix(ref Item ___item_0, ref QuestItemViewPanel ____questItemViewPanel)
        {
            try
            {
                // If the checkmark exists and if the color of the checkmark is custom
                if (____questItemViewPanel != null)
                {
                    // Get access to QuestItemViewPanel's private _questIconImage
                    BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                    FieldInfo iconImageField = typeof(QuestItemViewPanel).GetField("_questIconImage", bindFlags);
                    Image _questIconImage = iconImageField.GetValue(____questItemViewPanel) as Image;

                    if (_questIconImage != null)
                    {
                        _questIconImage.sprite = MoreCheckmarksMod.whiteCheckmark;
                    }
                }
            }
            catch
            {
                MoreCheckmarksMod.LogError("ItemSpecificationPanelShowPatch failed");
            }
        }
    }

    [HarmonyPatch]
    class AvailableActionsPatch
    {
        // This postfix will run after we get a list of all actions available to interact with the item we are pointing at
        [HarmonyPatch(typeof(InteractionController), "smethod_4")]
        static void Postfix(GamePlayerOwner owner, LootItem lootItem, ref InteractionInstance __result)
        {
            try
            {
                foreach (Action action in __result.Actions)
                {
                    if (action.Name.Equals("Take"))
                    {
                        List<string> nullAreaNames = null;
                        NeededStruct neededStruct = MoreCheckmarksMod.GetNeeded(lootItem.TemplateId, ref nullAreaNames);
                        string craftTooltip = "";
                        bool craftRequired = MoreCheckmarksMod.GetNeededCraft(lootItem.TemplateId, ref craftTooltip, false);
                        bool wishlist = ItemUiContext.Instance.IsInWishList(lootItem.TemplateId);
                        bool questItem = MoreCheckmarksMod.IsQuestItem(owner.Player.Profile.QuestsData, lootItem.TemplateId);

                        if (neededStruct.foundNeeded)
                        {
                            if (wishlist && MoreCheckmarksMod.wishlistPriority > MoreCheckmarksMod.hideoutPriority)
                            {
                                if (questItem && MoreCheckmarksMod.questPriority > MoreCheckmarksMod.wishlistPriority)
                                {
                                    action.Name = "<font=\"BenderBold\"><color=#FFE433>Take</color></font>";
                                }
                                else
                                {
                                    action.Name = "<font=\"BenderBold\"><color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.wishlistColor) + ">Take</color></font>";
                                }

                            }
                            else
                            {
                                if (questItem && MoreCheckmarksMod.questPriority > MoreCheckmarksMod.hideoutPriority)
                                {
                                    action.Name = "<font=\"BenderBold\"><color=#FFE433>Take</color></font>";
                                }
                                else
                                {
                                    action.Name = "<font=\"BenderBold\"><color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.needMoreColor) + ">Take</color></font>";
                                }
                            }
                        }
                        else if (neededStruct.foundFulfilled)
                        {
                            if (wishlist && MoreCheckmarksMod.wishlistPriority > MoreCheckmarksMod.hideoutPriority)
                            {
                                if (questItem && MoreCheckmarksMod.questPriority > MoreCheckmarksMod.wishlistPriority)
                                {
                                    action.Name = "<font=\"BenderBold\"><color=#FFE433>Take</color></font>";
                                }
                                else
                                {
                                    action.Name = "<font=\"BenderBold\"><color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.wishlistColor) + ">Take</color></font>";
                                }
                            }
                            else
                            {
                                if (MoreCheckmarksMod.fulfilledAnyCanBeUpgraded)
                                {
                                    if (questItem && MoreCheckmarksMod.questPriority > MoreCheckmarksMod.hideoutPriority)
                                    {
                                        action.Name = "<font=\"BenderBold\"><color=#FFE433>Take</color></font>";
                                    }
                                    else
                                    {
                                        action.Name = "<font=\"BenderBold\"><color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.fulfilledColor) + ">Take</color></font>";
                                    }
                                }
                                else // We only want blue checkmark when ALL requiring this item can be upgraded (if all other requirements are fulfilled too but thats implied)
                                {
                                    // Check if we trully do not need more of this item for now
                                    if (neededStruct.possessedCount >= neededStruct.requiredCount)
                                    {
                                        if (questItem && MoreCheckmarksMod.questPriority > MoreCheckmarksMod.hideoutPriority)
                                        {
                                            action.Name = "<font=\"BenderBold\"><color=#FFE433>Take</color></font>";
                                        }
                                        else
                                        {
                                            action.Name = "<font=\"BenderBold\"><color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.fulfilledColor) + ">Take</color></font>";
                                        }
                                    }
                                    else // Still need more
                                    {
                                        if (questItem && MoreCheckmarksMod.questPriority > MoreCheckmarksMod.hideoutPriority)
                                        {
                                            action.Name = "<font=\"BenderBold\"><color=#FFE433>Take</color></font>";
                                        }
                                        else
                                        {
                                            action.Name = "<font=\"BenderBold\"><color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.needMoreColor) + ">Take</color></font>";
                                        }
                                    }
                                }
                            }
                        }
                        else if (wishlist) // We don't want to color it for hideout, but it is in wishlist
                        {
                            if (questItem && MoreCheckmarksMod.questPriority > MoreCheckmarksMod.wishlistPriority)
                            {
                                action.Name = "<font=\"BenderBold\"><color=#FFE433>Take</color></font>";
                            }
                            else
                            {
                                action.Name = "<font=\"BenderBold\"><color=#" + ColorUtility.ToHtmlStringRGB(MoreCheckmarksMod.wishlistColor) + ">Take</color></font>";
                            }
                        }
                        else if (questItem) // We don't want to color it for anything but it is a quest item
                        {
                            action.Name = "<font=\"BenderBold\"><color=#FFE433>Take</color></font>";
                        }
                        //else leave it as it is

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MoreCheckmarksMod.LogError("Failed to process available actions for loose item: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }

    [HarmonyPatch]
    class Quest0StatusPatch
    {
        private static EQuestStatus preStatus;

        // This prefix will run before a quest's status has been set 
        [HarmonyPatch(typeof(Quest), "SetStatus")]
        static void Prefix(Quest __instance)
        {
            preStatus = __instance.QuestStatus;
        }

        // This postfix will run after a quest's status has been set 
        [HarmonyPatch(typeof(Quest), "SetStatus")]
        static void Postfix(Quest __instance)
        {
            if (__instance == null)
            {
                MoreCheckmarksMod.LogError("Attempted setting queststatus but instance is null");
                return;
            }
            if (__instance.Template == null)
            {
                return;
            }

            MoreCheckmarksMod.LogInfo("Quest " + __instance.Template.Name + " queststatus set to " + __instance.QuestStatus);

            try
            {
                if (__instance.QuestStatus != preStatus)
                {
                    switch (__instance.QuestStatus)
                    {
                        case EQuestStatus.Started:
                            if (preStatus == EQuestStatus.AvailableForStart)
                            {
                                if (MoreCheckmarksMod.neededStartItemsByQuest.TryGetValue(__instance.Template.Id, out Dictionary<string, int> startItems))
                                {
                                    foreach (KeyValuePair<string, int> itemEntry in startItems)
                                    {
                                        if (MoreCheckmarksMod.questDataStartByItemTemplateID.TryGetValue(itemEntry.Key, out MoreCheckmarksMod.QuestPair questList))
                                        {
                                            questList.questData.Remove(__instance.Template.Id);
                                            questList.count -= itemEntry.Value;
                                            if (questList.questData.Count == 0)
                                            {
                                                MoreCheckmarksMod.questDataStartByItemTemplateID.Remove(itemEntry.Key);
                                            }
                                        }
                                    }

                                    MoreCheckmarksMod.neededStartItemsByQuest.Remove(__instance.Template.Id);
                                }
                            }
                            break;
                        case EQuestStatus.Success:
                        case EQuestStatus.Expired:
                        case EQuestStatus.Fail:
                            if (MoreCheckmarksMod.neededCompleteItemsByQuest.TryGetValue(__instance.Template.Id, out Dictionary<string, int> completeItems))
                            {
                                foreach (KeyValuePair<string, int> itemEntry in completeItems)
                                {
                                    if (MoreCheckmarksMod.questDataCompleteByItemTemplateID.TryGetValue(itemEntry.Key, out MoreCheckmarksMod.QuestPair questList))
                                    {
                                        questList.questData.Remove(__instance.Template.Id);
                                        questList.count -= itemEntry.Value;
                                        if (questList.questData.Count == 0)
                                        {
                                            MoreCheckmarksMod.questDataCompleteByItemTemplateID.Remove(itemEntry.Key);
                                        }
                                    }
                                }

                                MoreCheckmarksMod.neededCompleteItemsByQuest.Remove(__instance.Template.Id);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MoreCheckmarksMod.LogError("Failed to process change in status for quest " + __instance.Template.Name + " to " + __instance.QuestStatus + ": " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }

    class ProfileSelectionPatch
    {
        // This prefix will run right after a profile has been selected
        static void Postfix()
        {
            MoreCheckmarksMod.modInstance.LoadData();
        }
    }
}
