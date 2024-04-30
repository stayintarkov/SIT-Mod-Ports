using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using SAIN.Components;
using UnityEngine;
using SAIN.Helpers;

namespace SAIN.Layers.Combat.Solo
{
    internal class RushEnemyAction : SAINAction
    {
        public RushEnemyAction(BotOwner bot) : base(bot, nameof(RushEnemyAction))
        {
        }

        private float TryJumpTimer;

        public override void Update()
        {
            SAIN.Mover.SetTargetPose(1f);
            SAIN.Mover.SetTargetMoveSpeed(1f);
            Shoot.Update();

            if (SAIN.Enemy == null)
            {
                SAIN.Steering.SteerByPriority(true);
                return;
            }

            if (SAIN.Enemy.InLineOfSight)
            {
                if (SAIN.Info.PersonalitySettings.CanJumpCorners)
                {
                    if (_shallBunnyHop)
                    {
                        SAIN.Mover.TryJump();
                    }
                    else if (TryJumpTimer < Time.time)
                    {
                        TryJumpTimer = Time.time + 5f;
                        if (EFTMath.RandomBool(SAIN.Info.PersonalitySettings.JumpCornerChance))
                        {
                            if (!_shallBunnyHop && EFTMath.RandomBool(5))
                            {
                                _shallBunnyHop = true;
                            }
                            SAIN.Mover.TryJump();
                        }
                    }
                }

                SAIN.Mover.Sprint(false);

                if (SAIN.Enemy.IsVisible && SAIN.Enemy.CanShoot)
                {
                    SAIN.Steering.SteerByPriority();
                }
                else
                {
                    SAIN.Steering.LookToEnemy(SAIN.Enemy);
                }
                return;
            }

            Vector3[] EnemyPath = SAIN.Enemy.PathToEnemy.corners;
            Vector3 EnemyPos = SAIN.Enemy.EnemyPosition;
            if (NewDestTimer < Time.time)
            {
                NewDestTimer = Time.time + 1f;
                Vector3 Destination = EnemyPos;
                /*
                if (SAIN.Info.Personality == Personality.GigaChad)
                {
                    if (PathToEnemy.Length > 2)
                    {
                        Vector3 SecondToLastCorner = PathToEnemy[PathToEnemy.Length - 3];
                        Vector3 LastCornerDirection = LastCorner - SecondToLastCorner;
                        Vector3 AddToLast = LastCornerDirection.normalized * 3f;
                        Vector3 widePush = LastCorner + AddToLast;
                        if (SAIN.Mover.CanGoToPoint(widePush, out Vector3 PointToGo))
                        {
                            Destination = PointToGo;
                        }
                    }
                    else
                    {
                        Destination = Vector3.Lerp(EnemyPos, LastCorner, 0.75f);
                    }
                }
                else
                {
                    if (PathToEnemy.Length > 2)
                    {
                        Vector3 SecondToLastCorner = PathToEnemy[PathToEnemy.Length - 3];
                        Vector3 LastCornerDirection = LastCorner - SecondToLastCorner;
                        Vector3 AddToLast = LastCornerDirection.normalized * 0.15f;
                        Vector3 shortPush = LastCorner - AddToLast;
                        if (SAIN.Mover.CanGoToPoint(shortPush, out Vector3 PointToGo))
                        {
                            Destination = PointToGo;
                        }
                    }
                    else
                    {
                        Destination = LastCorner;
                    }
                }
                */
                if (SAIN.Enemy.Path.PathDistance > 5f)
                {
                    BotOwner.BotRun.Run(Destination, false);
                }
                else
                {
                    SAIN.Mover.GoToPoint(Destination, out bool calculating);
                }
            }

            if (_shallTryJump && TryJumpTimer < Time.time && SAIN.Enemy.Path.PathDistance > 5f)
            {
                var corner = SAIN.Enemy?.LastCornerToEnemy;
                if (corner != null)
                {
                    float distance = (corner.Value - BotOwner.Position).magnitude;
                    if (distance < 0.5f)
                    {
                        TryJumpTimer = Time.time + 3f;
                        if (EFTMath.RandomBool(SAIN.Info.PersonalitySettings.JumpCornerChance))
                        {
                            SAIN.Mover.TryJump();
                        }
                    }
                }
            }

        }

        private bool _shallBunnyHop = false;
        private float NewDestTimer = 0f;
        private Vector3? PushDestination;

        public override void Start()
        {
            _shallTryJump = SAIN.Info.PersonalitySettings.CanJumpCorners 
                && SAIN.Decision.CurrentSquadDecision != SquadDecision.PushSuppressedEnemy
                && EFTMath.RandomBool(SAIN.Info.PersonalitySettings.JumpCornerChance);

            _shallBunnyHop = false;
        }

        bool _shallTryJump = false;

        public override void Stop()
        {
        }
    }
}