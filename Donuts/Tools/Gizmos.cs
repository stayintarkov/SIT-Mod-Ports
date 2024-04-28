using EFT;
using EFT.Communications;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static Donuts.DonutComponent;

#pragma warning disable IDE0007, IDE0044

namespace Donuts
{
    internal class Gizmos
    {
        internal bool isGizmoEnabled = false;
        internal static HashSet<Vector3> drawnCoordinates;
        internal static List<GameObject> gizmoSpheres;
        internal static Coroutine gizmoUpdateCoroutine;
        internal static MonoBehaviour monoBehaviourRef;
        internal static StringBuilder DisplayedMarkerInfo = new StringBuilder();
        internal static StringBuilder PreviousMarkerInfo = new StringBuilder();
        internal static Coroutine resetMarkerInfoCoroutine;

        internal Gizmos(MonoBehaviour monoBehaviour)
        {
            monoBehaviourRef = monoBehaviour;
        }
        private IEnumerator UpdateGizmoSpheresCoroutine()
        {
            while (isGizmoEnabled)
            {
                RefreshGizmoDisplay(); // Refresh the gizmo display periodically

                yield return new WaitForSeconds(3f);
            }
        }
        internal static void DrawMarkers(List<Entry> locations, Color color, PrimitiveType primitiveType)
        {
            foreach (var hotspot in locations)
            {
                var newCoordinate = new Vector3(hotspot.Position.x, hotspot.Position.y, hotspot.Position.z);

                if (maplocation == hotspot.MapName && !drawnCoordinates.Contains(newCoordinate))
                {
                    var marker = GameObject.CreatePrimitive(primitiveType);
                    var material = marker.GetComponent<Renderer>().material;
                    material.color = color;
                    marker.GetComponent<Collider>().enabled = false;
                    marker.transform.position = newCoordinate;

                    if (DonutsPlugin.gizmoRealSize.Value)
                    {
                        marker.transform.localScale = new Vector3(hotspot.MaxDistance, 3f, hotspot.MaxDistance);
                    }
                    else
                    {
                        marker.transform.localScale = new Vector3(1f, 1f, 1f);
                    }

                    gizmoSpheres.Add(marker);
                    drawnCoordinates.Add(newCoordinate);
                }
            }
        }

        public void ToggleGizmoDisplay(bool enableGizmos)
        {
            isGizmoEnabled = enableGizmos;

            if (isGizmoEnabled && gizmoUpdateCoroutine == null)
            {
                RefreshGizmoDisplay(); // Refresh the gizmo display initially
                gizmoUpdateCoroutine = monoBehaviourRef.StartCoroutine(UpdateGizmoSpheresCoroutine());
            }
            else if (!isGizmoEnabled && gizmoUpdateCoroutine != null)
            {
                monoBehaviourRef.StopCoroutine(gizmoUpdateCoroutine);
                gizmoUpdateCoroutine = null;

                ClearGizmoMarkers(); // Clear the drawn markers
            }
        }

        internal static void RefreshGizmoDisplay()
        {
            ClearGizmoMarkers(); // Clear existing markers

            // Check the values of DebugGizmos and gizmoRealSize and redraw the markers accordingly
            if (DonutsPlugin.DebugGizmos.Value)
            {
                if (fightLocations != null && fightLocations.Locations != null && fightLocations.Locations.Count > 0)
                {
                    DrawMarkers(fightLocations.Locations, Color.green, PrimitiveType.Sphere);
                }

                if (sessionLocations != null && sessionLocations.Locations != null && sessionLocations.Locations.Count > 0)
                {
                    DrawMarkers(sessionLocations.Locations, Color.red, PrimitiveType.Cube);
                }
            }
        }

        internal static void ClearGizmoMarkers()
        {
            foreach (var marker in gizmoSpheres)
            {
                GameWorld.Destroy(marker);
            }
            gizmoSpheres.Clear();
            drawnCoordinates.Clear();
        }


        internal static void DisplayMarkerInformation()
        {
            if (gizmoSpheres.Count == 0)
            {
                return;
            }

            GameObject closestShape = null;
            float closestDistanceSq = float.MaxValue;

            // Find the closest primitive shape game object to the player
            foreach (var shape in gizmoSpheres)
            {
                Vector3 shapePosition = shape.transform.position;
                float distanceSq = (shapePosition - gameWorld.MainPlayer.Transform.position).sqrMagnitude;
                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    closestShape = shape;
                }
            }

