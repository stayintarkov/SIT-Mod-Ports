using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class LoggingPath
    {
        [JsonProperty("path")]
        public string Path { get; set; } = "";

        public LoggingPath()
        {

        }
    }
}
