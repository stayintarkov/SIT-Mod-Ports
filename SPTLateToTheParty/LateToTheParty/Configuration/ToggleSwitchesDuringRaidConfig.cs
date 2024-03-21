using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class ToggleSwitchesDuringRaidConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("time_between_events_ms")]
        public float TimeBetweenEvents { get; set; } = 5000;

        [JsonProperty("exclusion_radius")]
        public double ExclusionRadius { get; set; } = 150;

        [JsonProperty("min_raid_ET_for_exfil_switches")]
        public float MinRaidETForExfilSwitches { get; set; } = 50;

        [JsonProperty("delay_after_pressing_prereq_switch_s_per_m")]
        public float DelayAfterPressingPrereqSwitch { get; set; } = 1;

        [JsonProperty("raid_fraction_when_toggling")]
        public MinMaxConfig RaidFractionWhenToggling { get; set; } = new MinMaxConfig();

        [JsonProperty("fraction_of_switches_to_toggle")]
        public MinMaxConfig FractionOfSwitchesToToggle { get; set; } = new MinMaxConfig();

        [JsonProperty("max_calc_time_per_frame_ms")]
        public int MaxCalcTimePerFrame { get; set; } = 5;

        public ToggleSwitchesDuringRaidConfig()
        {

        }
    }
}
