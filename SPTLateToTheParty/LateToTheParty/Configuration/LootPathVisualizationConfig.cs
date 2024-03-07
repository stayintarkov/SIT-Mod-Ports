using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class LootPathVisualizationConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("points_per_circle")]
        public int PointsPerCircle { get; set; } = 10;

        [JsonProperty("outline_loot")]
        public bool OutlineLoot { get; set; } = true;

        [JsonProperty("loot_outline_radius")]
        public float LootOutlineRadius { get; set; } = 0.1f;

        [JsonProperty("only_outline_loot_with_pathing")]
        public bool OnlyOutlineLootWithPathing { get; set; } = false;

        [JsonProperty("draw_incomplete_paths")]
        public bool DrawIncompletePaths { get; set; } = true;

        [JsonProperty("draw_complete_paths")]
        public bool DrawCompletePaths { get; set; } = true;

        [JsonProperty("outline_obstacles")]
        public bool OutlineObstacles { get; set; } = true;

        [JsonProperty("only_outline_filtered_obstacles")]
        public bool OnlyOutlineFilteredObstacles { get; set; } = false;

        [JsonProperty("show_obstacle_collision_points")]
        public bool ShowObstacleCollisionPoints { get; set; } = true;

        [JsonProperty("collision_point_radius")]
        public float CollisionPointRadius { get; set; } = 0.05f;

        [JsonProperty("show_door_obstacles")]
        public bool ShowDoorObstacles { get; set; } = true;

        [JsonProperty("door_obstacle_min_radius")]
        public float DoorObstacleMinRadius { get; set; } = 0.05f;

        public LootPathVisualizationConfig()
        {

        }
    }
}
