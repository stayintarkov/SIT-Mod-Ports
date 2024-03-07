using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class OpenDoorsDuringRaidConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("can_open_locked_doors")]
        public bool CanOpenLockedDoors { get; set; } = true;

        [JsonProperty("can_breach_doors")]
        public bool CanBreachDoors { get; set; } = true;

        [JsonProperty("exclusion_radius")]
        public double ExclusionRadius { get; set; } = 50;

        [JsonProperty("min_raid_ET")]
        public double MinRaidET { get; set; } = 180;

        [JsonProperty("min_raid_time_remaining")]
        public double MinRaidTimeRemaining { get; set; } = 300;

        [JsonProperty("time_between_door_events")]
        public double TimeBetweenEvents { get; set; } = 60;

        [JsonProperty("percentage_of_doors_per_event")]
        public double PercentageOfDoorsPerEvent { get; set; } = 10;

        [JsonProperty("chance_of_unlocking_doors")]
        public double ChanceOfUnlockingDoors { get; set; } = 50;

        [JsonProperty("chance_of_closing_doors")]
        public double ChanceOfClosingDoors { get; set; } = 30;

        [JsonProperty("max_calc_time_per_frame_ms")]
        public int MaxCalcTimePerFrame { get; set; } = 5;

        public OpenDoorsDuringRaidConfig()
        {

        }
    }
}
