using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LookRankingDataReader.Models
{
    internal class LootRankingForParentConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("weighting")]
        public double Weighting { get; set; }

        public LootRankingForParentConfig()
        {

        }
    }
}
