using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Layers.Combat.Solo;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using UnityEngine;

namespace SAIN.Layers.Combat.Squad
{
    internal class SuppressAction : SAINAction
    {
        public SuppressAction(BotOwner bot) : base(bot, nameof(SuppressAction))
        {
        }

        public override void Update()
        {
            var enemy = SAIN.Enemy;

            bool needReload = !BotOwner.WeaponManager.HaveBullets || SAIN.Decision.SelfActionDecisions.LowOnAmmo();
            if (!BotOwner.WeaponManager.HaveBullets || SAIN.Decision.SelfActionDecisions.LowOnAmmo())
            {
                SAIN.SelfActions.TryReload();
            }
            if (enemy != null)
            {
                if (enemy.IsVisible && enemy.CanShoot)
                {
                    BotOwner.StopMove();
                    Shoot.Update();
                }
                else if (CanSeeLastCorner(out var pos))
                {
                    BotOwner.StopMove();

                    bool hasMachineGun = SAIN.Info.WeaponInfo.IWeaponClass == IWeaponClass.machinegun;
                    if (hasMachineGun 
                        && BotOwner.GetPlayer?.IsInPronePose == false
                        && SAIN.Mover.Prone.ShallProne(true))
                    {
                        SAIN.Mover.Prone.SetProne(true);
                    }

                    SAIN.Steering.LookToPoint(pos.Value);

                    if (!BotOwner.WeaponManager.HaveBullets)
                    {
                        SAIN.SelfActions.TryReload();
                    }
                    else if (
                        WaitShootTimer < Time.time 
                        && SAIN.Shoot(true, true, SAINComponentClass.EShootReason.SquadSuppressing))
                    {
                        enemy.EnemyIsSuppressed = true;
                        float waitTime = hasMachineGun ? 0.1f : 0.5f;
                        WaitShootTimer = Time.time + (waitTime * Random.Range(0.75f, 1.25f));
                    }
                }
                else
                {
                    SAIN.Shoot(false);
                    if (needReload)
                    {
                        SAIN.SelfActions.TryReload();
                    }
                    if (!BotOwner.ShootData.Shooting)
                    {
                        SAIN.Steering.SteerByPriority();
                    }
                    if (_recalcPathTimer < Time.time)
                    {
                        if (SAIN.Mover.GoToPoint(enemy.EnemyPosition, out _))
                        {
                            _recalcPathTimer = Time.time + 4f;
                        }
                        else
                        {
                            _recalcPathTimer = Time.time + 1f;
                        }
                    }
                }
            }
        }

        private float _recalcPathTimer;

        private float WaitShootTimer;

        private bool CanSeeLastCorner(out Vector3? pos)
        {
            pos = SAIN.Enemy?.Path.LastCornerToEnemy;
            return SAIN.Enemy?.Path.CanSeeLastCornerToEnemy == true;
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
        }
    }
}