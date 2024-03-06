using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class OnlyMakeChangesAfterSpawningAffectedSystemsConfig
    {
        [JsonProperty("loot_destruction")]
        public bool LootDestruction { get; set; } = true;

        [JsonProperty("opening_unlocked_doors")]
        public bool OpeningUnlockedDoors { get; set; } = true;

        [JsonProperty("opening_locked_doors")]
        public bool OpeningLockedDoors { get; set; } = true;

        [JsonProperty("closing_doors")]
        public bool ClosingDoors { get; set; } = true;

        [JsonProperty("car_departures")]
        public bool CarDepartures { get; set; } = true;

        [JsonProperty("toggling_switches")]
        public bool TogglingSwitches { get; set; } = true;

        public OnlyMakeChangesAfterSpawningAffectedSystemsConfig()
        {

        }
    }
}
