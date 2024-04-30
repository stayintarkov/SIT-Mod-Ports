using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.Personalities
{
    public sealed partial class PersonalitySettingsClass
    {
        public class PersonalityVariablesClass
        {
            [Hidden]
            [JsonIgnore]
            public static readonly PersonalityVariablesClass Defaults = new PersonalityVariablesClass();

            [JsonIgnore]
            [Hidden]
            const string PowerLevelDescription = " Power level is a combined number that takes into account armor, the class of that armor, and the weapon class that is currently used by a bot." +
                " Power Level usually falls within 30 to 120 on average, and almost never goes above 150";

            [Name("Personality Enabled")]
            [Description("Enables or Disables this Personality, if a All Chads, All GigaChads, or AllRats is enabled in global settings, this value is ignored")]
            [Default(true)]
            public bool Enabled = true;

            [NameAndDescription("Can Be Randomly Assigned", "A percentage chance that this personality can be applied to any bot, regardless of bot stats, power, player level, or anything else.")]
            [Default(true)]
            public bool CanBeRandomlyAssigned = true;

            [NameAndDescription("Randomly Assigned Chance", "If personality can be randomly assigned, this is the chance that will happen")]
            [Default(3)]
            [MinMax(0, 100)]
            public float RandomlyAssignedChance = 3;

            [NameAndDescription("Max Level", "The max level that a bot can be to be eligible for this personality.")]
            [Default(100)]
            [MinMax(1, 100)]
            public float MaxLevel = 100;

            [NameAndDescription("Random Chance If Meets Requirements", "If the bot meets all conditions for this personality, this is the chance the personality will actually be assigned.")]
            [Default(50)]
            [MinMax(0, 100, 1)]
            public float RandomChanceIfMeetRequirements = 50;

            [NameAndDescription("Power Level Minimum", "Minimum Power level for a bot to use this personality." + PowerLevelDescription)]
            [Default(0)]
            [MinMax(0, 800, 1)]
            public float PowerLevelMin = 0;

            [NameAndDescription("Power Level Maximum", "Maximum Power level for a bot to use this personality." + PowerLevelDescription)]
            [Default(800)]
            [MinMax(0, 800, 1)]
            public float PowerLevelMax = 800;

            [Name("Aggression Multiplier")]
            [Description("Linearly increases or decreases search time and hold ground time.")]
            [Default(1f)]
            [MinMax(0.01f, 5f, 100)]
            public float AggressionMultiplier = 1f;

            [Name("Hold Ground Base Time")]
            [Description("The base time, before modifiers, that a personality will stand their ground and shoot or return fire on an enemy if caught out of cover.")]
            [Default(1f)]
            [Advanced]
            [MinMax(0, 3f, 10)]
            public float HoldGroundBaseTime = 1f;

            [Default(0.66f)]
            [Advanced]
            [MinMax(0.1f, 2f, 10)]
            public float HoldGroundMinRandom = 0.66f;

            [Default(1.5f)]
            [Advanced]
            [MinMax(0.1f, 2f, 10)]
            public float HoldGroundMaxRandom = 1.5f;

            [Name("Start Search Base Time")]
            [Description("The base time, before modifiers, that a personality will usually start searching for their enemy.")]
            [Default(40)]
            [MinMax(0.1f, 500f)]
            public float SearchBaseTime = 40;

            [Default(true)]
            [Advanced]
            public bool WillSearchForEnemy = true;

            [Default(true)]
            [Advanced]
            public bool WillSearchFromAudio = true;


            [Default(true)]
            [Advanced]
            public bool CanShiftCoverPosition = true;

            [Default(1f)]
            [Advanced]
            public float ShiftCoverTimeMultiplier = 1f;

            [Name("Can Jump Push")]
            [Description("Can this personality jump when rushing an enemy?")]
            [Default(false)]
            public bool CanJumpCorners = false;

            [Name("Jump Push Chance")]
            [Description("If a bot can Jump Push, this is the chance they will actually do it.")]
            [Default(60f)]
            [Percentage()]
            public float JumpCornerChance = 60f;

            [Name("Can Bunny Hop during Jump Push")]
            [Description("Can this bot hit a clip on you?")]
            [Default(false)]
            public bool CanBunnyHop = false;

            [Name("Bunny Hop Chance")]
            [Description("If a bot can bunny hop, this is the chance they will actually do it.")]
            [Default(5f)]
            [Percentage()]
            public float BunnyHopChance = 5f;

            [Default(false)]
            [Advanced]
            public bool CanBegForLife = false;

            [Name("Can Yell Taunts")]
            [Description("Hey you, fuck you! You heard?")]
            [Default(false)]
            public bool CanTaunt = false;

            [Name("Can Yell Taunts Frequently")]
            [Description("HEY COCKSUCKAAAA")]
            [Default(false)]
            public bool FrequentTaunt = false;

            [Name("Can Yell Taunts Constantly")]
            [Description("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
            [Default(false)]
            public bool ConstantTaunt = false;

            [Description("Will this personality yell back at enemies taunting them")]
            [Default(true)]
            public bool CanRespondToVoice = true;

            [Default(false)]
            [Advanced]
            public bool Sneaky = false;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float SneakySpeed = 1f;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float SneakyPose = 1f;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float SearchNoEnemySpeed = 1f;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float SearchNoEnemyPose = 1f;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float SearchHasEnemySpeed = 1f;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float SearchHasEnemyPose = 1f;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float MoveToCoverNoEnemySpeed = 1f;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float MoveToCoverNoEnemyPose = 1f;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float MoveToCoverHasEnemySpeed = 1f;

            [Default(1f)]
            [Percentage0to1]
            [Advanced]
            public float MoveToCoverHasEnemyPose = 1f;

            [Default(20f)]
            [Advanced]
            [Percentage]
            public float TauntFrequency = 20f;

            [Default(20f)]
            [Advanced]
            [Percentage]
            public float TauntMaxDistance = 20f;

            [Default(false)]
            public bool SprintWhileSearch = false;

            [Default(false)]
            public bool FrequentSprintWhileSearch = false;

            [Default(false)]
            public bool CanRushEnemyReloadHeal = false;

            [Default(false)]
            [Advanced]
            public bool CanFakeDeathRare = false;

            [Name("Bots Who Can Use This")]
            [Description("Setting default on these always results in true")]
            [DefaultDictionary(nameof(BotTypeDefinitions.BotTypesNames))]
            [Advanced]
            [Hidden]
            public List<string> AllowedTypes = new List<string>();
        }
    }
}