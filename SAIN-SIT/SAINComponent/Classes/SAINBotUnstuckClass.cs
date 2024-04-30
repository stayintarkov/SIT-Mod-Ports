using Comfort.Common;
using EFT;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

using PathControllerClass = PathController;

namespace SAIN.SAINComponent.Classes.Debug
{
    public class SAINBotUnstuckClass : SAINBase, ISAINClass
    {
        static SAINBotUnstuckClass()
        {
            _pathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
        }

        private static readonly FieldInfo _pathControllerField;

        public SAINBotUnstuckClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            PathController = _pathControllerField.GetValue(BotOwner.Mover) as PathControllerClass;
            DontUnstuckMe = DontUnstuckTheseTypes.Contains(SAIN.Info.Profile.WildSpawnType);
        }

        public bool BotIsMoving { get; private set; }
        private bool _botIsMoving;

        public float TimeStartedMoving { get; private set; }
        public float TimeStoppedMoving { get; private set; }

        private float _timeStartMoving;

        private float _timeStopMoving;
        public float TimeSinceMovingStarted => TimeStartedMoving - Time.time;

        private void CheckIfMoving()
        {
            float time = Time.time;

            // Check if a bot is being told to move by the botowner pathfinder, and record the time this starts and stops
            bool botWasMoving = _botIsMoving;
            _botIsMoving = BotOwner.Mover.IsMoving;
            if (_botIsMoving && !botWasMoving)
            {
                _timeStartMoving = time;
            }
            else if (!_botIsMoving && botWasMoving)
            {
                _timeStopMoving = time;
            }

            // How long a bot must be "moving" for before we check
            const float movingForPeriod = 0.25f;

            // Has the bot been told to move for over x seconds? then record the state for use in other classes
            if (_botIsMoving && time - _timeStartMoving > movingForPeriod)
            {
                if (!BotIsMoving)
                {
                    TimeStartedMoving = time;
                }
                BotIsMoving = true;
            }
            else if (!_botIsMoving && time - _timeStopMoving > movingForPeriod)
            {
                if (BotIsMoving)
                {
                    TimeStoppedMoving = time;
                }
                BotIsMoving = false;
            }
        }

        private void CheckRecalcPath()
        {
            if (BotIsMoving 
                && Time.time - TimeStartedMoving > 0.5f 
                && _recalcPathVault < Time.time)
            {
                if (Player.MovementContext.TryVaulting())
                {
                    _botVaulted = true;
                    _recalcPathVault = Time.time + 2f;
                    return;
                }
                else
                {
                    _recalcPathVault = Time.time + 1f;
                }
            }

            if (_recalcPathTimer < Time.time 
                && BotIsMoving 
                && !BotHasChangedPosition 
                && Time.time - TimeStartedMoving > 1f)
            {
                _recalcPathTimer = Time.time + 2f;
                BotOwner.Mover.RecalcWay();
            }
        }

        private float _recalcPathVault;
        private float _recalcPathTimer;

        private void CheckIfPositionChanged()
        {
            if (CheckPositionTimer < Time.time)
            {
                CheckPositionTimer = Time.time + 0.5f;

                bool botChangedPositionLast = BotHasChangedPosition;

                const float DistThreshold = 0.1f;
                BotHasChangedPosition = (LastPos - BotOwner.Position).sqrMagnitude > DistThreshold * DistThreshold;

                if (botChangedPositionLast && !BotHasChangedPosition)
                {
                    TimeStartedChangingPosition = Time.time;
                }
                else if (BotHasChangedPosition)
                {
                    TimeStartedChangingPosition = 0f;
                }

                LastPos = BotOwner.Position;
            }
        }

        private float _nextVaultCheckTime;
        private bool DontUnstuckMe;

        private static readonly List<WildSpawnType> DontUnstuckTheseTypes = new List<WildSpawnType>
        {
            WildSpawnType.marksman,
            WildSpawnType.shooterBTR,
        };

        private void CheckBotVaulted()
        {
            if (_botVaulted)
            {
                _botVaulted = false;
                _botVaultedTime = Time.time;
            }

            if (_botVaultedTime != -1f 
                && _botVaultedTime + 0.75f < Time.time)
            {
                _botVaultedTime = -1f;
                BotOwner.Mover.RecalcWay();
            }
        }

