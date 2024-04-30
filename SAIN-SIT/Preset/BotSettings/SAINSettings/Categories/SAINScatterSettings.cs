using SAIN.Attributes;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINScatterSettings
    {
        [Name("EFT Scatter Multiplier")]
        [Description("Higher = more scattering. Modifies EFT's default scatter feature. 1.5 = 1.5x more scatter")]
        [Default(1f)]
        [MinMax(0.1f, 10f, 100f)]
        public float ScatterMultiplier = 1f;

        [Name("Arm Injury Recoil Multiplier")]
        [Description("Decreases Recoil control if arms are injured")]
        [Default(1.5f)]
        [MinMax(1f, 3f, 100f)]
        [Advanced]
        public float HandDamageRecoilMulti = 1.35f;

        [Name("Arm Injury Scatter Multiplier")]
        [Description("Increase scatter when a bots arms are injured.")]
        [Default(1.5f)]
        [MinMax(1f, 5f, 100f)]
        [Advanced]
        public float HandDamageScatteringMinMax = 1.35f;

        [Name("Arm Injury Aim Speed Multiplier")]
        [Description("Increase scatter when a bots arms are injured.")]
        [Default(1.5f)]
        [MinMax(1f, 5f, 100f)]
        [Advanced]
        public float HandDamageAccuracySpeed = 1.35f;
    }
}