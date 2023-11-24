using System.Collections.Generic;
using BepInEx.Configuration;

namespace DeadzoneMod;

public struct WeaponSettingsOverrides
{
    public bool Enabled;
    public bool UseDefault;
    public float Position;
    public float Sensitivity;
    public float MaxAngle;
    public float AimMultiplier;

    public WeaponSettingsOverrides(
        bool Enabled = true,
        bool UseDefault = false,
        float Position = 0.1f,
        float Sensitivity = 0.115f,
        float MaxAngle = 5.0f,
        float AimMultiplier = 0.0f
    )
    {
        this.Enabled = Enabled;
        this.UseDefault = UseDefault;
        this.Position = Position;
        this.Sensitivity = Sensitivity;
        this.MaxAngle = MaxAngle;
        this.AimMultiplier = AimMultiplier;
    }
}

public struct WeaponSettings
{
    public ConfigEntry<bool> Enabled;

    public readonly bool UseDefault => UseDefaultConfig.Value;
    public readonly ConfigEntry<bool> UseDefaultConfig;
    public readonly ConfigEntry<float> Position;
    public readonly ConfigEntry<float> Sensitivity;
    public readonly ConfigEntry<float> MaxAngle;
    public readonly ConfigEntry<float> AimMultiplier;
    public WeaponSettings(
        ConfigFile Config,
        WeaponSettingsOverrides settings,
        string GroupName = "Group"
    )
    {
        string group = $"{GroupName}s";
        Enabled = Config.Bind(group, $"{GroupName} deadzone enabled", settings.Enabled, new ConfigDescription("Will deadzone be enabled"));
        UseDefaultConfig = GroupName == "Default" ? null : Config.Bind(group, $"{GroupName} disable", settings.UseDefault, new ConfigDescription("Will this group use default values instead"));
        Position = Config.Bind(group, $"{GroupName} deadzone pivot", settings.Position, new ConfigDescription("How far back will the deadzone pivot"));
        Sensitivity = Config.Bind(group, $"{GroupName} deadzone sensitivity", settings.Sensitivity, new ConfigDescription("How fast will the gun move (less = slower)"));
        MaxAngle = Config.Bind(group, $"{GroupName} max deadzone angle", settings.MaxAngle, new ConfigDescription("How much will the gun be able to move (degrees)"));
        AimMultiplier = Config.Bind(group, $"{GroupName} aiming deadzone multiplier", settings.AimMultiplier, new ConfigDescription("How much deadzone will there be while aiming (0 = none)"));
    }
}

public struct WeaponSettingsGroup
{
    readonly Dictionary<string, WeaponSettings> settings = new();
    public WeaponSettings fallback;
    public WeaponSettingsGroup(WeaponSettings fallback)
    {
        this.fallback = fallback;
    }

    public readonly WeaponSettings this[string index]
    {
        get
        {
            if (settings.TryGetValue(index, out WeaponSettings chosen))
                return chosen;

            return fallback;
        }
        set => settings.Add(index, value);
    }
}

public struct PluginSettings
{
    public bool Initialized;
    public ConfigEntry<bool> Enabled;
    public WeaponSettingsGroup WeaponSettings;
}
