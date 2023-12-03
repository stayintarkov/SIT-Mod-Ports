using BepInEx.Logging;
using EFT;
using System;
using System.Text;
using UnityEngine;
using DrakiaXYZ.BigBrain.Brains;

namespace DrakiaXYZ.BotDebug
{
    internal class BotInfo
    {
        private static readonly StringBuilder stringBuilder = new StringBuilder();
        private static readonly string greyTextColor = new Color(0.8f, 0.8f, 0.8f).GetRichTextColor();
        private static readonly string greenTextColor = new Color(0.25f, 1f, 0.2f).GetRichTextColor();
        private static ManualLogSource Logger;

        public static StringBuilder GetInfoText(ActorDataStruct actorDataStruct, Player localPlayer, BotInfoMode mode)
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(sourceName: typeof(BotInfo).Name);
            }

            Color botNameColor = Color.white;
            if (actorDataStruct.PlayerOwner != null)
            {
                botNameColor = Color.green;
                foreach (var enemyInfo in actorDataStruct.PlayerOwner.AIData.BotOwner.EnemiesController.EnemyInfos.Values)
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
            var botData = actorDataStruct.BotData;
            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);
            stringBuilder.AppendLabeledValue("Layer", botData.LayerName, Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Nickname", actorDataStruct.PlayerOwner?.Nickname, Color.white, Color.white, true);
            if (string.IsNullOrEmpty(botData.CustomData))
            {
                stringBuilder.AppendLine("No Custom Data");
            }
            else
            {
                stringBuilder.AppendLine(botData.CustomData);
            }
            return stringBuilder;
        }

        private static StringBuilder GetSpecial(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = actorDataStruct.BotData;
            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);

            for (int i = 0; i < actorDataStruct.ProfileId.Length; i += 12)
            {
                int chunkSize = Math.Min(12, actorDataStruct.ProfileId.Length - i);
                if (i == 0)
                {
                    stringBuilder.AppendLabeledValue("Id", actorDataStruct.ProfileId.Substring(0, chunkSize), Color.white, Color.white, true);
                }
                else
                {
                    stringBuilder.AppendLabeledValue("", actorDataStruct.ProfileId.Substring(i, chunkSize), Color.white, Color.white, false);
                }
            }
            
            stringBuilder.AppendLabeledValue("WeapSpawn", $"{botData.IsInSpawnWeapon}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("AxeEnemy", $"{botData.HaveAxeEnemy}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Role", $"{botData.BotRole}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("State", $"{botData.PlayerState}", Color.white, Color.white, true);
            string data;
            try
            {
                data = actorDataStruct.PlayerOwner.CurrentStataName.ToString();
            }
            catch (Exception)
            {
                data = "no data";
            }
            stringBuilder.AppendLabeledValue("StateLc", data, Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Shoot", actorDataStruct.BotData.ToString(), Color.white, Color.white, true);
            return stringBuilder;
        }

        private static StringBuilder GetBehaviour(ActorDataStruct actorDataStruct, Color botNameColor, Player localPlayer)
        {
            var botData = actorDataStruct.BotData;
            stringBuilder.Clear();

            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);
            stringBuilder.AppendLabeledValue("Layer", botData.LayerName, Color.yellow, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Node", botData.NodeName, Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("EnterBy", botData.Reason, greenTextColor, greenTextColor, true);
            if (!string.IsNullOrEmpty(botData.PrevNodeName))
            {
                stringBuilder.AppendLabeledValue("PrevNode", botData.PrevNodeName, greyTextColor, greyTextColor, true);
                stringBuilder.AppendLabeledValue("ExitBy", botData.PrevNodeExitReason, greyTextColor, greyTextColor, true);
            }

            if (actorDataStruct.PlayerOwner != null)
            {
                int dist = Mathf.RoundToInt((actorDataStruct.PlayerOwner.iPlayer.Position - localPlayer.Transform.position).magnitude);
                stringBuilder.AppendLabeledValue("Dist", $"{dist}", Color.white, Color.white, true);
            }

            return stringBuilder;
        }

        private static StringBuilder GetBattleState(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = actorDataStruct.BotData;
            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);
            try
            {
                if (actorDataStruct.PlayerOwner != null)
                {
                    BotOwner botOwner = actorDataStruct.PlayerOwner.AIData.BotOwner;
                    Player.FirearmController firearmController = botOwner.GetPlayer.HandsController as Player.FirearmController;
                    if (firearmController != null)
                    {
                        int chamberAmmoCount = firearmController.Item.ChamberAmmoCount;
                        int currentMagazineCount = firearmController.Item.GetCurrentMagazineCount();
                        stringBuilder.AppendLabeledValue("Ammo", $"C: {chamberAmmoCount} M: {currentMagazineCount} T: {botData.Ammo}", Color.white, Color.white, true);
                    }
                    stringBuilder.AppendLabeledValue("Hits", $"{botData.HitsOnMe} / {botData.ShootsOnMe}", Color.white, Color.white, true);
                    stringBuilder.AppendLabeledValue("Reloading", $"{botData.Reloading}", Color.white, Color.white, true);
                    stringBuilder.AppendLabeledValue("CoverId", $"{botData.CoverIndex}", Color.white, Color.white, true);

                    bool weaponReady = botOwner.WeaponManager?.Selector?.IsWeaponReady == true;
                    bool hasMalfunction = botOwner.WeaponManager?.Malfunctions?.HaveMalfunction() == true;
                    stringBuilder.AppendLabeledValue("WeaponReady", $"{weaponReady}", Color.white, weaponReady ? Color.white : Color.red, true);
                    stringBuilder.AppendLabeledValue("Malfunction", $"{hasMalfunction}", Color.white, !hasMalfunction ? Color.white : Color.red, true);

                }
                else
                {
                    stringBuilder.Append("no battle data");
                }
            }
            catch (Exception ex)
            {
                stringBuilder.AppendLabeledValue("Error", "Debug panel firearms error", Color.red, Color.red, true);
                Logger.LogError(ex);
            }

            if (actorDataStruct.PlayerOwner != null)
            {
                var goalEnemy = actorDataStruct.PlayerOwner.AIData.BotOwner.Memory.GoalEnemy;
                if (goalEnemy?.Person?.IsAI == true)
                {
                    if (goalEnemy?.Person?.AIData?.BotOwner != null)
                    {
                        stringBuilder.AppendLabeledValue("GoalEnemy", $"{goalEnemy?.Person?.AIData?.BotOwner?.name}", Color.white, Color.white, true);
                    }
                }
                else
                {
                    stringBuilder.AppendLabeledValue("GoalEnemy", $"{goalEnemy?.Nickname}", Color.white, Color.white, true);
                }
            }

            return stringBuilder;
        }

