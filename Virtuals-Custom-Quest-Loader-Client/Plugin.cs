using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using VCQLQuestZones.Patches;
using VCQLQuestZones.Core;
using BepInEx.Logging;
using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using System.IO;

namespace VCQLQuestZones
{
    [BepInPlugin("com.virtual.vcql", "VCQL-Zones", "1.0.0")]
    [BepInProcess("EscapeFromTarkov.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }
        public static List<Zone> ExistingQuestZones { get; set; }

        private static string outputDir;
        private static string selectedZoneName;
        private static int currentSelectIndex = -1;
        private static readonly List<CustomZoneContainer> zones = new List<CustomZoneContainer>();
        private static readonly string[] acceptableTypes = new string[] { "placeitem", "visit", "flarezone", "botkillzone" };
        private static readonly string[] acceptableFlareTypes = new string[] { "", "Light", "Airdrop", "ExitActivate", "Quest", "AIFollowEvent" };

        internal static ConfigEntry<string> NewZoneName { get; private set; }
        internal static ConfigEntry<string> NewZoneType { get; private set; }
        internal static ConfigEntry<string> FlareZoneType { get; private set; }

        internal static ConfigEntry<float> ZoneAdjustmentValue { get; private set; }
        internal static ConfigEntry<float> PositionConfigX { get; private set; }
        internal static ConfigEntry<float> PositionConfigY { get; private set; }
        internal static ConfigEntry<float> PositionConfigZ { get; private set; }

        internal static ConfigEntry<float> ScaleConfigX { get; private set; }
        internal static ConfigEntry<float> ScaleConfigY { get; private set; }
        internal static ConfigEntry<float> ScaleConfigZ { get; private set; }

        internal static ConfigEntry<float> RotationConfigX { get; private set; }
        internal static ConfigEntry<float> RotationConfigY { get; private set; }
        internal static ConfigEntry<float> RotationConfigZ { get; private set; }

        internal static Color ColorZoneRed = new Color(1f, 0f, 0f, 0.7f);
        internal static Color ColorZoneGreen = new Color(0f, 1f, 0f, 0.7f);

        private void Awake()
        {
            Log = base.Logger;

            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            outputDir = Path.GetFullPath(Path.Combine(assemblyLocation, @"..\..\..\"));

            selectedZoneName = "No Zone Selected";
            ExistingQuestZones = new List<Zone>();

            // New Configs
            NewZoneName = Config.Bind("1. Create Zone", "Zone ID", "",
                new ConfigDescription("The name for the new zone", null,
                new ConfigurationManagerAttributes { Order = 4 }));
            NewZoneType = Config.Bind("1. Create Zone", "Zone Type", "",
                new ConfigDescription("Select the type of zone", new AcceptableValueList<string>(acceptableTypes),
                new ConfigurationManagerAttributes { Order = 3 }));
            FlareZoneType = Config.Bind("1. Create Zone", "(Optional) Flare Type", "",
                new ConfigDescription("Select the flare zone type", new AcceptableValueList<string>(acceptableFlareTypes), "",
                new ConfigurationManagerAttributes { Order = 2 }));
            Config.Bind("1. Create Zone", "Add Zone", "New Zone",
                new ConfigDescription("Adds a new zone with the zone ID", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerNewZone, Order = 1 }));

            // Select Configs
            Config.Bind("2. Select Zone", "Navigate Zones", "",
                new ConfigDescription("The ID for the currently selected Zone.", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerSwitchZone, ReadOnly = true }));

            // Adjustment Configs
            ZoneAdjustmentValue = Config.Bind("3.1. Adjustment Settings", "Zone Adjustment Value", 0.25f,
                new ConfigDescription("Sets the value used to adjust the position, scale and rotation of zones."));

            PositionConfigX = Config.Bind("3.2. Change Position", "Change Position X", 0f,
                new ConfigDescription("Change the position of the current zone", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerPositionX, ReadOnly = true }));
            PositionConfigY = Config.Bind("3.2. Change Position", "Change Position Y", 0f,
                new ConfigDescription("Change the position of the current zone", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerPositionY, ReadOnly = true }));
            PositionConfigZ = Config.Bind("3.2. Change Position", "Change Position Z", 0f,
                new ConfigDescription("Change the position of the current zone", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerPositionZ, ReadOnly = true }));

