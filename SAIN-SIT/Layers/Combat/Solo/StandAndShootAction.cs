using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.Components;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Solo
{
    public class StandAndShootAction : SAINAction
    {
        public StandAndShootAction(BotOwner bot) : base(bot, nameof(StandAndShootAction))
        {
        }

        public override void Update()
        {
            SAIN.Steering.SteerByPriority();

            if (SAIN.Cover.DuckInCover())
            {
                BotOwner.StopMove();
                SAIN.Mover.StopMove();
            }

            Shoot.Update();

            return;

            if (SAIN.Cover.BotIsAtCoverInUse())
            {
                return;
            }
            else
            {
                bool prone = SAIN.Mover.Prone.ShallProne(true);
                SAIN.Mover.Prone.SetProne(prone);
            }
        }

        public override void Start()
        {
            SAIN.Mover.Sprint(false);
        }

        public override void Stop()
        {
        }
    }
}