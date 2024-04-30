using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class ShootClass : BaseNodeClass
    {
        public ShootClass(BotOwner owner)
            : base(owner)
        {
            SAIN = owner.GetComponent<SAINComponentClass>();
            Shoot = new BotShoot(owner);
        }

        private readonly BotShoot Shoot;
        private BotOwner BotOwner => botOwner_0;

        private readonly SAINComponentClass SAIN;

        private float changeAimTimer;

        public override void Update()
        {
            if (SAIN.Player.IsSprintEnabled)
            {
                return;
            }

            SAIN.AimDownSightsController.UpdateADSstatus();
            AimAtEnemy();
        }

        private void AimAtEnemy()
        {
            if (!BotOwner.WeaponManager.HaveBullets)
            {
                SAIN.SelfActions.TryReload();
                return;
            }
            var enemy = SAIN.Enemy;
            if (enemy != null)
            {
                if (enemy.IsVisible)
                {
                    BotOwner.BotLight?.TurnOn(enemy.RealDistance < 30f);
                }

                Vector3? pointToShoot = null;
                if (enemy.IsVisible && enemy.CanShoot)
                {
                    pointToShoot = GetPointToShoot();
                }
                else if (!enemy.IsVisible 
                    && enemy.HeardRecently 
                    && enemy.InLineOfSight)
                {
                    EnemyPlace lastKnown = enemy.KnownPlaces.LastKnownPlace;
                    if (lastKnown != null 
                        && lastKnown.HasSeen)
                    {
                        pointToShoot = lastKnown.Position + Vector3.up;
                    }
                }
                if (pointToShoot != null)
                {
                    Target = pointToShoot.Value;
                    if (BotOwner.AimingData.IsReady && !SAIN.NoBushESP.NoBushESPActive && FriendlyFire.ClearShot)
                    {
                        ReadyToShoot();
                        Shoot.Update();
                    }
                }
            }
            else if (SAIN.CurrentTargetPosition != null)
            {
                float lightOnVisDist = BotOwner.Settings.FileSettings.Look.LightOnVisionDistance;
                float sqrMagnitude = (SAIN.Position - SAIN.CurrentTargetPosition.Value).sqrMagnitude;
                BotOwner.BotLight?.TurnOn(sqrMagnitude < lightOnVisDist * lightOnVisDist);
            }
        }

        protected virtual void ReadyToShoot()
        {
        }

        protected virtual Vector3? GetTarget()
        {
            var enemy = BotOwner.Memory.GoalEnemy;
            if (enemy != null)
            {
                Vector3 value;
                if (enemy.Distance < 10f)
                {
                    value = enemy.GetCenterPart();
                }
                else
                {
                    value = enemy.GetPartToShoot();
                    if (SAINPlugin.LoadedPreset.GlobalSettings.General.HeadShotProtection)
                    {
                        var transform = SAIN?.Enemy?.EnemyPerson?.Transform;
                        if (transform != null && (value - transform.Head).magnitude < 0.1f)
                        {
                            value = transform.Stomach;
                        }
                    }
                }
                return new Vector3?(value);
            }
            Vector3? result = null;
            if (BotOwner.Memory.LastEnemy != null)
            {
                result = new Vector3?(BotOwner.Memory.LastEnemy.CurrPosition + Vector3.up * BotOwner.Settings.FileSettings.Aiming.DANGER_UP_POINT);
            }
            return result;
        }

        protected virtual Vector3? GetPointToShoot()
        {
            Vector3? target = GetTarget();
            if (target != null)
            {
                Target = target.Value;
                BotOwner.AimingData.SetTarget(Target);
                BotOwner.AimingData.NodeUpdate();
                return new Vector3?(Target);
            }
            return null;
        }

        protected Vector3 Target;

        public SAINFriendlyFireClass FriendlyFire => SAIN.FriendlyFireClass;
    }

    public class BotShoot : BaseNodeClass
    {
        public BotShoot(BotOwner bot)
            : base(bot)
        {
        }

        public override void Update()
        {
            if (!this.botOwner_0.WeaponManager.HaveBullets)
            {
                return;
            }
            Vector3 position = this.botOwner_0.GetPlayer.PlayerBones.WeaponRoot.position;
            Vector3 realTargetPoint = this.botOwner_0.AimingData.RealTargetPoint;
            if (this.botOwner_0.ShootData.CheckFriendlyFire(position, realTargetPoint))
            {
                return;
            }
            if (this.botOwner_0.RecoilData.RecoilOffset.sqrMagnitude > 3f)
            {
                return;
            }
            if (this.botOwner_0.ShootData.Shoot())
            {
                if (this.int_0 > this.botOwner_0.WeaponManager.Reload.BulletCount)
                {
                    this.int_0 = this.botOwner_0.WeaponManager.Reload.BulletCount;
                }
                this.int_0 = this.botOwner_0.WeaponManager.Reload.BulletCount;

                this.botOwner_0.Memory.GoalEnemy?.SetLastShootTime();
            }
        }

        private int int_0;
    }
}