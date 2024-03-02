using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Custom.Airdrops;
using Comfort.Common;
using StayInTarkov.AkiSupport.Airdrops;
using StayInTarkov;

namespace SPTQuestingBots.Patches
{
    internal class AirdropLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AirdropBox).GetMethod("ReleaseAudioSource", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(AirdropBox __instance)
        {
            if (Singleton<SITAirdropsManager>.Instance.AirdropBox &&
                Singleton<SITAirdropsManager>.Instance.AirdropBox.enabled &&
                Singleton<SITAirdropsManager>.Instance.AirdropBox.gameObject) Controllers.Bots.BotQuestBuilder.AddAirdropChaserQuest(Singleton<SITAirdropsManager>.Instance.AirdropBox.transform.position);
        }
    }
}