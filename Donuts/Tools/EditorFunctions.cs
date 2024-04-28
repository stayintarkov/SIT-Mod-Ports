using BepInEx.Logging;
using EFT;
using EFT.Communications;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Donuts.DonutsPlugin;

#pragma warning disable IDE0007, IDE0044

namespace Donuts
{
    internal class EditorFunctions
    {
        internal static ManualLogSource Logger
        {
            get; private set;
        }

        public EditorFunctions()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(EditorFunctions));
            }

        }


        internal static void DeleteSpawnMarker()
        {
            // Check if any of the required objects are null
            if (Donuts.DonutComponent.gameWorld == null)
            {
                Logger.LogDebug("IBotGame Not Instantiated or gameWorld is null.");
                return;
            }

            //need to be able to see it to delete it
            if (DonutsPlugin.DebugGizmos.Value)
            {
                //temporarily combine fightLocations and sessionLocations so i can find the closest entry
                var combinedLocations = Donuts.DonutComponent.fightLocations.Locations.Concat(Donuts.DonutComponent.sessionLocations.Locations).ToList();

                // if for some reason its empty already return
                if (combinedLocations.Count == 0)
                {
                    return;
                }

                // Get the closest spawn marker to the player
                var closestEntry = combinedLocations.OrderBy(x => Vector3.Distance(Donuts.DonutComponent.gameWorld.MainPlayer.Position, new Vector3(x.Position.x, x.Position.y, x.Position.z))).FirstOrDefault();

                // Check if the closest entry is null
                if (closestEntry == null)
                {
                    var displayMessageNotificationMethod = Gizmos.GetDisplayMessageNotificationMethod();
                    if (displayMessageNotificationMethod != null)
                    {
                        var txt = $"Donuts: The Spawn Marker could not be deleted because closest entry could not be found";
                        displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Default, Color.grey });
                    }
                    return;
                }

                // Remove the entry from the list if the distance from the player is less than 5m
                if (Vector3.Distance(Donuts.DonutComponent.gameWorld.MainPlayer.Position, new Vector3(closestEntry.Position.x, closestEntry.Position.y, closestEntry.Position.z)) < 5f)
                {
                    // check which list the entry is in and remove it from that list
                    if (Donuts.DonutComponent.fightLocations.Locations.Count > 0 &&
                        Donuts.DonutComponent.fightLocations.Locations.Contains(closestEntry))
                    {
                        Donuts.DonutComponent.fightLocations.Locations.Remove(closestEntry);
                    }
                    else if (Donuts.DonutComponent.sessionLocations.Locations.Count > 0 &&
                        Donuts.DonutComponent.sessionLocations.Locations.Contains(closestEntry))
                    {
                        Donuts.DonutComponent.sessionLocations.Locations.Remove(closestEntry);
                    }

                    // Remove the timer if it exists from the list of hotspotTimer in DonutComponent.groupedHotspotTimers[closestEntry.GroupNum]
                    if (Donuts.DonutComponent.groupedHotspotTimers.ContainsKey(closestEntry.GroupNum))
                    {
                        var timerList = Donuts.DonutComponent.groupedHotspotTimers[closestEntry.GroupNum];
                        var timer = timerList.FirstOrDefault(x => x.Hotspot == closestEntry);

                        if (timer != null)
                        {
                            timerList.Remove(timer);
                        }
                        else
                        {
                            // Handle the case where no timer was found
                            Logger.LogDebug("Donuts: No matching timer found to delete.");
                        }
                    }
                    else
                    {
                        Logger.LogDebug("Donuts: GroupNum does not exist in groupedHotspotTimers.");
                    }

                    // Display a message to the player
                    var displayMessageNotificationMethod = Gizmos.GetDisplayMessageNotificationMethod();
                    if (displayMessageNotificationMethod != null)
                    {
                        var txt = $"Donuts: Spawn Marker Deleted for \n {closestEntry.Name}\n SpawnType: {closestEntry.WildSpawnType}\n Position: {closestEntry.Position.x}, {closestEntry.Position.y}, {closestEntry.Position.z}";
                        displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Default, Color.yellow });
                    }

                    // Edit the DonutComponent.drawnCoordinates and gizmoSpheres list to remove the objects
                    var coordinate = new Vector3(closestEntry.Position.x, closestEntry.Position.y, closestEntry.Position.z);
                    Gizmos.drawnCoordinates.Remove(coordinate);

                    var sphere = Gizmos.gizmoSpheres.FirstOrDefault(x => x.transform.position == coordinate);
                    Gizmos.gizmoSpheres.Remove(sphere);

                    // Destroy the sphere game object in the actual game world
                    if (sphere != null)
                    {
                        GameWorld.Destroy(sphere);
                    }
                }
            }
        }

        internal static void CreateSpawnMarker()
        {
            // Check if any of the required objects are null
            if (DonutComponent.gameWorld == null)
            {
                Logger.LogDebug("IBotGame Not Instantiated or gameWorld is null.");
                return;
            }

            // Create new Donuts.Entry
            Entry newEntry = new Entry
            {
                Name = spawnName.Value,
                GroupNum = groupNum.Value,
                MapName = DonutComponent.maplocation,
                WildSpawnType = wildSpawns.Value,
                MinDistance = minSpawnDist.Value,
                MaxDistance = maxSpawnDist.Value,
                MaxRandomNumBots = maxRandNumBots.Value,
                SpawnChance = spawnChance.Value,
                BotTimerTrigger = botTimerTrigger.Value,
                BotTriggerDistance = botTriggerDistance.Value,
                Position = new Position
                {
                    x = DonutComponent.gameWorld.MainPlayer.Position.x,
                    y = DonutComponent.gameWorld.MainPlayer.Position.y,
                    z = DonutComponent.gameWorld.MainPlayer.Position.z
                },

                MaxSpawnsBeforeCoolDown = maxSpawnsBeforeCooldown.Value,
                IgnoreTimerFirstSpawn = ignoreTimerFirstSpawn.Value,
                MinSpawnDistanceFromPlayer = minSpawnDistanceFromPlayer.Value
            };

            // Add new entry to sessionLocations.Locations list since we adding new ones

            // Check if Locations is null
            if (DonutComponent.sessionLocations.Locations == null)
            {
                DonutComponent.sessionLocations.Locations = new List<Entry>();
            }

            DonutComponent.sessionLocations.Locations.Add(newEntry);

            // make it testable immediately by adding the timer needed to the groupnum in DonutComponent.groupedHotspotTimers
            if (!DonutComponent.groupedHotspotTimers.ContainsKey(newEntry.GroupNum))
            {
                //create a new list for the groupnum and add the timer to it
                DonutComponent.groupedHotspotTimers.Add(newEntry.GroupNum, new List<HotspotTimer>());
            }

            //create a new timer for the entry and add it to the list
            var timer = new HotspotTimer(newEntry);
            DonutComponent.groupedHotspotTimers[newEntry.GroupNum].Add(timer);

            var txt = $"Donuts: Wrote Entry for {newEntry.Name}\n SpawnType: {newEntry.WildSpawnType}\n Position: {newEntry.Position.x}, {newEntry.Position.y}, {newEntry.Position.z}";
            var displayMessageNotificationMethod = Gizmos.GetDisplayMessageNotificationMethod();
            if (displayMessageNotificationMethod != null)
            {
                displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Default, Color.yellow });
            }


        }

        internal static void WriteToJsonFile()
        {
            // Check if any of the required objects are null
            if (Donuts.DonutComponent.gameWorld == null)
            {
                Logger.LogDebug("IBotGame Not Instantiated or gameWorld is null.");
                return;
            }

            string dllPath = Assembly.GetExecutingAssembly().Location;
            string directoryPath = Path.GetDirectoryName(dllPath);
            string jsonFolderPath = Path.Combine(directoryPath, "patterns");
            string json = string.Empty;
            string fileName = string.Empty;

            //check if saveNewFileOnly is true then we use the sessionLocations object to serialize.  Otherwise we use combinedLocations
            if (saveNewFileOnly.Value)
            {
                // take the sessionLocations object only and serialize it to json
                json = JsonConvert.SerializeObject(Donuts.DonutComponent.sessionLocations, Formatting.Indented);
                fileName = Donuts.DonutComponent.maplocation + "_" + UnityEngine.Random.Range(0, 1000) + "_NewLocOnly.json";
            }
            else
            {
                //combine the fightLocations and sessionLocations objects into one variable
                FightLocations combinedLocations = new Donuts.FightLocations
                {
                    Locations = Donuts.DonutComponent.fightLocations.Locations.Concat(Donuts.DonutComponent.sessionLocations.Locations).ToList()
                };

                json = JsonConvert.SerializeObject(combinedLocations, Formatting.Indented);
                fileName = Donuts.DonutComponent.maplocation + "_" + UnityEngine.Random.Range(0, 1000) + "_All.json";
            }

            //write json to file with filename == Donuts.DonutComponent.maplocation + random number
            string jsonFilePath = Path.Combine(jsonFolderPath, fileName);
            File.WriteAllText(jsonFilePath, json);

            var txt = $"Donuts: Wrote Json File to: {jsonFilePath}";
            var displayMessageNotificationMethod = Gizmos.GetDisplayMessageNotificationMethod();
            if (displayMessageNotificationMethod != null)
            {
                displayMessageNotificationMethod.Invoke(null, new object[] { txt, ENotificationDurationType.Long, ENotificationIconType.Default, Color.yellow });
            }
        }
    }
}
