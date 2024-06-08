using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using BepInEx;
using EFT;
using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using static RawQuestClass;
using QuestClass = Quest;

namespace DrakiaXYZ.TaskListFixes
{
    [BepInPlugin("xyz.drakia.tasklistfixes", "DrakiaXYZ-TaskListFixes", "1.4.0")]
    // [BepInDependency("com.spt-aki.core", "3.8.0")]
    public class TaskListFixesPlugin : BaseUnityPlugin
    {
        // Note: We use a cached quest progress dictionary because fetching quest progress actually
        //       triggers a calculation any time it's read
        public static readonly Dictionary<QuestClass, int> QuestProgressCache = new Dictionary<QuestClass, int>();

        private static MethodInfo _stringLocalizedMethod;

        private void Awake()
        {
            Settings.Init(Config);

            Type[] localizedParams = new Type[] { typeof(string), typeof(string) };
            Type stringLocalizeClass = PatchConstants.EftTypes.First(x => x.GetMethod("Localized", localizedParams) != null);
            _stringLocalizedMethod = AccessTools.Method(stringLocalizeClass, "Localized", localizedParams);

            new TasksScreenShowPatch().Enable();
            new QuestProgressViewPatch().Enable();
            new QuestsSortPanelSortPatch().Enable();
            new QuestsSortPanelShowRestoreSortPatch().Enable();
            new TasksScreenSortRememberPatch().Enable();

            new QuestStringFieldComparePatch().Enable();
            new QuestLocationComparePatch().Enable();
            new QuestStatusComparePatch().Enable();
            new QuestProgressComparePatch().Enable();
        }

        public static bool HandleNullOrEqualQuestCompare(QuestClass quest1, QuestClass quest2, out int result)
        {
            if (quest1 == quest2)
            {
                result = 0;
                return true;
            }

            if (quest1 == null)
            {
                result = -1;
                return true;
            }

            if (quest2 == null)
            {
                result = 1;
                return true;
            }

            result = 0;
            return false;
        }

        public static string Localized(string input)
        {
            return (string)_stringLocalizedMethod.Invoke(null, new object[] { input, null });
        }
    }