        private bool _botVaulted;
        private float _botVaultedTime;

        public void Update()
        {
            if (SAIN.BotActive
                && !SAIN.GameIsEnding)
            {
                if (DontUnstuckMe)
                {
                    return;
                }

                if (_nextVaultCheckTime < Time.time && BotOwner.Mover.IsMoving && TimeStartedMoving + 1f < Time.time)
                {
                    _nextVaultCheckTime = Time.time + 0.5f;
                    if (SAIN.Decision.CurrentSoloDecision != SoloDecision.HoldInCover)
                    {
                        if (Player.MovementContext.TryVaulting())
                        {
                            _botVaulted = true;
                            _nextVaultCheckTime = Time.time + 2f;
                        }
                        else
                        {
                            _nextVaultCheckTime = Time.time + 0.5f;
                        }
                    }
                }

                CheckIfMoving();
                CheckIfPositionChanged();
                CheckRecalcPath(); 
                CheckBotVaulted();

                if (CheckStuckTimer < Time.time)
                {
                    if (BotOwner.DoorOpener.Interacting)
                    {
                        CheckStuckTimer = Time.time + 1f;
                        BotIsStuck = false;
                    }
                    else
                    {
                        CheckStuckTimer = Time.time + 0.5f;

                        bool stuck = BotStuckGeneric()
                            || BotStuckOnObject()
                            || BotStuckOnPlayer();

                        if (!BotIsStuck && stuck)
                        {
                            TimeStuck = Time.time;
                        }
                        BotIsStuck = stuck;
                    }

                    // If the bot is no longer stuck, but we are checking if we can teleport them, cancel the coroutine
                    if (!BotIsStuck
                        && TeleportCoroutineStarted
                        && TeleportCoroutine != null)
                    {
                        SAIN.StopCoroutine(TeleportCoroutine);
                        TeleportCoroutineStarted = false;
                        HasTriedJumpOrVault = false;
                        JumpTimer = Time.time + 1f;
                        IsTeleporting = false;
                    }

                    if (BotIsStuck && TimeSinceStuck > 2f)
                    {
                        if (DebugStuckTimer < Time.time && TimeSinceStuck > 5f)
                        {
                            DebugStuckTimer = Time.time + 5f;
                            Logger.LogWarning($"[{BotOwner.name}] has been stuck for [{TimeSinceStuck}] seconds " +
                                $"on [{StuckHit.transform?.name}] object " +
                                $"at [{StuckHit.transform?.position}] " +
                                $"with Current Decision as [{SAIN.Memory.Decisions.Main.Current}]");
                        }

                        if (HasTriedJumpOrVault
                            && TimeSinceStuck > 10f
                            && TimeSinceTriedJumpOrVault + 2f < Time.time
                            && !TeleportCoroutineStarted)
                        {
                            TeleportCoroutineStarted = true;
                            TeleportCoroutine = SAIN.StartCoroutine(CheckIfTeleport());
                        }

                        if (JumpTimer < Time.time && TimeSinceStuck > 2f)
                        {
                            JumpTimer = Time.time + 1f;

                            if (!Player.MovementContext.TryVaulting())
                            {
                                if (TimeSinceStuck > 5f)
                                {
                                    if (!HasTriedJumpOrVault)
                                    {
                                        HasTriedJumpOrVault = true;
                                        TimeSinceTriedJumpOrVault = Time.time;
                                    }
                                    //SAIN.Mover.TryJump();
                                }
                            }
                            else
                            {
                                _botVaulted = true;
                            }
                        }
                    }
                }
            }
        }

        private float TimeSinceTriedJumpOrVault;
        private bool HasTriedJumpOrVault;
        private const float MinDistance = 100f;
        private const float MaxDistance = 300f;
        private const float PathLengthCoef = 1.25f;
        private const float MinDistancePathLength = MinDistance * PathLengthCoef;

        private bool TeleportCoroutineStarted;
        private Coroutine TeleportCoroutine;

