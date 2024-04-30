using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using System.Text;
using UnityEngine;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using UnityEngine.AI;
using SAIN.Helpers;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class RunToCoverAction : SAINAction
    {
        public RunToCoverAction(BotOwner bot) : base(bot, nameof(RunToCoverAction))
        {
        }

        private float _jumpTimer;
        private bool _shallJumpToCover;

        public override void Update()
        {
            SAIN.Mover.SetTargetMoveSpeed(1f);
            SAIN.Mover.SetTargetPose(1f);

            if (_shallJumpToCover 
                && BotOwner.GetPlayer.IsSprintEnabled 
                && MoveSuccess 
                && BotOwner.Mover.DistDestination < 2f 
                && _jumpTimer < Time.time)
            {
                _jumpTimer = Time.time + 5f;
                SAIN.Mover.TryJump();
            }

            if (RecalcTimer < Time.time)
            {
                MoveSuccess = false;
                bool shallProne = SAIN.Mover.Prone.ShallProneHide();
                if (FindTargetCover())
                {
                    if ((CoverDestination.GetPosition(SAIN) - BotOwner.Position).sqrMagnitude > 4f)
                    {
                        MoveSuccess = BotOwner.BotRun.Run(CoverDestination.GetPosition(SAIN), false, 0.6f);
                    }
                    else
                    {
                        bool shallCrawl = SAIN.Decision.CurrentSelfDecision != SelfDecision.None && CoverDestination.GetCoverStatus(SAIN) == CoverStatus.FarFromCover && shallProne;
                        MoveSuccess = SAIN.Mover.GoToPoint(CoverDestination.GetPosition(SAIN), out bool calculating, -1, shallCrawl, false);
                    }
                }
                if (MoveSuccess)
                {
                    RecalcTimer = Time.time + 4f;
                }
                else
                {
                    RecalcTimer = Time.time + 0.2f;
                }
            }
            if (!MoveSuccess)
            {
                EngageEnemy();
            }
        }

        private bool MoveSuccess;

        private float RecalcTimer;

        private bool FindTargetCover()
        {
            if (CoverDestination != null)
            {
                CoverDestination.SetBotIsUsingThis(false);
                CoverDestination = null;
            }

            CoverPoint coverPoint = SelectPoint();
            if (coverPoint != null && !coverPoint.GetSpotted(SAIN))
            {
                if (SAIN.Mover.CanGoToPoint(coverPoint.GetPosition(SAIN), out Vector3 pointToGo, true, 1f))
                {
                    //coverPoint.Position = pointToGo;
                    coverPoint.SetBotIsUsingThis(true);
                    CoverDestination = coverPoint;
                    return true;
                }
            }
            return false;
        }

        private CoverPoint SelectPoint()
        {
            CoverPoint fallback = SAIN.Cover.FallBackPoint;
            SoloDecision currentDecision = SAIN.Memory.Decisions.Main.Current;
            CoverPoint coverInUse = SAIN.Cover.CoverInUse;

            if (currentDecision == SoloDecision.Retreat && fallback != null && fallback.CheckPathSafety(SAIN))
            {
                return fallback;
            }
            else if (coverInUse != null && !coverInUse.GetSpotted(SAIN))
            {
                return coverInUse;
            }
            else
            {
                return SAIN.Cover.ClosestPoint;
            }
        }

        private CoverPoint CoverDestination;

        private void EngageEnemy()
        {
            SAIN.Steering.SteerByPriority();
            Shoot.Update();
        }

        public override void Start()
        {
            if (SAIN.Decision.CurrentSelfDecision == SelfDecision.RunAwayGrenade)
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.OnEnemyGrenade, ETagStatus.Combat, 0.5f);
            }
        }

        public override void Stop()
        {
            _shallJumpToCover = EFTMath.RandomBool(20) 
                && BotOwner.Memory.IsUnderFire 
                && SAIN.Info.Profile.IsPMC;

            CoverDestination = null;
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Run To Cover Info");
            var cover = SAIN.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);
            if (SAIN.CurrentTargetPosition != null)
            {
                stringBuilder.AppendLabeledValue("Current Target Position", $"{SAIN.CurrentTargetPosition.Value}", Color.white, Color.yellow, true);
            }
            else
            {
                stringBuilder.AppendLabeledValue("Current Target Position", null, Color.white, Color.yellow, true);
            }

            if (CoverDestination != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{CoverDestination.GetCoverStatus(SAIN)}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{CoverDestination.CoverHeight} {CoverDestination.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{CoverDestination.CalcPathLength(SAIN)}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(CoverDestination.GetPosition(SAIN) - SAIN.Position).magnitude}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Safe Path?", $"{CoverDestination.CheckPathSafety(SAIN)}", Color.white, Color.yellow, true);
            }
        }
    }
}