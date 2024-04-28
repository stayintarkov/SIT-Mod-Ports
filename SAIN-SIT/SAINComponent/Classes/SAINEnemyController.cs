using BepInEx.Logging;
using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.SAINComponent;
using SAIN.SAINComponent.BaseClasses;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyController : SAINBase, ISAINClass
    {
        public SAINEnemyController(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        private void UpdateEnemies()
        {
            foreach (var keyPair in Enemies)
            {
                string id = keyPair.Key;
                var enemy = keyPair.Value;
                var enemyPerson = enemy?.EnemyPerson;
                if (enemyPerson?.IsActive == true)
                {
                    enemy.Update();
                }
                else if (enemyPerson?.PlayerNull == true)
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
                    enemy.UpdateHearStatus();
                }
            }

            foreach (string idToRemove in EnemyIDsToRemove)
            {
                Enemies.Remove(idToRemove);
            }

            EnemyIDsToRemove.Clear();
        }

        public void UpdateHeardEnemy(bool canHear, string id)
        {

        }

        public void Update()
        {
            UpdateEnemies();
            CheckAddEnemy();
        }

        public void Dispose()
        {
            Enemies?.Clear();
        }

        public SAINEnemyClass GetEnemy(string id)
        {
            if (Enemies.ContainsKey(id))
            {
                return Enemies[id];
            }
            return null;
        }

        public bool HasEnemy => ActiveEnemy?.EnemyPerson?.IsActive == true;

        public SAINEnemyClass ActiveEnemy { get; private set; }

        public void ClearEnemy()
        {
            ActiveEnemy = null;
        }

        public void CheckAddEnemy()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            IPlayer IPlayer = goalEnemy?.Person;
            bool addEnemy = true;

            if (goalEnemy == null || IPlayer == null)
            {
                addEnemy = false;
            }
            else
            {
                if (IPlayer.IsAI && (IPlayer.AIData?.BotOwner == null || IPlayer.AIData.BotOwner.BotState != EBotState.Active))
                {
                    addEnemy = false;
                }
                if (IPlayer.ProfileId == SAIN.ProfileId)
                {
                    addEnemy = false;
                }
                if (!IPlayer.HealthController.IsAlive)
                {
                    addEnemy = false;
                }
            }

            if (addEnemy)
            {
                string id = IPlayer.ProfileId;

                // Check if the dictionary contains a previous SAINEnemy
                if (Enemies.ContainsKey(id))
                {
                    ActiveEnemy = Enemies[id];
                }
                else
                {
                    SAINPersonClass enemySAINPerson = GetSAINPerson(IPlayer);
                    ActiveEnemy = new SAINEnemyClass(SAIN, enemySAINPerson);
                    Enemies.Add(id, ActiveEnemy);
                }
            }
            else
            {
                ActiveEnemy = null;
            }
        }

        private static SAINPersonClass GetSAINPerson(IPlayer IPlayer)
        {
            SAINPersonClass enemySAINPerson = null;
            BotOwner botOwner = IPlayer.AIData.BotOwner;
            if (botOwner != null && botOwner.TryGetComponent(out SAINComponentClass enemySAIN))
            {
                //Logger.LogWarning("SAINPerson Found for AI");
                enemySAINPerson = enemySAIN.Person;
            }
            else if (IPlayer.IsYourPlayer)
            {
                Player player = Singleton<GameWorld>.Instance?.MainPlayer;

                if (player == null)
                {
                    //Logger.LogError("MainPlayer Null");
                    return new SAINPersonClass(IPlayer);
                }

                SAINMainPlayerComponent mainPlayerComponent = player.GetComponent<SAINMainPlayerComponent>();

                if (mainPlayerComponent == null)
                {
                    //Logger.LogError("mainPlayerComponent Null");
                    return new SAINPersonClass(IPlayer);
                }

                if (mainPlayerComponent.SAINPerson != null)
                {
                    //Logger.LogWarning("SAINPerson Found for MAIN PLAYER");
                    enemySAINPerson = mainPlayerComponent.SAINPerson;
                }
                else
                {
                    //Logger.LogError("SAINPerson Found for MAIN PLAYER but it is NULL");
                    enemySAINPerson = new SAINPersonClass(IPlayer);
                }
            }
            else
            {
                //Logger.LogWarning("No SAINPerson Found");
                enemySAINPerson = new SAINPersonClass(IPlayer);
            }
            return enemySAINPerson;
        }

        public readonly Dictionary<string, SAINEnemyClass> Enemies = new Dictionary<string, SAINEnemyClass>();
        private readonly List<string> EnemyIDsToRemove = new List<string>();
    }
}
