using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Text;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class ShiftCoverAction : SAINAction
    {
        public ShiftCoverAction(BotOwner bot) : base(bot, nameof(ShiftCoverAction))
        {
        }

        public override void Update()
        {
            SAIN.Steering.SteerByPriority();
            Shoot.Update();
            if (NewPoint == null 
                && FindPointToGo() 
                && SAIN.Mover.GoToPoint(NewPoint.GetPosition(SAIN), out bool calculating))
            {
                SAIN.Mover.SetTargetMoveSpeed(GetSpeed());
                SAIN.Mover.SetTargetPose(GetPose());
            }
            else if (NewPoint != null && NewPoint.GetCoverStatus(SAIN) == CoverStatus.InCover)
            {
                SAIN.Decision.EnemyDecisions.ShiftCoverComplete = true;
            }
            else
            {

            }
        }

        private float GetSpeed()
        {
            var settings = SAIN.Info.PersonalitySettings;
            return SAIN.HasEnemy ? settings.MoveToCoverHasEnemySpeed : settings.MoveToCoverNoEnemySpeed;
        }

        private float GetPose()
        {
            var settings = SAIN.Info.PersonalitySettings;
            return SAIN.HasEnemy ? settings.MoveToCoverHasEnemyPose : settings.MoveToCoverNoEnemyPose;
        }

        private bool FindPointToGo()
        {
            if (NewPoint != null)
            {
                return true;
            }

            var coverInUse = SAIN.Cover.CoverInUse;
            if (coverInUse != null)
            {
                if (NewPoint == null)
                {
                    if (!UsedPoints.Contains(coverInUse))
                    {
                        UsedPoints.Add(coverInUse);
                    }

                    var coverPoints = SAIN.Cover.CoverFinder.CoverPoints;
                    for (int i = 0; i < coverPoints.Count; i++)
                    {
                        var shiftCoverTarget = coverPoints[i];

                        if (shiftCoverTarget.CoverHeight > coverInUse.CoverHeight
                            && (!SAINPlugin.LoadedPreset.GlobalSettings.Cover.ShiftCoverMustBeSafe 
                                ||  shiftCoverTarget.CheckPathSafety(SAIN))
                            && !UsedPoints.Contains(shiftCoverTarget))
                        {
                            for (int j = 0; j < UsedPoints.Count; j++)
                            {
                                if ((UsedPoints[j].GetPosition(SAIN) - shiftCoverTarget.GetPosition(SAIN)).sqrMagnitude > 5f)
                                {
                                    coverInUse.SetBotIsUsingThis(false);
                                    shiftCoverTarget.SetBotIsUsingThis(true);
                                    NewPoint = shiftCoverTarget;
                                    return true;
                                }
                            }
                        }
                    }
                }
                if (NewPoint == null)
                {
                    SAIN.Decision.EnemyDecisions.ShiftCoverComplete = true;
                }
            }
            return false;
        }

        public override void Start()
        {
            SAIN.Decision.EnemyDecisions.ShiftCoverComplete = false;
        }

        private readonly List<CoverPoint> UsedPoints = new List<CoverPoint>();
        private CoverPoint NewPoint;

        public override void Stop()
        {
            NewPoint = null;
            UsedPoints.Clear();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Shift Cover Info");
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

            if (NewPoint != null)
            {
                stringBuilder.AppendLine("Cover In Use");
                stringBuilder.AppendLabeledValue("Status", $"{NewPoint.GetCoverStatus(SAIN)}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{NewPoint.CoverHeight} {NewPoint.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{NewPoint.CalcPathLength(SAIN)}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(NewPoint.GetPosition(SAIN) - SAIN.Position).magnitude}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Safe Path?", $"{NewPoint.CheckPathSafety(SAIN)}", Color.white, Color.yellow, true);
            }
        }
    }
}