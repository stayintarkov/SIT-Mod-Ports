using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class IgnoreItemsOnDeadBotsConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("only_if_you_killed_them")]
        public bool OnlyIfYouKilledThem { get; set; } = true;

        public IgnoreItemsOnDeadBotsConfig()
        {

        }
    }
}
