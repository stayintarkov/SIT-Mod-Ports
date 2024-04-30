using BepInEx.Logging;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Sense;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINVisionClass : SAINBase, ISAINClass
    {
        public SAINVisionClass(SAINComponentClass component) : base(component)
        {
            FlashLightDazzle = new DazzleClass(component);
        }

        public void Init()
        {
        }

        public void Update()
        {
            var Enemy = SAIN.Enemy;
            if (Enemy?.EnemyIPlayer != null && Enemy?.IsVisible == true)
            {
                FlashLightDazzle.CheckIfDazzleApplied(Enemy);
            }
        }

        public static float GetVisibilityModifier(Player player)
        {
            float result = 1f;
            if (player == null)
            {
                return result;
            }
            var pose = player.Pose;
            float speed = player.MovementContext.ClampedSpeed / player.MovementContext.MaxSpeed;
            if (player.MovementContext.IsSprintEnabled)
            {
                result *= 1.33f;
            }
            else
            {
                if (speed <= 0.2f)
                {
                    result *= 0.75f;
                }
                switch (pose)
                {
                    case EPlayerPose.Stand:
                        result *= 1.1f;
                        break;
                    case EPlayerPose.Duck:
                        result *= 0.9f;
                        break;
                    case EPlayerPose.Prone:
                        result *= 0.6f;
                        break;
                }
            }
            result = result.Round100();
            //Logger.LogInfo($"Result: {result} Speed: {speed} Pose: {pose} Sprint? {player.MovementContext.IsSprintEnabled}");
            return result;
        }

        public void Dispose()
        {
        }

        public DazzleClass FlashLightDazzle { get; private set; }
    }
}
