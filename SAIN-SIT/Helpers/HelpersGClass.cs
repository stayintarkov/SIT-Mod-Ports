using Comfort.Common;
using EFT;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using StayInTarkov;

using EFTCore = GClass536;
using EFTCoreContainer = GClass537;
using EFTFileSettings = BotSettingsComponents;
using EFTSettingsGroup = DangerData;
using EFTStatModifiersClass = GClass534;
using EFTTime = GClass1303;
using EFTSearchPoint = PlaceForCheck;
// using ScavBaseBrain = GClass290;
// using PMCBaseBrain = GClass286;
// 3.8.0 backport global https://github.com/stayintarkov/StayInTarkov.Client/blob/backtrack/Aki3.8/Source/GlobalUsings.cs says
// using ScavBaseBrain = BaseBrain29; and
// using PMCBaseBrain = BaseBrain25;
// but it doesn't appear to be correct BaseBrain25 and 29 doesn't resolve properly.
// testing 1 n 2 for now i vaguely remember it used in some other port but I can't find it :/
// belettee: PMC is BaseBrain27 but Scav is BaseBrain31

using ScavBaseBrain = BaseBrain31;
using PMCBaseBrain = BaseBrain27;

using BotDifficultySettingsClass = Settings9;
using PathControllerClass = PathController;

using BotEventHandler = GClass603;

using StandartBotBrain = BotBrainClass;
using GClass134 = AbstractCreateNode;

////////
// Fixed some GClass References here, but classes were renamed in the deobfuscation, so much of this isn't necessary anymore. Need to clean this up
////////

