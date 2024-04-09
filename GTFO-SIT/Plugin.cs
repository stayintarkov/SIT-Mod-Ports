using System.Reflection;
using StayInTarkov;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using UnityEngine;

namespace GTFO
{
    [BepInPlugin("com.dvize.GTFO", "dvize.GTFO", "1.0.8")]
    public class GTFOPlugin : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> enabledPlugin;
        internal static ConfigEntry<float> distanceLimit;
        internal static ConfigEntry<KeyboardShortcut> extractKeyboardShortcut;
        internal static ConfigEntry<KeyboardShortcut> questKeyboardShortcut;
        internal static ConfigEntry<float> displayTime;
        internal static ConfigEntry<bool> showOnlyNecessaryObjectives;

        private void Awake()
        {

            enabledPlugin = Config.Bind(
                "Main Settings",
                "Enable Mod",
                true,
                "Enable the plugin to show with extracts/quests objectives");

            showOnlyNecessaryObjectives = Config.Bind(
                "Main Settings",
                "Display Only Necessary Quest Conditions",
                false,
                "Only Display Necessary Quest Conditions");

            distanceLimit = Config.Bind(
                "Main Settings",
                "Distance Limit",
                500f,
                "Show Extracts at a Maximum Distance of Up To");

            extractKeyboardShortcut = Config.Bind(
                "Main Settings",
                "Extract Keyboard Shortcut",
                new KeyboardShortcut(KeyCode.O),
                "Toggle Extracts Display");

            questKeyboardShortcut = Config.Bind(
                "Main Settings",
                "Quest Keyboard Shortcut",
                new KeyboardShortcut(KeyCode.P),
                "Toggle Quest Display");

            displayTime = Config.Bind(
                "Main Settings",
                "Display Time",
                10f,
                "Amount of Time to Display Objective Points");

            new NewGamePatch().Enable();
        }

    }

    //re-initializes each new game
    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix()
        {
            GTFOComponent.Enable();
        }
    }
}
