using StayInTarkov;
//using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TechHappy.MinimapSender;
using UnityEngine;
using static LocationSettings;


namespace TechHappy.MinimapSender
{
    public class MinimapSenderController : MonoBehaviour
    {
        internal static MinimapSenderController Instance;
        private MinimapSenderBroadcastService _minimapSenderService;
        //private readonly ZoneData _zoneDataHelper = ZoneData.Instance;
        private ZoneData _zoneDataHelper;
        private LocalizedHelper _localizedHelper;
        private List<QuestMarkerInfo> _questMarkerData;

        [UsedImplicitly]
        public void Start()
        {
            try
            {
                Instance = this;

                _zoneDataHelper = new ZoneData();
                _localizedHelper = new LocalizedHelper();

                var gamePlayerOwner = GetLocalPlayerFromWorld().GetComponentInChildren<GamePlayerOwner>();

                MinimapSenderPlugin.RefreshIntervalMilliseconds.SettingChanged += RefreshIntervalSecondsOnSettingChanged;

                if (_minimapSenderService == null)
                {
                    _minimapSenderService = new MinimapSenderBroadcastService(gamePlayerOwner);
                }

                _questMarkerData = new List<QuestMarkerInfo>();

                IEnumerable<TriggerWithId> allTriggers = FindObjectsOfType<TriggerWithId>();

                _zoneDataHelper.AddTriggers(allTriggers);

                UpdateQuestData();

                _minimapSenderService.UpdateQuestData(_questMarkerData);

                //foreach ( var questMarkerData in _questMarkerData)
                //{
                //    MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"QuestMarker: {_localizedHelper.Localized(questMarkerData.NameKey)} Position: {questMarkerData.Where.x}, {questMarkerData.Where.z} Neccessary: {!questMarkerData.IsNotNecessary}");
                //}

                _minimapSenderService.StartBroadcastingPosition(MinimapSenderPlugin.RefreshIntervalMilliseconds.Value);
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        private void RefreshIntervalSecondsOnSettingChanged(object sender, EventArgs e)
        {
            _minimapSenderService.ChangeInterval(MinimapSenderPlugin.RefreshIntervalMilliseconds.Value);
        }

        [UsedImplicitly]
        public void Stop()
        {
            _minimapSenderService?.StopBroadcastingPosition();
        }

        public void Update()
        {
            //// TODO: Comment the code below before release
            //if (Input.GetKeyUp(KeyCode.Backslash))
            //{
            //    MinimapSenderPlugin.MinimapSenderLogger.LogInfo("Backslash was pressed");

            //    //_AirdropInitMethod.Invoke(Singleton<GameWorld>.Instance, [])

            //    var points = LocationScene.GetAll<AirdropPoint>().Any();

            //    Singleton<GameWorld>.Instance.gameObject.AddComponent<AirdropsManager>().isFlareDrop = true;
            //}
        }

        private Player GetLocalPlayerFromWorld()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null || gameWorld.MainPlayer == null)
            {
                return null;
            }

            return gameWorld.MainPlayer;
        }

        private GameWorld GetGameWorld()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                return null;
            }

