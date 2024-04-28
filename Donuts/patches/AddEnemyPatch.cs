using Aki.Reflection.Patching;
using EFT;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Stolen from DanW's Questing Bots, thanks Dan!

namespace Donuts
{
    public class AddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsGroup).GetMethod("AddEnemy", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotsGroup __instance, IPlayer person, EBotEnemyCause cause)
        {
            // We only care about bot groups adding you as an enemy
            if (!person.IsYourPlayer)
            {
                return true;
            }

            // This only matters in Scav raids
            // TO DO: This might also matter in PMC raids if a mod adds groups that are friendly to the player
            /*if (!Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                return true;
            }*/

            // We only care about one enemy cause
            if (cause != EBotEnemyCause.pmcBossKill)
            {
                return true;
            }

            // Get the ID's of all group members
            List<BotOwner> groupMemberList = new List<BotOwner>();
            for (int m = 0; m < __instance.MembersCount; m++)
            {
                groupMemberList.Add(__instance.Member(m));
            }
            string[] groupMemberIDs = groupMemberList.Select(m => m.Profile.Id).ToArray();

            return true;
        }
    }
}
