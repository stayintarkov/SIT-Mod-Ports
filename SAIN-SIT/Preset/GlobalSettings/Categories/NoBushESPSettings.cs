using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class NoBushESPSettings
    {
        [Name("No Bush ESP")]
        [Description("Adds extra vision check for bots to help prevent bots seeing or shooting through foliage.")]
        [Default(true)]
        public bool NoBushESPToggle = true;

        [Name("No Bush ESP Enhanced Raycasts")]
        [Description("Experimental: Increased Accuracy and extra checks")]
        [Default(false)]
        public bool NoBushESPEnhanced = false;

        [Name("No Bush ESP Enhanced Raycast Frequency p/ Second")]
        [Description("Experimental: How often to check for foliage vision blocks")]
        [Default(0.1f)]
        [MinMax(0f, 1f, 100f)]
        [Advanced]
        public float NoBushESPFrequency = 0.1f;

        [Name("No Bush ESP Enhanced Raycasts Ratio")]
        [Description("Experimental: Increased Accuracy and extra checks. " +
            "Sets the ratio of visible to not visible body parts to not block vision. " +
            "0.5 means half the body parts of the player must be visible to not block vision.")]
        [Default(0.5f)]
        [MinMax(0.2f, 1f, 10f)]
        [Advanced]
        public float NoBushESPEnhancedRatio = 0.5f;

        [Name("No Bush ESP Debug")]
        [Default(false)]
        [Advanced]
        public bool NoBushESPDebugMode = false;
    }
}