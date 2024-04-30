using SAIN.Plugin;
using System;

namespace SAIN.Preset
{
    public sealed class SAINPresetDefinition
    {
        public string Name;
        public string Description;
        public string Creator;
        public string SAINVersion;
        public string DateCreated;
        public bool IsCustom = true;
        public string PresetCopiedFrom = string.Empty;
        public bool CanEditName = true;

        public SAINPresetDefinition Clone()
        {
            return new SAINPresetDefinition()
            {
                Name = Name,
                Description = Description,
                Creator = "None",
                SAINVersion = AssemblyInfoClass.SAINPresetVersion,
                DateCreated = DateTime.Now.ToString(),
                IsCustom = true,
                PresetCopiedFrom = Name,
            };
        }

        public static SAINPresetClass CreateDefault(string difficulty, string description = null)
        {
            var preset = new SAINPresetDefinition
            {
                Name = difficulty,
                Description = description ?? $"The Default {difficulty} SAIN Preset.",
                Creator = "Solarint",
                SAINVersion = AssemblyInfoClass.SAINPresetVersion,
                DateCreated = DateTime.Now.ToString(),
                IsCustom = false,
                CanEditName = false,
            };
            PresetHandler.SavePresetDefinition(preset);
            return new SAINPresetClass(preset);
        }
    }
}