namespace SAIN.Helpers
{
    public class UpdateEFTSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EFTFileSettings), "smethod_1");
        }

        [PatchPrefix]
        public static void PatchPrefix(ref EFTSettingsGroup __result, BotDifficulty d, WildSpawnType role)
        {
            // UpdateSettingClass.ManualSettingsUpdate(role, d, __result);
        }
    }

    internal class HelpersGClass
    {
        static HelpersGClass()
        {
            InventoryControllerProp = AccessTools.Field(typeof(Player), "_inventoryController");
            EFTBotSettingsProp = AccessTools.Property(typeof(BotDifficultySettingsClass), "FileSettings");
            RefreshSettingsMethod = AccessTools.Method(typeof(BotDifficultySettingsClass), "method_0");
            PathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
        }

        public static bool UpdateBaseBrain(BotOwner botOwner)
        {
            bool brainSwapped = false;
            WildSpawnType wildSpawnType = botOwner.Profile.Info.Settings.Role;
            switch (wildSpawnType)
            {
                case WildSpawnType.assault:
                case WildSpawnType.assaultGroup:
                case WildSpawnType.crazyAssaultEvent:
                case WildSpawnType.cursedAssault:
                    botOwner.Brain.BaseBrain = new ScavBaseBrain(botOwner);
                    brainSwapped = true;
                    break;

                    default:
                    break;
            }
            if (!brainSwapped && EnumValues.WildSpawn.IsPMC(wildSpawnType))
            {
                botOwner.Brain.BaseBrain = new PMCBaseBrain(botOwner, false);
                brainSwapped = true;
            }

            if (brainSwapped)
            {
                BaseBrain baseBrain = botOwner.Brain.BaseBrain;
                string name = botOwner.name + " " + botOwner.Profile.Info.Settings.Role.ToString();

                botOwner.Brain.Agent = new GClass26<BotLogicDecision>(
                    botOwner.BotsController.AICoreController,
                    baseBrain,
                    AIActionNodeAssigner.ActionsList(botOwner),
                    botOwner.gameObject,
                    name,
                    new Func<BotLogicDecision, GClass134>(botOwner.Brain.method_0));

                if (!InvokeOnSetStrategy(baseBrain, botOwner))
                {
                    return false;
                }
            }
            return brainSwapped;
        }

        private static FieldInfo _onSetStratField;

        private static bool InvokeOnSetStrategy(BaseBrain strategy, BotOwner botOwner)
        {
            // Get the event field using reflection
            if (_onSetStratField == null)
            {
                _onSetStratField = AccessTools.Field(typeof(StandartBotBrain), "OnSetStrategy");
            }

            // Check if the event field exists
            if (_onSetStratField != null)
            {
                // Get the event delegate
                var eventDelegate = (MulticastDelegate)_onSetStratField.GetValue(botOwner.Brain);

                // Check if there are subscribers
                if (eventDelegate != null)
                {
                    // Get the list of event handlers
                    Delegate[] eventHandlers = eventDelegate.GetInvocationList();

                    // Invoke each event handler
                    foreach (Delegate handler in eventHandlers)
                    {
                        handler.DynamicInvoke(strategy);
                    }
                    return true;
                }
            }
            return false;
        }

        public static void RefreshSettings(BotDifficultySettingsClass settings)
        {
            RefreshSettingsMethod.Invoke(settings, null);
        }

        private static readonly MethodInfo RefreshSettingsMethod;

        public static readonly PropertyInfo EFTBotSettingsProp;
        public static readonly FieldInfo InventoryControllerProp;
        public static readonly FieldInfo PathControllerField;

        public static InventoryControllerClass GetInventoryController(Player player)
        {
            return (InventoryControllerClass)InventoryControllerProp.GetValue(player);
        }

        public static BotSettingsComponents GetEFTSettings(WildSpawnType type, BotDifficulty difficulty)
        {
            return (BotSettingsComponents)SAINPlugin.LoadedPreset.BotSettings.GetEFTSettings(type, difficulty);
        }

        public static PathControllerClass GetPathControllerClass(BotMover botMover)
        {
            return (PathControllerClass)PathControllerField.GetValue(botMover);
        }

        public static DateTime UtcNow => EFTTime.UtcNow;
        public static EFTCoreSettings EFTCore => SAINPlugin.LoadedPreset.GlobalSettings.EFTCoreSettings;
        public static float LAY_DOWN_ANG_SHOOT => EFTCore.Core.LAY_DOWN_ANG_SHOOT;
        public static float Gravity => EFTCore.Core.G;
        public static float SMOKE_GRENADE_RADIUS_COEF => EFTCore.Core.SMOKE_GRENADE_RADIUS_COEF;

        public static void PlaySound(IPlayer player, Vector3 pos, float range, AISoundType soundtype)
        {
            Singleton<BotEventHandler>.Instance?.PlaySound(player, pos, range, soundtype);
        }
    }

    public class TemporaryStatModifiers
    {
        public TemporaryStatModifiers(float precision, float accuracySpeed, float gainSight, float scatter, float priorityScatter)
        {
            Modifiers = new EFTStatModifiersClass
            {
                PrecicingSpeedCoef = precision,
                AccuratySpeedCoef = accuracySpeed,
                GainSightCoef = gainSight,
                ScatteringCoef = scatter,
                PriorityScatteringCoef = priorityScatter,
            };
        }

        public EFTStatModifiersClass Modifiers;
    }

    public class SearchPoint
    {
        public EFTSearchPoint Point;
    }

    public class EFTCoreSettings
    {
        public static EFTCoreSettings GetCore()
        {
            UpdateCoreSettings();
            return new EFTCoreSettings
            {
                Core = EFTCoreContainer.Core,
            };
        }

        public static void UpdateCoreSettings()
        {
            var core = EFTCoreContainer.Core;
            core.SCAV_GROUPS_TOGETHER = false;
            core.DIST_NOT_TO_GROUP = 50f;
            core.DIST_NOT_TO_GROUP_SQR = 50f * 50f;
            core.MIN_DIST_TO_STOP_RUN = 0f;
            core.CAN_SHOOT_TO_HEAD = false;
            core.ARMOR_CLASS_COEF = 6f;
            core.SHOTGUN_POWER = 40f;
            core.RIFLE_POWER = 50f;
            core.PISTOL_POWER = 20f;
            core.SMG_POWER = 60f;
            core.SNIPE_POWER = 5f;
            core.SOUND_DOOR_OPEN_METERS = 30f;
            core.SOUND_DOOR_BREACH_METERS = 60f;
            core.JUMP_SPREAD_DIST = 70f;
            core.BASE_WALK_SPEREAD2 = 70f;
        }

        public static void UpdateArmorClassCoef(float coef)
        {
            EFTCoreContainer.Core.ARMOR_CLASS_COEF = coef;
        }

        public static void UpdateCoreSettings(EFTCoreSettings newCore)
        {
            EFTCoreContainer.Core = newCore.Core;
        }

        public EFTCore Core;
    }

    public class EFTBotSettings
    {
        [JsonConstructor]
        public EFTBotSettings()
        { }

        public EFTBotSettings(string name, WildSpawnType type, BotDifficulty[] difficulties)
        {
            Name = name;
            WildSpawnType = type;
            foreach (BotDifficulty diff in difficulties)
            {
                Settings.Add(diff, EFTCoreContainer.GetSettings(diff, type));
            }
        }

        [JsonProperty]
        public string Name;
        [JsonProperty]
        public WildSpawnType WildSpawnType;
        [JsonProperty]
        public Dictionary<BotDifficulty, BotSettingsComponents> Settings = new Dictionary<BotDifficulty, BotSettingsComponents>();
    }
}