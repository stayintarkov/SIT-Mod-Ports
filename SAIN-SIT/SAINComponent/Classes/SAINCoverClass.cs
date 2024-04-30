using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public enum CoverFinderState
    {
        off = 0,
        on = 1,
        forceOff = 2,
        forceOn = 3,
    }

    public class SAINCoverClass : SAINBase, ISAINClass
    {
        public SAINCoverClass(SAINComponentClass sain) : base(sain)
        {
            CoverFinder = sain.GetOrAddComponent<CoverFinderComponent>();
            Player.HealthController.ApplyDamageEvent += OnBeingHit;
        }

        public void Init()
        {
            CoverFinder.Init(SAIN);
        }

        public void ForceCoverFinderState(bool value, float duration = 30f)
        {
            ForcedCoverFinderState = value ? CoverFinderState.forceOn : CoverFinderState.forceOff;
            _forcedStateTimer = Time.time + duration;
        }

        public CoverFinderState CurrentCoverFinderState { get; private set; }
        private CoverFinderState ForcedCoverFinderState;
        private float _forcedStateTimer;

        public void Update()
        {
            if (!SAIN.BotActive || SAIN.GameIsEnding)
            {
                ActivateCoverFinder(false);
                return;
            }

            if (ForcedCoverFinderState != CoverFinderState.off && _forcedStateTimer < Time.time)
            {
                ForcedCoverFinderState = CoverFinderState.off;
            }
            if (ForcedCoverFinderState == CoverFinderState.forceOn)
            {
                //ActivateCoverFinder(true, true);
                //return;
            }
            if (ForcedCoverFinderState == CoverFinderState.forceOff)
            {
                //ActivateCoverFinder(false, true);
                //return;
            }

            ActivateCoverFinder(SAIN.Decision.SAINActive);
        }

        public void Dispose()
        {
            try
            {
                Player.HealthController.ApplyDamageEvent -= OnBeingHit;
                CoverFinder?.Dispose();
            }
            catch { }
        }

        private void OnBeingHit(EBodyPart part, float unused, DamageInfo damage)
        {
            LastHitTime = Time.time;

            SAINEnemy enemy = SAIN.Enemy;
            bool HitInCoverKnown = enemy != null && damage.Player != null && enemy.EnemyPlayer.ProfileId == damage.Player.iPlayer.ProfileId;
            bool HitInCoverCantSee = enemy != null && enemy.IsVisible == false;

            foreach (var coverPoint in CoverPoints)
            {
                if (coverPoint.GetCoverStatus(SAIN) == CoverStatus.InCover)
                {
                    if (HitInCoverCantSee)
                    {
                        coverPoint.GetHit(SAIN, 0, 1, 0);
                    }
                    else if (HitInCoverKnown)
                    {
                        coverPoint.GetHit(SAIN, 0, 0, 1);
                    }
                    else
                    {
                        coverPoint.GetHit(SAIN, 1, 0, 0);
                    }
                }
            }
        }

        private void ActivateCoverFinder(bool value, bool forced = false)
        {
            if (value)
            {
                CoverFinder?.LookForCover();
                CurrentCoverFinderState = forced ? CoverFinderState.forceOn : CoverFinderState.on;
            }
            if (!value)
            {
                CoverFinder?.StopLooking();
                CurrentCoverFinderState = forced ? CoverFinderState.forceOff : CoverFinderState.off;
            }
        }

        public CoverPoint ClosestPoint
        {
            get
            {
                foreach (var point in CoverPoints)
                {
                    point?.CalcPathLength(SAIN);
                }

                CoverFinderComponent.OrderPointsByPathDist(CoverPoints, SAIN);

                for (int i = 0; i < CoverPoints.Count; i++)
                {
                    CoverPoint point = CoverPoints[i];
                    if (point != null)
                    {
                        if (point != null && point.GetSpotted(SAIN) == false)
                        {
                            return point;
                        }
                    }
                }
                return null;
            }
        }

        private bool GetPointToHideFrom(out Vector3? target)
        {
            target = SAIN.Grenade.GrenadeDangerPoint ?? SAIN.CurrentTargetPosition;
            return target != null;
        }

        public bool DuckInCover()
        {
            var point = CoverInUse;
            if (point != null)
            {
                var move = SAIN.Mover;
                var prone = move.Prone;
                bool shallProne = prone.ShallProneHide();

                if (shallProne && (SAIN.Decision.CurrentSelfDecision != SelfDecision.None || SAIN.Suppression.IsHeavySuppressed))
                {
                    prone.SetProne(true);
                    return true;
                }
                if (move.Pose.SetPoseToCover())
                {
                    return true;
                }
                if (shallProne && point.Collider.bounds.size.y < 1f)
                {
                    prone.SetProne(true);
                    return true;
                }
            }
            return false;
        }

        public bool CheckLimbsForCover()
        {
            var enemy = SAIN.Enemy;
            if (enemy?.IsVisible == true)
            {
                if (CheckLimbTimer < Time.time)
                {
                    CheckLimbTimer = Time.time + 0.1f;
                    bool cover = false;
                    var target = enemy.EnemyIPlayer.WeaponRoot.position;
                    if (CheckLimbForCover(BodyPartType.leftLeg, target, 4f) || CheckLimbForCover(BodyPartType.leftArm, target, 4f))
                    {
                        cover = true;
                    }
                    else if (CheckLimbForCover(BodyPartType.rightLeg, target, 4f) || CheckLimbForCover(BodyPartType.rightArm, target, 4f))
                    {
                        cover = true;
                    }
                    HasLimbCover = cover;
                }
            }
            else
            {
                HasLimbCover = false;
            }
            return HasLimbCover;
        }

        private bool HasLimbCover;
        private float CheckLimbTimer = 0f;

        private bool CheckLimbForCover(BodyPartType bodyPartType, Vector3 target, float dist = 2f)
        {
            var position = BotOwner.MainParts[bodyPartType].Position;
            Vector3 direction = target - position;
            return Physics.Raycast(position, direction, dist, LayerMaskClass.HighPolyWithTerrainMask);
        }

        public bool BotIsAtCoverInUse(out CoverPoint coverInUse)
        {
            coverInUse = CoverInUse;
            return coverInUse != null && coverInUse.BotInThisCover(SAIN);
        }

        public bool BotIsAtCoverPoint(CoverPoint coverPoint)
        {
            return coverPoint != null && coverPoint.BotInThisCover(SAIN);
        }

        public bool BotIsAtCoverInUse()
        {
            var coverPoint = CoverInUse;
            return coverPoint != null && coverPoint.BotInThisCover(SAIN);
        }

        public CoverPoint CoverInUse
        {
            get
            {
                if (FallBackPoint != null 
                    && (FallBackPoint.GetBotIsUsingThis() 
                    || BotIsAtCoverPoint(FallBackPoint)))
                {
                    return FallBackPoint;
                }
                foreach (var point in CoverPoints)
                {
                    if (point != null 
                        && (point.GetBotIsUsingThis() 
                        || BotIsAtCoverPoint(point)))
                    {
                        return point;
                    }
                }
                return null;
            }
        }

        public List<CoverPoint> CoverPoints => CoverFinder.CoverPoints;
        public CoverFinderComponent CoverFinder { get; private set; }
        public CoverPoint CurrentCoverPoint => ClosestPoint;
        public CoverPoint FallBackPoint => CoverFinder.FallBackPoint;

        public float LastHitTime { get; private set; }
    }
}