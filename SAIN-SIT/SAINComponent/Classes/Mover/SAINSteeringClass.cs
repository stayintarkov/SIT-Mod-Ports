using EFT;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static RootMotion.FinalIK.AimPoser;
using static UnityEngine.UI.GridLayoutGroup;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINSteeringController : MonoBehaviour
    {
        public void Init(SAINComponentClass sain)
        {
            SAIN = sain;
            SteeringClass = new SAINSteeringClass(sain);
        }

        private SAINComponentClass SAIN;
        public SAINSteeringClass SteeringClass { get; private set; }

        public bool SteerByPriority(bool lookRandom = true) => SteeringClass.SteerByPriority(lookRandom);
        public void LookToPoint(Vector3 point) => SteeringClass.LookToPoint(point);
        public void LookToPoint(Vector3? point) => SteeringClass.LookToPoint(point);
    }

    public class SAINSteeringClass : SAINBase, ISAINClass
    {
        public SAINSteeringClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        private float updateSteerTimer;

        private SteerPriority UpdateBotSteering(bool skipTimer = false)
        {
            if (skipTimer || updateSteerTimer < Time.time)
            {
                LastSteerPriority = CurrentSteerPriority;
                CurrentSteerPriority = FindSteerPriority();

                float timeAdd = 0.01f;
                if (LastSteerPriority != CurrentSteerPriority)
                {
                    timeAdd = 0.1f;
                }
                updateSteerTimer = Time.time + timeAdd;
            }
            return CurrentSteerPriority;
        }

        public void Dispose()
        {
        }

        private bool SteerRandomToggle;
        private PlaceForCheck LastHeardSound;

        public bool SteerByPriority(bool lookRandom = true)
        {
            SteerRandomToggle = lookRandom;

            UpdateBotSteering();

            HeardSoundSanityCheck();

            if (BotOwner.ShootData.Shooting && CurrentSteerPriority != SteerPriority.Shooting)
            {
                CurrentSteerPriority = SteerPriority.Shooting;
            }

            switch (CurrentSteerPriority)
            {
                case SteerPriority.None: 
                    if (SAIN.HasEnemy && SAIN.Enemy.Path.LastCornerToEnemy != null)
                    {
                        LookToPathToEnemy();
                    }
                    else if (SAIN.EnemyController.ClosestHeardEnemy != null)
                    {
                        LookToPoint(SAIN.EnemyController.ClosestHeardEnemy.LastKnownPosition);
                    }
                    break;

                case SteerPriority.Shooting:
                    // Steering is handled by aim code in eft manually, so do nothing here.
                    break;

                case SteerPriority.Enemy:
                    LookToEnemy();
                    break;

                case SteerPriority.UnderFire:
                    LookToUnderFirePos();
                    break;

                case SteerPriority.LastHit:
                    LookToLastHitPos();
                    break;

                case SteerPriority.ClosestHeardEnemy:
                    var closestHeardEnemy = SAIN.EnemyController.ClosestHeardEnemy;
                    if (closestHeardEnemy != null)
                    {
                        LookToPoint(closestHeardEnemy.LastHeardPosition);
                    }
                    else
                    {
                        LookToRandomPosition();
                    }
                    break;

                case SteerPriority.LastKnownLocation:
                    if (SAIN.Enemy != null)
                    {
                        EnemyPlace lastKnownPlace = SAIN.Enemy.KnownPlaces.LastKnownPlace;
                        if (lastKnownPlace != null)
                        {
                            Vector3? blindCornerToEnemy = SAIN.Enemy.Path.BlindCornerToEnemy;
                            if (blindCornerToEnemy != null && (blindCornerToEnemy.Value - SAIN.Transform.Head).sqrMagnitude > 1f)
                            {
                                LookToPoint(blindCornerToEnemy.Value, 150f);
                                break;
                            }
                            if (lastKnownPlace.Position != null)
                            {
                                LookToPoint(lastKnownPlace.Position.Value, 150f);
                                break;
                            }
                        }
                    }
                    break;

                case SteerPriority.LastSeenEnemy:
                    LookToLastKnownEnemyPosition();
                    break;

                case SteerPriority.Hear:
                    if (LastHeardSound != null)
                    {
                        LookToHearPos(LastHeardSound.Position);
                    }
                    else if (SAINPlugin.DebugMode)
                    {
                        Logger.LogError("Cannot look toward null PlaceForCheck.");
                    }
                    break;

                case SteerPriority.LastSeenEnemyLong:
                    LookToLastKnownEnemyPosition();
                    break;

                case SteerPriority.MoveDirection:
                    LookToMovingDirection();
                    break;

                case SteerPriority.Search:
                    // Search steering is handled in the Search Layer
                    break;

                case SteerPriority.Random:
                    LookToRandomPosition();
                    break;
            }

            return CurrentSteerPriority != SteerPriority.None && CurrentSteerPriority != SteerPriority.Random;
        }

        private void HeardSoundSanityCheck()
        {
            if (CurrentSteerPriority == SteerPriority.Hear && LastHeardSound == null)
            {
                if (SAINPlugin.DebugMode)
                {
                    Logger.LogDebug("Bot was told to steer toward something they heard, but the place to check is null.");
                }
                UpdateBotSteering(true);
            }
        }

        // How long a bot will look in the direction they were shot from instead of other places
        private readonly float Steer_LastHitTime = 1f;
        // How long a bot will look at where they last saw an enemy instead of something they hear
        private readonly float Steer_TimeSinceLocationKnown_Threshold = 8f;
        // How long a bot will look at where they last saw an enemy instead of something they hear
        private readonly float Steer_TimeSinceSeen_Short = 4f;
        // How long a bot will look at where they last saw an enemy if they don't hear any other threats
        private readonly float Steer_TimeSinceSeen_Long = 15f;
        // How far a sound can be for them to react by looking toward it.
        private readonly float Steer_HeardSound_Dist = 75f;
        // How old a sound can be, in seconds, for them to react by looking toward it.
        private readonly float Steer_HeardSound_Age = 3f;

        public SteerPriority FindSteerPriority()
        {
            // return values are ordered by priority, so the targets get less "important" as they descend down this function.
            if (LookToAimTarget())
            {
                return SteerPriority.Shooting;
            }
            if (EnemyVisible())
            {
                return SteerPriority.Enemy;
            }
            if (BotOwner.Memory.IsUnderFire)
            {
                return SteerPriority.UnderFire;
            }
            if (Time.time - BotOwner.Memory.LastTimeHit < Steer_LastHitTime)
            {
                return SteerPriority.LastHit;
            }
            EnemyPlace lastKnownPlace = SAIN.Enemy?.KnownPlaces?.LastKnownPlace;
            if (lastKnownPlace != null 
                && lastKnownPlace.TimePositionUpdated < Steer_TimeSinceLocationKnown_Threshold 
                && !lastKnownPlace.HasSeen)
            {
                return SteerPriority.LastKnownLocation;
            }
            if (SAIN.Enemy?.TimeSinceSeen < Steer_TimeSinceSeen_Short && SAIN.Enemy.Seen)
            {
                return SteerPriority.LastSeenEnemy;
            }
            LastHeardSound = BotOwner.BotsGroup.YoungestFastPlace(BotOwner, Steer_HeardSound_Dist, Steer_HeardSound_Age);
            if (LastHeardSound != null)
            {
                return SteerPriority.Hear;
            }
            if (SAIN.Enemy?.TimeSinceSeen < Steer_TimeSinceSeen_Long && SAIN.Enemy.Seen)
            {
                return SteerPriority.LastSeenEnemyLong;
            }
            if (SAIN.Memory.Decisions.Main.Current == SoloDecision.Investigate)
            {
                return SteerPriority.MoveDirection;
            }
            if (SAIN.Memory.Decisions.Main.Current == SoloDecision.Search)
            {
                return SteerPriority.Search;
            }
            if (SAIN.EnemyController.FindClosestHeardEnemy() != null)
            {
                return SteerPriority.ClosestHeardEnemy;
            }
            if (SteerRandomToggle)
            {
                return SteerPriority.Random;
            }
            return SteerPriority.None;
        }


        public SteerPriority CurrentSteerPriority { get; private set; } = SteerPriority.None;
        public SteerPriority LastSteerPriority { get; private set; } = SteerPriority.None;

        private bool LookToVisibleSound()
        {
            return false;
        }

        public bool LookToLastKnownEnemyPosition()
        {
            SAINEnemy enemy = SAIN.Enemy;
            if (enemy == null || enemy.IsVisible)
            {
                return false;
            }

            EnemyPlace lastKnownPlace = enemy.KnownPlaces.LastKnownPlace;
            if (lastKnownPlace != null)
            {
                Vector3? blindCornerToEnemy = enemy.Path.BlindCornerToEnemy;
                if (blindCornerToEnemy != null && (blindCornerToEnemy.Value - SAIN.Transform.Head).sqrMagnitude > 1f)
                {
                    LookToPoint(blindCornerToEnemy.Value, 150f);
                    return true;
                }
            }

            Vector3? LastKnownPosition = enemy.LastKnownPosition;
            if (LastKnownPosition != null)
            {
                Vector3 pos = LastKnownPosition.Value;
                LookToPoint(pos, 150f);
                return true;
            }
            return false;
        }

        public void LookToMovingDirection()
        {
            BotOwner.Steering.LookToMovingDirection();
        }

        public void LookToPoint(Vector3 point, float rotateSpeed = -1)
        {
            if (rotateSpeed < 0)
            {
                BotOwner.Steering.LookToPoint(point);
            }
            else
            {
                BotOwner.Steering.LookToPoint(point, rotateSpeed);
            }
        }

        public void LookToPoint(Vector3? point, float rotateSpeed = -1)
        {
            if (point != null)
            {
                LookToPoint(point.Value, rotateSpeed);
            }
        }

        public void LookToDirection(Vector3 direction, bool flat)
        {
            if (flat)
            {
                direction.y = 0f;
            }
            Vector3 pos = SAIN.Transform.Head + direction;
            LookToPoint(pos);
        }

        public bool LookToAimTarget()
        {
            if (BotOwner.WeaponManager.Reload?.Reloading == true)
            {
                return false;
            }
            if (SAIN.Enemy?.IsVisible == true && SAIN.Enemy?.CanShoot == true)
            {
                return true;
            }
            if (BotOwner.ShootData.Shooting)
            {
                return true;
            }
            return false;
        }

        public bool EnemyVisible()
        {
            SAINEnemy enemy = SAIN.Enemy;

            if (enemy != null 
                && enemy.IsVisible)
            {
                return true;
            }

            if (enemy != null 
                && enemy.InLineOfSight 
                && enemy.TimeSinceSeen < 3f)
            {
                return true;
            }

            return false;
        }

        public void LookToEnemy(SAINEnemy enemy)
        {
            if (enemy != null)
            {
                LookToPoint(enemy.EnemyPosition + Vector3.up);
            }
        }

        public void LookToEnemy()
        {
            LookToEnemy(SAIN.Enemy);
        }

        public void LookToUnderFirePos()
        {
            var pos = SAIN.Memory.UnderFireFromPosition;
            pos.y += 1f;
            LookToPoint(pos);
        }

        public void LookToHearPos(Vector3 soundPos, bool visionCheck = false)
        {
            if (visionCheck)
            {
                soundPos.y += 0.1f;
                Vector3 headPos = SAIN.Transform.Head;
                var direction = soundPos - headPos;

                if (!Physics.Raycast(headPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    LookToPoint(soundPos);
                    return;
                }
            }

            float turnSpeed = SAIN.HasEnemy ? 200f : 100f;
            if ((soundPos - SAIN.Position).sqrMagnitude > 75f * 75f)
            {
                LookToPoint(soundPos, turnSpeed);
                return;
            }

            if (HearPath == null)
            {
                HearPath = new NavMeshPath();
            }
            if (LastSoundTimer < Time.time || (LastSoundCheckPos - soundPos).magnitude > 1f)
            {
                LastSoundTimer = Time.time + 1f;
                LastSoundCheckPos = soundPos;
                LastSoundHeardCorner = Vector3.zero;

                HearPath.ClearCorners();
                if (NavMesh.CalculatePath(SAIN.Position, soundPos, -1, HearPath))
                {
                    if (HearPath.corners.Length > 2)
                    {
                        for (int i = HearPath.corners.Length - 1; i >= 0; i--)
                        {
                            Vector3 corner = HearPath.corners[i];
                            corner.y += 1f;
                            Vector3 headPos = SAIN.Transform.Head;
                            Vector3 cornerDir = corner - headPos;
                            if (!Physics.Raycast(headPos, cornerDir.normalized, cornerDir.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                            {
                                LastSoundHeardCorner = corner;
                                break;
                            }
                        }
                    }
                }
            }

            if (LastSoundHeardCorner != Vector3.zero)
            {
                LookToPoint(LastSoundHeardCorner, turnSpeed);
            }
            else
            {
                LookToPoint(soundPos, turnSpeed);
            }
        }

        private float LastSoundTimer;
        private Vector3 LastSoundCheckPos;
        private Vector3 LastSoundHeardCorner;
        private NavMeshPath HearPath;

        public void LookToLastHitPos()
        {
            var pos = BotOwner.Memory.LastHitPos;
            pos.y += 1f;
            LookToPoint(pos, 150f);
        }

        private bool LookRandom;
        private bool LookRandom2;

        public void LookToRandomPosition()
        {
            if (RandomLookTimer < Time.time)
            {
                RandomLookTimer = Time.time + 3f * Random.Range(0.66f, 1.33f);
                Vector3 pointToLook = Vector3.zero;

                LookRandom = !LookRandom;
                if (LookRandom)
                {
                    var Mask = LayerMaskClass.HighPolyWithTerrainMask;
                    var headPos = SAIN.Transform.Head;
                    float pointDistance = 0f;
                    for (int i = 0; i < 10; i++)
                    {
                        var random = Random.onUnitSphere * 5f;
                        random.y = 0f;
                        if (!Physics.Raycast(headPos, random, out var hit, 4f, Mask))
                        {
                            pointToLook = random + headPos;
                            break;
                        }
                        else
                        {
                            if (hit.distance > pointDistance)
                            {
                                pointDistance = hit.distance;
                                pointToLook = hit.point;
                            }
                        }
                    }
                }
                else
                {
                    LookRandom2 = !LookRandom2;
                    if (LookRandom2 && BotOwner.Memory.LastEnemy != null)
                    {
                        pointToLook = BotOwner.Memory.LastEnemy.PersonalLastPos;
                    }
                    else if (SAIN.CurrentTargetPosition != null)
                    {
                        pointToLook = SAIN.CurrentTargetPosition.Value;
                    }
                }
                if (pointToLook == Vector3.zero)
                {
                    pointToLook = _lastRandomLookPos;
                }
                if (pointToLook != Vector3.zero)
                {
                    _lastRandomLookPos = pointToLook;
                    LookToPoint(pointToLook, Random.Range(50f, 90f));
                }
            }
        }

        private Vector3 _lastRandomLookPos;

        public bool LookToPathToEnemy()
        {
            var enemy = SAIN.Enemy;
            if (enemy != null && !enemy.IsVisible)
            {
                if (enemy.Seen && enemy.TimeSinceSeen < 10f)
                {
                    if (enemy.PathToEnemy.corners.Length > 1)
                    {
                        Vector3 pos = enemy.PathToEnemy.corners[1];
                        pos += Vector3.up * 1f;
                        LookToPoint(pos, 80f);
                        return true;
                    }
                }
            }
            return false;
        }

        private float RandomLookTimer = 0f;
    }

    public enum SteerPriority
    {
        None,
        Shooting,
        Enemy,
        Hear,
        LastSeenEnemy,
        LastSeenEnemyLong,
        Random,
        LastHit,
        UnderFire,
        MoveDirection,
        LastKnownLocation,
        ClosestHeardEnemy,
        Search
    }
}