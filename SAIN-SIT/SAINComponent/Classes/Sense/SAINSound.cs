using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public sealed class SAINSound
    {
        public SAINSound()
        {
            TimeCreated = Time.time;
        }

        public readonly float TimeCreated;

        public string SourcePlayerProfileId;
        public Vector3 Position;
        public AISoundType AISoundType;
        public float SoundPower;
        public bool WasHeard;
        public bool BulletFelt;
        public float DistanceAtCreation;
        public bool IsTooFar;
        public bool IsTooOld;
        public bool IsCheckedByBot;
    }
}