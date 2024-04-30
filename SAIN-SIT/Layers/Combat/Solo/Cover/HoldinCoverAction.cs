using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using SAIN.Helpers;
using SAIN.Components.MainPlayer;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class HoldinCoverAction : SAINAction
    {
        public HoldinCoverAction(BotOwner bot) : base(bot, nameof(HoldinCoverAction))
        {
        }

        private bool Stopped;

        public override void Update()
        {
            if (CoverInUse == null)
            {
                return;
            }

            if (!Stopped && !CoverInUse.GetSpotted(SAIN) && (CoverInUse.GetPosition(SAIN) - BotOwner.Position).sqrMagnitude < 0.33f)
            {
                SAIN.Mover.StopMove();
                Stopped = true;
            }

            SAIN.Steering.SteerByPriority();
            Shoot.Update();
            SAIN.Cover.DuckInCover();

            if (SAIN.Enemy != null 
                && SAIN.Player.MovementContext.CanProne
                && SAIN.Player.PoseLevel <= 0.1 
                && SAIN.Enemy.IsVisible 
                && BotOwner.WeaponManager.Reload.Reloading)
            {
                SAIN.Mover.Prone.SetProne(true);
            }

            if (SAIN.Suppression.IsSuppressed)
            {
                ChangeLeanTimer = Time.time + 2f * Random.Range(0.66f, 1.33f);
                SAIN.Mover.FastLean(LeanSetting.None);
                CurrentLean = LeanSetting.None;
            }
            else
            {
                if (!ShallHoldLean() && ChangeLeanTimer < Time.time)
                {
                    ChangeLeanTimer = Time.time + 2f * Random.Range(0.66f, 1.33f);
                    LeanSetting newLean;
                    switch (CurrentLean)
                    {
                        case LeanSetting.Left:
                        case LeanSetting.Right:
                            newLean = LeanSetting.None;
                            break;

                        default:
                            newLean = EFTMath.RandomBool() ? LeanSetting.Left : LeanSetting.Right;
                            break;
                    }
                    CurrentLean = newLean;
                    SAIN.Mover.FastLean(newLean);
                }
            }
        }

        private bool ShallHoldLean()
        {
            bool holdLean = false;

            if (SAIN.Suppression.IsSuppressed)
            {
                return false;
            }

            if (SAIN.HasEnemy && SAIN.Enemy.IsVisible && SAIN.Enemy.CanShoot)
            {
                if (SAIN.Enemy.IsVisible && SAIN.Enemy.CanShoot)
                {
                    holdLean = true;
                }
                else if (SAIN.Enemy.TimeSinceSeen < 3f)
                {
                    holdLean = true;
                }
            }
            return holdLean;
        }

        private void Lean(LeanSetting setting, bool holdLean)
        {
            if (holdLean)
            {
                return;
            }
            CurrentLean = setting;
            SAIN.Mover.FastLean(setting);
        }

        private LeanSetting CurrentLean;
        private float ChangeLeanTimer;

        private CoverPoint CoverInUse;

        public override void Start()
        {
            ChangeLeanTimer = Time.time + 2f;
            CoverInUse = SAIN.Cover.CoverInUse;
        }

        public override void Stop()
        {
            SAIN.Mover.Prone.SetProne(false);
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Hold In Cover Info");
            var cover = SAIN.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("Current Cover Status", $"{CoverInUse?.GetCoverStatus(SAIN)}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Current Cover Height", $"{CoverInUse?.CoverHeight}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Current Cover Value", $"{CoverInUse?.CoverValue}", Color.white, Color.yellow, true);
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

            if (CoverInUse != null)
            {
                stringBuilder.AppendLine("Cover In Use");
                stringBuilder.AppendLabeledValue("Status", $"{CoverInUse.GetCoverStatus(SAIN)}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{CoverInUse.CoverHeight} {CoverInUse.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{CoverInUse.CalcPathLength(SAIN)}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(CoverInUse.GetPosition(SAIN) - SAIN.Position).magnitude}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Safe Path?", $"{CoverInUse.CheckPathSafety(SAIN)}", Color.white, Color.yellow, true);
            }

        }
    }
}