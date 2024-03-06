using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class CheckLootAccessibilityConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("exclusion_radius")]
        public double ExclusionRadius { get; set; } = 25;

        [JsonProperty("max_path_search_distance")]
        public double MaxPathSearchDistance { get; set; } = 300;

        [JsonProperty("navmesh_search_max_distance_player")]
        public float NavMeshSearchMaxDistancePlayer { get; set; } = 10;

        [JsonProperty("navmesh_search_max_distance_loot")]
        public float NavMeshSearchMaxDistanceLoot { get; set; } = 2;

        [JsonProperty("navmesh_height_offset_complete")]
        public float NavMeshHeightOffsetComplete { get; set; } = 1.25f;

        [JsonProperty("navmesh_height_offset_incomplete")]
        public float NavMeshHeightOffsetIncomplete { get; set; } = 1;

        [JsonProperty("navmesh_obstacle_min_height")]
        public double NavMeshObstacleMinHeight { get; set; } = 0.9;

        [JsonProperty("navmesh_obstacle_min_volume")]
        public double NavMeshObstacleMinVolume { get; set; } = 2;

        [JsonProperty("max_calc_time_per_frame_ms")]
        public double MaxCalcTimePerFrame { get; set; } = 5;

        [JsonProperty("door_obstacle_update_time")]
        public double DoorObstacleUpdateTime { get; set; } = 2;

        public CheckLootAccessibilityConfig()
        {

        }
    }
}