            return gameWorld;
        }

        public void UpdateQuestData()
        {
            try
            {
                _questMarkerData.Clear();

                Player player = GetLocalPlayerFromWorld();

                var questData = Traverse.Create(player).Field("_questController").GetValue<object>();

                var quests = Traverse.Create(questData).Field("Quests").GetValue<object>();

                var questsList = Traverse.Create(quests).Field("list_0").GetValue<IList>();

                var lootItemsList = Traverse.Create(GetGameWorld()).Field("LootItems").Field("list_0").GetValue<List<LootItem>>();

                (string Id, LootItem Item)[] questItems =
                    lootItemsList.Where(x => x.Item.QuestItem).Select(x => (x.TemplateId, x)).ToArray();


                //foreach (QuestClass item in questsList)
                foreach (Quest item in questsList)
                {
                    if (item.QuestStatus != EQuestStatus.Started)
                        continue;

                    var template = Traverse.Create(item).Property("Template").GetValue<object>();

                    var nameKey = Traverse.Create(template).Property("NameLocaleKey").GetValue<string>();

                    var traderId = Traverse.Create(template).Field("TraderId").GetValue<string>();

                    var availableForFinishConditions =
                        Traverse.Create(item).Property("AvailableForFinishConditions").GetValue<object>();

                    var availableForFinishConditionsList =
                        Traverse.Create(availableForFinishConditions).Field("list_0").GetValue<IList<Condition>>();

                    foreach (var condition in availableForFinishConditionsList)
                    {
                        // Check if this condition of the quest has already been completed
                        //MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Quest -> IsConditionDone: {item.IsConditionDone(condition)}");
                        if (item.IsConditionDone(condition))
                        {
                            continue;
                        }

                        switch (condition)
                        {
                            case ConditionLeaveItemAtLocation location:
                                {
                                    var zoneId = location.zoneId;


                                    if (ZoneData.Instance.TryGetValues(zoneId,
                                            out IEnumerable<PlaceItemTrigger> triggers))
                                    {
                                        foreach (var trigger in triggers)
                                        {
                                            var staticInfo = new QuestMarkerInfo
                                            {
                                                Id = location.id,
                                                Where = trigger.transform.position,
                                                ZoneId = zoneId,
                                                Target = location.target,
                                                NameKey = nameKey,
                                                NameText = _localizedHelper.Localized(nameKey),
                                                DescriptionKey = location.id,
                                                DescriptionText = _localizedHelper.Localized(location.id),
                                                TraderId = traderId,
                                                TraderText = _localizedHelper.Localized(traderId),
                                                IsNotNecessary = !location.IsNecessary,
                                                InfoType = QuestMarkerInfo.Type.ConditionLeaveItemAtLocation
                                            };

                                            _questMarkerData.Add(staticInfo);
                                        }
                                    }

                                    break;
                                }
                            case ConditionPlaceBeacon beacon:
                                {
                                    var zoneId = beacon.zoneId;

                                    if (_zoneDataHelper.TryGetValues(zoneId,
                                            out IEnumerable<PlaceItemTrigger> triggers))
                                    {
                                        foreach (var trigger in triggers)
                                        {
                                            var staticInfo = new QuestMarkerInfo
                                            {
                                                Id = beacon.id,
                                                Where = trigger.transform.position,
                                                ZoneId = zoneId,
                                                Target = beacon.target,
                                                NameText = _localizedHelper.Localized(nameKey),
                                                DescriptionKey = beacon.id,
                                                DescriptionText = _localizedHelper.Localized(beacon.id),
                                                TraderId = traderId,
                                                TraderText = _localizedHelper.Localized(traderId),
                                                IsNotNecessary = !beacon.IsNecessary,
                                                InfoType = QuestMarkerInfo.Type.ConditionPlaceBeacon
                                            };

                                            _questMarkerData.Add(staticInfo);
                                        }
                                    }

                                    break;
                                }
                            case ConditionFindItem findItem:
                                {
                                    var itemIds = findItem.target;

                                    foreach (var itemId in itemIds)
                                    {
                                        foreach (var questItem in questItems)
                                        {
                                            if (questItem.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase))
                                            {
                                                var staticInfo = new QuestMarkerInfo
                                                {
                                                    Id = findItem.id,
                                                    Where = questItem.Item.transform.position,
                                                    Target = new[] { itemId },
                                                    NameText = _localizedHelper.Localized(nameKey),
                                                    DescriptionKey = findItem.id,
                                                    DescriptionText = _localizedHelper.Localized(findItem.id),
                                                    TraderId = traderId,
                                                    TraderText = _localizedHelper.Localized(traderId),
                                                    IsNotNecessary = !findItem.IsNecessary,
                                                    InfoType = QuestMarkerInfo.Type.ConditionFindItem
                                                };

                                                _questMarkerData.Add(staticInfo);
                                            }
                                        }
                                    }

                                    break;
                                }
                            case ConditionCounterCreator counterCreator:
                                {
                                    var counter = Traverse.Create(counterCreator).Field("counter").GetValue<object>();

                                    var conditions = Traverse.Create(counter).Property("conditions").GetValue<object>();

                                    var conditionsList = Traverse.Create(conditions).Field("list_0").GetValue<IList>();

                                    foreach (var condition2 in conditionsList)
                                    {
                                        switch (condition2)
                                        {
                                            case ConditionVisitPlace place:
                                                {
                                                    var zoneId = place.target;

                                                    if (_zoneDataHelper.TryGetValues(zoneId,
                                                            out IEnumerable<ExperienceTrigger> triggers))
                                                    {
                                                        foreach (var trigger in triggers)
                                                        {
                                                            var staticInfo = new QuestMarkerInfo
                                                            {
                                                                Id = counterCreator.id,
                                                                Where = trigger.transform.position,
                                                                ZoneId = zoneId,
                                                                NameText = _localizedHelper.Localized(nameKey),
                                                                DescriptionKey = counterCreator.id,
                                                                DescriptionText = _localizedHelper.Localized(counterCreator.id),
                                                                TraderId = traderId,
                                                                TraderText = _localizedHelper.Localized(traderId),
                                                                IsNotNecessary = !counterCreator.IsNecessary,
                                                                InfoType = QuestMarkerInfo.Type.ConditionVisitPlace
                                                            };

                                                            _questMarkerData.Add(staticInfo);
                                                        }
                                                    }

                                                    break;
                                                }
                                            case ConditionInZone inZone:
                                                {
                                                    var zoneIds = inZone.zoneIds;

                                                    foreach (var zoneId in zoneIds)
                                                    {
                                                        if (_zoneDataHelper.TryGetValues(zoneId,
                                                                out IEnumerable<ExperienceTrigger> triggers))
                                                        {
                                                            foreach (var trigger in triggers)
                                                            {
                                                                var staticInfo = new QuestMarkerInfo
                                                                {
                                                                    Id = counterCreator.id,
                                                                    Where = trigger.transform.position,
                                                                    ZoneId = zoneId,
                                                                    NameText = _localizedHelper.Localized(nameKey),
                                                                    DescriptionKey = counterCreator.id,
                                                                    DescriptionText = _localizedHelper.Localized(counterCreator.id),
                                                                    TraderId = traderId,
                                                                    TraderText = _localizedHelper.Localized(traderId),
                                                                    IsNotNecessary = !counterCreator.IsNecessary,
                                                                    InfoType = QuestMarkerInfo.Type.ConditionInZone
                                                                };

                                                                _questMarkerData.Add(staticInfo);
                                                            }
                                                        }
                                                    }

                                                    break;
                                                }
                                        }
                                    }

                                    break;
                                }
                        }
                    }
                }

                _minimapSenderService.UpdateQuestData(_questMarkerData);
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            _minimapSenderService.Dispose();
            Destroy(this);
        }
    }
}

