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
    public class SAINBotMedicine : SAINBase, ISAINClass
    {
        public EHitReaction HitReaction { get; private set; }

        public SAINBotMedicine(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            if (Player != null)
            {
                Player.BeingHitAction += GetHit;
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

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
        }
    }
}