using EFT;

using static Donuts.DonutComponent;

namespace Donuts
{
    // Custom GetGroupAndSetEnemies wrapper that handles grouping bots into multiple groups within the same botzone
    internal class GetGroupWrapper
    {
        private BotsGroup group = null;

        public BotsGroup GetGroupAndSetEnemies(BotOwner bot, BotZone zone)
        {
            // If we haven't found/created our BotsGroup yet, do so, and then lock it so nobody else can use it
            if (group == null)
            {
                group = botSpawnerClass.GetGroupAndSetEnemies(bot, zone);
                group.Lock();
            }

            return group;
        }
    }


}
