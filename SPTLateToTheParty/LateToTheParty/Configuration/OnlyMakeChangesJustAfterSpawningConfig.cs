using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class OnlyMakeChangesJustAfterSpawningConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("time_limit")]
        public double TimeLimit { get; set; } = 30;

        [JsonProperty("affected_systems")]
        public OnlyMakeChangesAfterSpawningAffectedSystemsConfig AffectedSystems { get; set; } = new OnlyMakeChangesAfterSpawningAffectedSystemsConfig();

        public OnlyMakeChangesJustAfterSpawningConfig()
        {

        }
    }
}
