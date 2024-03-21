using LateToTheParty.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTTPConfigEditor.Configuration
{
    public class ConfigEditorConfig
    {
        [JsonProperty("supported_versions")]
        public ConfigVersionConfig SupportedVersions { get; set; } = new ConfigVersionConfig();

        [JsonProperty("settings")]
        public Dictionary<string, ConfigSettingsConfig> Settings { get; set; } = new Dictionary<string, ConfigSettingsConfig>();

        public ConfigEditorConfig()
        {

        }
    }
}
