using System.Collections.Generic;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using UnityEngine;
using UnityEngine.AI;
using static Donuts.DonutComponent;

#pragma warning disable IDE0007, IDE0044

namespace Donuts
{
    internal class SpawnChecks
    {
        #region spawnchecks

        internal static async Task<Vector3?> GetValidSpawnPosition(Entry hotspot, Vector3 coordinate, int maxSpawnAttempts)
        {
            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                Vector3 spawnPosition = GenerateRandomSpawnPosition(hotspot, coordinate);

                if (NavMesh.SamplePosition(spawnPosition, out var navHit, 2f, NavMesh.AllAreas))
                {
                    spawnPosition = navHit.position;

                    if (SpawnChecks.IsValidSpawnPosition(spawnPosition, hotspot))
                    {
#if DEBUG
                        DonutComponent.Logger.LogDebug("Found spawn position at: " + spawnPosition);
#endif
                        return spawnPosition;
                    }
                }

                await Task.Delay(1);
            }

            return null;
        }

        private static Vector3 GenerateRandomSpawnPosition(Entry hotspot, Vector3 coordinate)
        {
            float randomX = Random.Range(-hotspot.MaxDistance, hotspot.MaxDistance);
            float randomZ = Random.Range(-hotspot.MaxDistance, hotspot.MaxDistance);

            return new Vector3(coordinate.x + randomX, coordinate.y, coordinate.z + randomZ);
        }

        internal static bool IsValidSpawnPosition(Vector3 spawnPosition, Entry hotspot)
        {
            if (spawnPosition != null && hotspot != null)
            {
                bool validPosition = !IsSpawnPositionInsideWall(spawnPosition) &&
                                    !IsSpawnPositionInPlayerLineOfSight(spawnPosition) &&
                                    !IsSpawnInAir(spawnPosition) &&
                                    !IsMinSpawnDistanceFromPlayerTooShort(spawnPosition, hotspot);

                // Only check proximity to other bots if the corresponding setting is enabled
                if (DonutsPlugin.globalMinSpawnDistanceFromOtherBotsBool.Value)
                {
                    validPosition &= !IsPositionTooCloseToOtherBots(spawnPosition, hotspot);
                }
                return validPosition;
            }
            return false;
        }

