using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class ScavRaidAdjustmentsConfig
    {
        [JsonProperty("always_spawn_late")]
        public bool AlwaysSpawnLate { get; set; } = true;

        public ScavRaidAdjustmentsConfig()
        {

        }
    }
}
