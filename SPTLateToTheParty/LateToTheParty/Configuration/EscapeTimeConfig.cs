using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class EscapeTimeConfig
    {
        [JsonProperty("modification_chance")]
        public double Chance { get; set; } = 0;

        [JsonProperty("min_time_remaining")]
        public double TimeFactorMin { get; set; } = 1;

        [JsonProperty("max_time_remaining")]
        public double TimeFactorMax { get; set; } = 1;

        public EscapeTimeConfig()
        {

        }
    }
}