        internal static bool IsSpawnPositionInPlayerLineOfSight(Vector3 spawnPosition)
        {
            //add try catch for when player is null at end of raid
            try
            {
                foreach (var player in playerList)
                {
                    if (player == null || player.HealthController == null)
                    {
                        continue;
                    }
                    if (!player.HealthController.IsAlive)
                    {
                        continue;
                    }
                    Vector3 playerPosition = player.MainParts[BodyPartType.head].Position;
                    Vector3 direction = (playerPosition - spawnPosition).normalized;
                    float distance = Vector3.Distance(spawnPosition, playerPosition);
                    RaycastHit hit;
                    if (!Physics.Raycast(spawnPosition, direction, out hit, distance, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        // No objects found between spawn point and player
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        internal static bool IsSpawnPositionInsideWall(Vector3 position)
        {
            // Check if any game object parent has the name "WALLS" in it
            Vector3 boxSize = new Vector3(1f, 1f, 1f);
            Collider[] colliders = Physics.OverlapBox(position, boxSize, Quaternion.identity, LayerMaskClass.LowPolyColliderLayer);

            foreach (var collider in colliders)
            {
                Transform currentTransform = collider.transform;
                while (currentTransform != null)
                {
                    if (currentTransform.gameObject.name.ToUpper().Contains("WALLS"))
                    {
                        return true;
                    }
                    currentTransform = currentTransform.parent;
                }
            }

            return false;
        }

        /*private bool IsSpawnPositionObstructed(Vector3 position)
        {
            Ray ray = new Ray(position, Vector3.up);
            float distance = 5f;

            if (Physics.Raycast(ray, out RaycastHit hit, distance, LayerMaskClass.TerrainMask))
            {
                // If the raycast hits a collider, it means the position is obstructed
                return true;
            }

            return false;
        }*/
        internal static bool IsSpawnInAir(Vector3 position)
        {
            // Raycast down and determine if the position is in the air or not
            Ray ray = new Ray(position, Vector3.down);
            float distance = 10f;

            if (Physics.Raycast(ray, out RaycastHit hit, distance, LayerMaskClass.HighPolyWithTerrainMask))
            {
                // If the raycast hits a collider, it means the position is not in the air
                return false;
            }
            return true;
        }

        private static float GetMinDistanceFromPlayer(Entry hotspot)
        {
            if (DonutsPlugin.globalMinSpawnDistanceFromPlayerBool.Value)
            {
                switch (hotspot.MapName.ToLower())
                {
                    case "bigmap": return DonutsPlugin.globalMinSpawnDistanceFromPlayerCustoms.Value;
                    case "factory4_day": return DonutsPlugin.globalMinSpawnDistanceFromPlayerFactory.Value;
                    case "factory4_night": return DonutsPlugin.globalMinSpawnDistanceFromPlayerFactory.Value;
                    case "tarkovstreets": return DonutsPlugin.globalMinSpawnDistanceFromPlayerStreets.Value;
                    case "sandbox": return DonutsPlugin.globalMinSpawnDistanceFromPlayerGroundZero.Value;
                    case "rezervbase": return DonutsPlugin.globalMinSpawnDistanceFromPlayerReserve.Value;
                    case "lighthouse": return DonutsPlugin.globalMinSpawnDistanceFromPlayerLighthouse.Value;
                    case "shoreline": return DonutsPlugin.globalMinSpawnDistanceFromPlayerShoreline.Value;
                    case "woods": return DonutsPlugin.globalMinSpawnDistanceFromPlayerWoods.Value;
                    case "laboratory": return DonutsPlugin.globalMinSpawnDistanceFromPlayerLaboratory.Value;
                    case "interchange": return DonutsPlugin.globalMinSpawnDistanceFromPlayerInterchange.Value;
                    default: return hotspot.MinSpawnDistanceFromPlayer;
                }
            }
            else
            {
                return hotspot.MinSpawnDistanceFromPlayer;
            }
        }

        private static float GetMinDistanceFromOtherBots(Entry hotspot)
        {
            switch (hotspot.MapName.ToLower())
            {
                case "bigmap": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsCustoms.Value;
                case "factory4_day": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsFactory.Value;
                case "factory4_night": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsFactory.Value;
                case "tarkovstreets": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsStreets.Value;
                case "sandbox": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsGroundZero.Value;
                case "rezervbase": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsReserve.Value;
                case "lighthouse": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsLighthouse.Value;
                case "shoreline": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsShoreline.Value;
                case "woods": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsWoods.Value;
                case "laboratory": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsLaboratory.Value;
                case "interchange": return DonutsPlugin.globalMinSpawnDistanceFromOtherBotsInterchange.Value;
                default: return 0f;
            }
        }

        internal static bool IsMinSpawnDistanceFromPlayerTooShort(Vector3 position, Entry hotspot)
        {
            float minDistanceFromPlayer = GetMinDistanceFromPlayer(hotspot);

            foreach (var player in playerList)
            {
                if (player == null || player.HealthController == null)
                {
                    continue;
                }

                if (!player.HealthController.IsAlive)
                {
                    continue;
                }

                if ((player.Position - position).sqrMagnitude < (minDistanceFromPlayer * minDistanceFromPlayer))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsPositionTooCloseToOtherBots(Vector3 position, Entry hotspot)
        {
            float minDistanceFromOtherBots = GetMinDistanceFromOtherBots(hotspot);
            List<Player> players = Singleton<GameWorld>.Instance.AllAlivePlayersList;

            foreach (var player in players)
            {
                if (player == null || !player.HealthController.IsAlive || player.IsYourPlayer)

                    continue;

                if ((player.Position - position).sqrMagnitude < minDistanceFromOtherBots * minDistanceFromOtherBots)
                {
                    return true;
					
                }
            }
            return false;
        }


        #endregion
    }
}
