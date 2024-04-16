using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillsExtended.Helpers
{
    internal static class SEConfig
    {
        public static ConfigEntry<bool> disableEliteRequirement;

        public static ConfigEntry<float> firstAidSpeedMult;
        public static ConfigEntry<float> fieldMedicineSpeedMult;
        public static ConfigEntry<float> medicalSkillCoolDownTime;
        
        public static ConfigEntry<float> usecWeaponSpeedMult;
        public static ConfigEntry<float> bearWeaponSpeedMult;

        public static void InitializeConfig(ConfigFile Config)
        {
            disableEliteRequirement = Config.Bind(
                "Skills Extended",
                "Disable elite requirements",
                false,
                new ConfigDescription("Removes the requirement to have elite in your factions skill, to unlock the opposing factions skill.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 0 }));

            firstAidSpeedMult = Config.Bind(
                "Skills Extended",
                "First aid leveling speed multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for first aid.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 1 }));

            fieldMedicineSpeedMult = Config.Bind(
                "Skills Extended",
                "Field medicine leveling speed multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for field medicine.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 2 }));

            medicalSkillCoolDownTime = Config.Bind(
                "Skills Extended",
                "Cool down time for xp per limb",
                60f,
                new ConfigDescription("Time in seconds for a limb to become available for XP again. \n\nNOTE: When using meds from a hotkey without selecting a specific bodypart, bodypart common is chosen everytime, this leads to a global cooldown of the skill for hotkey use. Specific limb selection works off of a different patch. I'll figure out how to fix this in the future.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 3 }));

            usecWeaponSpeedMult = Config.Bind(
                "Skills Extended",
                "Usec rifle and carbine proficiency Leveling Speed Multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for usec Rifle and carbine proficiency.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 4 }));

            bearWeaponSpeedMult = Config.Bind(
                "Skills Extended",
                "Bear rifle and carbine proficiency Leveling Speed Multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for bear Rifle and carbine proficiency.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 5 }));
        }
    }
}
