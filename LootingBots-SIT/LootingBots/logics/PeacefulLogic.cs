extern alias PortBrain;

using PortBrainZ = PortBrain::DrakiaXYZ.BigBrain.Brains;
using EFT;

using PeacefulNodeClass = GClass177;

namespace LootingBots.Brain.Logics
{
    internal class PeacefulLogic : PortBrainZ.CustomLogic
    {
        private readonly PeacefulNodeClass _baseLogic;

        // PatrolAssault peaceful logic
        public PeacefulLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new PeacefulNodeClass(botOwner);
        }

        public override void Update()
        {
            _baseLogic.Update();
        }
    }
}