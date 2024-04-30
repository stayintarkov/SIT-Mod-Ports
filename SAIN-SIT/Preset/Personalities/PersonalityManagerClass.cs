using EFT;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;
using System.Linq;
using static SAIN.Helpers.EnumValues;

namespace SAIN.Preset.Personalities
{
    public class PersonalityManagerClass : BasePreset
    {
        public PersonalityManagerClass(SAINPresetClass preset) : base(preset)
        {
            ImportPersonalities();
        }

        public bool VerificationPassed = true;

        private void ImportPersonalities()
        {
            foreach (var item in EnumValues.Personalities)
            {
                if (SAINPresetClass.Import(out PersonalitySettingsClass personality, Preset.Info.Name, item.ToString(), nameof(Personalities)))
                {
                    Personalities.Add(item, personality);
                }
            }

            InitDefaults();

            bool hadToFix = false;
            foreach (var item in Personalities)
            {
                if (item.Value.Variables.AllowedTypes.Count == 0)
                {
                    hadToFix = true;
                    if (item.Key == IPersonality.Chad || item.Key == IPersonality.GigaChad)
                    {
                        AddPMCTypes(item.Value.Variables.AllowedTypes);
                    }
                    else
                    {
                        AddAllBotTypes(item.Value.Variables.AllowedTypes);
                    }
                }
            }
            if (hadToFix)
            {
                string message = "The Preset you are using is out of date, and required manual fixing. Its recommended you create a new one.";
                NotificationManagerClass.DisplayMessageNotification(message);
                Logger.LogWarning(message);
            }
        }

        public void ResetToDefaults()
        {
            Personalities.Clear();
            InitDefaults();
        }

        private void InitDefaults()
        {
            var pers = IPersonality.GigaChad;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A true alpha threat. Hyper Aggressive and typically wearing high tier equipment.";
                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 60,
                        RandomlyAssignedChance = 3,
                        PowerLevelMin = 250f,

                        CanTaunt = true,
                        CanRespondToVoice = true,
                        TauntFrequency = 8,
                        TauntMaxDistance = 50f,
                        ConstantTaunt = true,

                        HoldGroundBaseTime = 1.25f,
                        HoldGroundMaxRandom = 1.5f,
                        HoldGroundMinRandom = 0.65f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 8f,
                        SprintWhileSearch = true,
                        FrequentSprintWhileSearch = true,

                        CanJumpCorners = true,
                        JumpCornerChance = 40f,
                        CanBunnyHop = true,
                        BunnyHopChance = 5f,

                        CanRushEnemyReloadHeal = true,
                        CanFakeDeathRare = true,

                        AggressionMultiplier = 1f,
                        ShiftCoverTimeMultiplier = 0.5f,

                        SearchHasEnemySpeed = 1f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 1f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                };

                AddPMCTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Wreckless;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "Rush B Cyka Blyat. Who care if I die? Gotta get the clip";
                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 3,
                        RandomlyAssignedChance = 1,

                        CanTaunt = true,
                        CanRespondToVoice = true,
                        TauntFrequency = 4,
                        TauntMaxDistance = 70f,
                        ConstantTaunt = true,

                        HoldGroundBaseTime = 2.5f,
                        HoldGroundMaxRandom = 1.5f,
                        HoldGroundMinRandom = 0.65f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 0.1f,
                        SprintWhileSearch = true,
                        FrequentSprintWhileSearch = true,

                        CanJumpCorners = true,
                        JumpCornerChance = 75f,
                        CanBunnyHop = true,
                        BunnyHopChance = 25f,
                        CanRushEnemyReloadHeal = true,
                        CanFakeDeathRare = true,

                        AggressionMultiplier = 1f,
                        ShiftCoverTimeMultiplier = 0.5f,

                        SearchHasEnemySpeed = 1f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 1f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                };

                AddAllBotTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.SnappingTurtle;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A player who finds the balance between rat and chad, yin and yang. Will rat you out but can spring out at any moment.";
                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 30,
                        RandomlyAssignedChance = 1,
                        PowerLevelMin = 250f,

                        CanTaunt = true,
                        CanRespondToVoice = false,
                        TauntFrequency = 15,
                        TauntMaxDistance = 50f,
                        ConstantTaunt = false,

                        HoldGroundBaseTime = 1.5f,
                        HoldGroundMaxRandom = 1.2f,
                        HoldGroundMinRandom = 0.8f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 90f,
                        SprintWhileSearch = false,
                        FrequentSprintWhileSearch = false,

                        CanJumpCorners = true,
                        CanRushEnemyReloadHeal = true,
                        CanFakeDeathRare = true,

                        AggressionMultiplier = 1f,
                        ShiftCoverTimeMultiplier = 0.5f,

