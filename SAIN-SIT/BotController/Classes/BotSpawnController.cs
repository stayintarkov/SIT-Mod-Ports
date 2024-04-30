using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.Preset;
using static EFT.SpeedTree.TreeWind;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.BaseClasses;
using System.Text;

namespace SAIN.Components.BotController
{
    public class BotSpawnController : SAINControl
    {
        public static BotSpawnController Instance;
        public BotSpawnController() {
            Instance = this;
        }

        public Dictionary<string, SAINComponentClass> SAINBotDictionary = new Dictionary<string, SAINComponentClass>();
        private BotSpawner BotSpawner => BotController?.BotSpawner;

        private static readonly WildSpawnType[] ExclusionList =
        {
            WildSpawnType.bossZryachiy,
            WildSpawnType.followerZryachiy,
            WildSpawnType.peacefullZryachiyEvent,
            WildSpawnType.ravangeZryachiyEvent,
            WildSpawnType.shooterBTR
        };

        public void Update()
        {
            if (BotSpawner != null)
            {
                if (!Subscribed && !GameEnding)
                {
                    BotSpawner.OnBotRemoved += RemoveBot;
                    Subscribed = true;
                }
                if (Subscribed)
                {
                    var status = BotController?.BotGame?.Status;
                    if (status == GameStatus.Stopping || status == GameStatus.Stopped || status == GameStatus.SoftStopping)
                    {
                        BotSpawner.OnBotRemoved -= RemoveBot;
                        Subscribed = false;
                        GameEnding = true;
                    }
                }
            }
        }

        private bool GameEnding = false;
        private bool Subscribed = false;

        private void SetBrainInfo(BotOwner botOwner)
        {
            if (!SAINPlugin.EditorDefaults.CollectBotLayerBrainInfo)
            {
                return;
            }

            WildSpawnType role = botOwner.Profile.Info.Settings.Role;
            string brain = botOwner.Brain.BaseBrain.ShortName();
            BotType botType = BotTypeDefinitions.GetBotType(role);
            if (botType.BaseBrain.IsNullOrEmpty())
            {
                botType.BaseBrain = brain;
                Logger.LogInfo($"Set {role} BaseBrain to {brain}");
                BotTypeDefinitions.ExportBotTypes();
            }
        }

        public SAINComponentClass GetSAIN(BotOwner botOwner, StringBuilder debugString)
        {
            if (botOwner == null)
            {
                Logger.LogAndNotifyError("Botowner is null, cannot get SAIN!");
                debugString = null;
                return null;
            }

            if (debugString == null && SAINPlugin.DebugMode)
            {
                debugString = new StringBuilder();
            }

            string name = botOwner.name;
            SAINComponentClass result = GetSAIN(name, debugString);

            if ( result == null )
            {
                debugString?.AppendLine( $"[{name}] not found in SAIN Bot Dictionary. Getting Component Manually..." );

                result = botOwner.gameObject.GetComponent<SAINComponentClass>();

                if (result != null)
                {
                    debugString?.AppendLine($"[{name}] found after using GetComponent.");
                }
            }

            if ( result == null )
            {
                debugString?.AppendLine( $"[{name}] could not be retrieved from SAIN Bots. WildSpawnType: [{botOwner.Profile.Info.Settings.Role}] Returning Null" );
            }

            if ( result == null && debugString != null )
            {
                Logger.LogAndNotifyError( debugString, EFT.Communications.ENotificationDurationType.Long );
            }
            return result;
        }

        public SAINComponentClass GetSAIN(Player player, StringBuilder debugString)
        {
            if (debugString == null && SAINPlugin.DebugMode)
            {
                debugString = new StringBuilder();
            }

            if (player == null)
            {
                if (debugString != null)
                {
                    debugString.AppendLine("Player is Null, cannot get SAIN!");
                    Logger.LogAndNotifyError(debugString, EFT.Communications.ENotificationDurationType.Long);
                }
                return null;
            }

            if (player.AIData?.BotOwner != null)
            {
                return GetSAIN(player.AIData.BotOwner, debugString);
            }
            return null;
        }

        public SAINComponentClass GetSAIN(string botName, StringBuilder debugString)
        {
            if (debugString == null && SAINPlugin.DebugMode)
            {
                debugString = new StringBuilder();
            }

            SAINComponentClass result = null;
            if ( SAINBotDictionary.ContainsKey(botName) )
            {
                result = SAINBotDictionary[botName];
            }
            if ( result == null )
            {
                debugString?.AppendLine( $"[{botName}] not found in SAIN Bot Dictionary. Comparing names manually to find the bot..." );

                foreach ( var bot in SAINBotDictionary )
                {
                    if (bot.Value != null && bot.Value.name == botName)
                    {
                        result = bot.Value;
                        debugString?.AppendLine($"[{botName}] found after comparing names");
                        break;
                    }
                }
            }
            if ( result == null )
            {
                debugString?.AppendLine( $"[{botName}] Still not found in SAIN Bot Dictionary. Comparing Profile Id instead..." );

                foreach ( var bot in SAINBotDictionary )
                {
                    if ( bot.Value != null && bot.Value.ProfileId == botName )
                    {
                        result = bot.Value;
                        debugString?.AppendLine($"[{botName}] found after comparing profileID. Bot Name was [{bot.Value.name}]");
                        break;
                    }
                }
            }
            return result;
        }

        public void AddBot(BotOwner botOwner)
        {
            try
            {
                if (botOwner != null)
                {
                    var settings = botOwner.Profile?.Info?.Settings;
                    if (settings == null)
                    {
                        return;
                    }

                    Player player = botOwner.GetPlayer;
                    botOwner.LeaveData.OnLeave += RemoveBot;
                    SetBrainInfo(botOwner);

                    if (ExclusionList.Contains(settings.Role))
                    {
                        if (SAINPersonComponent.TryAddSAINPersonToBot(botOwner, out var personComponent) == false)
                        {
                            Logger.LogError("Could not add SAINPerson to bot");
                        }
                        AddNoBushESP(botOwner);
                        return;
                    }

                    if (SAINComponentClass.TryAddSAINToBot(botOwner, out SAINComponentClass component))
                    {
                        string name = botOwner.name;
                        if (SAINBotDictionary.ContainsKey(name))
                        {
                            SAINBotDictionary.Remove(name);
                        }
                        SAINBotDictionary.Add(name, component);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"AddBot: Add Component Error: {ex}");
            }
        }

        public void AddNoBushESP(BotOwner botOwner)
        {
            botOwner.GetOrAddComponent<SAINNoBushESP>().Init(botOwner);
        }

        private bool CheckIfSAINEnabled(BotOwner botOwner)
        {
            Brain brain = BotBrains.Parse(botOwner.Brain.BaseBrain.ShortName());
            return BotBrains.AllBrainsList.Contains(brain);
        }

        public void RemoveBot(BotOwner botOwner)
        {
            try
            {
                if (botOwner != null)
                {
                    SAINBotDictionary.Remove(botOwner.name);
                    if (botOwner.TryGetComponent(out SAINComponentClass component))
                    {
                        component.Dispose();
                    }
                    if (botOwner.TryGetComponent(out SAINNoBushESP noBush))
                    {
                        UnityEngine.Object.Destroy(noBush);
                    }
                    if (botOwner.GetPlayer?.gameObject?.TryGetComponent(out SAINPersonComponent person) == true)
                    {
                        UnityEngine.Object.Destroy(person);
                    }
                }
                else
                {
                    Logger.LogError("Bot is null, cannot dispose!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose Component Error: {ex}");
            }
        }
    }
}
