using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using StayInTarkov;

namespace dvize.Donuts
{
    internal class PatchStandbyTeleport : ModulePatch
    {
        private static MethodInfo _method1;

        protected override MethodBase GetTargetMethod()
        {
            Type standbyClassType = typeof(BotStandBy);
            _method1 = AccessTools.Method(standbyClassType, "method_1");

            return AccessTools.Method(typeof(BotStandBy), nameof(BotStandBy.UpdateNode));
        }

        [PatchPrefix]
        public static bool Prefix(BotStandBy __instance, BotStandByType ___standByType, BotOwner ___botOwner_0)
        {
            if (!___botOwner_0.Settings.FileSettings.Mind.CAN_STAND_BY)
            {
                return false;
            }

            if (!__instance.CanDoStandBy)
            {
                return false;
            }

            if (___standByType == BotStandByType.goToSave)
            {
                _method1.Invoke(__instance, new object[] { });
            }

            return false;
        }
    }
}
