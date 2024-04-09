using System;
using System.Reflection;
using System.Threading.Tasks;
using StayInTarkov;
using EFT.Interactive;
using GTFO;

namespace CactusPie.MapLocation.Patches
{
    public class TryNotifyConditionChangedPatch : ModulePatch
    {
        internal static TriggerWithId[] triggers;
        protected override MethodBase GetTargetMethod()
        {
            foreach (Type type in typeof(EFT.AbstractGame).Assembly.GetTypes())
            {
                MethodInfo method = type.GetMethod("TryNotifyConditionChanged", BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null && type.BaseType == typeof(GClass3205))
                {
                    return method;
                }
            }

            Logger.LogError("Could not find TryNotifyConditionChanged method");

            return null;
        }

        [PatchPostfix]
        public static void PatchPostfix(Quest quest)
        {
            try
            {
                triggers = ZoneDataHelper.GetAllTriggers();

                Task.Run(
                    () =>
                    {
                        try
                        {
                            GTFOComponent.questManager.OnQuestsChanged(triggers);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
                        }
                    });
            }
            catch (Exception e)
            {
                Logger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }
    }
}