        private IEnumerator CheckIfTeleport()
        {
            bool shallTeleport = true;
            var humanPlayers = GetHumanPlayers();

            Vector3? teleportDestination = null;

            const float minTeleDist = 1f;
            if (BotOwner.Mover.HavePath)
            {
                for (int i = PathController.CurPath.CurIndex; i < PathController.CurPath.Length - 1; i++)
                {
                    Vector3 corner = PathController.CurPath.GetPoint(i);
                    Vector3 cornerDirection = corner - SAIN.Position;
                    float cornerDistance = cornerDirection.sqrMagnitude;
                    if (cornerDirection.sqrMagnitude >= minTeleDist * minTeleDist)
                    {
                        teleportDestination = new Vector3?(corner);
                        yield break;
                    }
                    yield return null;
                }
            }

            Vector3 botPosition = SAIN.Position;

            if (teleportDestination != null)
            {
                var allPlayers = Singleton<GameWorld>.Instance?.AllAlivePlayersList;
                if (allPlayers != null)
                {
                    foreach (var player in allPlayers)
                    {
                        if (ShallCheckPlayer(player))
                        {
                            if (!BotIsStuck)
                            {
                                shallTeleport = false;
                                yield break;
                            }

                            // Make sure the player isn't visible to the bot
                            if (SAIN.Memory.VisiblePlayers.Contains(player))
                            {
                                shallTeleport = false;
                                break;
                            }

                            Vector3 playerPosition = player.Position;

                            // Makes sure the bot isn't too close to a human for them to hear
                            float sqrMag = (playerPosition - botPosition).sqrMagnitude;
                            if (sqrMag < MinDistance * MinDistance)
                            {
                                shallTeleport = false;
                                break;
                            }

                            // Checks the max distance to do a path calculation
                            if (sqrMag < MaxDistance * MaxDistance)
                            {
                                NavMeshPath path = CalcPath(botPosition, playerPosition, out float pathLength);
                                if (CheckPathLength(playerPosition, path, pathLength) == false)
                                {
                                    shallTeleport = false;
                                    break;
                                }
                            }

                            // Check next player on the next frame
                            yield return null;
                        }
                    }
                }
            }

            IsTeleporting = BotIsStuck && shallTeleport && teleportDestination != null;

            if (IsTeleporting)
            {
                Teleport(teleportDestination.Value + Vector3.up * 0.25f);
                float distance = (teleportDestination.Value - botPosition).magnitude;
                Logger.LogDebug($"Teleporting stuck bot: [{Player.name}] [{distance}] meters to the next corner they are trying to go to");
            }

            yield return null;
        }

        private bool IsTeleporting;

        private bool CheckVisibilityToAllPlayers(Vector3 point)
        {
            var allPlayers = Singleton<GameWorld>.Instance?.AllAlivePlayersList;
            if (allPlayers == null)
            {
                return false;
            }

            Vector3 testPoint = point + Vector3.up;

            foreach (var player in allPlayers)
            {
                if (ShallCheckPlayer(player))
                {

                }
            }
            return false;
        }

        private bool ShallCheckPlayer(Player player)
        {
            if (Player == null || Player.HealthController == null || Player.AIData == null)
            {
                return false;
            }
            return Player.HealthController.IsAlive == true && Player.AIData.IsAI == false;
        }

        private void Teleport(Vector3 position)
        {
            if (teleportTimer < Time.time)
            {
                teleportTimer = Time.time + 3f;
                Player.Teleport(position);
            }
        }

        private float teleportTimer;

        public PathControllerClass PathController { get; private set; }

        private static NavMeshPath CalcPath(Vector3 start, Vector3 end, out float pathLength)
        {
            if (PathToPlayer == null)
            {
                PathToPlayer = new NavMeshPath();
            }
            if (NavMesh.SamplePosition(end, out NavMeshHit hit, 1f, -1))
            {
                PathToPlayer.ClearCorners();
                if (NavMesh.CalculatePath(start, hit.position, -1, PathToPlayer))
                {
                    pathLength = PathToPlayer.CalculatePathLength();
                    return PathToPlayer;
                }
            }
            pathLength = 0f;
            return null;
        }