                        SearchHasEnemySpeed = 0.7f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0.8f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                };

                AddPMCTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Chad;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "An aggressive player. Typically wearing high tier equipment, and is more aggressive than usual.";
                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,

                        RandomChanceIfMeetRequirements = 60,
                        RandomlyAssignedChance = 5,
                        PowerLevelMin = 200f,

                        CanTaunt = true,
                        CanRespondToVoice = true,
                        TauntFrequency = 15,
                        TauntMaxDistance = 30f,
                        FrequentTaunt = false,

                        HoldGroundBaseTime = 1f,
                        HoldGroundMaxRandom = 1.5f,
                        HoldGroundMinRandom = 0.65f,

                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 25f,
                        SprintWhileSearch = true,

                        CanJumpCorners = true,
                        JumpCornerChance = 25f,
                        CanRushEnemyReloadHeal = true,
                        AggressionMultiplier = 1f,

                        SearchHasEnemySpeed = 1f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0.7f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 0.7f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                };

                AddAllBotTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Rat;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "Scum of Tarkov. Rarely Seeks out enemies, and will hide and ambush.";
                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        RandomChanceIfMeetRequirements = 33,
                        RandomlyAssignedChance = 15,
                        HoldGroundBaseTime = 0.75f,
                        WillSearchForEnemy = false,
                        WillSearchFromAudio = false,
                        SearchBaseTime = 240f,
                        PowerLevelMax = 125f,
                        AggressionMultiplier = 1f,

                        Sneaky = true,
                        SneakyPose = 0f,
                        SneakySpeed = 0f,

                        SearchHasEnemySpeed = 0f,
                        SearchHasEnemyPose = 0f,
                        SearchNoEnemySpeed = 0f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 0.5f,
                        MoveToCoverHasEnemyPose = 0.5f,
                        MoveToCoverNoEnemySpeed = 0.3f,
                        MoveToCoverNoEnemyPose = 0.7f,

                        CanShiftCoverPosition = false
                    }
                };

                AddAllBotTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Timmy;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A New Player, terrified of everything.";

                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        RandomlyAssignedChance = 25,
                        PowerLevelMax = 80f,
                        MaxLevel = 10,
                        HoldGroundBaseTime = 0.5f,
                        WillSearchForEnemy = true,
                        WillSearchFromAudio = false,
                        SearchBaseTime = 90f,
                        AggressionMultiplier = 1f,
                        ShiftCoverTimeMultiplier = 0.66f,
                        CanBegForLife = true,

                        SearchHasEnemySpeed = 0f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                };

                AddPMCTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Coward;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "A player who is more passive and afraid than usual.";
                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        RandomlyAssignedChance = 25,
                        HoldGroundBaseTime = 0.5f,
                        WillSearchForEnemy = false,
                        WillSearchFromAudio = false,
                        SearchBaseTime = 110f,
                        AggressionMultiplier = 1f,
                        CanShiftCoverPosition = false,
                        CanBegForLife = true,

                        SearchHasEnemySpeed = 0f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                };
                AddAllBotTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
            }

            pers = IPersonality.Normal;
            if (!Personalities.ContainsKey(pers))
            {
                string name = pers.ToString();
                string description = "An Average Tarkov Enjoyer";
                var settings = new PersonalitySettingsClass(pers, name, description)
                {
                    Variables =
                    {
                        Enabled = true,
                        HoldGroundBaseTime = 0.65f,
                        WillSearchForEnemy = true,
                        WillSearchFromAudio = true,
                        SearchBaseTime = 60f,
                        CanRespondToVoice = true,

                        SearchHasEnemySpeed = 0.6f,
                        SearchHasEnemyPose = 1f,
                        SearchNoEnemySpeed = 0.33f,
                        SearchNoEnemyPose = 1f,

                        MoveToCoverHasEnemySpeed = 1f,
                        MoveToCoverHasEnemyPose = 1f,
                        MoveToCoverNoEnemySpeed = 1f,
                        MoveToCoverNoEnemyPose = 1f,
                    }
                };

                AddAllBotTypes(settings.Variables.AllowedTypes);
                Personalities.Add(pers, settings);
                SAINPresetClass.Export(settings, Preset.Info.Name, pers.ToString(), nameof(Personalities));
            }
        }

        private static void AddAllBotTypes(List<string> allowedTypes)
        {
            allowedTypes.Clear();
            allowedTypes.AddRange(BotTypeDefinitions.BotTypesNames);
        }

        private static void AddPMCTypes(List<string> allowedTypes)
        {
            allowedTypes.Add(BotTypeDefinitions.BotTypes[WildSpawn.Usec].Name);
            allowedTypes.Add(BotTypeDefinitions.BotTypes[WildSpawn.Bear].Name);
        }

        public Dictionary<IPersonality, PersonalitySettingsClass> Personalities = new Dictionary<IPersonality, PersonalitySettingsClass>();
    }
}