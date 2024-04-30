using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent.BaseClasses;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Debug;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Info;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes.Talk;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent
{
    public class SAINComponentClass : MonoBehaviour, IBotComponent
    {
        public static bool TryAddSAINToBot(BotOwner botOwner, out SAINComponentClass sainComponent)
        {
            Player player = EFTInfo.GetPlayer(botOwner?.ProfileId);
            GameObject gameObject = botOwner?.gameObject;

            if (gameObject != null && player != null)
            {
                // If Somehow this bot already has SAIN attached, destroy it.
                if (gameObject.TryGetComponent(out sainComponent))
                {
                    sainComponent.Dispose();
                }

                // Create a new Component
                sainComponent = gameObject.AddComponent<SAINComponentClass>();

                if (SAINPersonComponent.TryAddSAINPersonToPlayer(player, out SAINPersonComponent personComponent)
                    && sainComponent?.Init(personComponent?.SAINPerson) == true)
                {
                    return true;
                }
            }
            sainComponent = null;
            return false;
        }

        private void PlayerKilled(EDamageType damageType)
        {
            if (damageType == EDamageType.Bullet)
            {
                IFirearmHandsController firearmHandsController = Player?.HandsController as IFirearmHandsController;
                if (firearmHandsController != null)
                {
                    // firearmHandsController.SetTriggerPressed(true);
                }
            }
        }

        public Action<string, BotOwner> OnSAINDisposed { get; set; }
        public SAINPersonClass Person { get; private set; }

        public bool Init(SAINPersonClass person)
        {
            if (person == null)
            {
                Logger.LogAndNotifyError("Person is Null in SAINComponent Init");
                return false;
            }

            Person = person;

            try
            {
                NoBushESP = this.GetOrAddComponent<SAINNoBushESP>();
                NoBushESP.Init(person.BotOwner, this);
                FlashLight = person.Player?.gameObject?.AddComponent<SAINFlashLightComponent>();

                // Must be first, other classes use it
                Squad = new SAINSquadClass(this);
                Equipment = new SAINBotEquipmentClass(this);
                Info = new SAINBotInfoClass(this);
                Memory = new SAINMemoryClass(this);
                BotStuck = new SAINBotUnstuckClass(this);
                Hearing = new SAINHearingSensorClass(this);
                Talk = new SAINBotTalkClass(this);
                Decision = new SAINDecisionClass(this);
                Cover = new SAINCoverClass(this);
                SelfActions = new SAINSelfActionClass(this);
                Steering = new SAINSteeringClass(this);
                Grenade = new SAINBotGrenadeClass(this);
                Mover = new SAINMoverClass(this);
                EnemyController = new SAINEnemyController(this);
                Sounds = new SAINSoundsController(this);
                FriendlyFireClass = new SAINFriendlyFireClass(this);
                Vision = new SAINVisionClass(this);
                Search = new SAINSearchClass(this);
                Vault = new SAINVaultClass(this);
                Suppression = new SAINBotSuppressClass(this);
                AILimit = new SAINAILimit(this);
                AimDownSightsController = new AimDownSightsController(this);
                BotHitReaction = new SAINBotHitReaction(this);
                SpaceAwareness = new SAINBotSpaceAwareness(this);

                NavMeshAgent = this.GetComponent<NavMeshAgent>();
                if (NavMeshAgent == null)
                {
                    Logger.LogError("Agent Null");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Init SAIN ERROR, Disposing.");
                Logger.LogError(ex);
                Dispose();
                return false;
            }

            Search.Init();
            Memory.Init();
            EnemyController.Init();
            FriendlyFireClass.Init();
            Sounds.Init();
            Vision.Init();
            Equipment.Init();
            Mover.Init();
            BotStuck.Init();
            Hearing.Init();
            Talk.Init();
            Decision.Init();
            Cover.Init();
            Info.Init();
            Squad.Init();
            SelfActions.Init();
            Grenade.Init();
            Steering.Init();
            Vault.Init();
            Suppression.Init();
            AILimit.Init();
            AimDownSightsController.Init();
            BotHitReaction.Init();
            SpaceAwareness.Init();

            try
            {
                Player.HealthController.DiedEvent += PlayerKilled;
            }
            catch
            {

            }

            TimeBotCreated = Time.time;

            return true;
        }

        public NavMeshAgent NavMeshAgent { get; private set; }

        public float TimeBotCreated { get; private set; }


        private void Update()
        {
            if (IsDead || Singleton<GameWorld>.Instance == null)
            {
                Dispose();
                return;
            }

            if (GameIsEnding)
            {
                return;
            }

            if (BotActive)
            {
                HandlePatrolData();

                AILimit.UpdateAILimit();

                if (AILimit.LimitAIThisFrame)
                {
                    return;
                }

                Search.Update();
                Memory.Update();
                EnemyController.Update();
                FriendlyFireClass.Update();
                Sounds.Update();
                Vision.Update();
                Equipment.Update();
                Mover.Update();
                BotStuck.Update();
                Hearing.Update();
                Talk.Update();
                Decision.Update();
                Cover.Update();
                Info.Update();
                Squad.Update();
                SelfActions.Update();
                Grenade.Update();
                Steering.Update();
                Vault.Update();
                Suppression.Update();
                AimDownSightsController.Update();
                BotHitReaction.Update();
                SpaceAwareness.Update();

                BotOwner.DoorOpener.Update(); 
                UpdateGoalTarget();
            }
        }

        private Stopwatch Stopwatch = new Stopwatch();

        private void LateUpdate()
        {
        }

        private static float LogTimer;

        public bool IsHumanACareEnemy
        {
            get
            {
                if (HasEnemy && !Enemy.IsAI)
                {
                    return true;
                }
                var closestHeard = EnemyController.ClosestHeardEnemy;
                if (closestHeard != null && !closestHeard.IsAI)
                {
                    return true;
                }

                foreach (var player in Memory.VisiblePlayers)
                {
                    if (!player.IsAI)
                    {
                        return true;
                    }
                }
                
                return false;
            }
        }

        private void HandlePatrolData()
        {
            if (CurrentTargetPosition == null)
            {
                BotOwner.PatrollingData?.Unpause();
            }
            else
            {
                BotOwner.PatrollingData?.Pause();
            }
        }

        public AILimitSetting CurrentAILimit => AILimit.CurrentAILimit;

        public bool BotIsAlive => Player?.HealthController?.IsAlive == true;

        public float DistanceToAimTarget
        {
            get
            {
                if (BotOwner.AimingData != null)
                {
                    return BotOwner.AimingData.LastDist2Target;
                }
                if (Enemy != null)
                {
                    return Enemy.RealDistance;
                }
                else if (CurrentTargetPosition != null)
                {
                    return (CurrentTargetPosition.Value - Position).magnitude;
                }
                return 200f;
            }
        }

        public bool Shoot(bool value, bool checkFF = true, EShootReason reason = EShootReason.None)
        {
            ManualShootReason = value ? reason : EShootReason.None;

            if (value)
            {
                if (checkFF && !FriendlyFireClass.ClearShot)
                {
                    BotOwner.ShootData.EndShoot();
                    return false;
                }
                else
                {
                    return BotOwner.ShootData.Shoot();
                }
            }
            return false;
        }

        public EShootReason ManualShootReason { get; private set; }

        public enum EShootReason
        {
            None = 0,
            SquadSuppressing = 1,
            Blindfire = 2,
            WalkToCoverSuppress = 3,
        }

        private bool SAINActive => BigBrainHandler.IsBotUsingSAINLayer(BotOwner);

        public bool LayersActive
        {
            get
            {
                if (RecheckTimer < Time.time)
                {
                    CombatLayersActive = BigBrainHandler.IsBotUsingSAINCombatLayer(BotOwner);
                    if (SAINActive)
                    {
                        RecheckTimer = Time.time + 0.5f;
                        Active = true;
                    }
                    else
                    {
                        RecheckTimer = Time.time + 0.05f;
                        Active = false;
                    }
                }
                return Active;
            }
        }
        public bool CombatLayersActive { get; private set; }

        private bool Active;
        private float RecheckTimer = 0f;

        public void Dispose()
        {
            try
            {
                OnSAINDisposed?.Invoke(ProfileId, BotOwner);

                try
                {
                    Player.HealthController.DiedEvent -= PlayerKilled;
                }
                catch { }

                StopAllCoroutines();

                Search.Dispose();
                Memory.Dispose();
                EnemyController.Dispose();
                FriendlyFireClass.Dispose();
                Sounds.Dispose();
                Vision.Dispose();
                Equipment.Dispose();
                Mover.Dispose();
                BotStuck.Dispose();
                Hearing.Dispose();
                Talk.Dispose();
                Decision.Dispose();
                Cover.Dispose();
                Info.Dispose();
                Squad.Dispose();
                SelfActions.Dispose();
                Grenade.Dispose();
                Steering.Dispose();
                Enemy?.Dispose();
                Vault?.Dispose();
                Suppression?.Dispose();
                AILimit?.Dispose();
                AimDownSightsController?.Dispose();
                BotHitReaction?.Dispose();
                SpaceAwareness?.Dispose();

                try
                {
                    GameObject.Destroy(SteeringController);
                    GameObject.Destroy(NoBushESP);
                    GameObject.Destroy(FlashLight);
                }
                catch { }

                Destroy(this);
            }
            catch { }
        }

        private void UpdateGoalTarget()
        {
            if (updateGoalTargetTimer < Time.time)
            {
                updateGoalTargetTimer = Time.time + 0.5f;
                var Target = GoalTargetPosition;
                if (Target != null)
                {
                    if ((Target.Value - Position).sqrMagnitude < 2f)
                    {
                        if (EFTMath.RandomBool(33))
                        {
                            Talk.Say(EFTMath.RandomBool() ? EPhraseTrigger.Clear : EPhraseTrigger.LostVisual, null, true);
                        }
                        BotOwner.Memory.GoalTarget.Clear();
                        BotOwner.CalcGoal();
                    }
                }
            }
            var placesForCheck = Squad.SquadInfo?.GroupPlacesForCheck;
            if (placesForCheck != null)
            {
                foreach (var place in placesForCheck)
                {
                    if (place != null)
                    {
                        DebugGizmos.Line(place.Position, Position, 0.025f, Time.deltaTime, true);
                    }
                }
            }
        }

        private float updateGoalTargetTimer;

        public Vector3? GoalTargetPosition => BotOwner.Memory.GoalTarget.Position;

        public Vector3? CurrentTargetPosition
        {
            get
            {
                if (HasEnemy && Enemy.IsVisible)
                {
                    return Enemy.EnemyPosition;
                }
                if (BotOwner.Memory.IsUnderFire)
                {
                    return Memory.UnderFireFromPosition;
                }

                    // This is incredibly inefficient, need to limit the times this is called per frame.
                if (HasEnemy)
                {
                    Vector3? lastPlaceHaventSeen = Enemy.KnownPlaces.GetPlaceHaventSeen()?.Position;
                    if (lastPlaceHaventSeen != null)
                    {
                        return lastPlaceHaventSeen;
                    }

                    lastPlaceHaventSeen = Enemy.KnownPlaces.GetPlaceHaventArrived()?.Position;
                    if (lastPlaceHaventSeen != null)
                    {
                        return lastPlaceHaventSeen;
                    }

                    if (Enemy.LastKnownPosition != null)
                    {
                        return Enemy.LastKnownPosition;
                    }
                    return Enemy.EnemyPosition;
                }
                var placeForCheck = BotOwner.Memory.GoalTarget?.GoalTarget;
                if (placeForCheck != null)
                {
                    return placeForCheck.Position;
                }
                if (Time.time - BotOwner.Memory.LastTimeHit < 10f)
                {
                    return BotOwner.Memory.LastHitPos;
                }
                return null;
            }
        }

        public SAINBotSpaceAwareness SpaceAwareness { get; private set; }
        public SAINBotHitReaction BotHitReaction { get; private set; }
        public AimDownSightsController AimDownSightsController { get; private set; }
        public SAINAILimit AILimit { get; private set; }
        public SAINBotSuppressClass Suppression { get; private set; }
        public SAINVaultClass Vault { get; private set; }
        public SAINSearchClass Search { get; private set; }
        public SAINEnemy Enemy => HasEnemy ? EnemyController.ActiveEnemy : null;
        public SAINPersonTransformClass Transform => Person.Transform;
        public SAINMemoryClass Memory { get; private set; }
        public SAINEnemyController EnemyController { get; private set; }
        public SAINNoBushESP NoBushESP { get; private set; }
        public SAINFriendlyFireClass FriendlyFireClass { get; private set; }
        public SAINSoundsController Sounds { get; private set; }
        public SAINVisionClass Vision { get; private set; }
        public SAINBotEquipmentClass Equipment { get; private set; }
        public SAINMoverClass Mover { get; private set; }
        public SAINBotUnstuckClass BotStuck { get; private set; }
        public SAINFlashLightComponent FlashLight { get; private set; }
        public SAINHearingSensorClass Hearing { get; private set; }
        public SAINBotTalkClass Talk { get; private set; }
        public SAINDecisionClass Decision { get; private set; }
        public SAINCoverClass Cover { get; private set; }
        public SAINBotInfoClass Info { get; private set; }
        public SAINSquadClass Squad { get; private set; }
        public SAINSelfActionClass SelfActions { get; private set; }
        public SAINBotGrenadeClass Grenade { get; private set; }
        public SAINSteeringClass Steering { get; private set; }
        public SAINSteeringController SteeringController { get; private set; }

        public bool IsDead => BotOwner == null || BotOwner.IsDead == true || Player == null || Player.HealthController.IsAlive == false;
        public bool BotActive => IsDead == false && BotOwner.enabled && Player.enabled && BotOwner.BotState == EBotState.Active && BotOwner.StandBy.StandByType == BotStandByType.active;
        public bool GameIsEnding => Singleton<IBotGame>.Instance == null || Singleton<IBotGame>.Instance.Status == GameStatus.Stopping;

        public Vector3 Position => Person.Position;
        public BotOwner BotOwner => Person?.BotOwner;
        public string ProfileId => Person?.ProfileId;
        public Player Player => Person?.Player;
        public bool HasEnemy => EnemyController.HasEnemy;
    }
}