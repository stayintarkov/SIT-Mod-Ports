using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using JetBrains.Annotations;
using UnityEngine;
using KeyboardShortcut = BepInEx.Configuration.KeyboardShortcut;

namespace Terkoiz.Freecam
{
    [BepInPlugin("com.terkoiz.freecam", "Terkoiz.Freecam", "1.4.1")]
    public class FreecamPlugin : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger { get; private set; }

        // Fall damage config entries
        private const string FallDamageSectionName = "Fall Damage";
        internal static ConfigEntry<bool> GlobalDisableFallDamage;
        internal static ConfigEntry<bool> SmartDisableFallDamage;

        // Keyboard shortcut config entries
        private const string KeybindSectionName = "Keybinds";
        internal static ConfigEntry<KeyboardShortcut> ToggleFreecamMode;
        internal static ConfigEntry<KeyboardShortcut> ToggleFreecamControls;
        internal static ConfigEntry<KeyboardShortcut> TeleportToCamera;
        internal static ConfigEntry<KeyboardShortcut> ToggleUi;

        // Camera settings config entries
        private const string CameraSettingsSectionName = "Camera Settings";
        internal static ConfigEntry<float> CameraMoveSpeed;
        internal static ConfigEntry<float> CameraFastMoveSpeed;
        internal static ConfigEntry<float> CameraLookSensitivity;
        internal static ConfigEntry<float> CameraZoomSpeed;
        internal static ConfigEntry<float> CameraFastZoomSpeed;

        // General toggles
        private const string TogglesSectionName = "Toggles";
        internal static ConfigEntry<bool> CameraHeightMovement;
        internal static ConfigEntry<bool> CameraMousewheelZoom;
        internal static ConfigEntry<bool> CameraRememberLastPosition;

        [UsedImplicitly]
        internal void Start()
        {
            Logger = base.Logger;
            InitConfiguration();

            new FreecamPatch().Enable();
            new FallDamagePatch().Enable();
        }

        private void InitConfiguration()
        {
            GlobalDisableFallDamage = Config.Bind(
                FallDamageSectionName,
                "Globally Disable Fall Damage",
                false,
                "Completely disables fall damage. This is the safest option for using freecam. Will fully override the 'Smart Fall Damage Prevention' setting.");

            SmartDisableFallDamage = Config.Bind(
                FallDamageSectionName,
                "Smart Fall Damage Prevention",
                true,
                "Fall damage will only be disabled after using teleport, until your player lands. Less cheat-y way to save yourself from fall damage, but might sometimes be unreliable.");

            ToggleFreecamMode = Config.Bind(
                KeybindSectionName,
                "Toggle Freecam",
                new KeyboardShortcut(KeyCode.KeypadPlus),
                "The keyboard shortcut that toggles Freecam");

            ToggleFreecamControls = Config.Bind(
                KeybindSectionName,
                "Toggle Freecam Controls",
                new KeyboardShortcut(KeyCode.KeypadPeriod),
                "The keyboard shortcut that toggles Freecam Controls");

            TeleportToCamera = Config.Bind(
                KeybindSectionName,
                "Teleport To Camera",
                new KeyboardShortcut(KeyCode.KeypadEnter),
                "The keyboard shortcut that teleports the player to camera position");

            ToggleUi = Config.Bind(
                KeybindSectionName,
                "Toggle UI",
                new KeyboardShortcut(KeyCode.KeypadMultiply),
                "The keyboard shortcut that toggles the game UI");

            CameraMoveSpeed = Config.Bind(
                CameraSettingsSectionName,
                "Camera Speed",
                10f,
                new ConfigDescription(
                    "The speed at which the camera will move normally",
                    new AcceptableValueRange<float>(0.01f, 100f)));

            CameraFastMoveSpeed = Config.Bind(
                CameraSettingsSectionName,
                "Camera Sprint Speed",
                100f,
                new ConfigDescription(
                    "The speed at which the camera will move when the Shift key is held down",
                    new AcceptableValueRange<float>(0.01f, 1000f)));

            CameraLookSensitivity = Config.Bind(
                CameraSettingsSectionName,
                "Camera Mouse Sensitivity",
                3f,
                new ConfigDescription(
                    "Camera free look mouse sensitivity",
                    new AcceptableValueRange<float>(0.1f, 10f)));

            CameraZoomSpeed = Config.Bind(
                CameraSettingsSectionName,
                "Camera Zoom Speed",
                10f,
                new ConfigDescription(
                    "Amount to zoom the camera when using the mouse wheel",
                    new AcceptableValueRange<float>(0.01f, 100f)));

            CameraFastZoomSpeed = Config.Bind(
                CameraSettingsSectionName,
                "Camera Zoom Sprint Speed",
                50f,
                new ConfigDescription(
                    "Amount to zoom the camera when using the mouse wheel while holding Shift",
                    new AcceptableValueRange<float>(0.01f, 1000f)));
            
            CameraHeightMovement = Config.Bind(
                TogglesSectionName,
                "Camera Height Movement Keys",
                true,
                "Enables or disables the camera height movement keys, which default to Q, E, R, F." + 
                " \nUseful to disable if you want to let your character lean in Freecam mode");

            CameraMousewheelZoom = Config.Bind(
                TogglesSectionName,
                "Camera Mousewheel Zoom",
                true,
                "Enables or disables camera movement on mousewheel scroll. Just in case you find it annoying and want that disabled.");

            CameraRememberLastPosition = Config.Bind(
                TogglesSectionName,
                "Remember Last Camera Position",
                false,
                "If enabled, returning to Freecam mode will put the camera to it's last position which is saved when exiting Freecam mode.");
        }
    }
}
