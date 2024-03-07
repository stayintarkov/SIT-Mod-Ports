using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTTPConfigEditor.Configuration
{
    public class ConfigVersionConfig
    {
        [JsonProperty("min")]
        public string Min { get; set; } = "0.0.0";

        [JsonProperty("max")]
        public string Max { get; set; } = "99.99.99";

        public ConfigVersionConfig()
        {

        }
    }
}
