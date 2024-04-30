using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SAIN.SAINComponent.SAINComponentClass;
using UnityEngine.UIElements;
using UnityEngine;
using Comfort.Common;

namespace SAIN.SAINComponent.Classes
{
    public class SAINAILimit : SAINBase, ISAINClass
    {
        public SAINAILimit(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init() { }

        public void Update() { }

        public void UpdateAILimit()
        {
            if (SAIN == null || BotOwner == null)
            {
                return;
            }
            CheckForAILimit();
            LimitAIThisFrame = ShallLimitAI();
        }

        public void Dispose() { }

        public bool LimitAIThisFrame { get; private set; }
        public AILimitSetting CurrentAILimit { get; private set; }

        private AILimitSetting CheckForAILimit()
        {
            if (SAIN.IsHumanACareEnemy)
            {
                CurrentAILimit = AILimitSetting.Close;
                return CurrentAILimit;
            }

            if (CheckDistancesTimer < Time.time)
            {
                CheckDistancesTimer = Time.time + 3f;
                var gameWorld = GameWorldHandler.SAINGameWorld;
                if (gameWorld != null && gameWorld.FindClosestPlayer(out float closestPlayerSqrMag, SAIN.Position) != null)
                {
                    CurrentAILimit = CheckDistances(closestPlayerSqrMag);
                }
            }
            return CurrentAILimit;
        }

        private AILimitSetting CheckDistances(float closestPlayerSqrMag)
        {
            const float NarniaDist = 600;
            const float VeryFarDist = 400;
            const float FarDist = 200f;

            if (closestPlayerSqrMag < FarDist * FarDist)
            {
                return AILimitSetting.Close;
            }
            else if (closestPlayerSqrMag < VeryFarDist * VeryFarDist)
            {
                return AILimitSetting.Far;
            }
            else if (closestPlayerSqrMag < NarniaDist * NarniaDist)
            {
                return AILimitSetting.VeryFar;
            }
            else
            {
                return AILimitSetting.Narnia;
            }
        }

        private float CheckDistancesTimer;

        private bool ShallLimitAI()
        {
            float timeToAdd = 0f;
            switch (CurrentAILimit)
            {
                case AILimitSetting.Far:
                    timeToAdd = 0.5f;
                    break;

                case AILimitSetting.VeryFar:
                    timeToAdd = 1f;
                    break;

                case AILimitSetting.Narnia:
                    timeToAdd = 2f;
                    break;

                default:
                    break;
            }

            if (CurrentAILimit != AILimitSetting.Close && AILimitTimer + timeToAdd > Time.time)
            {
                return true;
            }
            AILimitTimer = Time.time;
            return false;
        }

        private float AILimitTimer;
    }
}
