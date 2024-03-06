using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class LootRankingWeightingConfig
    {
        [JsonProperty("default_inventory_id")]
        public string DefaultInventoryId { get; set; } = "";

        [JsonProperty("cost_per_slot")]
        public double CostPerSlot { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("size")]
        public double Size { get; set; }

        [JsonProperty("gridSize")]
        public double GridSize { get; set; }

        [JsonProperty("max_dim")]
        public double MaxDim { get; set; }

        [JsonProperty("armor_class")]
        public double ArmorClass { get; set; }

        [JsonProperty("parents")]
        public Dictionary<string, NameValueConfig> Parents { get; set; } = new Dictionary<string, NameValueConfig>();

        [JsonProperty("items")]
        public Dictionary<string, LootRankingItemDataConfig> Items { get; set; } = new Dictionary<string, LootRankingItemDataConfig>();

        public LootRankingWeightingConfig()
        {

        }
    }
}
