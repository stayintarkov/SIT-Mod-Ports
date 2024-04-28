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
    public class SAINBotSuppressClass : SAINBase, ISAINClass
    {
        public SAINBotSuppressClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            UpdateSuppressedStatus();
        }

        public void Dispose()
        {
        }

        public float SuppressionNumber { get; private set; }
        public float SuppressionAmount => IsSuppressed ? SuppressionNumber - SuppressionThreshold : 0;
        public float SuppressionStatModifier => 1 + (SuppressionAmount * (SuppressionSpreadMultiPerPoint));
        public bool IsSuppressed => SuppressionNumber > SuppressionThreshold;
        public bool IsHeavySuppressed => SuppressionNumber > SuppressionHeavyThreshold;

        public readonly float SuppressionSpreadMultiPerPoint = 0.15f;
        private readonly float SuppressionThreshold = 4f;
        private readonly float SuppressionHeavyThreshold = 10f;
        private readonly float SuppressionDecayAmount = 0.5f;
        private readonly float SuppressionDecayUpdateFreq = 0.5f;
        private readonly float SuppressionAddDefault = 2f;
        private float SuppressionDecayTimer;

        public void AddSuppression(float num = -1)
        {
            if (num < 0)
            {
                num = SuppressionAddDefault;
            }
            SuppressionNumber += num;
            if (SuppressionNumber > 15)
            {
                SuppressionNumber = 15;
            }
        }

        public void UpdateSuppressedStatus()
        {
            if (SuppressionDecayTimer < Time.time && SuppressionNumber > 0)
            {
                SuppressionDecayTimer = Time.time + SuppressionDecayUpdateFreq;
                SuppressionNumber -= SuppressionDecayAmount;
                if (SuppressionNumber < 0)
                {
                    SuppressionNumber = 0;
                }
            }
        }
    }
}