            // Check if the closest shape is within 15m and directly visible to the player
            if (closestShape != null && closestDistanceSq <= 10f * 10f)
            {
                Vector3 direction = closestShape.transform.position - gameWorld.MainPlayer.Transform.position;
                float angle = Vector3.Angle(gameWorld.MainPlayer.Transform.forward, direction);

                if (angle < 20f)
                {
                    // Create a HashSet of positions for fast containment checks
                    var locationsSet = new HashSet<Vector3>();
                    foreach (var entry in fightLocations.Locations.Concat(sessionLocations.Locations))
                    {
                        locationsSet.Add(new Vector3(entry.Position.x, entry.Position.y, entry.Position.z));
                    }

                    // Check if the closest shape's position is contained in the HashSet
                    Vector3 closestShapePosition = closestShape.transform.position;
                    if (locationsSet.Contains(closestShapePosition))
                    {
                        if (displayMessageNotificationMethod != null)
                        {
                            Entry closestEntry = GetClosestEntry(closestShapePosition);
                            if (closestEntry != null)
                            {
                                PreviousMarkerInfo.Clear();
                                PreviousMarkerInfo.Append(DisplayedMarkerInfo);

                                DisplayedMarkerInfo.Clear();

                                DisplayedMarkerInfo.AppendLine("Donuts: Marker Info");
                                DisplayedMarkerInfo.AppendLine($"GroupNum: {closestEntry.GroupNum}");
                                DisplayedMarkerInfo.AppendLine($"Name: {closestEntry.Name}");
                                DisplayedMarkerInfo.AppendLine($"SpawnType: {closestEntry.WildSpawnType}");
                                DisplayedMarkerInfo.AppendLine($"Position: {closestEntry.Position.x}, {closestEntry.Position.y}, {closestEntry.Position.z}");
                                DisplayedMarkerInfo.AppendLine($"Bot Timer Trigger: {closestEntry.BotTimerTrigger}");
                                DisplayedMarkerInfo.AppendLine($"Spawn Chance: {closestEntry.SpawnChance}");
                                DisplayedMarkerInfo.AppendLine($"Max Random Number of Bots: {closestEntry.MaxRandomNumBots}");
                                DisplayedMarkerInfo.AppendLine($"Max Spawns Before Cooldown: {closestEntry.MaxSpawnsBeforeCoolDown}");
                                DisplayedMarkerInfo.AppendLine($"Ignore Timer for First Spawn: {closestEntry.IgnoreTimerFirstSpawn}");
                                DisplayedMarkerInfo.AppendLine($"Min Spawn Distance From Player: {closestEntry.MinSpawnDistanceFromPlayer}");
                                string txt = DisplayedMarkerInfo.ToString();

                                // Check if the marker info has changed since the last update
                                if (txt != PreviousMarkerInfo.ToString())
                                {
                                    MethodInfo displayMessageNotificationMethod;
                                    if (methodCache.TryGetValue("DisplayMessageNotification", out displayMessageNotificationMethod))
                                    {
                                        displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Default, Color.yellow });
                                    }

                                    // Stop the existing coroutine if it's running
                                    if (resetMarkerInfoCoroutine != null)
                                    {
                                        monoBehaviourRef.StopCoroutine(resetMarkerInfoCoroutine);
                                    }

                                    // Start a new coroutine to reset the marker info after a delay
                                    resetMarkerInfoCoroutine = monoBehaviourRef.StartCoroutine(ResetMarkerInfoAfterDelay());
                                }
                            }
                        }
                    }
                }
            }
        }
        internal static IEnumerator ResetMarkerInfoAfterDelay()
        {
            yield return new WaitForSeconds(5f);

            // Reset the marker info
            DisplayedMarkerInfo.Clear();
            resetMarkerInfoCoroutine = null;
        }
        internal static Entry GetClosestEntry(Vector3 position)
        {
            Entry closestEntry = null;
            float closestDistanceSq = float.MaxValue;

            foreach (var entry in fightLocations.Locations.Concat(sessionLocations.Locations))
            {
                Vector3 entryPosition = new Vector3(entry.Position.x, entry.Position.y, entry.Position.z);
                float distanceSq = (entryPosition - position).sqrMagnitude;
                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    closestEntry = entry;
                }
            }

            return closestEntry;
        }
        public static MethodInfo GetDisplayMessageNotificationMethod() => displayMessageNotificationMethod;
    }
}
