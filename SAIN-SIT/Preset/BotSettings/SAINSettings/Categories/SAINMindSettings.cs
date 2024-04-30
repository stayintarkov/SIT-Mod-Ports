using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public partial class SAINMindSettings
    {
        [Name("Global Aggression Multiplier")]
        [Description("How quickly bots will move to search for enemies after losing sight, and how carefully they will search. Higher number equals higher aggression.")]
        [Default(0.5f)]
        [MinMax(0.01f, 3f, 10f)]
        public float Aggression = 1f;

        [Name("Weapon Proficiency")]
        [Description("How Well this bot can fire any weapon type, affects recoil, fire-rate, and burst length. Higher number equals harder bots.")]
        [Default(0.5f)]
        [Percentage01to99]
        public float WeaponProficiency = 0.5f;

        [Name("Talk Frequency")]
        [Description("How often this bot can say voicelines.")]
        [Default(2f)]
        [MinMax(0f, 30f)]
        public float TalkFrequency = 2f;

        [Default(true)]
        public bool CanTalk = true;

        [Default(true)]
        public bool BotTaunts = true;

        [Default(true)]
        public bool SquadTalk = true;

        [Name("Squad Talk Frequency")]
        [Default(3f)]
        [MinMax(0f, 60f)]
        public float SquadMemberTalkFreq = 3f;

        [Name("Squad Leader Talk Frequency")]
        [Default(3f)]
        [MinMax(0f, 60f)]
        public float SquadLeadTalkFreq = 3f;

        [Name("Max Raid Percentage before Extract")]
        [Description("The longest possible time before this bot can decide to move to extract. Based on total raid timer and time remaining. 60 min total raid time with 6 minutes remaining would be 10 percent")]
        [Default(30f)]
        [MinMax(0f, 100f)]
        public float MaxExtractPercentage = 30f;

        [Name("Min Raid Percentage before Extract")]
        [Description("The longest possible time before this bot can decide to move to extract. Based on total raid timer and time remaining. 60 min total raid time with 6 minutes remaining would be 10 percent")]
        [Default(5f)]
        [MinMax(0f, 100f)]
        public float MinExtractPercentage = 5f;

        [Name("Enable Extracts")]
        [Default(true)]
        public bool EnableExtracts = true;

        [Hidden]
        [JsonIgnore]
        public float CHANCE_FUCK_YOU_ON_CONTACT_100 = 0f;

        [Hidden]
        [JsonIgnore]
        public float PART_PERCENT_TO_HEAL = 0.9f;

        [Hidden]
        [JsonIgnore]
        public bool SURGE_KIT_ONLY_SAFE_CONTAINER = false;

        [Hidden]
        [JsonIgnore]
        public bool CAN_USE_MEDS = true;

        [Hidden]
        [JsonIgnore]
        public bool CAN_USE_FOOD_DRINK = true;

        [Hidden]
        [JsonIgnore]
        public float MAX_AGGRO_BOT_DIST_UPPER_LIMIT = 500;

        [Hidden]
        [JsonIgnore]
        public float MAX_AGGRO_BOT_DIST = 500;

        [Hidden]
        [JsonIgnore]
        public float MAX_DIST_TO_PERSUE_AXEMAN = 300f;

        [Hidden]
        [JsonIgnore]
        public bool AMBUSH_WHEN_UNDER_FIRE = false;
    }
}