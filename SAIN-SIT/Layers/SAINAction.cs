extern alias PortBrain;

using BepInEx.Logging;

using PortBrainZ = PortBrain::DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Components;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Decision;
using System.Text;

namespace SAIN.Layers
{
    public abstract class SAINAction : PortBrainZ.CustomLogic
    {
        public SAINAction(BotOwner botOwner, string name) : base(botOwner)
        {
            SAIN = botOwner.GetComponent<SAINComponentClass>();
            Shoot = new ShootClass(botOwner);
        }

        public SAINBotControllerComponent BotController => SAINPlugin.BotController;
        public DecisionWrapper Decisions => SAIN.Memory.Decisions;

        public readonly SAINComponentClass SAIN;

        public readonly ShootClass Shoot;

        //public override void BuildDebugText(StringBuilder stringBuilder)
        //{
        //    DebugOverlay.AddBaseInfo(SAIN, BotOwner, stringBuilder);
        //}
    }
}
