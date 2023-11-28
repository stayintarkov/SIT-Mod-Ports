using StayInTarkov;
//using Aki.Reflection.Patching;
using System;
using System.Reflection;

namespace TechHappy.MinimapSender
{
    public class TryNotifyConditionChangedPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            foreach (var type in typeof(EFT.AbstractGame).Assembly.GetTypes())
            {
                if (
                  type.GetMethod("TryNotifyConditionChanged", BindingFlags.NonPublic | BindingFlags.Instance) != null &&
                  type.BaseType == typeof(QuestControllerClass))
                {
                    return type.GetMethod("TryNotifyConditionChanged", BindingFlags.NonPublic| BindingFlags.Instance);
                }
            }

            MinimapSenderPlugin.MinimapSenderLogger.LogError($"Unable to find class derived from QuestControllerClass with method TryNotifyConditionChanged");

            return null;
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
            try
            {
                if (MinimapSenderController.Instance != null)
                {
                    MinimapSenderController.Instance.UpdateQuestData();
                }
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }
    }
}
