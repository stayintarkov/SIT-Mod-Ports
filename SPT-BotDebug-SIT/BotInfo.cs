using BepInEx.Logging;
using EFT;
using System;
using System.Text;
using UnityEngine;
using DrakiaXYZ.BigBrain.Brains;
using System.Reflection;
using DrakiaXYZ.BotDebug.Helpers;

namespace DrakiaXYZ.BotDebug
{
    internal class BotInfo
    {
        private static readonly StringBuilder stringBuilder = new StringBuilder();
        private static readonly string greyTextColor = "#CCCCCC";
        private static readonly string greenTextColor = "#40FF33";
        private static ManualLogSource Logger;

        public static StringBuilder GetInfoText(ActorDataStruct actorDataStruct, Player localPlayer, BotInfoMode mode)
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(sourceName: typeof(BotInfo).Name);
            }

            Color botNameColor = Color.white;
            var playerOwner = FieldHelper.PlayerOwnerField.GetValue(actorDataStruct);
            if (playerOwner != null)
            {
                botNameColor = Color.green;
                AIData aiData = FieldHelper.Property<AIData>(playerOwner, "AIData");
                foreach (var enemyInfo in aiData.BotOwner.EnemiesController.EnemyInfos.Values)
                {
                    if (enemyInfo.ProfileId == localPlayer.ProfileId)
                    {
                        botNameColor = Color.red;
                        break;
                    }
                }
            }

