using UnityEngine;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class SpottedCoverPoint
    {
        public SpottedCoverPoint(CoverPoint coverPoint, float expireTime = 2f)
        {
            CoverPoint = coverPoint;
            ExpireTime = expireTime;
            TimeCreated = Time.time;
        }

        public bool TooClose(Vector3 coverInfoPosition, Vector3 newPos, float sqrdist = 3f)
        {
            return (coverInfoPosition - newPos).sqrMagnitude > sqrdist;
        }

        public CoverPoint CoverPoint { get; private set; }
        public float TimeCreated { get; private set; }
        public float TimeSinceCreated => Time.time - TimeCreated;

        private readonly float ExpireTime;
        public bool IsValidAgain => TimeSinceCreated > ExpireTime;
    }
}