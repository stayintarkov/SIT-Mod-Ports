﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using StayInTarkov.Coop.Matchmaker;

namespace SPTQuestingBots.Patches
{
    public class OnGameStartedPatch: ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            if (SITMatchmaking.IsServer || SITMatchmaking.IsSinglePlayer)
                __instance.GetOrAddComponent<BotLogic.HiveMind.BotHiveMindMonitor>();
        }
    }
}
