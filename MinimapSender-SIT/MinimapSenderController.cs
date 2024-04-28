using Aki.Reflection.Utils;
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

namespace TechHappy.MinimapSender
{
    /// <summary>
    /// Represents a controller for sending minimap data.
    /// </summary>
    public class MinimapSenderController : MonoBehaviour
    {
        internal static MinimapSenderController Instance;
        private MinimapSenderBroadcastService _minimapSenderService;
        private ZoneData _zoneDataHelper;
        private LocalizedHelper _localizedHelper;
        private List<QuestData> _questMarkerData;

        /// <summary>
        /// This method initializes the MinimapSenderController instance, starts the broadcasting of player position, and updates the quest marker data.
        /// </summary>
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

                _questMarkerData = new List<QuestData>();

                // IEnumerable<TriggerWithId> allTriggers = FindObjectsOfType<TriggerWithId>();
                //
                // _zoneDataHelper.AddTriggers(allTriggers);

                // UpdateQuestData();

                // _minimapSenderService.UpdateQuestData(_questMarkerData);
                
                TriggerWithId[] triggers = ZoneDataHelper.GetAllTriggers();
                
                UpdateQuestData(triggers);

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

        /// <summary>
        /// Retrieves the local player from the game world instance.
        /// </summary>
        /// <returns>
        /// The Player instance if it exists, otherwise null.
        /// </returns>
        private Player GetLocalPlayerFromWorld()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null || gameWorld.MainPlayer == null)
            {
                return null;
            }

