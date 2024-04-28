using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;

namespace TechHappy.MinimapSender.Patches
{
    /// <summary>
    /// This class is responsible for patching the OnGameStarted method in the GameWorld class.
    /// It adds the MinimapSenderController component to the GameWorld gameObject after the original method is executed.
    /// </summary>
    public class OnGameStartedPatch : ModulePatch
    {
        /// <summary>
        /// Returns the target method to be patched, which is the "OnGameStarted" method in the GameWorld class.
        /// </summary>
        /// <returns>The target method to be patched.</returns>
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Attaches a new MinimapSenderController component to the GameWorld instance after clearing the airdrops
        /// and incrementing the raid counter.
        /// </summary>
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