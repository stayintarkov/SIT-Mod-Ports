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
using System.Text;
using UnityEngine;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class WalkToCoverAction : SAINAction
    {
        public WalkToCoverAction(BotOwner bot) : base(bot, nameof(WalkToCoverAction))
        {
        }

        public override void Update()
        {
            if (CoverDestination != null)
            {
                if (!SAIN.Cover.CoverPoints.Contains(CoverDestination) || CoverDestination.GetSpotted(SAIN))
                {
                    CoverDestination.SetBotIsUsingThis(false);
                    CoverDestination = null;
                }
            }

            SAIN.Mover.SetTargetMoveSpeed(1f);
            SAIN.Mover.SetTargetPose(1f);

            if (CoverDestination == null)
            {
                var coverPoint = SAIN.Cover.ClosestPoint;
                if (coverPoint != null 
                    && !coverPoint.GetSpotted(SAIN) 
                    && SAIN.Mover.GoToPoint(coverPoint.GetPosition(SAIN), out bool calculating, -1, false, false))
                {
                    CoverDestination = coverPoint;
                    CoverDestination.SetBotIsUsingThis(true);
                    RecalcPathTimer = Time.time + 2f;
                    SAIN.Mover.SetTargetMoveSpeed(1f);
                    SAIN.Mover.SetTargetPose(1f);
                }
            }
            if (CoverDestination != null && RecalcPathTimer < Time.time)
            {
                if (SAIN.Mover.GoToPoint(CoverDestination.GetPosition(SAIN), out bool calculating))
                {
                    RecalcPathTimer = Time.time + 2f;

                    var personalitySettings = SAIN.Info.PersonalitySettings;
                    float moveSpeed = personalitySettings.Sneaky ? personalitySettings.SneakySpeed : 1f;
                    SAIN.Mover.SetTargetMoveSpeed(1f);
                    SAIN.Mover.SetTargetPose(1f);
                }
                else
                {
                    RecalcPathTimer = Time.time + 0.25f;
                }
            }

            EngageEnemy();
        }

        private float FindTargetCoverTimer = 0f;
        private float RecalcPathTimer = 0f;

        private bool FindTargetCover()
        {
            var coverPoint = SAIN.Cover.ClosestPoint;
            if (coverPoint != null && !coverPoint.GetSpotted(SAIN))
            {
                coverPoint.SetBotIsUsingThis(true);
                CoverDestination = coverPoint;
                DestinationPosition = coverPoint.GetPosition(SAIN);
            }
            return false;
        }

        private bool MoveTo(Vector3 position)
        {
            if (SAIN.Mover.GoToPoint(position, out bool calculating))
            {
                CoverDestination.SetBotIsUsingThis(true);
                if (SAIN.HasEnemy || BotOwner.Memory.IsUnderFire)
                {
                    SAIN.Mover.SetTargetMoveSpeed(1f);
                }
                else
                {
                    SAIN.Mover.SetTargetMoveSpeed(0.75f);
                }
                SAIN.Mover.SetTargetPose(1f);
                return true;
            }
            return false;
        }

        private CoverPoint CoverDestination;
        private Vector3 DestinationPosition;
        private float SuppressTimer;

        private void EngageEnemy()
        {
            if (SAIN.Enemy?.IsVisible == false && SAIN.Enemy.Seen && SAIN.Enemy.TimeSinceSeen < 5f && SAIN.Enemy.LastCornerToEnemy != null && SAIN.Enemy.CanSeeLastCornerToEnemy)
            {
                Vector3 corner = SAIN.Enemy.LastCornerToEnemy.Value;
                corner += Vector3.up * 1f;
                SAIN.Steering.LookToPoint(corner);
                if (SuppressTimer < Time.time 
                    && BotOwner.WeaponManager.HaveBullets 
                    && SAIN.Shoot(true, true, SAINComponentClass.EShootReason.WalkToCoverSuppress))
                {
                    SAIN.Enemy.EnemyIsSuppressed = true;
                    if (SAIN.Info.WeaponInfo.IWeaponClass == IWeaponClass.machinegun)
                    {
                        SuppressTimer = Time.time + 0.1f * Random.Range(0.75f, 1.25f);
                    }
                    else
                    {
                        SuppressTimer = Time.time + 0.5f * Random.Range(0.66f, 1.33f);
                    }
                }
            }
            else
            {
                SAIN.Shoot(false);
                if (!BotOwner.ShootData.Shooting)
                {
                    SAIN.Steering.SteerByPriority(false);
                }
                Shoot.Update();
            }
        }

        public override void Start()
        {
            SAIN.Mover.Sprint(false);
        }

        public override void Stop()
        {
            if (CoverDestination != null)
            {
                CoverDestination.SetBotIsUsingThis(false);
                CoverDestination = null;
            }
            SAIN.Shoot(false, true, SAINComponentClass.EShootReason.None);
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Walk To Cover Info");
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
            }
        }
    }
}