public class ZoneData
{
    public static readonly ZoneData Instance = new ZoneData();

    internal readonly List<TriggerWithId> TriggerPoints = new List<TriggerWithId>();

    public IEnumerable<ExperienceTrigger> ExperienceTriggers => TriggerPoints.OfType<ExperienceTrigger>();

    public IEnumerable<PlaceItemTrigger> PlaceItemTriggers => TriggerPoints.OfType<PlaceItemTrigger>();

    public IEnumerable<QuestTrigger> QuestTriggers => TriggerPoints.OfType<QuestTrigger>();

    public ZoneData()
    {
    }

    public void AddTriggers(IEnumerable<TriggerWithId> allTriggers)
    {
        //MinimapSenderPlugin.MinimapSenderLogger.LogError($"AddTriggers()");

        TriggerPoints.AddRange(allTriggers);
    }

    public bool TryGetValues<T>(string id, out IEnumerable<T> triggers) where T : TriggerWithId
    {
        if (typeof(T) == typeof(ExperienceTrigger))
        {
            triggers = (IEnumerable<T>)ExperienceTriggers.Where(x => x.Id == id);
        }
        else if (typeof(T) == typeof(PlaceItemTrigger))
        {
            triggers = (IEnumerable<T>)PlaceItemTriggers.Where(x => x.Id == id);
        }
        else if (typeof(T) == typeof(QuestTrigger))
        {
            triggers = (IEnumerable<T>)QuestTriggers.Where(x => x.Id == id);
        }
        else
        {
            triggers = null;
        }

        return triggers != null && triggers.Any();
    }
}

public class LocalizedHelper
{
    //public static readonly LocalizedHelper Instance = new LocalizedHelper();

    private delegate string LocalizedDelegate(string id, string prefix = null);

    private readonly LocalizedDelegate _refLocalized;

    public LocalizedHelper()
    {
        try
        {
            //GClass1823

            var flags = BindingFlags.Static | BindingFlags.Public;

            //var type = PatchConstants.EftTypes.Single(x => x.GetMethod("ParseLocalization", flags) != null);
            var type = StayInTarkovHelperConstants.EftTypes.Single(x => x.GetMethod("ParseLocalization", flags) != null);

            var localizeFunc = type.GetMethod("Localized", new Type[] { typeof(string), typeof(string) });
            //var localizeCaseFunc = type.GetMethod("Localized", new Type[] { typeof(string), typeof(EStringCase) });
            //var transliterateFunc = type.GetMethod("Transliterate", new Type[] { typeof(string) });

            _refLocalized = (LocalizedDelegate)Delegate.CreateDelegate(typeof(LocalizedDelegate), null, localizeFunc);

            //_refLocalized = AccessTools.MethodDelegate<Func<string, string, string>>(localizeFunc);

            //_refLocalizedCase = AccessTools.MethodDelegate<Func<string, EStringCase, string>>(localizeCaseFunc);

            //_refTransliterate = AccessTools.MethodDelegate<Func<string, string>>(transliterateFunc);
        }
        catch (Exception e)
        {
            MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
        }
    }

    public string Localized(string id, string prefix = null)
    {
        return _refLocalized(id, prefix);
    }

    //public string Localized(string id, EStringCase @case)
    //{
    //    return _refLocalizedCase(id, @case);
    //}

    //public string Transliterate(string text)
    //{
    //    return _refTransliterate(text);
    //}
}

public struct QuestMarkerInfo
{
    public string Id;

    public Vector3 Where;

    public string ZoneId;

    public string[] Target;

    public string NameKey;

    public string NameText;

    public string DescriptionKey;

    public string DescriptionText;

    public string TraderId;

    public string TraderText;

    public int ExIndex;

    public int ExIndex2;

    public bool IsNotNecessary;

    public Type InfoType;

    public enum Type
    {
        Airdrop,
        Exfiltration,
        Switch,
        ConditionLeaveItemAtLocation,
        ConditionPlaceBeacon,
        ConditionFindItem,
        ConditionVisitPlace,
        ConditionInZone
    }
}