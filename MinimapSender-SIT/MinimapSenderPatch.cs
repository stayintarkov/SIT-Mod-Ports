using StayInTarkov;
using System.Reflection;
//using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;

namespace TechHappy.MinimapSender
{
    public class MinimapSenderPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        public static void PatchPostFix()
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