            switch (mode)
            {
                case BotInfoMode.Behaviour:
                    return GetBehaviour(actorDataStruct, botNameColor, localPlayer);
                case BotInfoMode.BattleState:
                    return GetBattleState(actorDataStruct, botNameColor);
                case BotInfoMode.Health:
                    return GetHealth(actorDataStruct, botNameColor);
                case BotInfoMode.Specials:
                    return GetSpecial(actorDataStruct, botNameColor);
                case BotInfoMode.Custom:
                    return GetCustom(actorDataStruct, botNameColor);
#if !STANDALONE
                case BotInfoMode.BigBrainLayer:
                    return GetBigBrainLayer(actorDataStruct, botNameColor);
                case BotInfoMode.BigBrainLogic:
                    return GetBigBrainLogic(actorDataStruct, botNameColor);
#endif
                default:
                    return null;
            }
        }

        private static string GetBlackoutLabel(bool val)
        {
            return val ? "(BL)" : "";
        }

        private static string GetBrokenLabel(bool val)
        {
            return val ? "(Broken)" : "";
        }

        private static StringBuilder GetCustom(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = FieldHelper.BotDataField.GetValue(actorDataStruct);
            string name = FieldHelper.Field<string>(botData, "Name");
            string strategyName = FieldHelper.Field<string>(botData, "StrategyName");
            string layerName = FieldHelper.Field<string>(botData, "LayerName");
            string customData = FieldHelper.Field<string>(botData, "CustomData");

            var playerOwner = FieldHelper.PlayerOwnerField.GetValue(actorDataStruct);
            string nickname = "";
            if (playerOwner != null)
            {
                nickname = FieldHelper.Property<string>(playerOwner, "Nickname");
            }

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);
            AppendLabeledValue(stringBuilder, "Layer", layerName, Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Nickname", nickname, Color.white, Color.white, true);
            if (string.IsNullOrEmpty(customData))
            {
                stringBuilder.AppendLine("No Custom Data");
            }
            else
            {
                stringBuilder.AppendLine(customData);
            }
            return stringBuilder;
        }

        private static StringBuilder GetSpecial(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = FieldHelper.BotDataField.GetValue(actorDataStruct);
            string name = FieldHelper.Field<string>(botData, "Name");
            string strategyName = FieldHelper.Field<string>(botData, "StrategyName");
            bool isInSpawnWeapon = FieldHelper.Field<bool>(botData, "IsInSpawnWeapon");
            bool haveAxeEnemy = FieldHelper.Field<bool>(botData, "HaveAxeEnemy");
            int botRole = FieldHelper.Field<int>(botData, "BotRole");
            EPlayerState playerState = FieldHelper.Field<EPlayerState>(botData, "PlayerState");

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);

            for (int i = 0; i < actorDataStruct.ProfileId.Length; i += 12)
            {
                int chunkSize = Math.Min(12, actorDataStruct.ProfileId.Length - i);
                if (i == 0)
                {
                    AppendLabeledValue(stringBuilder, "Id", actorDataStruct.ProfileId.Substring(0, chunkSize), Color.white, Color.white, true);
                }
                else
                {
                    AppendLabeledValue(stringBuilder, "", actorDataStruct.ProfileId.Substring(i, chunkSize), Color.white, Color.white, false);
                }
            }

            AppendLabeledValue(stringBuilder, "WeapSpawn", $"{isInSpawnWeapon}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "AxeEnemy", $"{haveAxeEnemy}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Role", $"{botRole}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "State", $"{playerState}", Color.white, Color.white, true);
            string data = "no data";
            var playerOwner = FieldHelper.PlayerOwnerField.GetValue(actorDataStruct);
            if (playerOwner != null)
            {
                data = FieldHelper.Property<EPlayerState>(playerOwner, "CurrentStataName").ToString();
            }

            AppendLabeledValue(stringBuilder, "StateLc", data, Color.white, Color.white, true);
            return stringBuilder;
        }

        private static StringBuilder GetBehaviour(ActorDataStruct actorDataStruct, Color botNameColor, Player localPlayer)
        {
            var botData = FieldHelper.BotDataField.GetValue(actorDataStruct);
            string name = FieldHelper.Field<string>(botData, "Name");
            string strategyName = FieldHelper.Field<string>(botData, "StrategyName");
            string layerName = FieldHelper.Field<string>(botData, "LayerName");
            string nodeName = FieldHelper.Field<string>(botData, "NodeName");
            string reason = FieldHelper.Field<string>(botData, "Reason");
            string prevNodeName = FieldHelper.Field<string>(botData, "PrevNodeName");
            string prevNodeExitReason = FieldHelper.Field<string>(botData, "PrevNodeExitReason");

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);
            AppendLabeledValue(stringBuilder, "Layer", layerName, Color.yellow, Color.yellow, true);
            AppendLabeledValue(stringBuilder, "Node", nodeName, Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "EnterBy", reason, greenTextColor, greenTextColor, true);
            if (!string.IsNullOrEmpty(prevNodeName))
            {
                AppendLabeledValue(stringBuilder, "PrevNode", prevNodeName, greyTextColor, greyTextColor, true);
                AppendLabeledValue(stringBuilder, "ExitBy", prevNodeExitReason, greyTextColor, greyTextColor, true);
            }

            var playerOwner = FieldHelper.PlayerOwnerField.GetValue(actorDataStruct);
            if (playerOwner != null)
            {
                IPlayer iPlayer = FieldHelper.Property<IPlayer>(playerOwner, "iPlayer");
                int dist = Mathf.RoundToInt((iPlayer.Position - localPlayer.Transform.position).magnitude);
                AppendLabeledValue(stringBuilder, "Dist", $"{dist}", Color.white, Color.white, true);
            }

            return stringBuilder;
        }

        private static StringBuilder GetBattleState(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = FieldHelper.BotDataField.GetValue(actorDataStruct);
            string name = FieldHelper.Field<string>(botData, "Name");
            string strategyName = FieldHelper.Field<string>(botData, "StrategyName");
            int ammo = FieldHelper.Field<int>(botData, "Ammo");
            int hitsOnMe = FieldHelper.Field<int>(botData, "HitsOnMe");
            int shootsOnMe = FieldHelper.Field<int>(botData, "ShootsOnMe");
            bool reloading = FieldHelper.Field<bool>(botData, "Reloading");
            int coverIndex = FieldHelper.Field<int>(botData, "CoverIndex");

            var playerOwner = FieldHelper.PlayerOwnerField.GetValue(actorDataStruct);

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);
            try
            {
                if (playerOwner != null)
                {
                    AIData aiData = FieldHelper.Property<AIData>(playerOwner, "AIData");
                    BotOwner botOwner = aiData.BotOwner;
                    Player.FirearmController firearmController = botOwner.GetPlayer.HandsController as Player.FirearmController;
                    if (firearmController != null)
                    {
                        int chamberAmmoCount = firearmController.Item.ChamberAmmoCount;
                        int currentMagazineCount = firearmController.Item.GetCurrentMagazineCount();
                        AppendLabeledValue(stringBuilder, "Ammo", $"C: {chamberAmmoCount} M: {currentMagazineCount} T: {ammo}", Color.white, Color.white, true);
                    }
                    AppendLabeledValue(stringBuilder, "Hits", $"{hitsOnMe} / {shootsOnMe}", Color.white, Color.white, true);
                    AppendLabeledValue(stringBuilder, "Reloading", $"{reloading}", Color.white, Color.white, true);
                    AppendLabeledValue(stringBuilder, "CoverId", $"{coverIndex}", Color.white, Color.white, true);

                    bool weaponReady = botOwner.WeaponManager?.Selector?.IsWeaponReady == true;
                    bool hasMalfunction = botOwner.WeaponManager?.Malfunctions?.HaveMalfunction() == true;
                    AppendLabeledValue(stringBuilder, "WeaponReady", $"{weaponReady}", Color.white, weaponReady ? Color.white : Color.red, true);
                    AppendLabeledValue(stringBuilder, "Malfunction", $"{hasMalfunction}", Color.white, !hasMalfunction ? Color.white : Color.red, true);

                }
                else
                {
                    stringBuilder.Append("no battle data");
                }
            }
            catch (Exception ex)
            {
                AppendLabeledValue(stringBuilder, "Error", "Debug panel firearms error", Color.red, Color.red, true);
                Logger.LogError(ex);
            }

            if (playerOwner != null)
            {
                AIData aiData = FieldHelper.Property<AIData>(playerOwner, "AIData");
                var goalEnemy = aiData.BotOwner.Memory.GoalEnemy;
                if (goalEnemy?.Person?.IsAI == true)
                {
                    if (goalEnemy?.Person?.AIData?.BotOwner != null)
                    {
                        AppendLabeledValue(stringBuilder, "GoalEnemy", $"{goalEnemy?.Person?.AIData?.BotOwner?.name}", Color.white, Color.white, true);
                    }
                }
                else
                {
                    AppendLabeledValue(stringBuilder, "GoalEnemy", $"{goalEnemy?.Nickname}", Color.white, Color.white, true);
                }
            }

            return stringBuilder;
        }

        private static StringBuilder GetHealth(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = FieldHelper.BotDataField.GetValue(actorDataStruct);
            string name = FieldHelper.Field<string>(botData, "Name");
            string strategyName = FieldHelper.Field<string>(botData, "StrategyName");

            var healthData = FieldHelper.HealthDataField.GetValue(actorDataStruct);
            int healthHead = FieldHelper.Field<int>(healthData, "HealthHead");
            bool healthHeadBL = FieldHelper.Field<bool>(healthData, "HealthHeadBL");
            bool healthHeadBroken = FieldHelper.Field<bool>(healthData, "HealthHeadBroken");
            int healthBody = FieldHelper.Field<int>(healthData, "HealthBody");
            bool healthBodyBL = FieldHelper.Field<bool>(healthData, "HealthBodyBL");
            int healthStomach = FieldHelper.Field<int>(healthData, "HealthStomach");
            bool healthStomachBL = FieldHelper.Field<bool>(healthData, "HealthStomachBL");
            int healthLeftArm = FieldHelper.Field<int>(healthData, "HealthLeftArm");
            bool healthLeftArmBL = FieldHelper.Field<bool>(healthData, "HealthLeftArmBL");
            bool healthLeftArmBroken = FieldHelper.Field<bool>(healthData, "HealthLeftArmBroken");
            int healthRightArm = FieldHelper.Field<int>(healthData, "HealthRightArm");
            bool healthRightArmBL = FieldHelper.Field<bool>(healthData, "HealthRightArmBL");
            bool healthRightArmBroken = FieldHelper.Field<bool>(healthData, "HealthRightArmBroken");
            int healthLeftLeg = FieldHelper.Field<int>(healthData, "HealthLeftLeg");
            bool healthLeftLegBL = FieldHelper.Field<bool>(healthData, "HealthLeftLegBL");
            bool healthLeftLegBroken = FieldHelper.Field<bool>(healthData, "HealthLeftLegBroken");
            int healthRightLeg = FieldHelper.Field<int>(healthData, "HealthRightLeg");
            bool healthRightLegBL = FieldHelper.Field<bool>(healthData, "HealthRightLegBL");
            bool healthRightLegBroken = FieldHelper.Field<bool>(healthData, "HealthRightLegBroken");

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{name} ({strategyName})", Color.white, botNameColor, false);
            AppendLabeledValue(stringBuilder, "Head", $"{healthHead}{GetBlackoutLabel(healthHeadBL)}{GetBrokenLabel(healthHeadBroken)}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Chest", $"{healthBody}{GetBlackoutLabel(healthBodyBL)}{GetBrokenLabel(healthHeadBroken)}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Stomach", $"{healthStomach}{GetBlackoutLabel(healthStomachBL)}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Arms", $"{healthLeftArm}{GetBlackoutLabel(healthLeftArmBL)}{GetBrokenLabel(healthLeftArmBroken)} {healthRightArm}{GetBlackoutLabel(healthRightArmBL)}{GetBrokenLabel(healthRightArmBroken)}", Color.white, Color.white, true);
            AppendLabeledValue(stringBuilder, "Legs", $"{healthLeftLeg}{GetBlackoutLabel(healthLeftLegBL)}{GetBrokenLabel(healthLeftLegBroken)} {healthRightLeg}{GetBlackoutLabel(healthRightLegBL)}{GetBrokenLabel(healthRightLegBroken)}", Color.white, Color.white, true);
            return stringBuilder;
        }

        private static void AppendLabeledValue(StringBuilder builder, string label, string data, Color labelColor, Color dataColor, bool labelEnabled = true)
        {
            string labelColorString = GetColorString(labelColor);
            string dataColorString = GetColorString(dataColor);

            AppendLabeledValue(builder, label, data, labelColorString, dataColorString, labelEnabled);
        }

        private static void AppendLabeledValue(StringBuilder builder, string label, string data, string labelColor, string dataColor, bool labelEnabled = true)
        {
            if (labelEnabled)
            {
                builder.AppendFormat("<color={0}>{1}:</color>", labelColor, label);
            }

            builder.AppendFormat("<color={0}>{1}</color>\n", dataColor, data);
        }

        private static string GetColorString(Color color)
        {
            if (color == Color.black) return "black";
            if (color == Color.white) return "white";
            if (color == Color.yellow) return "yellow";
            if (color == Color.red) return "red";
            if (color == Color.green) return "green";
            if (color == Color.blue) return "blue";
            if (color == Color.cyan) return "cyan";
            if (color == Color.magenta) return "magenta";
            if (color == Color.gray) return "gray";
            if (color == Color.clear) return "clear";
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

#if !STANDALONE
        private static StringBuilder GetBigBrainLayer(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = actorDataStruct.BotData;

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);

            object activeLayer = BrainManager.GetActiveLayer(actorDataStruct.PlayerOwner.AIData.BotOwner);
            if (activeLayer != null)
            {
                AppendLabeledValue(stringBuilder, "Class", $"{activeLayer.GetType().Name}", Color.white, Color.white, true);
                AddActiveLayer(stringBuilder, activeLayer);
                (activeLayer as CustomLayer)?.BuildDebugText(stringBuilder);
            }

            return stringBuilder;
        }

        private static StringBuilder GetBigBrainLogic(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = actorDataStruct.BotData;

            stringBuilder.Clear();
            AppendLabeledValue(stringBuilder, "Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);

            object activeLayer = BrainManager.GetActiveLayer(actorDataStruct.PlayerOwner.AIData.BotOwner);
            AddActiveLayer(stringBuilder, activeLayer);

            object activeLogic = BrainManager.GetActiveLogic(actorDataStruct.PlayerOwner.AIData.BotOwner);
            if (activeLogic != null)
            {
                AppendLabeledValue(stringBuilder, "Logic", $"{activeLogic.GetType().Name}", Color.white, Color.white, true);
                if (activeLogic is CustomLogic customLogic)
                {
                    customLogic?.BuildDebugText(stringBuilder);
                }
            }

            return stringBuilder;
        }

        private static void AddActiveLayer(StringBuilder stringBuilder, object activeLayer)
        {
            if (activeLayer != null)
            {
                if (activeLayer is CustomLayer customLayer)
                {
                    AppendLabeledValue(stringBuilder, "Layer", $"{customLayer.GetName()}", Color.white, Color.white, true);
                }
                else if (activeLayer is BaseLogicLayerClass logicLayer)
                {
                    AppendLabeledValue(stringBuilder, "Layer", $"{logicLayer.Name()}", Color.grey, Color.grey, true);
                }
            }
        }
#endif

        public enum BotInfoMode
        {
            Minimized = 0,
            Behaviour,
            BattleState,
            Health,
            Specials,
            Custom,
#if !STANDALONE
            BigBrainLayer,
            BigBrainLogic
#endif
        }
    }
}