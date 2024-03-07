using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using LateToTheParty.CoroutineExtensions;
using LateToTheParty.Models;
using UnityEngine;
using UnityEngine.AI;

namespace LateToTheParty.Controllers
{
    public class NavMeshController: MonoBehaviour
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool IsUpdatingDoorsObstacles { get; private set; } = false;

        private static Dictionary<Vector3, Vector3> nearestNavMeshPoint = new Dictionary<Vector3, Vector3>();
        private static Dictionary<Door, DoorObstacle> doorObstacles = new Dictionary<Door, DoorObstacle>();
        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.MaxCalcTimePerFrame);
        private static Stopwatch updateTimer = Stopwatch.StartNew();

        private void OnDisable()
        {
            Clear();
        }

        private void LateUpdate()
        {
            if (IsClearing)
            {
                return;
            }

            // Clear all arrays if not in a raid to reset them for the next raid
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                StartCoroutine(Clear());
                return;
            }

            // Ensure enough time has passed since the last check
            if (IsUpdatingDoorsObstacles || (updateTimer.ElapsedMilliseconds < ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.DoorObstacleUpdateTime * 1000))
            {
                return;
            }

            if (doorObstacles.Count() > 0)
            {
                // Update the nav mesh to reflect the door state changes
                StartCoroutine(UpdateDoorObstacles());
                updateTimer.Restart();

                return;
            }

            // Wait until DoorController is done finding doors
            if (InteractiveObjectController.ToggleableInteractiveObjectCount == 0)
            {
                return;
            }

            // Search for all colliders attached to doors
            foreach (Collider collider in FindObjectsOfType<Collider>())
            {
                CheckIfColliderIsDoor(collider);
            }
        }

        public static IEnumerator Clear()
        {
            IsClearing = true;

            if (IsUpdatingDoorsObstacles)
            {
                enumeratorWithTimeLimit.Abort();

                EnumeratorWithTimeLimit conditionWaiter = new EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsUpdatingDoorsObstacles, nameof(IsUpdatingDoorsObstacles), 3000);

                IsUpdatingDoorsObstacles = false;
            }

            // Make sure the obstacle is removed from the map before deleting that record from the dictionary
            foreach (DoorObstacle doorObstacle in doorObstacles.Values)
            {
                doorObstacle.Remove();
            }
            doorObstacles.Clear();

            nearestNavMeshPoint.Clear();

            updateTimer.Restart();

            IsClearing = false;
        }

        public static Player GetNearestPlayer(Vector3 position)
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                return null;
            }

            float closestDistance = float.MaxValue;
            Player closestPlayer = Singleton<GameWorld>.Instance.MainPlayer;

            foreach (Player player in Singleton<GameWorld>.Instance.AllPlayersEverExisted)
            {
                if ((player == null) || (!player.isActiveAndEnabled))
                {
                    continue;
                }

                float distance = Vector3.Distance(position, player.Transform.position);
                if (distance < closestDistance)
                {
                    closestPlayer = player;
                    closestDistance = distance;
                }
            }

            return closestPlayer;
        }

        public static float GetDistanceToNearestLockedDoor(Vector3 position)
        {
            float closestDistance = float.MaxValue;

            foreach (DoorObstacle obstacle in doorObstacles.Values)
            {
                // Make sure the door is locked (by checking if it has a DoorObstacle attached to it)
                if (!obstacle.Position.HasValue)
                {
                    continue;
                }

                float distance = Vector3.Distance(position, obstacle.Position.Value);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }

            return closestDistance;
        }

        public static void CheckIfColliderIsDoor(Collider collider)
        {
            if (collider.gameObject.layer != LayerMaskClass.DoorLayer)
            {
                return;
            }

            GameObject doorObject = collider.transform.parent.gameObject;
            Door door = doorObject.GetComponent<Door>();
            if (door == null)
            {
                return;
            }

            bool isToggleable = InteractiveObjectController.IsToggleableInteractiveObject(door);
            doorObstacles.Add(door, new DoorObstacle(collider, door, isToggleable));
        }

        public static IEnumerator UpdateDoorObstacles()
        {
            IsUpdatingDoorsObstacles = true;

            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(doorObstacles.Keys.ToArray(), UpdateDoorObstacle);

            IsUpdatingDoorsObstacles = false;
        }

        public static void UpdateDoorObstacle(Door door)
        {
            doorObstacles[door].Update();
        }

        public static Vector3? FindNearestNavMeshPosition(Vector3 position, float searchDistance)
        {
            if (nearestNavMeshPoint.ContainsKey(position))
            {
                return nearestNavMeshPoint[position];
            }

            if (NavMesh.SamplePosition(position, out NavMeshHit sourceNearestPoint, searchDistance, NavMesh.AllAreas))
            {
                nearestNavMeshPoint.Add(position, sourceNearestPoint.position);
                return sourceNearestPoint.position;
            }

            return null;
        }

        public static PathAccessibilityData GetPathAccessibilityData(Vector3 sourcePosition, Vector3 targetPosition, string targetPositionName)
        {
            PathAccessibilityData accessibilityData = new PathAccessibilityData();

            // Draw a sphere around the loot item (white = accessibility is undetermined)
            if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.OutlineLoot)
            {
                Vector3[] targetCirclePoints = PathRender.GetSpherePoints
                (
                    targetPosition,
                    ConfigController.Config.Debug.LootPathVisualization.LootOutlineRadius,
                    ConfigController.Config.Debug.LootPathVisualization.PointsPerCircle
                );
                accessibilityData.LootOutlineData = new PathVisualizationData(targetPositionName + "_itemOutline", targetCirclePoints, Color.white);
            }

            // Find the nearest NavMesh point to the source position. If one can't be found, give up. 
            Vector3? sourceNearestPoint = FindNearestNavMeshPosition(sourcePosition, ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshSearchMaxDistancePlayer);
            if (!sourceNearestPoint.HasValue)
            {
                return accessibilityData;
            }

            // Find the nearest NavMesh point to the target position. If one can't be found, give up. 
            Vector3? targetNearestPoint = FindNearestNavMeshPosition(targetPosition, ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshSearchMaxDistanceLoot);
            if (!targetNearestPoint.HasValue)
            {
                return accessibilityData;
            }

            // Try to find a path using the NavMesh from the source position to the target position (using the nearest NavMesh points found above)
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(sourceNearestPoint.Value, targetNearestPoint.Value, NavMesh.AllAreas, path);

            // Modify the path vertices so they're off the ground
            Vector3[] pathPoints = new Vector3[path.corners.Length];
            float heightOffset = ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshHeightOffsetComplete;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                pathPoints[i] = new Vector3(path.corners[i].x, path.corners[i].y + heightOffset, path.corners[i].z);
            }

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                // Lower the path vertices so they're clearly separate from complete paths when drawn in the game
                for (int i = 0; i < pathPoints.Length; i++)
                {
                    pathPoints[i].y = path.corners[i].y + ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshHeightOffsetIncomplete;
                }

                if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.DrawIncompletePaths)
                {
                    accessibilityData.PathData = new PathVisualizationData(targetPositionName + "_path", pathPoints, Color.white);

                    // Draw a sphere around the target NavMesh point
                    Vector3 targetNavMeshPosition = new Vector3
                    (
                        targetNearestPoint.Value.x,
                        targetNearestPoint.Value.y + ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshHeightOffsetIncomplete,
                        targetNearestPoint.Value.z
                    );
                    Vector3[] targetCirclePoints = PathRender.GetSpherePoints
                    (
                        targetNavMeshPosition,
                        ConfigController.Config.Debug.LootPathVisualization.CollisionPointRadius,
                        ConfigController.Config.Debug.LootPathVisualization.PointsPerCircle
                    );
                    accessibilityData.LastNavPointOutline = new PathVisualizationData(targetPositionName + "_targetNavMeshPoint", targetCirclePoints, Color.yellow);
                }

                return accessibilityData;
            }

            // Draw the path in the game
            Vector3[] endLine = new Vector3[] { pathPoints.Last(), targetPosition };
            if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.DrawCompletePaths)
            {
                accessibilityData.PathData = new PathVisualizationData(targetPositionName + "_path", pathPoints, Color.blue);
            }

            // Check for obstacles between the last NavMesh point (determined above) and the actual target position
            float distToNavMesh = Vector3.Distance(targetPosition, pathPoints.Last());
            Vector3 direction = targetPosition - pathPoints.Last();
            RaycastHit[] targetRaycastHits = Physics.RaycastAll(pathPoints.Last(), direction, distToNavMesh, LayerMaskClass.HighPolyWithTerrainMask);

            // Draw boxes enveloping the colliders for all obstacles between the two points
            if
            (
                ConfigController.Config.Debug.LootPathVisualization.Enabled
                && ConfigController.Config.Debug.LootPathVisualization.OutlineObstacles
                && !ConfigController.Config.Debug.LootPathVisualization.OnlyOutlineFilteredObstacles
            )
            {
                for (int ray = 0; ray < targetRaycastHits.Length; ray++)
                {
                    Vector3[] boundingBoxPoints = PathRender.GetBoundingBoxPoints(targetRaycastHits[ray].collider.bounds);
                    accessibilityData.BoundingBoxes.Add(new PathVisualizationData(targetPositionName + "_boundingBox" + ray, boundingBoxPoints, Color.magenta));

                    /*LoggingController.LogInfo(
                        targetPositionName
                        + " Collider: "
                        + targetRaycastHits[ray].collider.name
                        + " (Bounds Size: "
                        + targetRaycastHits[ray].collider.bounds.size.ToString()
                        + ")"
                    );*/
                }
            }

            // Filter obstacles to remove ones we don't care about
            RaycastHit[] targetRaycastHitsFiltered = targetRaycastHits
                .Where(r => r.collider.bounds.size.y > ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshObstacleMinHeight)
                .Where(r => r.collider.attachedRigidbody == null)
                .Where(r => r.collider.bounds.Volume() > ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshObstacleMinVolume)
                .ToArray();
            
            // After filtering, draw spheres at all collision points and outline all obstacles
            if (targetRaycastHitsFiltered.Length > 0)
            {
                for (int ray = 0; ray < targetRaycastHitsFiltered.Length; ray++)
                {
                    if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.OutlineObstacles)
                    {
                        Vector3[] boundingBoxPoints = PathRender.GetBoundingBoxPoints(targetRaycastHitsFiltered[ray].collider.bounds);
                        accessibilityData.BoundingBoxes.Add(new PathVisualizationData(targetPositionName + "_boundingBoxFiltered" + ray, boundingBoxPoints, Color.red));
                    }

                    if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.ShowObstacleCollisionPoints)
                    {
                        Vector3[] circlepoints = PathRender.GetSpherePoints
                        (
                            targetRaycastHitsFiltered[ray].point,
                            ConfigController.Config.Debug.LootPathVisualization.CollisionPointRadius,
                            ConfigController.Config.Debug.LootPathVisualization.PointsPerCircle
                        );
                        accessibilityData.RaycastHitMarkers.Add(new PathVisualizationData(targetPositionName + "_ray" + ray, circlepoints, Color.red));
                    }

                    /*LoggingController.LogInfo(
                        targetPositionName
                        + " Collider: "
                        + targetRaycastHitsFiltered[ray].collider.name
                        + " (Bounds Size: "
                        + targetRaycastHitsFiltered[ray].collider.bounds.size.ToString()
                        + ")"
                    );*/
                }

                // Draw a line from the last NavMesh point (determined above) and the actual target position
                if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.ShowObstacleCollisionPoints)
                {
                    accessibilityData.PathEndPointData = new PathVisualizationData(targetPositionName + "_end", endLine, Color.red);
                }
                return accessibilityData;
            }

            // Draw a line from the last NavMesh point (determined above) and the actual target position
            if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.DrawCompletePaths)
            {
                accessibilityData.PathEndPointData = new PathVisualizationData(targetPositionName + "_end", endLine, Color.green);
            }

            // Update accessibility and the color of the sphere around the item
            accessibilityData.IsAccessible = true;
            if (accessibilityData.LootOutlineData != null)
            {
                accessibilityData.LootOutlineData.LineColor = Color.green;
            }

            return accessibilityData;
        }
    }
}
