using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINEnemyVision : EnemyBase
    {
        public SAINEnemyVision(SAINEnemy enemy) : base(enemy)
        {
        }

        public void Update(bool isCurrentEnemy)
        {
            if (Enemy == null || BotOwner == null || BotOwner.Settings?.Current == null || EnemyPlayer == null)
            {
                return;
            }

            float timeToAdd;
            bool performanceMode = SAINPlugin.LoadedPreset.GlobalSettings.General.PerformanceMode;
            if (!isCurrentEnemy && Enemy.IsAI)
            {
                timeToAdd = performanceMode ? 6f : 4f;
            }
            else if (performanceMode)
            {
                timeToAdd = isCurrentEnemy ? 0.15f : 2f;
            }
            else
            {
                timeToAdd = isCurrentEnemy ? 0.1f : 1f;
            }

            bool visible = false;
            bool canshoot = false;

            if (CheckLosTimer + timeToAdd < Time.time)
            {
                CheckLosTimer = Time.time;
                InLineOfSight = CheckLineOfSight(true);
            }

            var enemyInfo = EnemyInfo;
            if (enemyInfo?.IsVisible == true && InLineOfSight)
            {
                visible = true;
            }
            if (enemyInfo?.CanShoot == true)
            {
                canshoot = true;
            }

            UpdateVisible(visible);
            UpdateCanShoot(canshoot);
        }

        private bool CheckLineOfSight(bool noDistRestrictions = false)
        {
            if (Enemy == null || BotOwner == null || BotOwner.Settings?.Current == null || EnemyPlayer == null)
            {
                return false;
            }
            if (SAINPlugin.DebugMode && EnemyPlayer.IsYourPlayer)
            {
                //Logger.LogInfo($"EnemyDistance [{Enemy.RealDistance}] Vision Distance [{BotOwner.Settings.Current.CurrentVisibleDistance}]");
            }
            if (noDistRestrictions || Enemy.RealDistance <= BotOwner.Settings.Current.CurrentVisibleDistance)
            {
                foreach (var part in EnemyPlayer.MainParts.Values)
                {
                    Vector3 headPos = BotOwner.LookSensor._headPoint;
                    Vector3 direction = part.Position - headPos;
                    if (!Physics.Raycast(headPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckInVisionCone()
        {
            Vector3 enemyDir = EnemyPosition - BotOwner.Position;
            Vector3 lookDir = BotOwner.LookDirection;
            float angle = Vector3.Angle(lookDir, enemyDir);
            float maxVisionCone = BotOwner.Settings.FileSettings.Core.VisibleAngle / 2f;
            return angle <= maxVisionCone;
        }

        public void UpdateVisible(bool visible)
        {
            bool wasVisible = IsVisible;
            IsVisible = visible;

            if (IsVisible)
            {
                TimeLastSeen = Time.time;
                if (!wasVisible)
                {
                    VisibleStartTime = Time.time;
                }
                if (!Seen)
                {
                    TimeFirstSeen = Time.time;
                    Seen = true;
                }
                LastSeenPosition = EnemyPerson.Position;
                Enemy.UpdateKnownPosition(EnemyPerson.Position, false, true);
            }

            if (!IsVisible)
            {
                VisibleStartTime = -1f;
            }

            if (IsVisible != wasVisible)
            {
                LastChangeVisionTime = Time.time;
            }
        }

        private void CheckForAimingDelay()
        {

        }

        public void UpdateCanShoot(bool value)
        {
            CanShoot = value;
        }

        public bool InLineOfSight { get; private set; }
        public bool IsVisible { get; private set; }
        public bool CanShoot { get; private set; }
        public Vector3? LastSeenPosition { get; set; }
        public float VisibleStartTime { get; private set; }
        public float TimeSinceSeen => Seen ? Time.time - TimeLastSeen : -1f;
        public bool Seen { get; private set; }
        public float TimeFirstSeen { get; private set; }
        public float TimeLastSeen { get; private set; }
        public float LastChangeVisionTime { get; private set; }

        private float CheckLosTimer;
    }
}