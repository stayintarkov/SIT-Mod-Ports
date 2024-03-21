using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class FenceStockChangesConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("always_regenerate")]
        public bool AlwaysRegenerate { get; set; } = false;

        [JsonProperty("assort_size")]
        public int AssortSize { get; set; } = 190;

        [JsonProperty("assort_size_discount")]
        public int AssortSizeDiscount { get; set; } = 90;

        [JsonProperty("assort_restock_threshold")]
        public double AssortRestockThreshold { get; set; } = 70;

        [JsonProperty("maxPresetsPercent")]
        public double MaxPresetsPercent { get; set; } = 25;

        [JsonProperty("max_preset_cost")]
        public double MaxPresetCost { get; set; } = 150000;

        [JsonProperty("min_allowed_item_value")]
        public double MinAllowedItemValue { get; set; } = 20000;

        [JsonProperty("max_ammo_stack")]
        public double MaxAmmoStack { get; set; } = 5000;

        [JsonProperty("sell_chance_multiplier")]
        public double SellChanceMultiplier { get; set; } = 5;

        [JsonProperty("itemTypeLimits_Override")]
        public Dictionary<string, int> ItemTypeLimitsOverride { get; set; } = new Dictionary<string, int>();

        [JsonProperty("blacklist_append")]
        public string[] BlacklistAppend { get; set; } = new string[0];

        [JsonProperty("blacklist_remove")]
        public string[] BlacklistRemove { get; set; } = new string[0];

        [JsonProperty("blacklist_ammo_penetration_limit")]
        public int BlacklistAmmoPenetrationLimit { get; set; } = 31;

        [JsonProperty("blacklist_ammo_damage_limit")]
        public int BlacklistAmmoDamageLimit { get; set; } = 85;

        public FenceStockChangesConfig()
        {

        }
    }
}
