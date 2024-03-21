using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTTPConfigEditor.Configuration
{
    public class ConfigSettingsConfig
    {
        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("unit")]
        public string Unit { get; set; } = "";

        [JsonProperty("max")]
        public double Max { get; set; } = double.MaxValue;

        [JsonProperty("min")]
        public double Min { get; set; } = double.MinValue;

        [JsonProperty("decimal_places")]
        public byte DecimalPlaces { get; set; } = 16;

        [JsonProperty("default")]
        public double Default { get; set; } = double.NaN;

        public ConfigSettingsConfig()
        {

        }
    }
}
