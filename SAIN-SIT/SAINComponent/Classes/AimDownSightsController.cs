using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class AimDownSightsController : SAINBase, ISAINClass
    {
        public AimDownSightsController(SAINComponentClass sain) : base(sain)
        {
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

        public void UpdateADSstatus()
        {
            bool shallADS = ShallAimDownSights(SAIN.CurrentTargetPosition);
            SetADS(shallADS);
        }

        public bool ShallAimDownSights(Vector3? targetPosition = null)
        {
            bool result = false;
            EAimDownSightsStatus status = EAimDownSightsStatus.None;
            if (targetPosition != null)
            {
                status = GetADSStatus(targetPosition.Value);
            }
            float timeSinceChangeDecision = SAIN.Decision.TimeSinceChangeDecision;
            switch (status)
            {
                case EAimDownSightsStatus.None:
                    break;

                case EAimDownSightsStatus.HoldInCover:
                    result = timeSinceChangeDecision > 3f;
                    break;

                case EAimDownSightsStatus.StandAndShoot:
                    result = SAIN.Enemy != null && SAIN.Enemy.RealDistance > 10f;
                    break;

                case EAimDownSightsStatus.EnemyVisible:
                    result = true;
                    break;

                case EAimDownSightsStatus.Sprinting:
                    result = false;
                    break;

                case EAimDownSightsStatus.MovingToCover:
                    result = SAIN.ManualShootReason == SAINComponentClass.EShootReason.WalkToCoverSuppress;
                    break;

                case EAimDownSightsStatus.Suppressing:
                    result = SAIN.ManualShootReason == SAINComponentClass.EShootReason.SquadSuppressing;
                    break;

                case EAimDownSightsStatus.DogFight:
                    result = SAIN.Enemy != null && SAIN.Enemy.RealDistance > 10;
                    break;

                case EAimDownSightsStatus.EnemySeenRecent:
                    result = true;
                    break;

                case EAimDownSightsStatus.EnemyHeardRecent:
                    result = true;
                    break;

                default:
                    break;
            }

            LastADSstatus = CurrentADSstatus;
            CurrentADSstatus = status;
            return result;
        }

        public bool SetADS(bool value)
        {
            var shootController = BotOwner.WeaponManager.ShootController;
            if (shootController != null && shootController.IsAiming != value)
            {
                shootController?.SetAim(value);
                AimingDownSights = value;
                return true;
            }
            return false;
        }

        public EAimDownSightsStatus CurrentADSstatus { get; private set; }
        public EAimDownSightsStatus LastADSstatus { get; private set; }

        public EAimDownSightsStatus GetADSStatus(Vector3 targetPosition)
        {
            var enemy = SAIN.Enemy;
            var decisions = SAIN.Memory.Decisions;
            float sqrMagToTarget = (targetPosition - SAIN.Position).sqrMagnitude;

            EAimDownSightsStatus result;
            if (SAIN.Player.IsSprintEnabled)
            {
                result = EAimDownSightsStatus.Sprinting;
            }
            else if (enemy != null && enemy.CanShoot && enemy.IsVisible && enemy.RealDistance > 20f)
            {
                result = EAimDownSightsStatus.EnemyVisible;
            }
            else if (enemy != null && enemy.Seen && enemy.TimeSinceSeen < 5)
            {
                result = EAimDownSightsStatus.EnemySeenRecent;
            }
            else if (enemy != null && enemy.Heard && enemy.TimeSinceHeard < 5)
            {
                result = EAimDownSightsStatus.EnemyHeardRecent;
            }
            else if (decisions.Squad.Current == SquadDecision.Suppress && SAIN.ManualShootReason == SAINComponentClass.EShootReason.SquadSuppressing)
            {
                result = EAimDownSightsStatus.Suppressing;
            }
            else
            {
                switch (decisions.Main.Current)
                {
                    case SoloDecision.RunToCover:
                    case SoloDecision.WalkToCover:
                        result = EAimDownSightsStatus.MovingToCover;
                        break;

                    case SoloDecision.HoldInCover:
                        result = EAimDownSightsStatus.HoldInCover;
                        break;

                    case SoloDecision.StandAndShoot:
                        result = EAimDownSightsStatus.StandAndShoot;
                        break;

                    case SoloDecision.DogFight:
                        result = EAimDownSightsStatus.DogFight;
                        break;

                    default:
                        result = EAimDownSightsStatus.None;
                        break;
                }
            }
            return result;
        }

        public enum EAimDownSightsStatus
        {
            None = 0,
            HoldInCover = 1,
            StandAndShoot = 2,
            EnemyVisible = 3,
            Sprinting = 4,
            MovingToCover = 5,
            Suppressing = 6,
            DogFight = 7,
            EnemySeenRecent = 8,
            EnemyHeardRecent = 9,
        }

        public bool AimingDownSights { get; private set; }

    }
}
