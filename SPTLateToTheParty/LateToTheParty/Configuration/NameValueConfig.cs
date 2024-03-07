using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class NameValueConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("value")]
        public double Value { get; set; }

        public NameValueConfig()
        {

        }
    }
}
