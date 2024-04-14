using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.Interactive;
using EFT.PrefabSettings;



namespace VCQLQuestZones.Core
{
    internal class QuestZones
    {
        public static List<Zone> GetZones()
        {
            var request = Utils.Get<List<Zone>>("/vcql/zones/get");
#if DEBUG
            int loadedZoneCount = 0;
            if (request != null)
            {
                foreach (var zone in request)
                {
                    if (zone.ZoneLocation.ToLower() == Singleton<GameWorld>.Instance.MainPlayer.Location.ToLower())
                    {
                        ConsoleScreen.Log("-------------------------------------");
                        ConsoleScreen.Log($"Scale Z: {zone.Scale.Z}");
                        ConsoleScreen.Log($"Scale Y: {zone.Scale.Y}");
                        ConsoleScreen.Log($"Scale X: {zone.Scale.X}");
                        ConsoleScreen.Log($"ZoneScale:");
                        ConsoleScreen.Log($"Position Z: {zone.Position.Z}");
                        ConsoleScreen.Log($"Position Y: {zone.Position.Y}");
                        ConsoleScreen.Log($"Position X: {zone.Position.X}");
                        ConsoleScreen.Log("ZonePosition:");
                        ConsoleScreen.Log($"ZoneType: {zone.ZoneType}");
                        if (!string.IsNullOrEmpty(zone.FlareType)) ConsoleScreen.Log($"FlareType: {zone.FlareType}");  
                        else ConsoleScreen.Log($"FlareType: N/A");
                        ConsoleScreen.Log($"ZoneLocation: {zone.ZoneLocation}");
                        ConsoleScreen.Log($"ZoneId: {zone.ZoneId}");
                        ConsoleScreen.Log($"ZoneName: {zone.ZoneName}");
                        ConsoleScreen.Log("-------------------------------------");
                        loadedZoneCount++;
                    }
                }
            }
            ConsoleScreen.Log("-------------------------------------");
            ConsoleScreen.Log($"Loaded Zone Count: {loadedZoneCount}");
            ConsoleScreen.Log($"Player Map Location: {Singleton<GameWorld>.Instance.MainPlayer.Location}");
#endif   
            return request;
        }

        public static GameObject ZoneCreateItem(Zone zone)
        {
            GameObject newZone = new GameObject();

            BoxCollider boxCollider = newZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            Vector3 position = new Vector3(float.Parse(zone.Position.X), float.Parse(zone.Position.Y), float.Parse(zone.Position.Z));
            Vector3 scale = new Vector3(float.Parse(zone.Scale.X), float.Parse(zone.Scale.Y), float.Parse(zone.Scale.Z));

            newZone.transform.position = position;
            newZone.transform.localScale = scale;

            EFT.Interactive.PlaceItemTrigger trigger = newZone.AddComponent<EFT.Interactive.PlaceItemTrigger>();
            trigger.SetId(zone.ZoneId);

            newZone.layer = LayerMask.NameToLayer("Triggers");
            newZone.name = zone.ZoneId;

            return newZone;
        }

        public static GameObject ZoneCreateVisit(Zone zone)
        {
            GameObject newZone = new GameObject();

            BoxCollider boxCollider = newZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            Vector3 position = new Vector3(float.Parse(zone.Position.X), float.Parse(zone.Position.Y), float.Parse(zone.Position.Z));
            Vector3 scale = new Vector3(float.Parse(zone.Scale.X), float.Parse(zone.Scale.Y), float.Parse(zone.Scale.Z));

            newZone.transform.position = position;
            newZone.transform.localScale = scale;

            EFT.Interactive.ExperienceTrigger trigger = newZone.AddComponent<EFT.Interactive.ExperienceTrigger>();
            trigger.SetId(zone.ZoneId);

            newZone.layer = LayerMask.NameToLayer("Triggers");
            newZone.name = zone.ZoneId;

            return newZone;
        }

        public static GameObject ZoneCreateBotKillZone(Zone zone)
        {
            GameObject newZone = new GameObject();

            BoxCollider boxCollider = newZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            Vector3 position = new Vector3(float.Parse(zone.Position.X), float.Parse(zone.Position.Y), float.Parse(zone.Position.Z));
            Vector3 scale = new Vector3(float.Parse(zone.Scale.X), float.Parse(zone.Scale.Y), float.Parse(zone.Scale.Z));

            newZone.transform.position = position;
            newZone.transform.localScale = scale;

            EFT.Interactive.TriggerWithId trigger = newZone.AddComponent<EFT.Interactive.TriggerWithId>();
            trigger.SetId(zone.ZoneId);

            newZone.layer = LayerMask.NameToLayer("Triggers");
            newZone.name = zone.ZoneId;

            return newZone;
        }

        public static GameObject ZoneCreateFlareZone(Zone zone)
        {
            // Thank you Groovey :)
            GameObject newZone = new GameObject();

            BoxCollider boxCollider = newZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;


            Vector3 position = new Vector3(float.Parse(zone.Position.X), float.Parse(zone.Position.Y), float.Parse(zone.Position.Z));
            Vector3 scale = new Vector3(float.Parse(zone.Scale.X), float.Parse(zone.Scale.Y), float.Parse(zone.Scale.Z));

            newZone.transform.position = position;
            newZone.transform.localScale = scale;

            ZoneFlareTrigger flareTrigger = newZone.AddComponent<ZoneFlareTrigger>();
            flareTrigger.SetId(zone.ZoneId);

            MoveObjectsToAdditionalPhysSceneMarker moveObjectsToAdditionalPhysSceneMarker = newZone.AddComponent<MoveObjectsToAdditionalPhysSceneMarker>();

            FlareShootDetectorZone flareDetector = newZone.AddComponent<FlareShootDetectorZone>();

            Type flareDetectorType = typeof(FlareShootDetectorZone);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo zoneIDField = flareDetectorType.GetField("zoneID", bindingFlags);
            zoneIDField.SetValue(flareDetector, zone.ZoneId);

            FlareEventType flareType = (FlareEventType)Enum.Parse(typeof(FlareEventType), zone.FlareType);
            FieldInfo flareTypeForHandleField = flareDetectorType.GetField("flareTypeForHandle", bindingFlags);
            flareTypeForHandleField.SetValue(flareDetector, flareType);

            PhysicsTriggerHandler triggerHandler = newZone.AddComponent<PhysicsTriggerHandler>();
            triggerHandler.trigger = boxCollider;

            FieldInfo triggerHandlersField = flareDetectorType.GetField("_triggerHandlers", bindingFlags);
            List<PhysicsTriggerHandler> triggerHandlers = (List<PhysicsTriggerHandler>)triggerHandlersField.GetValue(flareDetector);
            triggerHandlers.Add(triggerHandler);

            newZone.layer = LayerMask.NameToLayer("Triggers");
            newZone.name = zone.ZoneId;

            return newZone;
        }


        public static void CreateZones(List<Zone> zones, string Location)
        {
            foreach (Zone zone in zones)
            {
                if (zone.ZoneLocation.ToLower() == Location.ToLower())
                {
                    if (zone.ZoneType.ToLower() == "placeitem")
                    {
                        ZoneCreateItem(zone);
                    }

                    if (zone.ZoneType.ToLower() == "visit")
                    {
                        ZoneCreateVisit(zone);
                    }

                    if (zone.ZoneType.ToLower() == "flarezone")
                    {
                        ZoneCreateFlareZone(zone);
                    }

                    if (zone.ZoneType.ToLower() == "botkillzone")
                    {
                        ZoneCreateBotKillZone(zone);
                    }
                }
            }
        }
    }
}
