using BepInEx.Logging;
using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotHitReaction : SAINBase, ISAINClass
    {
        public EHitReaction HitReaction { get; private set; }

        public SAINBotHitReaction(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            if (Player != null)
            {
                Player.BeingHitAction += GetHit;
            }
            else
            {
                Logger.LogAndNotifyError("Player is Null in SAINBotHitReaction");
            }
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            if (Player != null)
            {
                Player.BeingHitAction -= GetHit;
            }
        }

        private float HitReceiveTime = 0.33f;
        private float HitRecoverBaseTime = 1f;

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
            PlayerWhoLastShotMe = damageInfo.Player;
            switch (bodyPart)
            {
                case EBodyPart.Head:
                    GetHitInHead(damageInfo);
                    break;

                case EBodyPart.Chest:
                case EBodyPart.Stomach:
                    GetHitInCenter(damageInfo);
                    break;

                case EBodyPart.LeftLeg:
                case EBodyPart.RightLeg:
                    GetHitInLegs(damageInfo);
                    break;

                default:
                    GetHitInArms(damageInfo); 
                    break;
            }
            if (PlayerWhoLastShotMe?.IsAI == false)
            {
                Logger.LogDebug($"{BotOwner.name} is hit in {HitReaction}");
            }
        }

        public GIPlayer PlayerWhoLastShotMe { get; private set; }

        private void GetHitInLegs(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Legs;
        }

        private void GetHitInArms(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Arms;
        }

        private void GetHitInCenter(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Center;
        }

        private void GetHitInHead(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Head;
        }

        const float StunDamageThreshold = 50;
        const float BaseStunTime = 3f;

        private float TimeStunHappened;
        private float StunTime;

        public bool IsStunned 
        { 
            get
            {
                if (_isStunned && StunTime < Time.time)
                {
                    _isStunned = false;
                }
                return _isStunned;
            }
            set
            {
                if (value)
                {
                    TimeStunHappened = Time.time;
                    StunTime = Time.time + BaseStunTime * UnityEngine.Random.Range(0.75f, 1.25f);
                }
                _isStunned = value;
            }
        }

        private bool _isStunned;

        private bool IsStunnedFromDamage(DamageInfo damageInfo)
        {
            return false;
        }
    }

    public enum EHitReaction
    {
        None,
        Head,
        Center,
        Legs,
        Arms,
    }
}