            ScaleConfigX = Config.Bind("3.3. Change Scale", "Change Scale X", 0f,
                new ConfigDescription("Change the scale of the current zone", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerScaleX, ReadOnly = true }));
            ScaleConfigY = Config.Bind("3.3. Change Scale", "Change Scale Y", 0f,
                new ConfigDescription("Change the scale of the current zone", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerScaleY, ReadOnly = true }));
            ScaleConfigZ = Config.Bind("3.3. Change Scale", "Change Scale Z", 0f,
                new ConfigDescription("Change the scale of the current zone", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerScaleZ, ReadOnly = true }));

            RotationConfigX = Config.Bind("3.4. Change Rotation", "Change Rotation X", 0f,
                new ConfigDescription("Change the rotation of the current zone", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerRotationX, ReadOnly = true }));
            RotationConfigY = Config.Bind("3.4. Change Rotation", "Change Rotation Y", 0f,
                new ConfigDescription("Change the rotation of the current zone", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerRotationY, ReadOnly = true }));
            RotationConfigZ = Config.Bind("3.4. Change Rotation", "Change Rotation Z", 0f,
                new ConfigDescription("Change the rotation of the current zone", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerRotationZ, ReadOnly = true }));

            // Output Configs
            Config.Bind("4. Output", "Output Zones", "",
                new ConfigDescription("Outputs all zones to the root SPT directory.", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerOutputZones }));

            // View Zones
            Config.Bind("5. View Zones", "Add Existing Zones", false,
                new ConfigDescription("Adds any currently loaded custom zones to the editor.", null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawerViewZones }));

            // Reset all configs on launch to remove old values
            NewZoneName.Value = (string)NewZoneName.DefaultValue;
            NewZoneType.Value = (string)NewZoneType.DefaultValue;
            FlareZoneType.Value = (string)FlareZoneType.DefaultValue;
            PositionConfigX.Value = (float)PositionConfigX.DefaultValue;
            PositionConfigY.Value = (float)PositionConfigY.DefaultValue;
            PositionConfigZ.Value = (float)PositionConfigZ.DefaultValue;
            ScaleConfigX.Value = (float)ScaleConfigX.DefaultValue;
            ScaleConfigY.Value = (float)ScaleConfigY.DefaultValue;
            ScaleConfigZ.Value = (float)ScaleConfigZ.DefaultValue;
            RotationConfigX.Value = (float)RotationConfigX.DefaultValue;
            RotationConfigY.Value = (float)RotationConfigY.DefaultValue;
            RotationConfigZ.Value = (float)RotationConfigZ.DefaultValue;

            new GameWorldPatch().Enable();
        }

        private static void DrawerViewZones(ConfigEntryBase entry)
        {
            if (GUILayout.Button("Add Existing Zones"))
            {
                ExistingQuestZones.ForEach(questZone =>
                {
                    GameObject cube = Utils.CreateNewZoneCube(questZone.ZoneName);
                    Vector3 position = new Vector3(float.Parse(questZone.Position.X), float.Parse(questZone.Position.Y), float.Parse(questZone.Position.Z));
                    Vector3 rotation = new Vector3(float.Parse(questZone.Rotation.X), float.Parse(questZone.Rotation.Y), float.Parse(questZone.Rotation.Z));
                    Vector3 scale = new Vector3(float.Parse(questZone.Scale.X), float.Parse(questZone.Scale.Y), float.Parse(questZone.Scale.Z));

                    cube.transform.position = position;
                    cube.transform.rotation = Quaternion.Euler(rotation);
                    cube.transform.localScale = scale;

                    CustomZoneContainer customZoneContainer = new CustomZoneContainer(cube, questZone.ZoneType, questZone.FlareType);
                    zones.Add(customZoneContainer);
                });
                ExistingQuestZones.Clear();
            }
        }

        // New and select zones
        private static void DrawerNewZone(ConfigEntryBase entry)
        {
            if (GUILayout.Button("New Zone", GUILayout.Width(100)))
            {
                // Ensure name isn't empty, or duplicated
                if (NewZoneName.Value == "") return;
                foreach (CustomZoneContainer zone in zones)
                {
                    if (zone.GameObject.name == NewZoneName.Value) return;
                }

                GameObject newZoneObject = Utils.CreateNewZoneCube(NewZoneName.Value);
                if (newZoneObject == null) return;

                string zoneType = NewZoneType.Value;
                string flareType = string.IsNullOrEmpty(FlareZoneType.Value) ? "" : FlareZoneType.Value;
                CustomZoneContainer newZone = new CustomZoneContainer(newZoneObject, zoneType, flareType);
                zones.Add(newZone);
                Log.LogInfo(zones.Count);
            }
        }

        private static void DrawerSwitchZone(ConfigEntryBase entry)
        {
            if (GUILayout.Button("Prev", GUILayout.Width(45))) MovePrevZone();

            GUILayout.TextField(selectedZoneName, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Next", GUILayout.Width(45))) MoveNextZone();
        }

        // Position
        private static void AdjustObjectPosition()
        {
            if (zones.Count < 1 || currentSelectIndex < 0) return;
            GameObject currentZone = zones[currentSelectIndex].GameObject;
            Vector3 newPosition = new Vector3(PositionConfigX.Value, PositionConfigY.Value, PositionConfigZ.Value);
            currentZone.transform.position = newPosition;
            Log.LogInfo("VCQL: Updated Rotation");
        }

        private static void DrawerPositionX(ConfigEntryBase entry)
        {
            if (GUILayout.Button("<--", GUILayout.Width(45))) PositionConfigX.Value -= ZoneAdjustmentValue.Value; AdjustObjectPosition();
            GUILayout.TextField(PositionConfigX.Value.ToString(), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-->", GUILayout.Width(45))) PositionConfigX.Value += ZoneAdjustmentValue.Value; AdjustObjectPosition();
        }

        private static void DrawerPositionY(ConfigEntryBase entry)
        {
            if (GUILayout.Button("<--", GUILayout.Width(45))) PositionConfigY.Value -= ZoneAdjustmentValue.Value; AdjustObjectPosition();
            GUILayout.TextField(PositionConfigY.Value.ToString(), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-->", GUILayout.Width(45))) PositionConfigY.Value += ZoneAdjustmentValue.Value; AdjustObjectPosition();
        }

        private static void DrawerPositionZ(ConfigEntryBase entry)
        {
            if (GUILayout.Button("<--", GUILayout.Width(45))) PositionConfigZ.Value -= ZoneAdjustmentValue.Value; AdjustObjectPosition();
            GUILayout.TextField(PositionConfigZ.Value.ToString(), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-->", GUILayout.Width(45))) PositionConfigZ.Value += ZoneAdjustmentValue.Value; AdjustObjectPosition();
        }

        // Scale
        private static void AdjustObjectScale()
        {
            if (zones.Count < 1 || currentSelectIndex < 0) return;
            GameObject currentZone = zones[currentSelectIndex].GameObject;
            Vector3 newScale = new Vector3(ScaleConfigX.Value, ScaleConfigY.Value, ScaleConfigZ.Value);
            currentZone.transform.localScale = newScale;
            Log.LogInfo("VCQL: Updated scale");
        }

        private static void DrawerScaleX(ConfigEntryBase entry)
        {
            if (GUILayout.Button("<--", GUILayout.Width(45))) ScaleConfigX.Value -= ZoneAdjustmentValue.Value; AdjustObjectScale();
            GUILayout.TextField(ScaleConfigX.Value.ToString(), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-->", GUILayout.Width(45))) ScaleConfigX.Value += ZoneAdjustmentValue.Value; AdjustObjectScale();
        }

        private static void DrawerScaleY(ConfigEntryBase entry)
        {
            if (GUILayout.Button("<--", GUILayout.Width(45))) ScaleConfigY.Value -= ZoneAdjustmentValue.Value; AdjustObjectScale();
            GUILayout.TextField(ScaleConfigY.Value.ToString(), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-->", GUILayout.Width(45))) ScaleConfigY.Value += ZoneAdjustmentValue.Value; AdjustObjectScale();
        }

        private static void DrawerScaleZ(ConfigEntryBase entry)
        {
            if (GUILayout.Button("<--", GUILayout.Width(45))) ScaleConfigZ.Value -= ZoneAdjustmentValue.Value; AdjustObjectScale();
            GUILayout.TextField(ScaleConfigZ.Value.ToString(), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-->", GUILayout.Width(45))) ScaleConfigZ.Value += ZoneAdjustmentValue.Value; AdjustObjectScale();
        }

        // Rotation
        private static void AdjustObjectRotation()
        {
            if (zones.Count < 1 || currentSelectIndex < 0) return;
            GameObject currentZone = zones[currentSelectIndex].GameObject;
            Quaternion newRotation = Quaternion.Euler(RotationConfigX.Value, RotationConfigY.Value, RotationConfigZ.Value);
            currentZone.transform.rotation = newRotation;
            Log.LogInfo("VCQL: Updated rotation");
        }

        private static void DrawerRotationX(ConfigEntryBase entry)
        {
            if (GUILayout.Button("<--", GUILayout.Width(45))) RotationConfigX.Value -= ZoneAdjustmentValue.Value; AdjustObjectRotation();
            GUILayout.TextField(RotationConfigX.Value.ToString(), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-->", GUILayout.Width(45))) RotationConfigX.Value += ZoneAdjustmentValue.Value; AdjustObjectRotation();
        }

        private static void DrawerRotationY(ConfigEntryBase entry)
        {
            if (GUILayout.Button("<--", GUILayout.Width(45))) RotationConfigY.Value -= ZoneAdjustmentValue.Value; AdjustObjectRotation();
            GUILayout.TextField(RotationConfigY.Value.ToString(), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-->", GUILayout.Width(45))) RotationConfigY.Value += ZoneAdjustmentValue.Value; AdjustObjectRotation();
        }

        private static void DrawerRotationZ(ConfigEntryBase entry)
        {
            if (GUILayout.Button("<--", GUILayout.Width(45))) RotationConfigZ.Value -= ZoneAdjustmentValue.Value; AdjustObjectRotation();
            GUILayout.TextField(RotationConfigZ.Value.ToString(), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("-->", GUILayout.Width(45))) RotationConfigZ.Value += ZoneAdjustmentValue.Value; AdjustObjectRotation();
        }

        private static void DrawerOutputZones(ConfigEntryBase entry)
        {
            if (GUILayout.Button("Output Zones", GUILayout.ExpandWidth(true))) ConvertOutputZones();
        }

        private static void MoveNextZone()
        {
            if (zones.Count < 1) return;
            if (currentSelectIndex >= 0) zones[currentSelectIndex].GameObject.GetComponent<Renderer>().material.color = ColorZoneRed;

            if (currentSelectIndex + 1 < zones.Count) currentSelectIndex++;
            else currentSelectIndex = 0;

            zones[currentSelectIndex].GameObject.GetComponent<Renderer>().material.color = ColorZoneGreen;
            selectedZoneName = zones[currentSelectIndex].GameObject.name;
            AdjustConfigValues();
            Log.LogInfo(selectedZoneName);
        }

        private static void MovePrevZone()
        {
            if (zones.Count < 1) return;
            if (currentSelectIndex >= 0) zones[currentSelectIndex].GameObject.GetComponent<Renderer>().material.color = ColorZoneRed;

            if (currentSelectIndex - 1 < 0) currentSelectIndex = zones.Count - 1;
            else currentSelectIndex--;
            
            zones[currentSelectIndex].GameObject.GetComponent<Renderer>().material.color = ColorZoneGreen;
            selectedZoneName = zones[currentSelectIndex].GameObject.name;
            AdjustConfigValues();
            Log.LogInfo(selectedZoneName);
        }

        private static void AdjustConfigValues()
        {
            if (zones.Count < 1 || currentSelectIndex < 0) return;
            GameObject currentZone = zones[currentSelectIndex].GameObject;

            PositionConfigX.Value = currentZone.transform.position.x;
            PositionConfigY.Value = currentZone.transform.position.y;
            PositionConfigZ.Value = currentZone.transform.position.z;

            ScaleConfigX.Value = currentZone.transform.localScale.x;
            ScaleConfigY.Value = currentZone.transform.localScale.y;
            ScaleConfigZ.Value = currentZone.transform.localScale.z;

            RotationConfigX.Value = currentZone.transform.rotation.x;
            RotationConfigY.Value = currentZone.transform.rotation.y;
            RotationConfigZ.Value = currentZone.transform.rotation.z;
        }

        private static void ConvertOutputZones()
        {
            if (zones.Count < 1) return;
            if (!Singleton<GameWorld>.Instance.MainPlayer) return;
            string locationId = Singleton<GameWorld>.Instance.MainPlayer.Location;

            string path = Path.Combine(outputDir, $"VCQL-Zone-Output-{DateTime.Now:yyyyMMddHHmmssffff}.json");

            using (StreamWriter streamWriter = File.CreateText(path))
            {
                List<Zone> convertedZones = Utils.ConvertZoneFormat(zones, locationId);
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(streamWriter, convertedZones);
            }

            Log.LogInfo($"VCQL: Output zones to file: {path}");
        }
    }
}