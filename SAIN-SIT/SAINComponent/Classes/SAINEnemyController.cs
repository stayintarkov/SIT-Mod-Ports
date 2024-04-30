using BepInEx.Logging;
using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent;
using SAIN.SAINComponent.BaseClasses;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyController : SAINBase, ISAINClass
    {
        public bool HasEnemy => ActiveEnemy?.EnemyPerson?.IsActive == true;
        public SAINEnemy ActiveEnemy { get; private set; }
        public SAINEnemy ClosestHeardEnemy { get; private set; }
        public bool IsHumanPlayerActiveEnemy => ActiveEnemy != null && ActiveEnemy.EnemyIPlayer != null && ActiveEnemy.EnemyIPlayer.AIData?.IsAI == false;

        public readonly Dictionary<string, SAINEnemy> Enemies = new Dictionary<string, SAINEnemy>();

        public SAINEnemyController(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            if (BotOwner != null)
            {
                BotOwner.Memory.OnAddEnemy += AddEnemy;
            }
            else
            {
                Logger.LogAndNotifyError("Botowner Null in EnemyController Init");
            }
        }

        public void Update()
        {
            UpdateEnemies();
            CheckAddEnemy();

            if (ClosestHeardEnemy != null && ClosestHeardEnemy.HeardRecently == false)
            {
                ClosestHeardEnemy = null;
            }

            UpdateDebug();
        }

        private void UpdateEnemies()
        {
            foreach (var keyPair in Enemies)
            {
                string id = keyPair.Key;
                var enemy = keyPair.Value;
                var enemyPerson = enemy?.EnemyPerson;

                if (enemyPerson?.PlayerNull == true)
                {
                    EnemyIDsToRemove.Add(id);
                }
                // Redundant Checks
                // Common checks between PMC and bots
                else if (enemy == null || enemy.EnemyPlayer == null || enemy.EnemyPlayer.HealthController?.IsAlive == false)
                {
                    EnemyIDsToRemove.Add(id);
                }
                // Checks specific to bots
                else if (enemy.EnemyPlayer.IsAI && (
                    enemy.EnemyPlayer.AIData?.BotOwner == null ||
                    enemy.EnemyPlayer.AIData.BotOwner.ProfileId == BotOwner.ProfileId ||
                    enemy.EnemyPlayer.AIData.BotOwner.BotState != EBotState.Active))
                {
                    EnemyIDsToRemove.Add(id);
                }
                else
                {
                    enemy.Update();
                }
            }

            foreach (string idToRemove in EnemyIDsToRemove)
            {
                Enemies.Remove(idToRemove);
            }

            EnemyIDsToRemove.Clear();
        }

        public SAINEnemy FindClosestHeardEnemy()
        {
            if (findClosestHeardTimer < Time.time)
            {
                findClosestHeardTimer = Time.time + 0.5f;
                float closestEnemyDist = float.MaxValue;
                ClosestHeardEnemy = null;
                foreach (var enemy in SAIN.EnemyController.Enemies)
                {
                    float enemyDist = (enemy.Value.EnemyPosition - SAIN.Position).sqrMagnitude;
                    if (enemy.Value?.HeardRecently == true && enemyDist < closestEnemyDist)
                    {
                        closestEnemyDist = enemyDist;
                        ClosestHeardEnemy = enemy.Value;
                    }
                }
            }
            return ClosestHeardEnemy;
        }

        private float findClosestHeardTimer;

        private void UpdateDebug()
        {
            if (ActiveEnemy != null)
            {
                if (SAINPlugin.DebugMode && SAINPlugin.DrawDebugGizmos)
                {
                    if (ActiveEnemy.LastHeardPosition != null)
                    {
                        if (debugLastHeardPosition == null)
                        {
                            debugLastHeardPosition = DebugGizmos.Line(ActiveEnemy.LastHeardPosition.Value, SAIN.Position, Color.yellow, 0.01f, false, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdatePositionLine(ActiveEnemy.LastHeardPosition.Value, SAIN.Position, debugLastHeardPosition);
                    }
                    if (ActiveEnemy.LastSeenPosition != null)
                    {
                        if (debugLastSeenPosition == null)
                        {
                            debugLastSeenPosition = DebugGizmos.Line(ActiveEnemy.LastSeenPosition.Value, SAIN.Position, Color.red, 0.01f, false, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdatePositionLine(ActiveEnemy.LastSeenPosition.Value, SAIN.Position, debugLastSeenPosition);
                    }
                }
                else if (debugLastHeardPosition != null || debugLastSeenPosition != null)
                {
                    GameObject.Destroy(debugLastHeardPosition);
                    GameObject.Destroy(debugLastSeenPosition);
                }
            }
            else if (debugLastHeardPosition != null || debugLastSeenPosition != null)
            {
                GameObject.Destroy(debugLastHeardPosition);
                GameObject.Destroy(debugLastSeenPosition);
            }
        }

        private GameObject debugLastSeenPosition;
        private GameObject debugLastHeardPosition;

        public void Dispose()
        {
            if (BotOwner != null)
                BotOwner.Memory.OnAddEnemy -= AddEnemy;

            foreach (var enemy in Enemies)
            {
                enemy.Value?.Dispose();
            }
            Enemies?.Clear();
        }

        public SAINEnemy GetEnemy(string id)
        {
            if (Enemies.ContainsKey(id))
            {
                return Enemies[id];
            }
            return null;
        }

        public void ClearEnemy()
        {
            ActiveEnemy = null;
            ClosestHeardEnemy = null;
        }

        public void RemoveEnemy(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                return;
            }
            if (ActiveEnemy?.EnemyPerson != null && ActiveEnemy.EnemyPerson.ProfileId == iPlayer.ProfileId)
            {
                ActiveEnemy = null;
            }
            if (Enemies.TryGetValue(iPlayer.ProfileId, out SAINEnemy enemy))
            {
                enemy.Dispose();
                Enemies.Remove(iPlayer.ProfileId);
            }
        }

        private void CheckAddEnemy()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            
            if (goalEnemy != null)
            {
                IPlayer iPlayer = goalEnemy?.Person;
                AddEnemy(iPlayer);
                if (Enemies.ContainsKey(iPlayer.ProfileId))
                {
                    ActiveEnemy = Enemies[iPlayer.ProfileId];
                }
            }
            else
            {
                ActiveEnemy = null;
            }
        }

        public SAINEnemy CheckAddEnemy(IPlayer IPlayer)
        {
            AddEnemy(IPlayer);

            if (IPlayer != null && Enemies.ContainsKey(IPlayer.ProfileId))
            {
                return Enemies[IPlayer.ProfileId];
            }
            return null;
        }

        private void AddEnemy(IPlayer player)
        {
            if (player == null 
                || !player.HealthController.IsAlive
                || Enemies.ContainsKey(player.ProfileId) 
                || player.ProfileId == SAIN.ProfileId 
                || player.IsAI && player.AIData?.BotOwner == null)
            {
                return;
            }

            if (BotOwner.EnemiesController.EnemyInfos.TryGetValue(player, out EnemyInfo enemyInfo))
            {
                SAINPersonClass enemySAINPerson 
                    = GetSAINPerson(player);

                SAINEnemy newEnemy = new SAINEnemy(SAIN, enemySAINPerson, enemyInfo);

                player.HealthController.DiedEvent += newEnemy.DeleteInfo;

                Enemies.Add(player.ProfileId, newEnemy);
            }
        }

        private bool CheckPlayerNull(IPlayer player)
        {
            bool isNull = false;
            if (player == null)
            {
                isNull = true;
            }
            else if (player.IsAI && (player.AIData?.BotOwner == null || player.AIData.BotOwner.BotState != EBotState.Active))
            {
                isNull = true;
            }
            else if (player.ProfileId == SAIN.ProfileId)
            {
                isNull = true;
            }
            else if (!player.HealthController.IsAlive)
            {
                isNull = true;
            }
            return isNull;
        }


        public bool IsHumanPlayerLookAtMe(out Player lookingPlayer)
        {
            if (CheckMainPlayerVisionTimer < Time.time)
            {
                CheckMainPlayerVisionTimer = Time.time + 0.25f;
                MainPlayerWasLookAtMe = false;
                _lookingPlayer = null;

                var gameworld = GameWorldHandler.SAINGameWorld?.GameWorld;
                if (gameworld != null)
                {
                    var players = gameworld.AllAlivePlayersList;
                    if (players != null)
                    {
                        foreach (var player in players)
                        {
                            if (SAIN.Memory.VisiblePlayers.Contains(player))
                            {
                                Vector3 lookDir = player.LookDirection;
                                Vector3 playerHeadPos = player.MainParts[BodyPartType.head].Position;

                                Vector3 botChestPos = SAIN.Person.Transform.Chest;
                                Vector3 botDir = (botChestPos - playerHeadPos);

                                if (Vector3.Dot(lookDir, botDir.normalized) > 0.75f)
                                {
                                    MainPlayerWasLookAtMe = true;
                                    _lookingPlayer = player;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            lookingPlayer = _lookingPlayer;
            return MainPlayerWasLookAtMe;
        }

        private Player _lookingPlayer;
        private float CheckMainPlayerVisionTimer;
        private bool MainPlayerWasLookAtMe;

        public bool IsPlayerAnEnemy(string profileID)
        {
            return Enemies.ContainsKey(profileID) && Enemies[profileID] != null;
        }

        private static SAINPersonClass GetSAINPerson(IPlayer IPlayer)
        {
            if (IPlayer == null)
            {
                return null;
            }

            Player player = Singleton<GameWorld>.Instance?.GetAlivePlayerByProfileID(IPlayer.ProfileId);
            if (player == null)
            {
                return null;
            }

            SAINPersonComponent _SAINPersonComponent = player?.gameObject.GetOrAddComponent<SAINPersonComponent>();

            if (_SAINPersonComponent?.SAINPerson == null)
            {
                _SAINPersonComponent?.Init(IPlayer);
            }

            return _SAINPersonComponent?.SAINPerson;
        }

        private readonly List<string> EnemyIDsToRemove = new List<string>();
    }
}