        private static bool CheckPathLength(Vector3 end, NavMeshPath path, float pathLength)
        {
            if (path == null)
            {
                return false;
            }
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                Vector3 lastCorner = path.corners[path.corners.Length - 1];
                float sqrMag = (lastCorner - end).magnitude;
                float combinedLength = sqrMag + pathLength;
                if (combinedLength < MinDistancePathLength)
                {
                    return false;
                }
            }
            if (path.status == NavMeshPathStatus.PathComplete && pathLength < MinDistancePathLength)
            {
                return false;
            }
            return path.status != NavMeshPathStatus.PathInvalid;
        }

        private List<Player> GetHumanPlayers()
        {
            HumanPlayers.Clear();
            var allPlayers = Singleton<GameWorld>.Instance?.AllAlivePlayersList;
            if (allPlayers != null)
            {
                foreach (var player in allPlayers)
                {
                    if (player != null && player.AIData.IsAI == false && player.HealthController.IsAlive)
                    {
                        HumanPlayers.Add(player);
                    }
                }
            }
            return HumanPlayers;
        }

        private bool ShallTeleport()
        {
            return false;
        }

        private static NavMeshPath PathToPlayer;
        private List<Player> HumanPlayers = new List<Player>();

        public void Dispose()
        {
        }

        private RaycastHit StuckHit = new RaycastHit();
        private float DebugStuckTimer = 0f;
        private float CheckStuckTimer = 0f;
        public float TimeSinceStuck => Time.time - TimeStuck;
        public float TimeStuck { get; private set; }

        private float CheckPositionTimer = 0f;

        private Vector3 LastPos = Vector3.zero;

        public float TimeSpentNotMoving => Time.time - TimeStartedChangingPosition;

        public float TimeStartedChangingPosition { get; private set; }

        public bool BotIsStuck { get; private set; }

        private bool CanBeStuckDecisions(SoloDecision decision)
        {
            return decision == SoloDecision.Search || decision == SoloDecision.WalkToCover || decision == SoloDecision.DogFight || decision == SoloDecision.RunToCover || decision == SoloDecision.RunAway || decision == SoloDecision.UnstuckSearch || decision == SoloDecision.UnstuckDogFight || decision == SoloDecision.UnstuckMoveToCover;
        }

        public bool BotStuckOnPlayer()
        {
            var decision = SAIN.Memory.Decisions.Main.Current;
            if (!BotHasChangedPosition && CanBeStuckDecisions(decision))
            {
                if (BotOwner.Mover == null)
                {
                    return false;
                }
                Vector3 botPos = BotOwner.Position;
                botPos.y += 0.4f;
                Vector3 moveDir = BotOwner.Mover.DirCurPoint;
                moveDir.y = 0;
                Vector3 lookDir = BotOwner.LookDirection;
                lookDir.y = 0;

                var moveHits = Physics.SphereCastAll(botPos, 0.15f, moveDir, 0.5f, LayerMaskClass.PlayerMask);
                if (moveHits.Length > 0)
                {
                    foreach (var move in moveHits)
                    {
                        if (move.transform.name != BotOwner.name)
                        {
                            StuckHit = move;
                            return true;
                        }
                    }
                }

                var lookHits = Physics.SphereCastAll(botPos, 0.15f, lookDir, 0.5f, LayerMaskClass.PlayerMask);
                if (lookHits.Length > 0)
                {
                    foreach (var look in lookHits)
                    {
                        if (look.transform.name != BotOwner.name)
                        {
                            StuckHit = look;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool BotStuckGeneric()
        {
            return BotIsMoving && !BotHasChangedPosition && !BotOwner.DoorOpener.Interacting && TimeSpentNotMoving > 2f;
        }

        public bool BotStuckOnObject()
        {
            if (CanBeStuckDecisions(SAIN.Memory.Decisions.Main.Current) && !BotHasChangedPosition && !BotOwner.DoorOpener.Interacting && SAIN.Decision.TimeSinceChangeDecision > 1f)
            {
                if (BotOwner.Mover == null)
                {
                    return false;
                }
                Vector3 botPos = BotOwner.Position;
                botPos.y += 0.4f;
                Vector3 moveDir = BotOwner.Mover.DirCurPoint;
                moveDir.y = 0;
                if (Physics.SphereCast(botPos, 0.15f, moveDir, out var hit, 0.25f, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    StuckHit = hit;
                    return true;
                }
            }
            return false;
        }

        public bool BotHasChangedPosition { get; private set; }

        private float JumpTimer = 0f;
    }
}