            return gameWorld.MainPlayer;
        }

        /// <summary>
        /// Retrieves the instance of the GameWorld.
        /// </summary>
        /// <returns>The GameWorld instance if it exists, otherwise null.</returns>
        private GameWorld GetGameWorld()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                return null;
            }

            return gameWorld;
        }

        /// <summary>
        /// Clears marker data, gets all quests and their conditions, and builds an array of marker data.
        /// </summary>
        public void UpdateQuestData(TriggerWithId[] allTriggers)
        {
            MinimapSenderPlugin.MinimapSenderLogger.LogDebug("Starting UpdateQuestData");
            
            try
            {
                _questMarkerData = new List<QuestData>(32);

                GameWorld gameWorld = Singleton<GameWorld>.Instance;
                Player player = gameWorld.MainPlayer;
                
                // FieldInfo _questControllerQuestsField = AccessTools.Field(typeof(AbstractQuestControllerClass), "Quests");
                // MethodInfo _getQuestMethod = AccessTools.Method(_questControllerQuestsField.FieldType, "GetQuest", new Type[] { typeof(string) });
                
                var questController = AccessTools.Field(typeof(Player), "_questController").GetValue(player);
                var quests = AccessTools.Property(typeof(AbstractQuestControllerClass), "Quests")
                    .GetValue(questController);
                
                if (quests == null)
                {
                    MinimapSenderPlugin.MinimapSenderLogger.LogError("quests is null");
                    return;
                }

                MinimapSenderPlugin.MinimapSenderLogger.LogDebug("Getting quest list from list_1");
                
                var questsList = Traverse.Create(quests).Field("list_1").GetValue<List<QuestDataClass>>();
                
                if (questsList == null)
                {
                    MinimapSenderPlugin.MinimapSenderLogger.LogError("questsList is null");
                    return;
                }
                
                MinimapSenderPlugin.MinimapSenderLogger.LogDebug("Getting LootItems from list_0");

                var lootItemsList = Traverse.Create(gameWorld).Field("LootItems").Field("list_0").GetValue<List<LootItem>>();

                (string Id, LootItem Item)[] questItems =
                    lootItemsList.Where(x => x.Item.QuestItem).Select(x => (x.TemplateId, x)).ToArray();

                MinimapSenderPlugin.MinimapSenderLogger.LogDebug("Starting foreach quest in questsList");
                
                foreach (QuestDataClass quest in questsList)
                {
                    if (quest.Status != EQuestStatus.Started)
                    {
                        continue;
                    }
                    
                    var template = Traverse.Create(quest).Field("Template").GetValue<RawQuestClass>();
                    
                    if (template == null)
                    {
                        MinimapSenderPlugin.MinimapSenderLogger.LogError("template is null");
                        continue;
                    }

                    var nameKey = Traverse.Create(template).Property("NameLocaleKey").GetValue<string>();

                    var traderId = Traverse.Create(template).Property("TraderId").GetValue<string>();

                    var questConditions =
                        Traverse.Create(template).Property("Conditions").GetValue<IDictionary>();

                    foreach (DictionaryEntry conditionList in questConditions)
                    {
                        if ((EQuestStatus)conditionList.Key == EQuestStatus.AvailableForFinish)
                        {
                            MinimapSenderPlugin.MinimapSenderLogger.LogDebug(
                                $"Condition value type: {conditionList.Value.GetType()}");

                            var conditionsAvailable = Traverse.Create(conditionList.Value).Field("list_0").GetValue();

                            foreach (Condition condition in conditionsAvailable as List<Condition>)
                            {
                                MinimapSenderPlugin.MinimapSenderLogger.LogDebug($"Condition: {condition}");

                                // Check if this condition of the quest has already been completed
                                //MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Quest -> IsConditionDone: {item.IsConditionDone(condition)}");
                                if (quest.CompletedConditions.Contains(condition.id))
                                {
                                    continue;
                                }

                                switch (condition)
                                {
                                    case ConditionLeaveItemAtLocation location:
                                    {
                                        string zoneId = location.zoneId;
                                        IEnumerable<PlaceItemTrigger> zoneTriggers =
                                            allTriggers.GetZoneTriggers<PlaceItemTrigger>(zoneId);

                                        if (zoneTriggers != null)
                                        {
                                            foreach (PlaceItemTrigger trigger in zoneTriggers)
                                            {
                                                var staticInfo = new QuestData
                                                {
                                                    Id = location.id,
                                                    Location = trigger.transform.position,
                                                    ZoneId = zoneId,
                                                    NameText = _localizedHelper.Localized(nameKey),
                                                    Description = _localizedHelper.Localized(location.id),
                                                    Trader = TraderIdToName(traderId),
                                                };

                                                _questMarkerData.Add(staticInfo);
                                            }
                                        }

                                        break;
                                    }
                                    case ConditionPlaceBeacon beacon:
                                    {
                                        MinimapSenderPlugin.MinimapSenderLogger.LogDebug($"Beacon IsHiddenValue: {beacon.IsHiddenValue}, IsNecessary: {beacon.IsNecessary}");
                                        
                                        string zoneId = beacon.zoneId;

                                        IEnumerable<PlaceItemTrigger> zoneTriggers =
                                            allTriggers.GetZoneTriggers<PlaceItemTrigger>(zoneId);

                                        if (zoneTriggers != null)
                                        {
                                            foreach (PlaceItemTrigger trigger in zoneTriggers)
                                            {
                                                var staticInfo = new QuestData
                                                {
                                                    Id = beacon.id,
                                                    Location = trigger.transform.position,
                                                    ZoneId = zoneId,
                                                    NameText = _localizedHelper.Localized(nameKey),
                                                    Description = _localizedHelper.Localized(beacon.id),
                                                    Trader = TraderIdToName(traderId),
                                                };

                                                _questMarkerData.Add(staticInfo);
                                            }
                                        }

                                        break;
                                    }
                                    case ConditionFindItem findItem:
                                    {
                                        string[] itemIds = findItem.target;

                                        foreach (string itemId in itemIds)
                                        {
                                            foreach ((string Id, LootItem Item) questItem in questItems)
                                            {
                                                if (questItem.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    var staticInfo = new QuestData
                                                    {
                                                        Id = findItem.id,
                                                        Location = questItem.Item.transform.position,
                                                        NameText = _localizedHelper.Localized(nameKey),
                                                        Description = _localizedHelper.Localized(findItem.id),
                                                        Trader = TraderIdToName(traderId),
                                                    };

                                                    _questMarkerData.Add(staticInfo);
                                                }
                                            }
                                        }

                                        break;
                                    }
                                    case ConditionCounterCreator counterCreator:
                                    {
                                        // var counter = Traverse.Create(counterCreator).Field("counter")
                                        //     .GetValue<object>();
                                        //
                                        // var conditions = Traverse.Create(counter).Property("conditions")
                                        //     .GetValue<object>();
                                        //
                                        // var conditionsList = Traverse.Create(conditions).Field("list_0")
                                        //     .GetValue<IList>();
                                        
                                        var conditionList2 = Traverse.Create(counterCreator.Conditions).Field("list_0").GetValue<IList>();

                                        foreach (object condition2 in conditionList2)
                                        {
                                            switch (condition2)
                                            {
                                                case ConditionVisitPlace place:
                                                {
                                                    string zoneId = place.target;

                                                    IEnumerable<ExperienceTrigger> zoneTriggers =
                                                        allTriggers.GetZoneTriggers<ExperienceTrigger>(zoneId);

                                                    if (zoneTriggers != null)
                                                    {
                                                        foreach (ExperienceTrigger trigger in zoneTriggers)
                                                        {
                                                            var staticInfo = new QuestData
                                                            {
                                                                Id = counterCreator.id,
                                                                Location = trigger.transform.position,
                                                                ZoneId = zoneId,
                                                                NameText = _localizedHelper.Localized(nameKey),
                                                                Description =
                                                                    _localizedHelper.Localized(counterCreator.id),
                                                                Trader = TraderIdToName(traderId),
                                                            };

                                                            _questMarkerData.Add(staticInfo);
                                                        }
                                                    }

                                                    break;
                                                }
                                                case ConditionInZone inZone:
                                                {
                                                    string[] zoneIds = inZone.zoneIds;

                                                    foreach (string zoneId in zoneIds)
                                                    {
                                                        IEnumerable<ExperienceTrigger> zoneTriggers =
                                                            allTriggers.GetZoneTriggers<ExperienceTrigger>(zoneId);

                                                        if (zoneTriggers != null)
                                                        {
                                                            foreach (ExperienceTrigger trigger in zoneTriggers)
                                                            {
                                                                var staticInfo = new QuestData
                                                                {
                                                                    Id = counterCreator.id,
                                                                    Location = trigger.transform.position,
                                                                    ZoneId = zoneId,
                                                                    NameText = _localizedHelper.Localized(nameKey),
                                                                    Description =
                                                                        _localizedHelper.Localized(counterCreator.id),
                                                                    Trader = TraderIdToName(traderId),
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
        
        private static QuestLocation ToQuestLocation(UnityEngine.Vector3 vector)
        {
            return new QuestLocation(vector.x, vector.y, vector.z);
        }

        private string TraderIdToName(string traderId)
        {
            if (traderId.Equals("5ac3b934156ae10c4430e83c", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Ragman";
            }

            if (traderId.Equals("54cb50c76803fa8b248b4571", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Prapor";
            }

            if (traderId.Equals("54cb57776803fa99248b456e", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Therapist";
            }

            if (traderId.Equals("579dc571d53a0658a154fbec", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Fence";
            }

            if (traderId.Equals("58330581ace78e27b8b10cee", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Skier";
            }

            if (traderId.Equals("5935c25fb3acc3127c3d8cd9", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Peacekeeper";
            }

            if (traderId.Equals("5a7c2eca46aef81a7ca2145d", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Mechanic";
            }

            if (traderId.Equals("5c0647fdd443bc2504c2d371", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Jaeger";
            }

            if (traderId.Equals("638f541a29ffd1183d187f57", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Prapor";
            }

            if (traderId.Equals("54cb50c76803fa8b248b4571", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Lighthouse Keeper ";
            }

            return _localizedHelper.Localized(traderId);
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
    private delegate string LocalizedDelegate(string id, string prefix = null);

    private readonly LocalizedDelegate _refLocalized;

    public LocalizedHelper()
    {
        try
        {
            var flags = BindingFlags.Static | BindingFlags.Public;

            var type = PatchConstants.EftTypes.Single(x => x.GetMethod("ParseLocalization", flags) != null);

            var localizeFunc = type.GetMethod("Localized", new Type[] { typeof(string), typeof(string) });

            _refLocalized = (LocalizedDelegate)Delegate.CreateDelegate(typeof(LocalizedDelegate), null, localizeFunc);
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
}

public struct QuestData
{
    public string Description { get; set; }

    public string Id { get; set; }

    public Vector3 Location { get; set; }

    public string NameText { get; set; }

    public string Trader { get; set; }

    public string ZoneId { get; set; }
}

public class QuestLocation
{
    public QuestLocation()
    {
    }

    public QuestLocation(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; set; }

    public double Y { get; set; }

    public double Z { get; set; }
}