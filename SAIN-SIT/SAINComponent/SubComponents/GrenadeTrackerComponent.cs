using Comfort.Common;
using CustomPlayerLoopSystem;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using BotEventHandler = GClass603;

namespace SAIN.SAINComponent.SubComponents
{
    public class GrenadeTracker : MonoBehaviour
    {
        private BotOwner BotOwner;

        public void Initialize(Grenade grenade, Vector3 dangerPoint, float reactionTime)
        {
            ReactionTime = reactionTime;
            DangerPoint = dangerPoint;
            Grenade = grenade;

            if (EnemyGrenadeHeard())
            {
                if (grenade != null && grenade.Player != null && grenade.Player.iPlayer != null)
                {
                    Singleton<BotEventHandler>.Instance?.PlaySound(grenade?.Player?.iPlayer, grenade.transform.position, 30f, AISoundType.gun);
                }
                GrenadeSpotted = true;
            }
        }

        private void Awake()
        {
            BotOwner = GetComponent<BotOwner>();
        }

        private bool EnemyGrenadeHeard()
        {
            if (BotOwner == null || BotOwner.IsDead || Grenade == null)
            {
                return false;
            }

            return (Grenade.transform.position - BotOwner.Position).sqrMagnitude < 15f * 15f;
        }

        private void Update()
        {
            if (BotOwner.IsDead || Grenade == null)
            {
                StopAllCoroutines();
                Destroy(this);
                return;
            }

            if (!GrenadeSpotted)
            {
                if (GrenadeClose)
                {
                    GrenadeSpotted = true;
                    return;
                }
                if (RayCastFreqTimer < Time.time && CheckVisibility())
                {
                    RayCastFreqTimer = Time.time + 0.1f;
                    if (FirstVisible)
                    {
                        TimeGrenadeSpotted = Time.time;
                        GrenadeSpotted = true;
                        return;
                    }
                    FirstVisible = true;
                }
            }
        }

        private bool CheckVisibility()
        {
            Vector3 grenadePos = Grenade.transform.position;

            if (BotOwner.LookSensor.IsPointInVisibleSector(grenadePos))
            {
                Vector3 headPos = BotOwner.LookSensor._headPoint;
                Vector3 grenadeDir = grenadePos - headPos;
                if (!Physics.Raycast(headPos, grenadeDir, grenadeDir.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI))
                {
                    return true;
                }
            }
            return false;
        }

        public float TimeGrenadeSpotted { get; private set; }
        public float TimeSinceSpotted => GrenadeSpotted ? Time.time - TimeGrenadeSpotted : 0f;
        public Grenade Grenade { get; private set; }
        public Vector3 DangerPoint { get; private set; }
        public bool GrenadeSpotted { get; private set; }
        public bool CanReact => GrenadeSpotted && TimeSinceSpotted > ReactionTime;
        public bool GrenadeClose => (Grenade.transform.position - BotOwner.Position).sqrMagnitude < 6f * 6f;

        private bool FirstVisible;

        private float ReactionTime;

        private float RayCastFreqTimer;
    }
}
