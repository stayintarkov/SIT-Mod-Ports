using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LateToTheParty.Configuration
{
    public class ModConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("debug")]
        public DebugConfig Debug { get; set; } = new DebugConfig();

        [JsonProperty("scav_raid_adjustments")]
        public ScavRaidAdjustmentsConfig ScavRaidAdjustments { get; set; } = new ScavRaidAdjustmentsConfig();

        [JsonProperty("car_extract_departures")]
        public CarExtractDeparturesConfig CarExtractDepartures { get; set; } = new CarExtractDeparturesConfig();

        [JsonProperty("adjust_bot_spawn_chances")]
        public AdjustBotSpawnChancesConfig AdjustBotSpawnChances { get; set; } = new AdjustBotSpawnChancesConfig();

        [JsonProperty("only_make_changes_just_after_spawning")]
        public OnlyMakeChangesJustAfterSpawningConfig OnlyMakeChangesJustAfterSpawning { get; set; } = new OnlyMakeChangesJustAfterSpawningConfig();

        [JsonProperty("destroy_loot_during_raid")]
        public DestroyLootDuringRaidConfig DestroyLootDuringRaid { get; set; } = new DestroyLootDuringRaidConfig();

        [JsonProperty("open_doors_during_raid")]
        public OpenDoorsDuringRaidConfig OpenDoorsDuringRaid { get; set; } = new OpenDoorsDuringRaidConfig();

        [JsonProperty("toggle_switches_during_raid")]
        public ToggleSwitchesDuringRaidConfig ToggleSwitchesDuringRaid { get; set; } = new ToggleSwitchesDuringRaidConfig();

        [JsonProperty("trader_stock_changes")]
        public TraderStockChangesConfig TraderStockChanges { get; set; } = new TraderStockChangesConfig();

        [JsonProperty("loot_multipliers")]
        public double[][] LootMultipliers { get; set; } = new double[0][];

        [JsonProperty("fraction_of_players_full_of_loot")]
        public double[][] FractionOfPlayersFullOfLoot { get; set; } = new double[0][];

        [JsonProperty("pmc_spawn_chance_multipliers")]
        public double[][] PMCSpawnChanceMultipliers { get; set; } = new double[0][];

        [JsonProperty("boss_spawn_chance_multipliers")]
        public double[][] BossSpawnChanceMultipliers { get; set; } = new double[0][];

        [JsonProperty("fence_item_value_permitted_chance")]
        public double[][] FenceItemValuePermittedChance { get; set; } = new double[0][];

        public ModConfig()
        {

        }
    }
}
