using BepInEx.Configuration;

namespace SkillsExtended.Helpers
{
    internal static class SEConfig
    {
        public static ConfigEntry<float> firstAidSpeedMult;
        public static ConfigEntry<float> fieldMedicineSpeedMult;

        public static ConfigEntry<float> usecWeaponSpeedMult;
        public static ConfigEntry<float> bearWeaponSpeedMult;

        public static void InitializeConfig(ConfigFile Config)
        {
            firstAidSpeedMult = Config.Bind(
                "Skills Extended",
                "First aid leveling speed multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for first aid.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 0 }));

            fieldMedicineSpeedMult = Config.Bind(
                "Skills Extended",
                "Field medicine leveling speed multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for field medicine.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 1 }));

            usecWeaponSpeedMult = Config.Bind(
                "Skills Extended",
                "Usec rifle and carbine proficiency Leveling Speed Multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for usec Rifle and carbine proficiency.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 2 }));

            bearWeaponSpeedMult = Config.Bind(
                "Skills Extended",
                "Bear rifle and carbine proficiency Leveling Speed Multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for bear Rifle and carbine proficiency.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 3 }));
        }
    }
}