using Aki.Reflection.Patching;
using EFT;
using SkillsExtended.Helpers;
using System.Reflection;

namespace SkillsExtended.Patches
{
    internal class LocationSceneAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => typeof(LocationScene).GetMethod(nameof(LocationScene.Awake));

        [PatchPostfix]
        private static void Postfix(LocationScene __instance)
        {
            foreach (var interactableObj in __instance.WorldInteractiveObjects)
            {
                if (interactableObj.KeyId != null && interactableObj.KeyId != string.Empty)
                {
                    if (Plugin.Keys.KeyLocale.ContainsKey(interactableObj.KeyId))
                    {
                        Plugin.Log.LogDebug($"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId} Key Name: {Plugin.Keys.KeyLocale[interactableObj.KeyId]}");
                    }
                    else
                    {
                        Plugin.Log.LogError($"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId} Key locale missing...");
                    }
                }
            }
        }
    }

    internal class OnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
#if DEBUG
            Plugin.Log.LogDebug($"Player map id: {__instance.MainPlayer.Location}");
#endif
            LockPickingHelpers.InspectedDoors.Clear();
        }
    }
}