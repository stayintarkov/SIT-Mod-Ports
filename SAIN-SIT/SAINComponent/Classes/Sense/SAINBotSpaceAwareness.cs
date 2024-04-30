using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Layers.Combat.Solo;
using SAIN.SAINComponent;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes
{
    public sealed class FlankRoute
    {
        public Vector3 FlankPoint;
        public Vector3 FlankPoint2;
        public NavMeshPath FirstPath;
        public NavMeshPath SecondPath;
        public NavMeshPath ThirdPath;
    }

    public class SAINBotSpaceAwareness : SAINBase, ISAINClass
    {
        public SAINBotSpaceAwareness(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (SAIN.HasEnemy && _findFlankTimer < Time.time && SAIN.Enemy?.EnemyPlayer?.IsYourPlayer == true)
            {
                _findFlankTimer = Time.time + 1f;
                // CurrentFlankRoute = FindFlankRoute();
                if (CurrentFlankRoute != null)
                {
                    Logger.NotifyDebug("Found Flank Route"); 
                    DrawDebug(CurrentFlankRoute);
                }
            }
        }

        public FlankRoute CurrentFlankRoute { get; private set; }

        private float _findFlankTimer;

        public void Dispose()
        {
        }

        public FlankRoute FindFlankRoute()
        {
            SAINEnemy enemy = SAIN.Enemy;
            if (enemy == null)
            {
                return null;
            }

            FlankRoute flankRoute = null;

            Vector3 enemyPosition = SAIN.Enemy.EnemyPosition;
            Vector3 botPosition = SAIN.Position;

            Vector3? middleNode = FindMiddlePoint(enemy.Path.PathToEnemy, enemy.Path.PathDistance, out int index);

            if (middleNode != null)
            {
                Vector3 directionFromMiddle = enemyPosition - middleNode.Value;

                SideTurn randomTurn = EFTMath.RandomBool() ? SideTurn.right : SideTurn.left;

                flankRoute = FindFlank(
                    middleNode.Value,
                    directionFromMiddle,
                    botPosition,
                    enemy,
                    randomTurn);

                if (flankRoute != null)
                {
                    return flankRoute;
                }

                randomTurn = randomTurn == SideTurn.left ? SideTurn.right : SideTurn.left;

                flankRoute = FindFlank(
                    middleNode.Value,
                    directionFromMiddle,
                    botPosition,
                    enemy,
                    randomTurn);

                if (flankRoute != null)
                {
                    return flankRoute;
                }
            }
            return null;
        }

        private IEnumerator CalculateFlank()
        {
            yield return null;
        }

        private FlankRoute FindFlank(Vector3 middleNode, Vector3 directionFromMiddle, Vector3 botPosition, SAINEnemy enemy, SideTurn sideTurn)
        {
            Vector3 flankDirection1 = Vector.Rotate90(directionFromMiddle, sideTurn);
            if (SamplePointAndCheckPath(flankDirection1, middleNode, out NavMeshPath path) 
                && SamplePointAndCheckPath(flankDirection1 * 0.5f, enemy.EnemyPosition, out NavMeshPath path2))
            {
                Vector3 flankPoint1 = path.corners[path.corners.Length - 1];
                Vector3 flankPoint2 = path2.corners[path2.corners.Length - 1];

                NavMeshPath pathToEnemy = enemy.Path.PathToEnemy;

                NavMeshPath flankPath = new NavMeshPath();
                if (NavMesh.CalculatePath(botPosition, flankPoint1, -1, flankPath)
                    && ArePathsDifferent(pathToEnemy, flankPath))
                {
                    NavMeshPath flankPath2 = new NavMeshPath();
                    if (NavMesh.CalculatePath(flankPoint1, flankPoint2, -1, flankPath2)
                        && ArePathsDifferent(pathToEnemy, flankPath2)
                        && ArePathsDifferent(flankPath, flankPath2) 
                        && CheckPathSafety(flankPath2, enemy.EnemyHeadPosition))
                    {
                        NavMeshPath flankPath3 = new NavMeshPath();
                        if (NavMesh.CalculatePath(flankPoint2, enemy.EnemyPosition, -1, flankPath3)
                            && ArePathsDifferent(pathToEnemy, flankPath3)
                            && ArePathsDifferent(flankPath, flankPath3)
                            && ArePathsDifferent(flankPath2, flankPath3))
                        {
                            return new FlankRoute
                            {
                                FlankPoint = flankPoint1,
                                FlankPoint2 = flankPoint2,
                                FirstPath = flankPath,
                                SecondPath = flankPath2,
                                ThirdPath = flankPath3,
                            };
                        }
                    }
                }
            }
            return null;
        }

        private void DrawDebug(FlankRoute route)
        {
            if (SAINPlugin.DebugMode && SAINPlugin.DrawDebugGizmos)
            {
                if (_timer < Time.time)
                {
                    _timer = Time.time + 60f;
                    list1.DrawTempPath(route.FirstPath, true, RandomColor, RandomColor, 0.1f, 60f);
                    list2.DrawTempPath(route.SecondPath, true, RandomColor, RandomColor, 0.1f, 60f);
                    list3.DrawTempPath(route.ThirdPath, true, RandomColor, RandomColor, 0.1f, 60f);
                    DebugGizmos.Ray(route.FlankPoint, Vector3.up, RandomColor, 3f, 0.2f, true, 60f);
                    DebugGizmos.Ray(route.FlankPoint2, Vector3.up, RandomColor, 3f, 0.2f, true, 60f);
                    DebugGizmos.Line(route.FirstPath.corners[0] + Vector3.up, route.ThirdPath.corners[route.SecondPath.corners.Length - 1] + Vector3.up, Color.white, 0.1f, true, 60f);
                }
            }
            else
            {
            }
        }

        private Color RandomColor = DebugGizmos.RandomColor;

        float _timer;

        DebugGizmos.DrawLists list1 = new DebugGizmos.DrawLists(Color.red, Color.red, "flankroute1");
        DebugGizmos.DrawLists list2 = new DebugGizmos.DrawLists(Color.blue, Color.blue, "flankroute2");
        DebugGizmos.DrawLists list3 = new DebugGizmos.DrawLists(Color.blue, Color.blue, "flankroute3");

        public static bool ArePathsDifferent(NavMeshPath path1, NavMeshPath path2, float minRatio = 0.5f, float sqrDistCheck = 0.05f)
        {
            Vector3[] path1Corners = path1.corners;
            int path1Length = path1Corners.Length;
            Vector3[] path2Corners = path2.corners;
            int path2Length = path2Corners.Length;

            int sameCount = 0;
            for (int i = 0; i < path1Length; i++)
            {
                Vector3 node = path1Corners[i];

                if (i < path2Length)
                {
                    Vector3 node2 = path2Corners[i];
                    if (node.IsEqual(node2, sqrDistCheck))
                    {
                        sameCount++;
                    }
                }
            }
            float ratio = (float)sameCount / (float)path1Length;
            //Logger.LogDebug($"Result = [{ratio <= minRatio}]Path 1 length: {path1.corners.Length} Path2 length: {path2.corners.Length} Same Node Count: {sameCount} ratio: {ratio}");
            return ratio <= minRatio;
        }

        private static bool SamplePointAndCheckPath(Vector3 point, Vector3 origin, out NavMeshPath path)
        {
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                path = new NavMeshPath();
                return NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, path);
            }
            path = null;
            return false;
        }

        private static Vector3? FindMiddlePoint(NavMeshPath path, float pathLength, out int index)
        {
            index = 0;
            if (path.corners.Length < 3)
            {
                return null;
            }

            const float maxDistance = 50f;
            Vector3 endGoal = path.corners[path.corners.Length - 1];
            float currentLength = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Vector3 cornerA = path.corners[i];
                Vector3 cornerB = path.corners[i + 1];
                currentLength += (cornerA - cornerB).magnitude;
                if (currentLength >= pathLength / 2)
                {
                    if ((cornerA - endGoal).sqrMagnitude <= maxDistance * maxDistance)
                    {
                        index = i;
                        return new Vector3?(cornerA);
                    }
                }
            }
            return null;
        }

        public static bool CheckPathSafety(NavMeshPath path, Vector3 enemyHeadPos, float ratio = 0.5f)
        {
            Vector3[] corners = path.corners;
            int max = corners.Length - 1;

            for (int i = 0; i < max; i++)
            {
                Vector3 pointA = corners[i];
                Vector3 pointB = corners[i + 1];

                float ratioResult = RaycastAlongDirection(pointA, pointB, enemyHeadPos);

                if (ratioResult < ratio)
                {
                    return false;
                }
            }

            return true;
        }

        public static float GetSegmentLength(int segmentCount, Vector3 direction, float minLength, float maxLength, out float dirMagnitude, out int countResult, int maxIterations = 10)
        {
            dirMagnitude = direction.magnitude;
            countResult = 0;
            if (dirMagnitude < minLength)
            {
                return 0f;
            }

            float segmentLength = 0f;
            for (int i = 0; i < maxIterations; i++)
            {
                if (segmentCount > 0)
                {
                    segmentLength = dirMagnitude / segmentCount;
                }
                if (segmentLength > maxLength)
                {
                    segmentCount++;
                }
                if (segmentLength < minLength)
                {
                    segmentCount--;
                }
                if (segmentLength <= maxLength && segmentLength >= minLength)
                {
                    break;
                }
                if (segmentCount <= 0)
                {
                    break;
                }
            }
            countResult = segmentCount;
            return segmentLength;
        }

        private static float RaycastAlongDirection(Vector3 pointA, Vector3 pointB, Vector3 rayOrigin, int SegmentCount = 5)
        {
            const float RayHeight = 1.1f;
            const float debugExpireTime = 12f;
            const float MinSegLength = 1f;
            const float MaxSegLength = 5f;

            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;

            Vector3 direction = pointB - pointA;

            // Make sure we aren't raycasting too often, set to MinSegLength for each raycast along a path
            float segmentLength = GetSegmentLength(SegmentCount, direction, MinSegLength, MaxSegLength, out float dirMagnitude, out int testCount);

            if (segmentLength <= 0 || testCount <= 0)
            {
                return 1f;
            }

            Vector3 dirNormal = direction.normalized;
            Vector3 dirSegment = dirNormal * segmentLength;

            Vector3 testPoint = pointA + (Vector3.up * RayHeight);

            int i = 0;
            int hits = 0;

            for (i = 0; (i < testCount); i++)
            {
                testPoint += dirSegment;

                Vector3 enemyDir = testPoint - rayOrigin;
                float rayLength = enemyDir.magnitude;

                Color debugColor = Color.red;
                if (Physics.Raycast(rayOrigin, enemyDir, rayLength, mask))
                {
                    debugColor = Color.white;
                    hits++;
                }

                if (SAINPlugin.EditorDefaults.DebugDrawSafePaths)
                {
                    DebugGizmos.Line(rayOrigin, testPoint, debugColor, 0.025f, true, debugExpireTime, true);
                    DebugGizmos.Sphere(testPoint, 0.05f, Color.green, true, debugExpireTime);
                    //DebugGizmos.Sphere(rayOrigin, 0.1f, Color.red, true, debugExpireTime);
                }
            }

            float result = hits / i;
            return result;
        }
    }
}
