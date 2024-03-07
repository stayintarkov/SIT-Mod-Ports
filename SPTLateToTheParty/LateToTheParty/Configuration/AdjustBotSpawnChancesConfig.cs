using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class AdjustBotSpawnChancesConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("adjust_bosses")]
        public bool AdjustBosses { get; set; } = true;

        [JsonProperty("adjust_pmc_conversion_chances")]
        public bool AdjustPMCConversionChances { get; set; } = false;

        [JsonProperty("pmc_conversion_update_rate")]
        public double PMCConversionUpdateRate { get; set; } = 10;

        [JsonProperty("excluded_bosses")]
        public string[] ExcludedBosses { get; set; } = new string[0];

        public AdjustBotSpawnChancesConfig()
        {

        }
    }
}
