using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using UnityEngine;

namespace TechHappy.MinimapSender
{
    [BepInPlugin("com.techhappy.webminimap", "TechHappy.WebMinimap", "1.0.6")]
    public class MinimapSenderPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource MinimapSenderLogger { get; private set; }
        internal static ConfigEntry<bool> OpenMapToggle { get ; private set; }
        internal static ConfigEntry<int> RefreshIntervalMilliseconds { get; private set; }
        internal static ConfigEntry<int> DestinationPort { get; private set; }
        internal static ConfigEntry<bool> ShowAirdrops { get; private set; }
        internal static ConfigEntry<bool> ShowQuestMarkers { get; private set; }
        internal static MinimapServer _server;
        internal static int raidCounter = 0;

        // TODO: Move this to a better spot than a global
        internal static List<Vector3> airdrops;

        private void Awake()
        {
            MinimapSenderLogger = Logger;
            MinimapSenderLogger.LogInfo("MinimapSender loaded");

            airdrops = new List<Vector3>();

            const string configSectionHelpers = "Helpers";

            OpenMapToggle = Config.Bind
            (
                configSectionHelpers,
                "Open Map",
                false,
                new ConfigDescription
                (
                    "Opens the map when toggled on"
                )
            );

            const string configSection = "Map settings";            

            OpenMapToggle.SettingChanged += OpenMapSettingChanged;

            RefreshIntervalMilliseconds = Config.Bind
            (
                configSection,
                "Refresh Interval (milliseconds)",
                250,
                new ConfigDescription
                (
                    "Map position refresh interval in milliseconds (1 second = 1000 milliseconds)",
                    new AcceptableValueRange<int>(50, 30000)
                )
            );

            DestinationPort = Config.Bind
            (
                configSection,
                "Map Port",
                8080,
                new ConfigDescription
                (
                    "Map URL is http://localhost:(this setting)/index.html",
                    new AcceptableValueRange<int>(1024, 65535)
                )
            );

            ShowAirdrops = Config.Bind
            (
                configSection,
                "Show Airdrops",
                true,
                new ConfigDescription
                (
                    "Should icons appear for airdrops?"
                )
            );

            ShowQuestMarkers = Config.Bind
            (
                configSection,
                "Show Quest Locations",
                true,
                new ConfigDescription
                (
                    "Should icons appear for quest spots?"
                )
            );

            try
            {
                // WebSocket server port
                int port = DestinationPort.Value;
                // WebSocket server content path
                string www = "BepInEx/plugins/TechHappy-MinimapSender/www";

                MinimapSenderLogger.LogInfo($"WebSocket server port: {port}");
                MinimapSenderLogger.LogInfo($"WebSocket server static content path: {www}");
                MinimapSenderLogger.LogInfo($"WebSocket server website: http://localhost:{port}/index.html");

                // Create a new WebSocket server
                _server = new MinimapServer(IPAddress.Any, port);
                _server.AddStaticContent(www, "/");

                // Start the server
                MinimapSenderLogger.LogInfo("Server starting...");
                _server.Start();

                MinimapSenderLogger.LogInfo("Done!");
            }
            catch (Exception e)
            {
                MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        private void Start()
        {
            try
            {
                // Enable patches
                new MinimapSenderPatch().Enable();
                new AirdropOnBoxLandPatch().Enable();
                new TryNotifyConditionChangedPatch().Enable();
            }
            catch (Exception e)
            {
                MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        static void OpenMapSettingChanged(object sender, EventArgs e)
        {
            MinimapSenderLogger.LogInfo($"OpenMap setting changed");

            if (OpenMapToggle.Value == true)
            {
                OpenMapToggle.Value = false;

                Process.Start($"http://localhost:{DestinationPort.Value}/index.html");
            }
        }
    }
}