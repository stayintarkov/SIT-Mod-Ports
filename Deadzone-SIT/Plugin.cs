using BepInEx;
using BepInEx.Configuration;

namespace DeadzoneMod;

[BepInPlugin("me.DJ.deadzone", "Deadzone", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static PluginSettings Settings = new();
    public static bool Enabled => Settings.Enabled != null && Settings.Enabled.Value;

    void Awake()
    {
        Settings.Enabled = Config.Bind("Values", "Global deadzone enabled", true, new ConfigDescription("Will deadzone be enabled for any group"));

        Settings.WeaponSettings = new WeaponSettingsGroup(
            new WeaponSettings(
                Config,
                new WeaponSettingsOverrides( // no idea why this being empty breaks it
                    Position: 0.1f
                ),
                "Default"
            )
        );

        Settings.Initialized = true;

        DeadzonePatch.Enable();
    }
}

