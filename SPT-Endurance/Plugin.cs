using BepInEx;
using BepInEx.Configuration;

namespace Endurance
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> enduranceMulti { get; set; }

        private void Awake()
        {
            string EnduranceSettings = "Endurance";
            enduranceMulti = Config.Bind<float>(EnduranceSettings, "Endurance Multi", 0.5f, new ConfigDescription("Requires Restart. Multiplier for how much Endurance XP is gained when overweight. 0 means no Endurance XP gain.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 1 }));

            new EnduranceSprintActionPatch().Enable();
            new EnduranceMovementActionPatch().Enable();
        }
    }
}
