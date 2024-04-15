using EFT;
using System;
using System.Collections.Generic;
using System.Reflection;
using VCQLQuestZones.Core;
using System.Linq;
using StayInTarkov;

namespace VCQLQuestZones.Patches
{
    internal class GameWorldPatch: ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            try
            {
                string current_map = __instance.MainPlayer.Location;
                List<Zone> questZones = QuestZones.GetZones();
                List<Zone> validZones = questZones.Where(zone => zone.ZoneLocation.ToLower() == current_map.ToLower()).ToList();
                Plugin.ExistingQuestZones = validZones;
                QuestZones.CreateZones(questZones, current_map);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}
