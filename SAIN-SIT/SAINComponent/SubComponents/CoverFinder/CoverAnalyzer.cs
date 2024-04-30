using BepInEx.Logging;
using EFT;
using EFT.Interactive;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverAnalyzer : SAINBase, ISAINClass
    {
        public CoverAnalyzer(SAINComponentClass botOwner, CoverFinderComponent coverFinder) : base(botOwner)
        {
            Path = new NavMeshPath();
            CoverFinder = coverFinder;
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        private readonly CoverFinderComponent CoverFinder; 

        public bool CheckCollider(Collider collider, out CoverPoint newPoint)
        {
            newPoint = null;
            NavMeshPath path = new NavMeshPath();
            if (CheckColliderForCover(collider, out Vector3 place, out bool isSafe, path))
            {
                newPoint = new CoverPoint(SAIN, place, collider, path);
                newPoint.SetIsSafePath(isSafe, SAIN);
                return true;
            }
            return false;
        }

        public bool CheckCollider(CoverPoint coverPoint)
        {
            // the closest edge to that farPoint
            NavMeshPath path = new NavMeshPath();
            if (CheckColliderForCover(coverPoint.Collider, out Vector3 place, out bool isSafe, coverPoint.PathToPoint))
            {
                coverPoint.SetIsSafePath(isSafe, SAIN);
                coverPoint.SetPosition(SAIN, place);
            }

            return false;
        }

        private bool CheckColliderForCover(Collider collider, out Vector3 place, out bool isSafe, NavMeshPath pathToPoint)
        {
            // the closest edge to that farPoint
            isSafe = false;

            if (GetPlaceToMove(collider, TargetPoint, BotOwner.Position, out place))
            {
                if (CheckPosition(place) && CheckHumanPlayerVisibility(place))
                {
                    if (CheckPath(place, out isSafe, pathToPoint))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool GetPlaceToMove(Collider collider, Vector3 targetPosition, Vector3 botPosition, out Vector3 place, float navSampleRange = 0.5f)
        {
            const float ExtendLengthThresh = 2f;

            place = Vector3.zero;
            if (collider == null 
                || collider.bounds.size.y < CoverMinHeight 
                || !CheckColliderDirection(collider, targetPosition, botPosition))
            {
                return false;
            }

            Vector3 colliderPos = collider.transform.position;

            // The direction from the target to the collider
            Vector3 colliderDir = (colliderPos - targetPosition).normalized;
            colliderDir.y = 0f;

            if (collider.bounds.size.z > ExtendLengthThresh && collider.bounds.size.x > ExtendLengthThresh)
            {
                float min = Mathf.Min(collider.bounds.size.z, collider.bounds.size.x);
                float multiplier = Mathf.Clamp(1f + (min - ExtendLengthThresh), 1f, 3f);
                colliderDir *= multiplier;
            }

            // a farPoint on opposite side of the target
            Vector3 farPoint = colliderPos + colliderDir;

            // the closest edge to that farPoint
            if (NavMesh.SamplePosition(farPoint, out var hit, navSampleRange, -1))
            {
                place = hit.position;
                return true;
            }
            return false;
        }

        private bool CheckHumanPlayerVisibility(Vector3 point)
        {
            Player closestPlayer = GameWorldHandler.SAINGameWorld?.FindClosestPlayer(out float sqrDist, point);
            if (SAIN.EnemyController.IsHumanPlayerActiveEnemy == false 
                && SAIN.EnemyController.IsHumanPlayerLookAtMe(out Player lookingPlayer) == true 
                && lookingPlayer != null)
            {
                bool VisibleCheckPass = (VisibilityCheck(point, lookingPlayer.Position));

                if (SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder)
                {
                    if (VisibleCheckPass)
                    {
                        // Main Player does not have vision on coverpoint position
                        Logger.LogWarning("PASS");
                    }
                    else
                    {
                        // Main Player has vision
                        Logger.LogWarning("FAIL");
                    }
                }

                return VisibleCheckPass;
            }
            return true;
        }

        private static bool CheckColliderDirection(Collider collider, Vector3 targetPosition, Vector3 botPosition)
        {
            Vector3 pos = collider.transform.position;

            Vector3 directionToTarget = targetPosition - botPosition;
            float targetDist = directionToTarget.magnitude;

            Vector3 directionToCollider = pos - botPosition;
            float colliderDist = directionToCollider.magnitude;

            float dot = Vector3.Dot(directionToTarget.normalized, directionToCollider.normalized);

            if (dot <= 0.33f)
            {
                return true;
            }
            if (dot <= 0.6f)
            {
                return colliderDist < targetDist * 0.75f;
            }
            if (dot <= 0.8f)
            {
                return colliderDist < targetDist * 0.5f;
            }
            return colliderDist < targetDist * 0.25f;
        }

        private bool CheckPosition(Vector3 position)
        {
            if (CoverFinder.SpottedCoverPoints.Count > 0)
            {
                foreach (var point in CoverFinder.SpottedCoverPoints)
                {
                    Vector3 coverPos = point.CoverPoint.GetPosition(SAIN);
                    if (!point.IsValidAgain && point.TooClose(coverPos, position))
                    {
                        return false;
                    }
                }
            }
            if (CheckPositionVsOtherBots(position))
            {
                if ((position - TargetPoint).sqrMagnitude > CoverMinEnemyDist * CoverMinEnemyDist)
                {
                    if (VisibilityCheck(position, TargetPoint))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckPath(Vector3 position, out bool isSafe, NavMeshPath pathToPoint)
        {
            if (pathToPoint == null)
            {
                pathToPoint = new NavMeshPath();
            }
            else
            {
                pathToPoint.ClearCorners();
            }
            if (NavMesh.CalculatePath(OriginPoint, position, -1, pathToPoint) && pathToPoint.status == NavMeshPathStatus.PathComplete)
            {
                if (PathToEnemy(pathToPoint))
                {
                    isSafe = CheckPathSafety(pathToPoint);
                    return true;
                }
            }

            isSafe = false;
            return false;
        }

        private bool CheckPathSafety(NavMeshPath path)
        {
            Vector3 target;
            if (SAIN.HasEnemy)
            {
                target = SAIN.Enemy.EnemyHeadPosition;
            }
            else
            {
                target = TargetPoint + Vector3.up;
            }
            return SAINBotSpaceAwareness.CheckPathSafety(path, target);
        }

        private readonly NavMeshPath Path;

        static bool DebugCoverFinder => SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder;

        private bool PathToEnemy(NavMeshPath path)
        {
            for (int i = 1; i < path.corners.Length - 1; i++)
            {
                var corner = path.corners[i];
                Vector3 cornerToTarget = TargetPoint - corner;
                Vector3 botToTarget = TargetPoint - OriginPoint;
                Vector3 botToCorner = corner - OriginPoint;

                if (cornerToTarget.magnitude < 0.5f)
                {
                    if (DebugCoverFinder)
                    {
                        //DrawDebugGizmos.Ray(OriginPoint, corner - OriginPoint, Color.red, (corner - OriginPoint).magnitude, 0.05f, true, 30f);
                    }

                    return false;
                }

                if (i == 1)
                {
                    if (Vector3.Dot(botToCorner.normalized, botToTarget.normalized) > 0.5f)
                    {
                        if (DebugCoverFinder)
                        {
                            //DrawDebugGizmos.Ray(corner, cornerToTarget, Color.red, cornerToTarget.magnitude, 0.05f, true, 30f);
                        }
                        return false;
                    }
                }
                else if (i < path.corners.Length - 2)
                {
                    Vector3 cornerB = path.corners[i + 1];
                    Vector3 directionToNextCorner = cornerB - corner;

                    if (Vector3.Dot(cornerToTarget.normalized, directionToNextCorner.normalized) > 0.5f)
                    {
                        if (directionToNextCorner.magnitude > cornerToTarget.magnitude)
                        {
                            if (DebugCoverFinder)
                            {
                                //DrawDebugGizmos.Ray(corner, cornerToTarget, Color.red, cornerToTarget.magnitude, 0.05f, true, 30f);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool CheckPositionVsOtherBots(Vector3 position)
        {
            if (SAIN.Squad.Members == null || SAIN.Squad.Members.Count < 2)
            {
                return true;
            }

            const float DistanceToBotCoverThresh = 1f;

            foreach (var member in SAIN.Squad.Members.Values)
            {
                if (member != null && member.BotOwner != BotOwner)
                {
                    CoverPoint currentCover = member.Cover.CurrentCoverPoint;
                    if (currentCover != null)
                    {
                        Vector3 coverPos = currentCover.GetPosition(SAIN);
                        if ((position - coverPos).sqrMagnitude < DistanceToBotCoverThresh * DistanceToBotCoverThresh)
                        {
                            return false;
                        }
                    }
                    else if (member.Cover.FallBackPoint != null)
                    {
                        Vector3 coverPos = member.Cover.FallBackPoint.GetPosition(SAIN);
                        if ((position - coverPos).sqrMagnitude < DistanceToBotCoverThresh * DistanceToBotCoverThresh)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool VisibilityCheck(Vector3 position, Vector3 target)
        {
            const float offset = 0.1f;

            if (CheckRayCast(position, target, 5f))
            {
                Vector3 enemyDirection = target - position;
                enemyDirection = enemyDirection.normalized * offset;

                Quaternion right = Quaternion.Euler(0f, 90f, 0f);
                Vector3 rightPoint = right * enemyDirection;
                rightPoint += position;

                if (CheckRayCast(rightPoint, target, 5f))
                {
                    Quaternion left = Quaternion.Euler(0f, -90f, 0f);
                    Vector3 leftPoint = left * enemyDirection;
                    leftPoint += position;

                    if (CheckRayCast(leftPoint, target, 5f))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
         
        private static bool CheckRayCast(Vector3 point, Vector3 target, float distance = 3f)
        {
            point.y += 0.5f;
            target.y += 1.25f;
            Vector3 direction = target - point;
            return Physics.Raycast(point, direction, distance, LayerMaskClass.HighPolyWithTerrainMask);
        }

        private static float CoverMinHeight => CoverFinderComponent.CoverMinHeight;
        private Vector3 OriginPoint => CoverFinder.OriginPoint;
        private Vector3 TargetPoint => CoverFinder.TargetPoint;
        private float CoverMinEnemyDist => CoverFinderComponent.CoverMinEnemyDist;
    }
}