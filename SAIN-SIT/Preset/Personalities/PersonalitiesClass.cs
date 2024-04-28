using EFT;
using Newtonsoft.Json;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Info;

namespace SAIN.Preset.Personalities
{
    public sealed partial class PersonalitySettingsClass
    {
        [JsonConstructor]
        public PersonalitySettingsClass()
        { }

        public PersonalitySettingsClass(IPersonality personality, string name, string description)
        {
            SAINPersonality = personality;
            Name = name;
            Description = description;
        }

        public IPersonality SAINPersonality;
        public string Name;
        public string Description;

        public PersonalityVariablesClass Variables = new PersonalityVariablesClass();

        public bool CanBePersonality(SAINBotInfoClass infoClass)
        {
            return CanBePersonality(infoClass.WildSpawnType, infoClass.PowerLevel, infoClass.PlayerLevel);
        }

        public bool CanBePersonality(WildSpawnType wildSpawnType, float PowerLevel, int PlayerLevel)
        {
            if (Variables.Enabled == false)
            {
                return false;
            }
            if (Variables.CanBeRandomlyAssigned && EFTMath.RandomBool(Variables.RandomlyAssignedChance))
            {
                return true;
            }

            if (!BotTypeDefinitions.BotTypes.ContainsKey(wildSpawnType))
            {
                return false;
            }

            string name = BotTypeDefinitions.BotTypes[wildSpawnType].Name;
            if (!Variables.AllowedTypes.Contains(name))
            {
                return false;
            }
            if (PowerLevel > Variables.PowerLevelMax || PowerLevel < Variables.PowerLevelMin)
            {
                return false;
            }
            if (PlayerLevel > Variables.MaxLevel)
            {
                return false;
            }
            return EFTMath.RandomBool(Variables.RandomChanceIfMeetRequirements);
        }
    }
}