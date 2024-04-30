using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using System.Reflection;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINCoreSettings
    {
        [Name("Horizontal Field of View")]
        [Default(160f)]
        [MinMax(45f, 180f)]
        public float VisibleAngle = 160f;

        [Name("Max Vision Distance")]
        [Default(150f)]
        [MinMax(50f, 500f)]
        [CopyValue]
        public float VisibleDistance = 150f;

        [Name("Gain Sight Coeficient")]
        [Description("Default EFT Config. Affects how quickly this bot will notice their enemies. Small changes to this have dramatic affects on bot vision speed.")]
        [Default(0.2f)]
        [MinMax(0.01f, 0.95f, 1000f)]
        [Advanced]
        [CopyValue]
        public float GainSightCoef = 0.2f;

        [Description("Default EFT Config. I do not know what this does exactly.")]
        [Default(0.08f)]
        [MinMax(0.001f, 1f, 1000f)]
        [Advanced]
        [CopyValue]
        public float ScatteringPerMeter = 0.08f;

        [Description("Default EFT Config. I do not know what this does exactly.")]
        [Default(0.12f)]
        [MinMax(0.001f, 1f, 1000f)]
        [Advanced]
        [CopyValue]
        public float ScatteringClosePerMeter = 0.12f;

        [Name("Hearing Sense Multiplier")]
        [Description("Modifies the distance that this bot can hear sounds")]
        [Default(1f)]
        [MinMax(0.1f, 3f, 1000f)]
        public float HearingSense = 1f;

        [Name("Can Use Grenades")]
        [Default(true)]
        public bool CanGrenade = true;

        [Hidden]
        [JsonIgnore]
        public bool CanRun = true;

        [Hidden]
        [JsonIgnore]
        public float DamageCoeff = 1f;
    }
}