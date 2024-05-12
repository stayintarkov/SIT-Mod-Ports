using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Gaylatea
{
    namespace UseLooseLoot
    {
        [BepInPlugin("com.gaylatea.uselooseloot", "SPT-UseLooseLoot", "1.1.2")]
        public class Plugin : BaseUnityPlugin
        {
            internal static ManualLogSource logger;

            public Plugin()
            {
                logger = Logger;

                new MakeFoodMedsUsablePatch().Enable();

                Logger.LogInfo($"[UseLooseLoot] Loaded.");
            }
        }
    }
}