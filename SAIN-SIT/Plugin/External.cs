using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace SAIN.Plugin
{
    public static class External
    {
        public static bool ExtractBot(BotOwner bot)
        {
            var component = bot.GetComponent<SAINComponentClass>();
            if (component == null)
            {
                return false;
            }

            component.Info.ForceExtract = true;

            return true;
        }

        public static bool TrySetExfilForBot(BotOwner bot)
        {
            var component = bot.GetComponent<SAINComponentClass>();
            if (component == null)
            {
                return false;
            }

            if (!Components.BotController.BotExtractManager.IsBotAllowedToExfil(component))
            {
                Logger.LogWarning($"{bot.name} is not allowed to use extracting logic.");
            }

            if (!SAINPlugin.BotController.BotExtractManager.TryFindExfilForBot(component))
            {
                return false;
            }

            return true;
        }

        private static bool DebugExternal => SAINPlugin.EditorDefaults.DebugExternal;

        public static bool ResetDecisionsForBot(BotOwner bot)
        {
            var component = bot.GetComponent<SAINComponentClass>();
            if (component == null)
            {
                return false;
            }

            // Do not do anything if the bot is currently in combat
            if (isBotInCombat(component, out ECombatReason reason))
            {
                if (DebugExternal)
                    Logger.LogInfo($"{bot.name} is currently engaging an enemy; cannot reset its decisions. Reason: [{reason}]");

                return true;
            }

            if (DebugExternal)
                Logger.LogInfo($"Forcing {bot.name} to reset its decisions...");

            PropertyInfo enemyLastSeenTimeSenseProperty = AccessTools.Property(typeof(BotSettingsClass), "EnemyLastSeenTimeSense");
            if (enemyLastSeenTimeSenseProperty == null)
            {
                Logger.LogError($"Could not reset EnemyLastSeenTimeSense for {bot.name}'s enemies");
                return false;
            }

            // Force the bot to think it has not seen any enemies in a long time
            foreach (IPlayer player in bot.BotsGroup.Enemies.Keys)
            {
                bot.BotsGroup.Enemies[player].Clear();
                enemyLastSeenTimeSenseProperty.SetValue(bot.BotsGroup.Enemies[player], 1);
            }

            // Until the bot next identifies an enemy, do not search anywhere
            component.Decision.GoalTargetDecisions.IgnorePlaceTarget = true;

            // Force the bot to "forget" what it was doing
            bot.Memory.GoalTarget.Clear();
            bot.Memory.GoalEnemy = null;
            component.EnemyController.ClearEnemy();
            component.Decision.ResetDecisions();

            return true;
        }

        public static float TimeSinceSenseEnemy(BotOwner botOwner)
        {
            var component = botOwner.GetComponent<SAINComponentClass>();
            if (component == null)
            {
                return float.MaxValue;
            }

            SAINEnemy enemy = component.Enemy;
            if (enemy == null)
            {
                return float.MaxValue;
            }

            return enemy.TimeSinceLastKnownUpdated;
        }

        public static bool IsPathTowardEnemy(NavMeshPath path, BotOwner botOwner, float ratioSameOverAll = 0.25f, float sqrDistCheck = 0.05f)
        {
            var component = botOwner.GetComponent<SAINComponentClass>();
            if (component == null)
            {
                return false;
            }

            SAINEnemy enemy = component.Enemy;
            if (enemy == null)
            {
                return false;
            }

            // Compare the corners in both paths, and check if the nodes used in each are the same.
            if (SAINBotSpaceAwareness.ArePathsDifferent(path, enemy.Path.PathToEnemy, ratioSameOverAll, sqrDistCheck))
            {
                return false;
            }

            return true;
        }

        private static bool isBotInCombat(SAINComponentClass component, out ECombatReason reason)
        {
            const float TimeSinceSeenThreshold = 10f;
            const float TimeSinceHeardThreshold = 2f;
            const float TimeSinceUnderFireThreshold = 10f;

            reason = ECombatReason.None;
            SAINEnemy enemy = component?.EnemyController?.ActiveEnemy;
            if (enemy == null)
            {
                return false;
            }
            if (enemy.IsVisible)
            {
                reason = ECombatReason.EnemyVisible;
                return true;
            }
            if (enemy.TimeSinceHeard < TimeSinceHeardThreshold)
            {
                reason = ECombatReason.EnemyHeardRecently;
                return true;
            }
            if (enemy.TimeSinceSeen < TimeSinceSeenThreshold)
            {
                reason = ECombatReason.EnemySeenRecently;
                return true;
            }
            BotMemoryClass memory = component.BotOwner.Memory;
            if (memory.IsUnderFire)
            {
                reason = ECombatReason.UnderFireNow;
                return true;
            }
            if (memory.UnderFireTime + TimeSinceUnderFireThreshold < Time.time)
            {
                reason = ECombatReason.UnderFireRecently;
                return true;
            }
            return false;
        }

        public enum ECombatReason
        {
            None = 0,
            EnemyVisible = 1,
            EnemyHeardRecently = 2,
            EnemySeenRecently = 3,
            UnderFireNow = 4,
            UnderFireRecently = 5,
        }
    }
}
