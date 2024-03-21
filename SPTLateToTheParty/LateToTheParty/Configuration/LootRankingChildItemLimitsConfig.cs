using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class LootRankingChildItemLimitsConfig
    {
        [JsonProperty("count")]
        public int Count { get; set; } = 10;

        [JsonProperty("total_weight")]
        public double TotalWeight { get; set; } = 10;

        public LootRankingChildItemLimitsConfig()
        {

        }
    }
}
