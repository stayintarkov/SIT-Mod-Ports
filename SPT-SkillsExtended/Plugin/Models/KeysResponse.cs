using System.Collections.Generic;

namespace SkillsExtended.Models
{
    public struct KeysResponse
    {
        public Dictionary<string, string> KeyLocale { get; set; }
        public Dictionary<string, string> ValueLocales { get; set; }
    }
}
