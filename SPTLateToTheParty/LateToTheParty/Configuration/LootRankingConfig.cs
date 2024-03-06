using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class LootRankingConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("randomness")]
        public double Randomness { get; set; } = 0;

        [JsonProperty("top_value_retain_count")]
        public int TopValueRetainCount { get; set; } = 0;

        [JsonProperty("alwaysRegenerate")]
        public bool AlwaysRegenerate { get; set; } = true;

        [JsonProperty("child_item_limits")]
        public LootRankingChildItemLimitsConfig ChildItemLimits { get; set; } = new LootRankingChildItemLimitsConfig();

        [JsonProperty("weighting")]
        public LootRankingWeightingConfig Weighting { get; set; } = new LootRankingWeightingConfig();

        public LootRankingConfig()
        {

        }
    }
}
