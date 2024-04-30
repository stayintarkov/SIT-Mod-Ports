using Newtonsoft.Json;
using SAIN.Attributes;
using System.ComponentModel;
using Description = SAIN.Attributes.DescriptionAttribute;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINAimingSettings
    {
        [Name("Distance Aim Time Multiplier")]
        [Description("Multiplies the time a bot takes to aim based on distance. So higher values will cause bots to take longer to aim depending on distance.")]
        [Default(1f)]
        [MinMax(0.1f, 5f, 100f)]
        public float DistanceAimTimeMultiplier = 1f;

        [Name("Faster CQB Reactions")]
        [Description("Sets whether this bot reacts and aims faster before being able to shoot at close ranges")]
        [Default(true)]
        public bool FasterCQBReactions = true;

        [Name("Faster CQB Reactions Max Distance")]
        [Description("Max distance a bot can react faster for Faster CQB Reactions. Scales with distance." +
            "Example: If Max distance is set to 20 meters, and an enemy is 10 meters away. they will react 2x as fast as usual, " +
            "or if an enemy is 15 meters away, they will react 1.5x as fast as usual. " +
            "If the enemy is at 20 meters or further, nothing will happen.")]
        [NameAndDescription(
            "Faster CQB Reactions Max Distance",
            "Max distance a bot can react faster for Faster CQB Reactions. Scales with distance.")]
        [Default(30f)]
        [MinMax(5f, 100f)]
        public float FasterCQBReactionsDistance = 30f;

        [Name("Faster CQB Reactions Minimum Speed")]
        [Description("Absolute minimum speed (in seconds) that bot can react and shoot")]
        [Default(0.33f)]
        [MinMax(0.05f, 0.75f, 100f)]
        public float FasterCQBReactionsMinimum = 0.33f;

        [Name("Accuracy Spread Multiplier")]
        [Description("Higher = less accurate. Modifies a bot's base accuracy and spread. 1.5 = 1.5x higher accuracy spread")]
        [Default(1f)]
        [MinMax(0.1f, 10f, 10f)]
        public float AccuracySpreadMulti = 1f;

        [NameAndDescription("Aiming Upgrade By Time")]
        [Default(0.8f)]
        [MinMax(0.1f, 0.95f, 100f)]
        [Advanced]
        [CopyValue]
        public float MAX_AIMING_UPGRADE_BY_TIME = 0.5f;

        [Default(2f)]
        [MinMax(0.1f, 6f, 100f)]
        [Advanced]
        public float COEF_IF_MOVE = 1.5f;

        [Hidden]
        [JsonIgnore]
        public float TIME_COEF_IF_MOVE = 1.5f;

        [Name("Max Aim Time")]
        [Description(null)]
        [Default(1f)]
        [MinMax(0.01f, 4f, 1000f)]
        [Advanced]
        [CopyValue]
        public float MAX_AIM_TIME = 1f;

        [Hidden]
        [JsonIgnore]
        public int AIMING_TYPE = 4;

        [Name("Friendly Fire Spherecast Size")]
        [Description("")]
        [Default(0.15f)]
        [MinMax(0f, 0.5f, 100f)]
        [Advanced]
        public float SHPERE_FRIENDY_FIRE_SIZE = 0.15f;

        [Hidden]
        public int RECALC_MUST_TIME = 1;

        [Hidden]
        public int RECALC_MUST_TIME_MIN = 1;

        [Hidden]
        public int RECALC_MUST_TIME_MAX = 2;

        [Hidden]
        public float DAMAGE_TO_DISCARD_AIM_0_100 = 100;

        [NameAndDescription(
            "Hit Reaction Recovery Time",
            "How much time it takes to recover a bot's aim when they get hit by a bullet")]
        [Default(0.5f)]
        [MinMax(0.1f, 0.99f, 100f)]
        [Advanced]
        public float BASE_HIT_AFFECTION_DELAY_SEC = 0.77f;

        [NameAndDescription(
            "Minimum Hit Reaction Angle",
            "How much to kick a bot's aim when they get hit by a bullet")]
        [Default(3f)]
        [MinMax(0f, 25f, 10f)]
        [Advanced]
        public float BASE_HIT_AFFECTION_MIN_ANG = 3f;

        [NameAndDescription(
            "Maximum Hit Reaction Angle",
            "How much to kick a bot's aim when they get hit by a bullet")]
        [Default(5f)]
        [MinMax(0f, 25f, 10f)]
        [Advanced]
        public float BASE_HIT_AFFECTION_MAX_ANG = 5f;

        [Hidden]
        [JsonIgnore]
        public float FIRST_CONTACT_ADD_SEC = 0.1f;

        [Hidden]
        [JsonIgnore]
        public float FIRST_CONTACT_ADD_CHANCE_100 = 100f;
    }
}