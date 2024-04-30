using SAIN.SAINComponent;

namespace SAIN.BotController.Classes
{
    public class MemberInfo
    {
        public MemberInfo(SAINComponentClass sain)
        {
            SAIN = sain;
            ProfileId = sain.ProfileId;
            Nickname = sain.Player.Profile.Nickname;

            HealthStatus = sain.Memory.HealthStatus;

            sain.Decision.OnDecisionMade += UpdateDecisions;
            sain.Memory.HealthStatusChanged += UpdateHealth;

            UpdatePowerLevel();
        }

        private void UpdateDecisions(SoloDecision solo, SquadDecision squad, SelfDecision self, float time)
        {
            SoloDecision = solo;
            SquadDecision = squad;
            SelfDecision = self;
            ChangeDecisionTime = time;

            // Update power level here just to see if equipment changed.
            UpdatePowerLevel();
        }

        public void UpdatePowerLevel()
        {
            var aiData = SAIN?.Player?.AIData;
            if (aiData != null)
            {
                PowerLevel = aiData.PowerOfEquipment;
            }
        }

        private void UpdateHealth(ETagStatus healthStatus)
        {
            HealthStatus = healthStatus;
        }

        public readonly SAINComponentClass SAIN;
        public readonly string ProfileId;
        public readonly string Nickname;

        public bool HasEnemy => SAIN?.HasEnemy == true;
        public ETagStatus HealthStatus;

        public SoloDecision SoloDecision { get; private set; }
        public SquadDecision SquadDecision { get; private set; }
        public SelfDecision SelfDecision { get; private set; }
        public float ChangeDecisionTime { get; private set; }
        public float PowerLevel { get; private set; }

        public void Dispose()
        {
            if (SAIN != null)
            {
                SAIN.Decision.OnDecisionMade -= UpdateDecisions;
                SAIN.Memory.HealthStatusChanged -= UpdateHealth;
            }
        }
    }
}
