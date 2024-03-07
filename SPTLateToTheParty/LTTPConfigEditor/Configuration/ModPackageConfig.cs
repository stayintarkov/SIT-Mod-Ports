using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTTPConfigEditor.Configuration
{
    public class ModPackageConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("version")]
        public string Version { get; set; } = "";

        [JsonProperty("author")]
        public string Author { get; set; } = "";

        [JsonProperty("akiVersion")]
        public string AkiVersion { get; set; } = "";

        public ModPackageConfig()
        {

        }
    }
}
