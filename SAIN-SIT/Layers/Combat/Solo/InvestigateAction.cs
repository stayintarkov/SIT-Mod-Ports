using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using UnityEngine;
using UnityEngine.AI;
using SAIN.Helpers;

namespace SAIN.Layers.Combat.Solo
{
    internal class InvestigateAction : SAINAction
    {
        public InvestigateAction(BotOwner bot) : base(bot, nameof(InvestigateAction))
        {
        }

        public override void Update()
        {
            Shoot.Update();

            if (PlaceForCheck == null)
            {
                SAIN.Decision.ResetDecisions();
                return;
            }

            if (SAIN.Enemy?.IsVisible == false && SAIN.Decision.SelfActionDecisions.LowOnAmmo(0.66f))
            {
                SAIN.SelfActions.TryReload();
            }

            SAIN.Mover.SetTargetMoveSpeed(0.7f);
            SAIN.Mover.SetTargetPose(1f);

            SAIN.Steering.SteerByPriority();

            if (RecalcPathtimer < Time.time)
            {
                bool moving = SAIN.Mover.GoToPoint(PlaceForCheck.Position, out bool calculating);
                float timeAdd = moving ? 4f : 0.5f;
                RecalcPathtimer = Time.time + timeAdd;
            }
            const float MinDistance = 10;
            if ((BotOwner.Position - PlaceForCheck.Position).sqrMagnitude < MinDistance * MinDistance)
            {
                Vector3 headPos = SAIN.Transform.Head;
                Vector3 searchPoint = PlaceForCheck.Position + Vector3.up;
                Vector3 direction = searchPoint - headPos;
                if (!Physics.SphereCast(headPos, 0.1f, direction, out var hit, LayerMaskClass.HighPolyWithTerrainMaskAI))
                {
                    HasSeenPlace = true;
                    if (!HaveArrived)
                    {
                        HaveArrived = true;
                        PlaceForCheck.IsCome = true;
                        BotOwner.BotsGroup.PointChecked(PlaceForCheck);
                    }
                }
            }
        }

        private float RecalcPathtimer = 0f;

        private bool HasSeenPlace;
        private bool HaveArrived;

        private PlaceForCheck PlaceForCheck;

        public override void Start()
        {
            PlaceForCheck = BotOwner.BotsGroup.YoungestPlace(BotOwner, 200f, true);
        }

        public override void Stop()
        {
        }

        private PlaceForCheck GetNextPlace()
        {
            return null;
        }
    }
}