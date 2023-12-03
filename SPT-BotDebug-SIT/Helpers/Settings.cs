using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.BotDebug.Components;
using System;
using UnityEngine;
using static DrakiaXYZ.BotDebug.BotInfo;

namespace DrakiaXYZ.BotDebug.Helpers
{
    internal class Settings
    {
        public static ConfigEntry<bool> Enable;
        public static ConfigEntry<BotInfoMode> ActiveMode;
        public static ConfigEntry<KeyboardShortcut> NextModeKey;
        public static ConfigEntry<KeyboardShortcut> PrevModeKey;
        public static ConfigEntry<int> MaxDrawDistance;
        public static ConfigEntry<int> FontSize;

        private static string _mainSettingsLabel = "Main Settings";

        public static void Init(ConfigFile Config)
        {
            Enable = Config.Bind(
                _mainSettingsLabel,
                "Enable",
                false,
                "Turn Off/On");
            Enable.SettingChanged += Enable_SettingChanged;

            ActiveMode = Config.Bind(
                _mainSettingsLabel,
                "ActiveMode",
                BotInfoMode.Behaviour,
                "Set the bot monitor mode");

            NextModeKey = Config.Bind(
                _mainSettingsLabel,
                "NextModeKey",
                new KeyboardShortcut(KeyCode.F10),
                "Key to switch to the next Monitor Mode");

            PrevModeKey = Config.Bind(
                _mainSettingsLabel,
                "PrevModeKey",
                new KeyboardShortcut(KeyCode.F9),
                "Key to switch to the previous Monitor Mode");

            MaxDrawDistance = Config.Bind(
                _mainSettingsLabel,
                "MaxDrawDistance",
                1500,
                new ConfigDescription("Max distance to draw a bot's debug box", new AcceptableValueRange<int>(0, 2000)));

            FontSize = Config.Bind(
                _mainSettingsLabel,
                "FontSize",
                24,
                new ConfigDescription("Font Size", new AcceptableValueRange<int>(8, 36)));
        }

        public static void Enable_SettingChanged(object sender, EventArgs e)
        {
            // If no game, do nothing
            if (!Singleton<IBotGame>.Instantiated)
            {
                return;
            }

            if (Enable.Value)
            {
                BotDebugComponent.Enable();
            }
            else
            {
                BotDebugComponent.Disable();
            }
        }
    }
}
