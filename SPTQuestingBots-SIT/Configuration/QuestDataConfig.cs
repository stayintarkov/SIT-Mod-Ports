using Newtonsoft.Json;

namespace SPTQuestingBots.Configuration
{
    public class QuestDataConfig
    {
        [JsonProperty("templates")]
        public Template2[] Templates { get; set; } = new Template2[0];

        [JsonProperty("quests")]
        public Models.QuestQB[] Quests { get; set; } = new Models.QuestQB[0];

        public QuestDataConfig()
        {

        }
    }
}