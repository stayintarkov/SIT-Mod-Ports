using System.Collections.Generic;

namespace Donuts
{
    internal class Folder
    {
        public string Name
        {
            get; set;
        }
        public int Weight
        {
            get; set;
        }
        public bool RandomSelection
        {
            get; set;
        }

        public PMCBotLimitPresets PMCBotLimitPresets
        {
            get; set;
        }

        public SCAVBotLimitPresets SCAVBotLimitPresets
        {
            get; set;
        }

        public string RandomScenarioConfig
        {
            get; set;
        }

        public List<Presets> presets
        {
            get; set;
        }

    }
}
