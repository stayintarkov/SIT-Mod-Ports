using StayInTarkov;
using System.Reflection;
//using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using StayInTarkov.Coop;
using System.Threading.Tasks;

namespace TechHappy.MinimapSender
{

    public class MinimapSenderPatchCoop : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(StayInTarkovPlugin).Assembly.GetType("StayInTarkov.Coop.CoopGame").GetMethod("vmethod_2", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void WaitForCoopGame(Task<LocalPlayer> task)
        {
            task.Wait();

            LocalPlayer localPlayer = task.Result;

            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                GameWorld gameWorld = Singleton<GameWorld>.Instance;

                if (gameWorld == null)
                {
                    return;
                }

                MinimapSenderPlugin.raidCounter++;
                MinimapSenderPlugin.airdrops.Clear();

                gameWorld.gameObject.AddComponent<MinimapSenderController>();
            }
        }

        [PatchPostfix]
        private static void PatchPostFix(Task<LocalPlayer> __result)
        {
            Task.Run(() => WaitForCoopGame(__result));
        }
    }

    public class MinimapSenderPatchLocal : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalPlayer).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostFix(Task<LocalPlayer> __result)
        {
            LocalPlayer localPlayer = __result.Result;

            if (localPlayer != null && localPlayer.IsYourPlayer)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;

            if (gameWorld == null)
            {
                return;
            }

            MinimapSenderPlugin.raidCounter++;
            MinimapSenderPlugin.airdrops.Clear();

            gameWorld.gameObject.AddComponent<MinimapSenderController>();
        }
    }
}
}