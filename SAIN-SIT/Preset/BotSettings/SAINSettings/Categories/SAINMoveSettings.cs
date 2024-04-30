using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINMoveSettings
    {
        [Hidden]
        [JsonIgnore]
        public float BASE_ROTATE_SPEED = 350;

        [Hidden]
        [JsonIgnore]
        public float FIRST_TURN_SPEED = 500;
        // 160 default

        [Hidden]
        [JsonIgnore]
        public float FIRST_TURN_BIG_SPEED = 500;
        // 320 default

        [Hidden]
        [JsonIgnore]
        public float TURN_SPEED_ON_SPRINT = 350;
        // 200 default

        [Hidden]
        [JsonIgnore]
        public float RUN_TO_COVER_MIN = 0f;

        // [Hidden]
        // [JsonIgnore]
        // public float BOT_MOVE_IF_DELTA = 0.025f;

        //[Hidden]
        //[JsonIgnore]
        //public float REACH_DIST = 0.4f;
        //
        //[Hidden]
        //[JsonIgnore]
        //public float REACH_DIST_RUN = 0.8f;

        [Hidden]
        [JsonIgnore]
        public float BASESTART_SLOW_DIST = 0.5f;

        [Hidden]
        [JsonIgnore]
        public float START_SLOW_DIST = 0.75f;

        [Hidden]
        [JsonIgnore]
        public float SLOW_COEF = 12;
    }
}