        private static StringBuilder GetHealth(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = actorDataStruct.BotData;
            var healthData = actorDataStruct.HeathsData;
            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);
            stringBuilder.AppendLabeledValue("Head", $"{healthData.HealthHead}{GetBlackoutLabel(healthData.HealthHeadBL)}{GetBrokenLabel(healthData.HealthHeadBroken)}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Chest", $"{healthData.HealthBody}{GetBlackoutLabel(healthData.HealthBodyBL)}{GetBrokenLabel(healthData.HealthHeadBroken)}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Stomach", $"{healthData.HealthStomach}{GetBlackoutLabel(healthData.HealthStomachBL)}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Arms", $"{healthData.HealthLeftArm}{GetBlackoutLabel(healthData.HealthLeftArmBL)}{GetBrokenLabel(healthData.HealthLeftArmBroken)} {healthData.HealthRightArm}{GetBlackoutLabel(healthData.HealthRightArmBL)}{GetBrokenLabel(healthData.HealthRightArmBroken)}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Legs", $"{healthData.HealthLeftLeg}{GetBlackoutLabel(healthData.HealthLeftLegBL)}{GetBrokenLabel(healthData.HealthLeftLegBroken)} {healthData.HealthRightLeg}{GetBlackoutLabel(healthData.HealthRightLegBL)}{GetBrokenLabel(healthData.HealthRightLegBroken)}", Color.white, Color.white, true);
            return stringBuilder;
        }

#if !STANDALONE
        private static StringBuilder GetBigBrainLayer(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = actorDataStruct.BotData;

            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);

            object activeLayer = BrainManager.GetActiveLayer(actorDataStruct.PlayerOwner.AIData.BotOwner);
            if (activeLayer != null)
            {
                stringBuilder.AppendLabeledValue("Class", $"{activeLayer.GetType().Name}", Color.white, Color.white, true);
                AddActiveLayer(stringBuilder, activeLayer);
                (activeLayer as CustomLayer)?.BuildDebugText(stringBuilder);
            }

            return stringBuilder;
        }

        private static StringBuilder GetBigBrainLogic(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            var botData = actorDataStruct.BotData;

            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);

            object activeLayer = BrainManager.GetActiveLayer(actorDataStruct.PlayerOwner.AIData.BotOwner);
            AddActiveLayer(stringBuilder, activeLayer);

            object activeLogic = BrainManager.GetActiveLogic(actorDataStruct.PlayerOwner.AIData.BotOwner);
            if (activeLogic != null)
            {
                stringBuilder.AppendLabeledValue("Logic", $"{activeLogic.GetType().Name}", Color.white, Color.white, true);
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
                    stringBuilder.AppendLabeledValue("Layer", $"{customLayer.GetName()}", Color.white, Color.white, true);
                }
                else if (activeLayer is BaseLogicLayerClass logicLayer)
                {
                    stringBuilder.AppendLabeledValue("Layer", $"{logicLayer.Name()}", Color.grey, Color.grey, true);
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
