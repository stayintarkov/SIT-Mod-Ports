using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class DebugConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("scav_cooldown_time")]
        public long ScavCooldownTime { get; set; } = 1500;

        [JsonProperty("free_labs_access")]
        public bool FreeLabsAccess { get; set; } = false;

        [JsonProperty("min_level_for_flea")]
        public byte MinLevelForFlea { get; set; } = 15;

        [JsonProperty("trader_resupply_time_factor")]
        public double TraderResupplyTimeFactor { get; set; } = 1;

        [JsonProperty("loot_path_visualization")]
        public LootPathVisualizationConfig LootPathVisualization { get; set; } = new LootPathVisualizationConfig();

        public DebugConfig()
        {

        }
    }
}
