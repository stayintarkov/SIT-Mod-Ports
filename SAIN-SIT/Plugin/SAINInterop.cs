using BepInEx.Bootstrap;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;

namespace SAIN.Plugin
{
    internal static class SAINInterop
    {
        private static bool _SAINLoadedChecked = false;
        private static bool _SAINInteropInited = false;

        private static bool _IsSAINLoaded;
        private static Type _SAINExternalType;

        private static MethodInfo _ExtractBotMethod;
        private static MethodInfo _SetExfilForBotMethod;
        private static MethodInfo _ResetDecisionsForBotMethod;
        private static MethodInfo _IsPathTowardEnemyMethod;
        private static MethodInfo _TimeSinceSenseEnemyMethod;

        /**
         * Return true if SAIN is loaded in the client
         */
        public static bool IsSAINLoaded()
        {
            // Only check for SAIN once
            if (!_SAINLoadedChecked)
            {
                _SAINLoadedChecked = true;
                _IsSAINLoaded = Chainloader.PluginInfos.ContainsKey("me.sol.sain");
            }

            return _IsSAINLoaded;
        }

        /**
         * Initialize the SAIN interop class data, return true on success
         */
        public static bool Init()
        {
            if (!IsSAINLoaded()) return false;

            // Only check for the External class once
            if (!_SAINInteropInited)
            {
                _SAINInteropInited = true;

                _SAINExternalType = Type.GetType("SAIN.Plugin.External, SAIN");

                // Only try to get the methods if we have the type
                if (_SAINExternalType != null)
                {
                    _ExtractBotMethod = AccessTools.Method(_SAINExternalType, "ExtractBot");
                    _SetExfilForBotMethod = AccessTools.Method(_SAINExternalType, "TrySetExfilForBot");
                    _ResetDecisionsForBotMethod = AccessTools.Method(_SAINExternalType, "ResetDecisionsForBot");
                    _IsPathTowardEnemyMethod = AccessTools.Method(_SAINExternalType, "IsPathTowardEnemy");
                    _TimeSinceSenseEnemyMethod = AccessTools.Method(_SAINExternalType, "TimeSinceSenseEnemy");
                }
            }

            // If we found the External class, at least some of the methods are (probably) available
            return (_SAINExternalType != null);
        }

        /**
         * Force a bot into the Extract layer if SAIN is loaded. Return true if the bot was set to extract.
         */
        public static bool TryExtractBot(BotOwner botOwner)
        {
            if (!Init()) return false;
            if (_ExtractBotMethod == null) return false;

            return (bool)_ExtractBotMethod.Invoke(null, new object[] { botOwner });
        }

        /**
         * Try to select an exfil point for the bot if SAIN is loaded. Return true if an exfil was assigned to the bot.
         */
        public static bool TrySetExfilForBot(BotOwner botOwner)
        {
            if (!Init()) return false;
            if (_SetExfilForBotMethod == null) return false;

            return (bool)_SetExfilForBotMethod.Invoke(null, new object[] { botOwner });
        }

        /**
         * Force a bot to reset its decisions if SAIN is loaded. Return true if successful.
         */
        public static bool TryResetDecisionsForBot(BotOwner botOwner)
        {
            if (!Init()) return false;
            if (_ResetDecisionsForBotMethod == null) return false;

            return (bool)_ResetDecisionsForBotMethod.Invoke(null, new object[] { botOwner });
        }

        /// <summary>
        /// Compare a NavMeshPath to the pre-calculated NavMeshPath that leads directly to a bot's Active Enemy.
        /// </summary>
        /// <param name="path">The Path To Test</param>
        /// <param name="botOwner">The Bot in Question</param>
        /// <param name="ratioSameOverAll">How many nodes along a path are allowed to be the same divided by the total nodes in the Path To Test. Example: 3 nodes are the same, with 10 total nodes = 0.3 ratio, so if the input value is 0.25, this will return false.</param>
        /// <param name="sqrDistCheck">How Close a node can be to be considered the same.</param>
        /// <returns>True if the path leads in the same direction as their active enemy.</returns>
        public static bool IsPathTowardEnemy(NavMeshPath path, BotOwner botOwner, float ratioSameOverAll = 0.25f, float sqrDistCheck = 0.05f)
        {
            if (!Init()) return false;
            if (_IsPathTowardEnemyMethod == null) return false;

            return (bool)_IsPathTowardEnemyMethod.Invoke(null, new object[] { path, botOwner, ratioSameOverAll, sqrDistCheck });
        }

        public static float TimeSinceSenseEnemy(BotOwner botOwner)
        {
            if (!Init()) return float.MaxValue;
            if (_TimeSinceSenseEnemyMethod == null) return float.MaxValue;

            return (float)_TimeSinceSenseEnemyMethod.Invoke(null, new object[] { botOwner });
        }
    }
}
