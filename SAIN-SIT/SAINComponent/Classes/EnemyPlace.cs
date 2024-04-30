using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class EnemyPlace
    {
        public EnemyPlace(Vector3 position)
        {
            Position = position;
        }

        public Vector3? Position
        {
            get
            {
                return _position;
            }
            set
            {
                HasArrived = false;
                HasSeen = false;
                TimePositionUpdated = Time.time;
                _position = value;
            }
        }

        private Vector3? _position;

        public float TimePositionUpdated;

        public bool HasArrived
        {
            get
            {
                return _hasArrived;
            }
            set
            {
                if (value)
                {
                    TimeArrived = Time.time;
                }
                _hasArrived = value;
            }
        }

        private bool _hasArrived;

        public float TimeArrived;

        public bool HasSeen
        {
            get
            {
                return _hasSeen;
            }
            set
            {
                if (value)
                {
                    TimeSeen = Time.time;
                }
                _hasSeen = value;
            }
        }

        private bool _hasSeen;

        public float TimeSeen;
    }
}