using Newtonsoft.Json;

namespace SPTQuestingBots.Configuration
{
    public class QuestDataConfig
    {
        [JsonProperty("templates")]
        public RawQuestClass[] Templates { get; set; } = new RawQuestClass[0];

        [JsonProperty("quests")]
        public Models.QuestQB[] Quests { get; set; } = new Models.QuestQB[0];

        public QuestDataConfig()
        {

        }
    }
}
