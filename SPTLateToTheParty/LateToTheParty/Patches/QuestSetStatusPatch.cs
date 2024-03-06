using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.Quests;
using LateToTheParty.Controllers;

namespace LateToTheParty.Patches
{
    public class QuestSetStatusPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Quest).GetMethod("SetStatus", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(Quest __instance, EQuestStatus status)
        {
            // Ignore quests that already have this status
            if (__instance.QuestStatus == status)
            {
                return;
            }

            // Ignore status changes that won't result in trader assort unlocks
            if ((status != EQuestStatus.Success) && (status != EQuestStatus.Started))
            {
                return;
            }

            LoggingController.LogInfo("Quest status for " + __instance.Id + " changed from " + __instance.QuestStatus.ToString() + " to " + status.ToString());
            ConfigController.ShareQuestStatusChange(__instance.Id, status.ToString());
        }
    }
}
