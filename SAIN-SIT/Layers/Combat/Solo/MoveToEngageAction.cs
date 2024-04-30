using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Solo
{
    internal class MoveToEngageAction : SAINAction
    {
        public MoveToEngageAction(BotOwner bot) : base(bot, nameof(MoveToEngageAction))
        {
        }

        private float RecalcPathTimer;

        public override void Update()
        {
            SAINEnemy enemy = SAIN.Enemy;
            if (enemy == null)
            {
                return;
            }

            SAIN.Mover.SetTargetPose(1f);
            SAIN.Mover.SetTargetMoveSpeed(1f);

            if (CheckShoot(enemy))
            {
                SAIN.Steering.SteerByPriority();
                Shoot.Update();
                return;
            }

            if (SAIN.Decision.SelfActionDecisions.LowOnAmmo(0.66f))
            {
                SAIN.SelfActions.TryReload();
            }

            Vector3? LastSeenPosition = enemy.LastSeenPosition;
            Vector3 movePos;
            if (LastSeenPosition != null)
            {
                movePos = LastSeenPosition.Value;
            }
            else if (enemy.TimeSinceSeen < 5f)
            {
                movePos = enemy.EnemyPosition;
            }
            else
            {
                SAIN.Steering.SteerByPriority();
                Shoot.Update();
                return;
            }

            float distance = enemy.RealDistance;
            if (distance > 40f && !BotOwner.Memory.IsUnderFire)
            {
                if (RecalcPathTimer < Time.time)
                {
                    RecalcPathTimer = Time.time + 4f;
                    BotOwner.BotRun.Run(movePos, false);
                }
            }
            else
            {
                SAIN.Mover.Sprint(false);

                if (RecalcPathTimer < Time.time)
                {
                    RecalcPathTimer = Time.time + 4f;
                    BotOwner.MoveToEnemyData.TryMoveToEnemy(movePos);
                }

                if (!SAIN.Steering.SteerByPriority(false))
                {
                    SAIN.Steering.LookToMovingDirection();
                    //SAIN.Steering.LookToPoint(movePos + Vector3.up * 1f);
                }
            }
        }

        private bool CheckShoot(SAINEnemy enemy)
        {
            float distance = enemy.RealDistance;
            bool enemyLookAtMe = enemy.EnemyLookingAtMe;
            float EffDist = SAIN.Info.WeaponInfo.EffectiveWeaponDistance;

            if (enemy.IsVisible)
            {
                if (enemyLookAtMe)
                {
                    return true;
                }
                if (distance <= EffDist && enemy.CanShoot)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
        }
    }
}