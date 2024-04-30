using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using System.ComponentModel;
using System.Reflection;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINHearingSettings
    {
        [NameAndDescription(
            "Max Footstep Audio Distance",
            "The Maximum Range that a bot can hear footsteps, sprinting, and jumping, in meters.")]
        [Default(71f)]
        [MinMax(10f, 150f, 1f)]
        public float MaxFootstepAudioDistance = 71f;
    }
}