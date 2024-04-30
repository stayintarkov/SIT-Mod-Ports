using BepInEx.Logging;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class EnemyDecisionClass : SAINBase, ISAINClass
    {
        public EnemyDecisionClass(SAINComponentClass sain) : base(sain)
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

        public bool GetDecision(out SoloDecision Decision)
        {
            if (BotOwner.Memory.IsUnderFire)
            {

            }

            SAINEnemy enemy = SAIN.Enemy;
            if (enemy == null)
            {
                Decision = SoloDecision.None;
                return false;
            }

            SAIN.Decision.GoalTargetDecisions.IgnorePlaceTarget = false;

            var CurrentDecision = SAIN.Memory.Decisions.Main.Current;

            if (StartDogFightAction(enemy))
            {
                Decision = SoloDecision.DogFight;
            }
            else if (StartThrowGrenade(enemy))
            {
                Decision = SoloDecision.ThrowGrenade;
            }
            else if (StartMoveToEngage(enemy))
            {
                Decision = SoloDecision.MoveToEngage;
            }
            else if (StartStandAndShoot(enemy))
            {
                if (CurrentDecision != SoloDecision.StandAndShoot)
                {
                    SAIN.Info.CalcHoldGroundDelay();
                }
                Decision = SoloDecision.StandAndShoot;
            }
            else if (StartRushEnemy(enemy))
            {
                Decision = SoloDecision.RushEnemy;
            }
            else if (StartSearch(enemy))
            {
                if (CurrentDecision != SoloDecision.Search)
                {
                    SAIN.Info.CalcTimeBeforeSearch();
                }
                Decision = SoloDecision.Search;
            }
            else if (StartShiftCover(enemy))
            {
                Decision = SoloDecision.ShiftCover;
            }
            else if (StartMoveToCover())
            {
                Decision = SoloDecision.WalkToCover;

                if (StartRunForCover())
                {
                    Decision = SoloDecision.RunToCover;
                }
            }
            else if (StartHoldInCover())
            {
                Decision = SoloDecision.HoldInCover;
            }
            else
            {
                Decision = SoloDecision.DebugNoDecision;
            }

            if (Decision != SoloDecision.WalkToCover && Decision != SoloDecision.RunToCover)
            {
                StartRunCoverTimer = 0f;
            }

            return true;
        }

        private bool StayInCoverToSelfCare()
        {
            SelfDecision currentSelf = SAIN.Memory.Decisions.Self.Current;
            SoloDecision currentMain = SAIN.Memory.Decisions.Main.Current;

            if (currentMain == SoloDecision.HoldInCover)
            {

            }
            if (currentSelf != SelfDecision.None && StartHoldInCover())
            {

            }
            return false;
        }

        private static readonly float GrenadeMaxEnemyDistance = 100f;

        private bool StartRunUnknownShooter()
        {

            return false;
        }

        private bool StartThrowGrenade(SAINEnemy enemy)
        {
            if (!GlobalSettings.General.BotsUseGrenades)
            {
                var core = BotOwner.Settings.FileSettings.Core;
                if (core.CanGrenade)
                {
                    core.CanGrenade = false;
                }
                return false;
            }

            var grenades = BotOwner.WeaponManager.Grenades;
            if (!grenades.HaveGrenade)
            {
                return false;
            }
            if (!enemy.IsVisible && enemy.TimeSinceSeen > SAIN.Info.FileSettings.Grenade.TimeSinceSeenBeforeThrow && enemy.RealDistance < GrenadeMaxEnemyDistance)
            {
                if (grenades.ReadyToThrow && grenades.AIGreanageThrowData.IsUpToDate())
                {
                    grenades.DoThrow();
                    return true;
                }
                grenades.CanThrowGrenade(enemy.EnemyPosition + Vector3.up);
                return false;
            }
            return false;
        }

        private static readonly float RushEnemyMaxPathDistance = 10f;
        private static readonly float RushEnemyMaxPathDistanceSprint = 25f;
        private static readonly float RushEnemyLowAmmoRatio = 0.5f;

        private bool StartRushEnemy(SAINEnemy enemy)
        {
            if (SAIN.Info.PersonalitySettings?.CanRushEnemyReloadHeal == true)
            {
                if (enemy != null 
                    && !SAIN.Decision.SelfActionDecisions.LowOnAmmo(RushEnemyLowAmmoRatio))
                {
                    bool inRange = false;
                    if (enemy.Path.PathDistance < RushEnemyMaxPathDistanceSprint
                        && BotOwner?.CanSprintPlayer == true)
                    {
                        inRange = true;
                    }
                    else if (enemy.Path.PathDistance < RushEnemyMaxPathDistance)
                    {
                        inRange = true;
                    }

                    if (inRange
                        && SAIN.Memory.HealthStatus != ETagStatus.Dying)
                    {
                        var enemyStatus = enemy.EnemyStatus;
                        if (enemyStatus.EnemyIsReloading || enemyStatus.EnemyIsHealing || enemyStatus.EnemyHasGrenadeOut)
                        {
                            return true;
                        }
                        ETagStatus enemyHealth = enemy.EnemyPlayer.HealthStatus;
                        if (enemyHealth == ETagStatus.Dying)
                        {
                            return true;
                        }
                        else if (enemyHealth == ETagStatus.BadlyInjured && enemy.EnemyPlayer.IsInPronePose)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private CoverSettings CoverSettings => SAINPlugin.LoadedPreset.GlobalSettings.Cover;
        private float ShiftCoverChangeDecisionTime => CoverSettings.ShiftCoverChangeDecisionTime;
        private float ShiftCoverTimeSinceSeen => CoverSettings.ShiftCoverTimeSinceSeen;
        private float ShiftCoverTimeSinceEnemyCreated => CoverSettings.ShiftCoverTimeSinceEnemyCreated;
        private float ShiftCoverNoEnemyResetTime => CoverSettings.ShiftCoverNoEnemyResetTime;
        private float ShiftCoverNewCoverTime => CoverSettings.ShiftCoverNewCoverTime;
        private float ShiftCoverResetTime => CoverSettings.ShiftCoverResetTime;

        private bool StartShiftCover(SAINEnemy enemy)
        {
            if (SAIN.Info.PersonalitySettings.CanShiftCoverPosition == false)
            {
                return false;
            }
            if (SAIN.Suppression.IsSuppressed)
            {
                return false;
            }

            if (ContinueShiftCover())
            {
                return true;
            }

            var CurrentDecision = SAIN.Memory.Decisions.Main.Current;

            if (CurrentDecision == SoloDecision.HoldInCover && SAIN.Info.PersonalitySettings.CanShiftCoverPosition)
            {
                if (SAIN.Decision.TimeSinceChangeDecision > ShiftCoverChangeDecisionTime && TimeForNewShift < Time.time)
                {
                    if (enemy != null)
                    {
                        if (enemy.Seen && !enemy.IsVisible && enemy.TimeSinceSeen > ShiftCoverTimeSinceSeen)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            return true;
                        }
                        if (!enemy.Seen && enemy.TimeSinceEnemyCreated > ShiftCoverTimeSinceEnemyCreated)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            return true;
                        }
                    }
                    if (enemy == null && SAIN.Decision.TimeSinceChangeDecision > ShiftCoverNoEnemyResetTime)
                    {
                        TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                        ShiftResetTimer = Time.time + ShiftCoverResetTime;
                        return true;
                    }
                }
            }

            ShiftResetTimer = -1f;
            return false;
        }

        private bool ContinueShiftCover()
        {
            var CurrentDecision = SAIN.Memory.Decisions.Main.Current;
            if (CurrentDecision == SoloDecision.ShiftCover)
            {
                if (ShiftResetTimer > 0f && ShiftResetTimer < Time.time)
                {
                    ShiftResetTimer = -1f;
                    return false;
                }
                if (!ShiftCoverComplete)
                {
                    return true;
                }
            }
            return false;
        }

        private float TimeForNewShift;

        private float ShiftResetTimer;
        public bool ShiftCoverComplete { get; set; }

        private bool StartDogFightAction(SAINEnemy enemy)
        {
            if (SAIN.Decision.CurrentSelfDecision != SelfDecision.None || BotOwner.WeaponManager.Reload.Reloading)
            {
                return false;
            }
            var currentSolo = SAIN.Decision.CurrentSoloDecision;
            if (Time.time - SAIN.Cover.LastHitTime < 2f 
                && currentSolo != SoloDecision.RunAway 
                && currentSolo != SoloDecision.RunToCover
                && currentSolo != SoloDecision.Retreat
                && currentSolo != SoloDecision.WalkToCover)
            {
                return true;
            }

            var pathStatus = enemy.CheckPathDistance();
            return (pathStatus == EnemyPathDistance.VeryClose && SAIN.Enemy.IsVisible) || SAIN.Cover.CoverInUse?.GetSpotted(SAIN) == true;
        }

        private bool StartMoveToEngage(SAINEnemy enemy)
        {
            if (SAIN.Suppression.IsSuppressed)
            {
                return false;
            }
            if (!enemy.Seen)
            {
                return false;
            }
            if (enemy.IsVisible && enemy.EnemyLookingAtMe)
            {
                return false;
            }
            if (BotOwner.Memory.IsUnderFire || Time.time - BotOwner.Memory.UnderFireTime < TimeBeforeSearch * 0.25f)
            {
                return false;
            }
            if (SAIN.Decision.CurrentSoloDecision == SoloDecision.HoldInCover)
            {
                return false;
            }
            if (enemy.RealDistance > SAIN.Info.WeaponInfo.EffectiveWeaponDistance)
            {
                return true;
            }
            return false;
        }

        private float EndThrowTimer = 0f;

        private bool ContinueThrow()
        {
            return false;
            //if (SAIN.Grenade.EFTBotGrenade.AIGreanageThrowData == null || Time.time - EndThrowTimer > 3f)
            //{
            //    return false;
            //}
            //return CurrentDecision == SoloDecision.ThrowGrenadeAction && SAIN.Grenade.EFTBotGrenade.AIGreanageThrowData?.ThrowComplete == false;
        }

        private bool StartRunForCover()
        {
            if (!BotOwner.CanSprintPlayer)
            {
                return false;
            }
            bool underFire = BotOwner.Memory.IsUnderFire;
            if (underFire 
                && SAIN.Enemy != null 
                && SAIN.Enemy.Seen
                && !SAIN.Enemy.IsVisible 
                && SAIN.Enemy.TimeSinceSeen > 3f)
            {
                return true;
            }
            if (StartRunCoverTimer < Time.time)
            {
                CoverPoint closestCover = SAIN.Cover.ClosestPoint;
                if (closestCover != null)
                {
                    return (closestCover.GetPosition(SAIN) - SAIN.Position).sqrMagnitude > 1f;
                }
            }
            return StartRunCoverTimer < Time.time;
        }

        private static readonly float RunToCoverTime = 1.5f;
        private static readonly float RunToCoverTimeRandomMin = 0.66f;
        private static readonly float RunToCoverTimeRandomMax = 1.33f;

        private bool StartMoveToCover()
        {
            bool inCover = SAIN.Cover.BotIsAtCoverInUse();

            if (!inCover)
            {
                var CurrentDecision = SAIN.Memory.Decisions.Main.Current;
                if (CurrentDecision != SoloDecision.WalkToCover && CurrentDecision != SoloDecision.RunToCover)
                {
                    StartRunCoverTimer = Time.time + RunToCoverTime * UnityEngine.Random.Range(RunToCoverTimeRandomMin, RunToCoverTimeRandomMax);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool StartAmbush()
        {
            bool startCheck = false;
            if (SAIN.Info.PersonalitySettings.Sneaky)
            {
                startCheck = true;
            }
            else if (AmbushCheckTimer < Time.time)
            {
                AmbushCheckTimer = Time.time + 5f;
                startCheck = EFTMath.RandomBool(25);
            }

            if (startCheck)
            {

            }

            return false;
        }

        private float AmbushCheckTimer;

        private bool StartSearch(SAINEnemy enemy)
        {
            if (!SAIN.Info.PersonalitySettings.WillSearchForEnemy)
            {
                return false;
            }
            if (SAIN.Suppression.IsSuppressed)
            {
                return false;
            }
            if (enemy.IsVisible == true)
            {
                return false;
            }
            if (BotOwner.Memory.IsUnderFire || Time.time - BotOwner.Memory.UnderFireTime < TimeBeforeSearch * 0.33f)
            {
                return false;
            }
            if (enemy.Seen && enemy.TimeSinceSeen >= TimeBeforeSearch)
            {
                return true;
            }
            if (!enemy.Seen && enemy.TimeSinceEnemyCreated >= TimeBeforeSearch)
            {
                return true;
            }
            return false;
        }

        private float TimeBeforeSearch => SAIN.Info.TimeBeforeSearch;

        private static readonly float HoldInCoverMaxCoverDist = 0.75f * 0.75f;

        public bool StartHoldInCover()
        {
            var cover = SAIN.Cover.CoverInUse;
            if (cover != null 
                && !cover.GetSpotted(SAIN) 
                && (cover.GetPosition(SAIN) - BotOwner.Position).sqrMagnitude < HoldInCoverMaxCoverDist)
            {
                return true;
            }
            return false;
        }

        private bool StartStandAndShoot(SAINEnemy enemy)
        {
            if (enemy.IsVisible && enemy.CanShoot)
            {
                if (enemy.RealDistance > SAIN.Info.WeaponInfo.EffectiveWeaponDistance * 1.25f)
                {
                    return false;
                }
                float holdGround = SAIN.Info.HoldGroundDelay;

                if (holdGround <= 0f)
                {
                    return false;
                }

                if (!enemy.EnemyLookingAtMe)
                {
                    CoverPoint closestPoint = SAIN.Cover.ClosestPoint;
                    if (!enemy.EnemyLookingAtMe && closestPoint != null && closestPoint.GetCoverStatus(SAIN) <= CoverStatus.CloseToCover)
                    {
                        return true;
                    }
                }

                float visibleFor = Time.time - enemy.VisibleStartTime;

                if (visibleFor < holdGround)
                {
                    if (visibleFor < holdGround / 1.5f)
                    {
                        return true;
                    }
                    else
                    {
                        return SAIN.Cover.CheckLimbsForCover();
                    }
                }
            }
            return false;
        }

        private float StartRunCoverTimer;
    }
}