    // Allow restoring the sort order to the last used ordering
    class QuestsSortPanelShowRestoreSortPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(QuestsSortPanel)).Single(x => x.Name == "Show");
        }

        [PatchPrefix]
        public static void PatchPrefix(ref EQuestsSortType defaultSortingType, ref bool defaultAscending)
        {
            // If we're not remembering sorting, do nothing
            if (!Settings.RememberSorting.Value) { return; }

            // Only restore these if we have a stored value
            if (Settings._LastSortBy.Value >= 0)
            {
                defaultSortingType = (EQuestsSortType)Settings._LastSortBy.Value;
                defaultAscending = Settings._LastSortAscend.Value;
            }
        }
    }

    // Store the last used sort column and ascend flag
    class TasksScreenSortRememberPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TasksScreen), "Sort");
        }

        [PatchPrefix]
        public static void PatchPrefix(EQuestsSortType sortType, bool sortDirection)
        {
            // If we're not remembering sorting, do nothing
            if (!Settings.RememberSorting.Value) { return; }

            Settings._LastSortBy.Value = (int)sortType;
            Settings._LastSortAscend.Value = sortDirection;
        }
    }

    class QuestStringFieldComparePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type questStringFieldComparerType = PatchConstants.EftTypes.First(x => x.Name == "QuestStringFieldComparer");
            return AccessTools.Method(questStringFieldComparerType, "Compare");
        }

        [PatchPostfix]
        public static void PatchPostfix(QuestClass x, QuestClass y, EQuestsSortType ____sortType, ref int __result)
        {
            switch (____sortType)
            {
                case EQuestsSortType.Trader:
                    __result = TraderCompare(x, y);
                    break;
                case EQuestsSortType.Type:
                    __result = TypeCompare(x, y);
                    break;
                case EQuestsSortType.Task:
                    __result = TaskNameCompare(x, y);
                    break;
            }

        }

        public static int TraderCompare(QuestClass quest1, QuestClass quest2)
        {
            if (TaskListFixesPlugin.HandleNullOrEqualQuestCompare(quest1, quest2, out int result))
            {
                return result;
            }

            // If the trader IDs are the same, sort by the start time
            string traderId1 = quest1.Template.TraderId;
            string traderId2 = quest2.Template.TraderId;

            // For tasks from the same trader, if grouping traders by map,
            // sort by map if map is different. Otherwise sort by start time (Original logic), or 
            // task name (New logic)
            if (traderId1 == traderId2)
            {
                string locationId1 = quest1.Template.LocationId;
                string locationId2 = quest2.Template.LocationId;

                // Sort by the map name
                if (Settings.GroupTraderByLoc.Value && locationId1 != locationId2)
                {
                    return QuestLocationComparePatch.LocationCompare(quest1, quest2, null);
                }

                // Sort by quest name
                if (Settings.SubSortByName.Value)
                {
                    return TaskNameCompare(quest1, quest2);
                }

                // Sort by quest start time
                return quest1.StartTime.CompareTo(quest2.StartTime);
            }

            // Otherwise compare the trader's nicknames
            string traderName1 = TaskListFixesPlugin.Localized(traderId1 + " Nickname");
            string traderName2 = TaskListFixesPlugin.Localized(traderId2 + " Nickname");
            return string.CompareOrdinal(traderName1, traderName2);
        }

        private static int TypeCompare(QuestClass quest1, QuestClass quest2)
        {
            if (TaskListFixesPlugin.HandleNullOrEqualQuestCompare(quest1, quest2, out int result))
            {
                return result;
            }

            // For non-matching types, sort by their name
            string type1 = Enum.GetName(typeof(EQuestType), quest1.Template.QuestType);
            string type2 = Enum.GetName(typeof(EQuestType), quest2.Template.QuestType);
            if (type1 != type2)
            {
                return string.CompareOrdinal(type1, type2);
            }

            if (Settings.SubSortByName.Value)
            {
                return TaskNameCompare(quest1, quest2);
            }

            return quest1.StartTime.CompareTo(quest2.StartTime);
        }

        public static int TaskNameCompare(QuestClass quest1, QuestClass quest2)
        {
            if (TaskListFixesPlugin.HandleNullOrEqualQuestCompare(quest1, quest2, out int result))
            {
                return result;
            }

            string questName1 = TaskListFixesPlugin.Localized(quest1.Template.Id + " name");
            string questName2 = TaskListFixesPlugin.Localized(quest2.Template.Id + " name");
            if (questName1 != questName2)
            {
                return string.CompareOrdinal(questName1, questName2);
            }

            return quest1.StartTime.CompareTo(quest2.StartTime);
        }
    }

    class QuestLocationComparePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type questLocationComparerType = PatchConstants.EftTypes.First(x => x.Name == "QuestLocationComparer");
            return AccessTools.Method(questLocationComparerType, "Compare");
        }

        [PatchPostfix]
        public static void PatchPostfix(QuestClass x, QuestClass y, ref int __result, string ____locationId)
        {
            __result = LocationCompare(x, y, ____locationId);
        }

        public static int LocationCompare(QuestClass quest1, QuestClass quest2, string locationId)
        {
            if (TaskListFixesPlugin.HandleNullOrEqualQuestCompare(quest1, quest2, out int result))
            {
                return result;
            }

            string locationId1 = quest1.Template.LocationId;
            string locationId2 = quest2.Template.LocationId;

            // For tasks on the same map, if grouping same map by trader,
            // sort by trader if trader is different.
            // Otherwise sort by start time (Original logic), or task name (New logic)
            if (locationId1 == locationId2)
            {
                string traderId1 = quest1.Template.TraderId;
                string traderId2 = quest2.Template.TraderId;
                if (Settings.GroupLocByTrader.Value && traderId1 != traderId2)
                {
                    return QuestStringFieldComparePatch.TraderCompare(quest1, quest2);
                }

                if (Settings.SubSortByName.Value)
                {
                    return QuestStringFieldComparePatch.TaskNameCompare(quest1, quest2);
                }

                return quest1.StartTime.CompareTo(quest2.StartTime);
            }

            // Sort quests on the same location as the player to the top of the list
            if (locationId2 == locationId)
            {
                return 1;
            }
            if (locationId1 == locationId)
            {
                return -1;
            }

            // Handle quests that can be done on any map
            if (locationId2 == "any")
            {
                return 1;
            }
            if (locationId1 == "any")
            {
                return -1;
            }

            // Finally sort by the actual quest location name
            string locationName1 = TaskListFixesPlugin.Localized(locationId1 + " Name");
            string locationName2 = TaskListFixesPlugin.Localized(locationId2 + " Name");
            return string.CompareOrdinal(locationName1, locationName2);
        }
    }

    class QuestStatusComparePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type questStatusComparerType = PatchConstants.EftTypes.First(x => x.Name == "QuestStatusComparer");
            return AccessTools.Method(questStatusComparerType, "Compare");
        }

        [PatchPostfix]
        public static void PatchPostfix(QuestClass x, QuestClass y, ref int __result)
        {
            __result = StatusCompare(x, y);
        }

        private static int StatusCompare(QuestClass quest1, QuestClass quest2)
        {
            if (TaskListFixesPlugin.HandleNullOrEqualQuestCompare(quest1, quest2, out int result))
            {
                return result;
            }

            // If the quest status is the same, sort by either the name or the start time
            EQuestStatus questStatus1 = quest1.QuestStatus;
            EQuestStatus questStatus2 = quest2.QuestStatus;
            if (questStatus1 == questStatus2)
            {
                if (Settings.SubSortByName.Value)
                {
                    // We do this opposite of other sorting, because status defaults to descending
                    return QuestStringFieldComparePatch.TaskNameCompare(quest2, quest1);
                }

                // We do this opposite of other sorting, because status defaults to descending
                return quest2.StartTime.CompareTo(quest1.StartTime);
            }

            // This is the original logic, but with sorting by name for "matched" things added
            if (questStatus2 != EQuestStatus.MarkedAsFailed)
            {
                if (questStatus1 != EQuestStatus.AvailableForFinish)
                {
                    if (questStatus2 != EQuestStatus.AvailableForFinish)
                    {
                        if (questStatus1 != EQuestStatus.MarkedAsFailed)
                        {
                            if (Settings.SubSortByName.Value)
                            {
                                // We do this opposite of other sorting, because status defaults to descending
                                return QuestStringFieldComparePatch.TaskNameCompare(quest2, quest1);
                            }

                            // We do this opposite of other sorting, because status defaults to descending
                            return quest2.StartTime.CompareTo(quest1.StartTime);
                        }
                    }
                    return -1;
                }
            }

            return 1;
        }
    }

    class QuestProgressComparePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type questProgressComparerType = PatchConstants.EftTypes.First(x => x.Name == "QuestProgressComparer");
            return AccessTools.Method(questProgressComparerType, "Compare");
        }

        [PatchPrefix]
        public static bool PatchPrefix(QuestClass x, QuestClass y, ref int __result)
        {
            __result = ProgressCompare(x, y);

            // We skip the original in this case to avoid the performance hit of fetching quest progress
            return false;
        }

        private static int ProgressCompare(QuestClass quest1, QuestClass quest2)
        {
            if (TaskListFixesPlugin.HandleNullOrEqualQuestCompare(quest1, quest2, out int result))
            {
                return result;
            }

            // Use a quest progress cache to avoid re-calculating quest progress constantly
            int quest1Progress, quest2Progress;
            if (!TaskListFixesPlugin.QuestProgressCache.TryGetValue(quest1, out quest1Progress))
            {
                quest1Progress = Mathf.FloorToInt(quest1.Progress.Item2 / quest1.Progress.Item1);
                TaskListFixesPlugin.QuestProgressCache[quest1] = quest1Progress;
            }
            if (!TaskListFixesPlugin.QuestProgressCache.TryGetValue(quest2, out quest2Progress))
            {
                quest2Progress = Mathf.FloorToInt(quest2.Progress.Item2 / quest2.Progress.Item1);
                TaskListFixesPlugin.QuestProgressCache[quest2] = quest2Progress;
            }

            // Sort by the progress number if they aren't equal
            if (quest1Progress != quest2Progress)
            {
                return quest1Progress.CompareTo(quest2Progress);
            }

            // Sort by name as the fallback is option is enabled
            if (Settings.SubSortByName.Value)
            {
                // We do this opposite of other sorting, because progress defaults to descending
                return QuestStringFieldComparePatch.TaskNameCompare(quest2, quest1);
            }

            // Otherwise use the default behaviour of sorting by start time
            // We do this opposite of other sorting, because progress defaults to descending
            return quest2.StartTime.CompareTo(quest1.StartTime);
        }
    }

    // Patch used for clearing our cached quest progress data
    class TasksScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TasksScreen), "Show");
        }

        [PatchPrefix]
        public static void PatchPrefix()
        {
            TaskListFixesPlugin.QuestProgressCache.Clear();
        }
    }

    // Patch used to cache quest progress any time a QuestProgressView is shown
    class QuestProgressViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestProgressView), "Show");
        }

        [PatchPostfix]
        public static void PatchPostfix(QuestClass quest, TextMeshProUGUI ____percentages)
        {
            // Luckily we can just go based on the text in the _percentages textmesh, because it's the progress as a percentage
            if (Int32.TryParse(____percentages.text, out int progress))
            {
                TaskListFixesPlugin.QuestProgressCache[quest] = progress;
            }
        }
    }

    // Patch used to change the default ordering when sorting by a new column
    class QuestsSortPanelSortPatch : ModulePatch
    {
        private static FieldInfo _sortDescendField;
        private static FieldInfo _filterButtonField;
        protected override MethodBase GetTargetMethod()
        {
            Type targetType = typeof(QuestsSortPanel).BaseType;
            _sortDescendField = AccessTools.GetDeclaredFields(targetType).First(x => x.FieldType == typeof(bool));
            _filterButtonField = AccessTools.GetDeclaredFields(targetType).First(x => x.FieldType == typeof(FilterButton));

            return AccessTools.Method(targetType, "method_1");
        }

        [PatchPrefix]
        public static void PatchPrefix(QuestsSortPanel __instance, EQuestsSortType sortType, FilterButton button)
        {
            // If we're restoring the sort order, and we're sorting by the same column as our stored one, don't change the default sort order here
            if (Settings.RememberSorting.Value && Settings._LastSortBy.Value == (int)sortType)
            {
                return;
            }

            FilterButton activeFilterButton = _filterButtonField.GetValue(__instance) as FilterButton;

            // If the button is different than the stored filterButton_0, it means we're sorting by a new column.
            if (Settings.NewDefaultOrder.Value && button != activeFilterButton)
            {
                switch (sortType)
                {
                    // Sort these default ascending
                    case EQuestsSortType.Task:
                    case EQuestsSortType.Trader:
                    case EQuestsSortType.Location:
                        _sortDescendField.SetValue(__instance, false);
                        break;

                    // Sort these default descending
                    case EQuestsSortType.Progress:
                    case EQuestsSortType.Status:
                        _sortDescendField.SetValue(__instance, true);
                        break;
                }
            }
        }
    }

}
