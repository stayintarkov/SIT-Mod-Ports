using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class IgnoreItemsDroppedByPlayerConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("only_items_brought_into_raid")]
        public bool OnlyItemsBroughtIntoRaid { get; set; } = false;

        public IgnoreItemsDroppedByPlayerConfig()
        {

        }
    }
}
