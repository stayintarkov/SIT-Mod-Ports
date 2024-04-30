using UnityEngine;
using Random = UnityEngine.Random;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyStatus : EnemyBase
    {
        public SAINEnemyStatus(SAINEnemy enemy) : base(enemy)
        {
        }

        public bool EnemyLookingAtMe
        {
            get
            {
                Vector3 directionToBot = (SAIN.Position - EnemyPosition).normalized;
                Vector3 enemyLookDirection = EnemyPerson.Transform.LookDirection.normalized;
                float dot = Vector3.Dot(directionToBot, enemyLookDirection);
                return dot >= 0.9f;
                //Vector3 dirToEnemy = Vector.NormalizeFastSelf(BotOwner.LookSensor._headPoint - EnemyPosition);
                //return Vector.IsAngLessNormalized(dirToEnemy, EnemyPerson.Transform.LookDirection, 0.9659258f);
            }
        }

        public bool EnemyIsReloading
        {
            get
            {
                if (_soundResetTimer < Time.time)
                {
                    _enemyIsReloading = false;
                }
                return _enemyIsReloading;
            }
            set
            {
                if (value == true)
                {
                    _enemyIsReloading = true;
                    _soundResetTimer = Time.time + 3f * Random.Range(0.75f, 1.5f);
                }
            }
        }

        public bool EnemyHasGrenadeOut
        {
            get
            {
                if (_grenadeResetTimer < Time.time)
                {
                    _enemyHasGrenade = false;
                }
                return _enemyHasGrenade;
            }
            set
            {
                if (value == true)
                {
                    _enemyHasGrenade = true;
                    _grenadeResetTimer = Time.time + 3f * Random.Range(0.75f, 1.5f);
                }
            }
        }

        public bool EnemyIsHealing
        {
            get
            {
                if (_healResetTimer < Time.time)
                {
                    _enemyIsHeal = false;
                }
                return _enemyIsHeal;
            }
            set
            {
                if (value == true)
                {
                    _enemyIsHeal = true;
                    _healResetTimer = Time.time + 4f * Random.Range(0.75f, 1.25f);
                }
            }
        }

        private bool _enemyIsReloading;
        private float _soundResetTimer;
        private bool _enemyHasGrenade;
        private float _grenadeResetTimer;
        private bool _enemyIsHeal;
        private float _healResetTimer;
    }
}