using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;

namespace Terkoiz.Freecam
{
    public class FreecamPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), "OnGameStarted");
        }

        [PatchPostfix]
        public static void PatchPostFix()
        {
            var gameworld = Singleton<GameWorld>.Instance;
            
            if (gameworld == null)
                return;

            // Add FreeCamController to GameWorld GameObject
            gameworld.gameObject.AddComponent<FreecamController>();
        }
    }
}