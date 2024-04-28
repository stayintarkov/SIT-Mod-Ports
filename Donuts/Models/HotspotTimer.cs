using UnityEngine;

namespace Donuts
{
    internal class HotspotTimer
    {
        private Entry hotspot;
        private float timer;
        public bool inCooldown;
        public int timesSpawned;
        private float cooldownTimer;
        public Entry Hotspot => hotspot;

        public HotspotTimer(Entry hotspot)
        {
            this.hotspot = hotspot;
            this.timer = 0f;
            this.inCooldown = false;
            this.timesSpawned = 0;
            this.cooldownTimer = 0f;
        }

        public void UpdateTimer()
        {
            timer += Time.deltaTime;
            if (inCooldown)
            {
                cooldownTimer += Time.deltaTime;
                if (cooldownTimer >= DonutsPlugin.coolDownTimer.Value)
                {
                    inCooldown = false;
                    cooldownTimer = 0f;
                    timesSpawned = 0;
                }
            }
        }

        public float GetTimer() => timer;
        public bool ShouldSpawn()
        {
            if (hotspot.IgnoreTimerFirstSpawn == true)
            {
                return true;
            }
            return timer >= hotspot.BotTimerTrigger;
        }

        public void ResetTimer() => timer = 0f;
    }
}
