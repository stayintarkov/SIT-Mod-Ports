using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class DestructionEventLimitsConfig
    {
        [JsonProperty("rate")]
        public double Rate { get; set; } = 1;

        [JsonProperty("items")]
        public int Items { get; set; } = 30;

        [JsonProperty("slots")]
        public int Slots { get; set; } = 50;

        public DestructionEventLimitsConfig()
        {

        }
    }
}
