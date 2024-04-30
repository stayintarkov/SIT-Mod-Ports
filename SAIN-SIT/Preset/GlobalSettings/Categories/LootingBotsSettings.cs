using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class LootingBotsSettings
    {
        [Name("Bot Extraction From Loot")]
        [Default(true)]
        public bool ExtractFromLoot = true;

        [Name("Min Loot Val PMC")]
        [Default(200000)]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValPMC = 200000;

        [Name("Min Loot Val SCAV")]
        [Default(125000)]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValSCAV = 125000;

        [Name("Min Loot Val Other")]
        [Default(150000)]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValOther = 150000;

        [Name("Min Loot Val Exception")]
        [Description("If a bot's loot value is greater than or equal to this, they will extract even with space available in their inventory.")]
        [Default(1000000)]
        [MinMax(1f, 5000000, 1f)]
        public float MinLootValException = 1000000;
    }
}