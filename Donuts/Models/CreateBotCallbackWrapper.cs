using EFT;
using System.Diagnostics;
using static Donuts.DonutComponent;
using BotCacheClass = Data1;

namespace Donuts
{
    // Wrapper around method_10 called after bot creation, so we can pass it the BotCacheClass data

    internal class CreateBotCallbackWrapper
    {
        public BotCacheClass botData;
        public Stopwatch stopWatch = new Stopwatch();

        public void CreateBotCallback(BotOwner bot)
        {
            bool shallBeGroup = botData.SpawnParams?.ShallBeGroup != null;

            // I have no idea why BSG passes a stopwatch into this call...
            stopWatch.Start();
            methodCache["method_10"].Invoke(botSpawnerClass, new object[] { bot, botData, null, shallBeGroup, stopWatch });
        }
    }
}
