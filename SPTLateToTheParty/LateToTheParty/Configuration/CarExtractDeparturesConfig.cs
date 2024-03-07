using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class CarExtractDeparturesConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("countdown_time")]
        public float CountdownTime { get; set; } = 60;

        [JsonProperty("delay_after_countdown_reset")]
        public double DelayAfterCountdownReset { get; set; } = 120;

        [JsonProperty("exclusion_radius")]
        public double ExclusionRadius { get; set; } = 150;

        [JsonProperty("exclusion_radius_hysteresis")]
        public float ExclusionRadiusHysteresis { get; set; } = 0.9f;

        [JsonProperty("chance_of_leaving")]
        public float ChanceOfLeaving { get; set; } = 50;

        [JsonProperty("raid_fraction_when_leaving")]
        public MinMaxConfig RaidFractionWhenLeaving { get; set; } = new MinMaxConfig();

        public CarExtractDeparturesConfig()
        {

        }
    }
}
