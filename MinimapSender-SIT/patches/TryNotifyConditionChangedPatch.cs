using System;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.Interactive;

namespace TechHappy.MinimapSender.Patches
{
    /// <summary>
    /// Represents a patch that is responsible for handling the notification of quest condition changes.
    /// </summary>
    public class TryNotifyConditionChangedPatch : ModulePatch
    {
        /// <summary>
        /// Retrieves the TryNotifyConditionChanged method of the QuestControllerClass for patching.
        /// </summary>
        /// <returns>The target method to be patched.</returns>
        protected override MethodBase GetTargetMethod()
        {
            foreach (var type in typeof(EFT.AbstractGame).Assembly.GetTypes())
            {
                var method = type.GetMethod("TryNotifyConditionChanged", BindingFlags.Public | BindingFlags.Instance);

                //if (method == null || method.IsAbstract || method.IsVirtual) continue;
                
                if (method != null && method.GetParameters()[0].Name == "quest")
                {
                    return method;
                }
                
                // if (
                //   type.GetMethod("FailConditional", BindingFlags.Public | BindingFlags.Instance) != null)
                // {
                //     return type.GetMethod("TryNotifyConditionChanged", BindingFlags.Public| BindingFlags.Instance);
                // }
            }

            MinimapSenderPlugin.MinimapSenderLogger.LogError($"Unable to find class derived from QuestControllerClass with method TryNotifyConditionChanged");

            return null;
        }

        /// <summary>
        /// Represents a method for patching the TryNotifyConditionChanged method of the QuestControllerClass.
        /// The patch calls the MinimapSenderController's UpdateQuestData method when a quest condition is changed.
        /// </summary>
        [PatchPostfix]
        public static void PatchPostfix()
        {
            try
            {
                TriggerWithId[] triggers = ZoneDataHelper.GetAllTriggers();
                
                if (MinimapSenderController.Instance != null)
                {
                    MinimapSenderController.Instance.UpdateQuestData(triggers);
                }
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }
    }
}
