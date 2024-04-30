using EFT;
using SAIN.Helpers;
using SAIN.Preset.BotSettings.SAINSettings;
using UnityEngine;
using static SAIN.Preset.Personalities.PersonalitySettingsClass;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class EnemyTalk : SAINBase, ISAINClass
    {
        public EnemyTalk(SAINComponentClass bot) : base(bot)
        {
            _randomizationFactor = Random.Range(0.66f, 1.33f);
        }

        public void Init()
        {
        }

        private float _randomizationFactor;

        public void Update()
        {
            if (PersonalitySettings == null || FileSettings == null)
            {
                return;
            }

            if (_nextCheckTime < Time.time && SAIN?.Enemy != null)
            {
                if (FakeDeath())
                {
                    _nextCheckTime = Time.time + 20f;
                    return;
                }

                if (BegForLife())
                {
                    _nextCheckTime = Time.time + 1f;
                }
                else if (CanRespondToVoice && LastEnemyCheckTime < Time.time)
                {
                    _nextCheckTime = Time.time + 0.5f;
                    LastEnemyCheckTime = Time.time + EnemyCheckFreq;
                    StartResponse();
                }
                else if (CanTaunt && TauntTimer < Time.time)
                {
                    _nextCheckTime = Time.time + 0.25f;
                    TauntTimer = Time.time + TauntFreq * Random.Range(0.5f, 1.5f);
                    TauntEnemy();
                }
            }
        }

        private float _nextCheckTime;

        public void Dispose()
        {
        }

        private const float EnemyCheckFreq = 0.25f;

        private float ResponseDist => TauntDist;
        private bool CanTaunt => PersonalitySettings.CanTaunt && FileSettings.Mind.BotTaunts;
        private bool CanRespondToVoice => PersonalitySettings.CanRespondToVoice;
        private float TauntDist => PersonalitySettings.TauntMaxDistance * _randomizationFactor;
        private float TauntFreq => PersonalitySettings.TauntFrequency * _randomizationFactor;

        private PersonalityVariablesClass PersonalitySettings => SAIN?.Info?.PersonalitySettings;
        private SAINSettingsClass FileSettings => SAIN?.Info?.FileSettings;

        private bool FakeDeath()
        {
            if (SAIN.Enemy != null && !SAIN.Squad.BotInGroup)
            {
                if (SAIN.Info.PersonalitySettings.CanFakeDeathRare)
                {
                    if (FakeTimer < Time.time)
                    {
                        FakeTimer = Time.time + 10f;
                        var health = SAIN.Memory.HealthStatus;
                        if (health == ETagStatus.Dying)
                        {
                            float dist = (SAIN.Enemy.EnemyPosition - BotOwner.Position).magnitude;
                            if (dist < 30f)
                            {
                                bool random = Helpers.EFTMath.RandomBool(1f);
                                if (random)
                                {
                                    SAIN.Talk.Say(EPhraseTrigger.OnDeath);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private float FakeTimer = 0f;

        private bool BegForLife()
        {
            if (PersonalitySettings.CanBegForLife && BegTimer < Time.time && SAIN.HasEnemy && !SAIN.Squad.BotInGroup)
            {
                bool random = Helpers.EFTMath.RandomBool(25);
                float timeAdd = random ? 8f : 2f;
                BegTimer = Time.time + timeAdd;

                var health = SAIN.Memory.HealthStatus;
                if (health != ETagStatus.Healthy)
                {
                    float dist = (SAIN.Enemy.EnemyPosition - BotOwner.Position).magnitude;
                    if (dist < 40f)
                    {
                        if (random)
                        {
                            SAIN.Talk.Say(BegPhrases.PickRandom());
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private float BegTimer = 0f;
        private readonly EPhraseTrigger[] BegPhrases = { EPhraseTrigger.Stop, EPhraseTrigger.OnBeingHurtDissapoinment, EPhraseTrigger.HoldFire };

        private bool TauntEnemy()
        {
            bool tauntEnemy = false;

            var sainEnemy = SAIN.Enemy;
            var type = SAIN.Info.Personality;

            float distanceToEnemy = Vector3.Distance(sainEnemy.EnemyPosition, BotOwner.Position);

            if (distanceToEnemy <= TauntDist)
            {
                if (sainEnemy.CanShoot && sainEnemy.IsVisible)
                {
                    tauntEnemy = sainEnemy.EnemyLookingAtMe || SAIN.Info.PersonalitySettings.FrequentTaunt;
                }
                if (SAIN.Info.PersonalitySettings.ConstantTaunt)
                {
                    tauntEnemy = true;
                }
            }

            if (!tauntEnemy && BotOwner.AimingData != null)
            {
                var aim = BotOwner.AimingData;
                if (aim != null && aim.IsReady)
                {
                    if (aim.LastDist2Target < TauntDist)
                    {
                        tauntEnemy = true;
                    }
                }
            }

            if (tauntEnemy)
            {
                SAIN.Talk.Say(EPhraseTrigger.OnFight, ETagStatus.Combat, true);
            }

            return tauntEnemy;
        }

        private void StartResponse()
        {
            if (LastEnemyTalk != null)
            {
                float delay = LastEnemyTalk.TalkDelay;
                if (LastEnemyTalk.TalkTime + delay < Time.time)
                {
                    SAIN.Talk.Say(EPhraseTrigger.OnFight, ETagStatus.Combat, true);
                    LastEnemyTalk = null;
                }
            }
        }

        public void SetEnemyTalk(Player player)
        {
            if (LastEnemyTalk == null)
            {
                if ((player.Position - SAIN.Position).sqrMagnitude < ResponseDist * ResponseDist)
                {
                    LastEnemyTalk = new EnemyTalkObject();
                }
            }
        }

        private const float FriendlyResponseChance = 50f;
        private const float FriendlyResponseDistance = 40f;

        public void SetFriendlyTalked(Player player)
        {
            if (EFTMath.RandomBool(FriendlyResponseChance) 
                && BotOwner.Memory.IsPeace 
                && (player.Position - SAIN.Position).sqrMagnitude < FriendlyResponseDistance * FriendlyResponseDistance)
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.MumblePhrase, ETagStatus.Unaware, Random.Range(0.5f, 1f));
            }
        }

        private float LastEnemyCheckTime = 0f;
        private EnemyTalkObject LastEnemyTalk;
        private float TauntTimer = 0f;
    }
}