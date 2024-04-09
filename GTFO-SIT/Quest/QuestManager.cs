using System.Collections.Generic;
using Comfort.Common;
using EFT.Interactive;
using EFT.UI;
using GTFO;
using StayInTarkov;

namespace GTFO
{
    internal class QuestManager
    {

        internal QuestDataService questDataService;
        public void Initialize()
        {
            questDataService = new QuestDataService(GTFOComponent.gameWorld, GTFOComponent.player);
            SetupInitialQuests();
        }
        internal void SetupInitialQuests()
        {
            if (!StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                GTFOComponent.Logger.LogInfo("Calling Reload Quest Data from SetupInitial Quests");
                questDataService.ReloadQuestData(ZoneDataHelper.GetAllTriggers());
            }
            else
            {
                GTFOComponent.Logger.LogInfo("Not calling Setup Quests as its a SCAV Raid.");
            }
        }
        internal void OnQuestsChanged(TriggerWithId[] allTriggers)
        {
            if (!StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                questDataService.ReloadQuestData(allTriggers);
            }
        }
    }
}
