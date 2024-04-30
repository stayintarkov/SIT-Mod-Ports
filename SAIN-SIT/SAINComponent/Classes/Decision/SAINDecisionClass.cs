using EFT;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SAINDecisionClass : SAINBase, ISAINClass
    {
        public SAINDecisionClass(SAINComponentClass sain) : base(sain)
        {
            SelfActionDecisions = new SelfActionDecisionClass(sain);
            EnemyDecisions = new EnemyDecisionClass(sain);
            GoalTargetDecisions = new TargetDecisionClass(sain);
            SquadDecisions = new SquadDecisionClass(sain);
        }

        public event Action<SoloDecision, SquadDecision, SelfDecision, float> OnDecisionMade;
        public event Action<SoloDecision, SquadDecision, SelfDecision, float> OnSAINStart;
        public event Action<float> OnSAINEnd;
        public bool SAINActive { get; private set; }

        public void Init()
        {
        }

        public void Update()
        {
            if (BotOwner == null || SAIN == null) return;
            if (!SAIN.BotActive || SAIN.GameIsEnding)
            {
                CurrentSoloDecision = SoloDecision.None;
                CurrentSquadDecision = SquadDecision.None;
                CurrentSelfDecision = SelfDecision.None;
                return;
            }

            CheckDecisionFrameCount++;

            if (CheckDecisionFrameCount >= CheckDecisionFrameTarget)
            {
                CheckEnemyFrameCount++;
                if (CheckEnemyFrameCount >= CheckEnemyFrameTarget)
                {
                    CheckEnemyFrameCount = 0;
                    EnemyDistance = SAIN.HasEnemy ? SAIN.Enemy.CheckPathDistance() : EnemyPathDistance.NoEnemy;
                }

                CheckDecisionFrameCount = 0;
                GetDecision();
            }

            SAINActive = CurrentSoloDecision != SoloDecision.None 
                || CurrentSelfDecision != SelfDecision.None 
                || CurrentSquadDecision != SquadDecision.None;
        }

        private const int CheckDecisionFrameTarget = 6;
        private int CheckDecisionFrameCount = 0;

        private const int CheckEnemyFrameTarget = 6;
        private int CheckEnemyFrameCount = 0;

        public void Dispose()
        {
        }

        public EnemyPathDistance EnemyDistance { get; private set; }

        public SoloDecision CurrentSoloDecision { get; private set; }
        public SoloDecision OldSoloDecision { get; private set; }

        public SquadDecision CurrentSquadDecision { get; private set; }
        public SquadDecision OldSquadDecision { get; private set; }

        public SelfDecision CurrentSelfDecision { get; private set; }
        public SelfDecision OldSelfDecision { get; private set; }

        public SelfActionDecisionClass SelfActionDecisions { get; private set; }
        public EnemyDecisionClass EnemyDecisions { get; private set; }
        public TargetDecisionClass GoalTargetDecisions { get; private set; }
        public SquadDecisionClass SquadDecisions { get; private set; }
        public List<SoloDecision> RetreatDecisions { get; private set; } = new List<SoloDecision> { SoloDecision.Retreat };
        public float ChangeDecisionTime { get; private set; }
        public float TimeSinceChangeDecision => Time.time - ChangeDecisionTime;

        public void ResetDecisions()
        {
            UpdateDecisionProperties(SoloDecision.None, SquadDecision.None, SelfDecision.None);
            BotOwner.CalcGoal();
        }

        private void GetDecision()
        {
            if (CheckContinueRetreat())
            {
                return;
            }

            SelfActionDecisions.GetDecision(out SelfDecision selfDecision);
            SquadDecisions.GetDecision(out SquadDecision squadDecision);

            if (CheckStuckDecision(out SoloDecision soloDecision))
            {
            }
            else if (EnemyDecisions.GetDecision(out soloDecision))
            {
            }
            else if (GoalTargetDecisions.GetDecision(out soloDecision))
            {
            }

            UpdateDecisionProperties(soloDecision, squadDecision, selfDecision);
        }

        private void UpdateDecisionProperties(SoloDecision solo, SquadDecision squad, SelfDecision self)
        {
            if (self != SelfDecision.None)
            {
                solo = SoloDecision.Retreat;
            }

            if (SAINPlugin.ForceSoloDecision != SoloDecision.None)
            {
                solo = SAINPlugin.ForceSoloDecision;
            }
            if (SAINPlugin.ForceSquadDecision != SquadDecision.None)
            {
                squad = SAINPlugin.ForceSquadDecision;
            }
            if (SAINPlugin.ForceSelfDecision != SelfDecision.None)
            {
                self = SAINPlugin.ForceSelfDecision;
            }

            OldSoloDecision = CurrentSoloDecision;
            CurrentSoloDecision = solo;

            OldSquadDecision = CurrentSquadDecision;
            CurrentSquadDecision = squad;

            OldSelfDecision = CurrentSelfDecision;
            CurrentSelfDecision = self;

            CheckForNewDecisions();
        }

        private void CheckForNewDecisions()
        {
            bool newDecision = false;
            float time = Time.time;

            if (CurrentSoloDecision != OldSoloDecision)
            {
                ChangeDecisionTime = time;
                newDecision = true;
            }
            if (CurrentSelfDecision != OldSelfDecision)
            {
                ChangeSelfDecisionTime = time;
                newDecision = true;
            }
            if (CurrentSquadDecision != OldSquadDecision)
            {
                ChangeSquadDecisionTime = time;
                newDecision = true;
            }

            if (newDecision)
            {
                OnDecisionMade?.Invoke(CurrentSoloDecision, CurrentSquadDecision, CurrentSelfDecision, time);

                // If previously all decisions were none, sain has now started.
                if (OldSoloDecision == SoloDecision.None 
                    && OldSelfDecision == SelfDecision.None
                    && OldSquadDecision == SquadDecision.None)
                {
                    OnSAINStart?.Invoke(CurrentSoloDecision, CurrentSquadDecision, CurrentSelfDecision, time);
                }

                // Are all decisions None? Then SAIN is no longer active.
                if (CurrentSoloDecision == SoloDecision.None
                    && CurrentSelfDecision == SelfDecision.None
                    && CurrentSquadDecision == SquadDecision.None)
                {
                    OnSAINEnd?.Invoke(time);
                }
            }
        }

        public float ChangeSelfDecisionTime { get; private set; }
        public float ChangeSquadDecisionTime { get; private set; }

        private bool CheckContinueRetreat()
        {
            float timeChangeDec = SAIN.Decision.TimeSinceChangeDecision;
            bool Running = CurrentSoloDecision == SoloDecision.Retreat || CurrentSoloDecision == SoloDecision.RunToCover;
            if (Running && !SAIN.BotStuck.BotHasChangedPosition && SAIN.BotStuck.TimeSpentNotMoving > 1f && timeChangeDec > 0.5f)
            {
                return false;
            }
            CoverPoint pointInUse = SAIN.Cover.CoverInUse;
            if (pointInUse != null && pointInUse.BotInThisCover(SAIN))
            {
                return false;
            }

            bool CheckTime = timeChangeDec < 30f; 
            bool Moving = BotOwner.Mover.HasPathAndNoComplete;
             
            return Running && BotOwner.Mover.HasPathAndNoComplete && CheckTime;
        }

        private bool CheckStuckDecision(out SoloDecision Decision)
        {
            Decision = SoloDecision.None;
            bool stuck = SAIN.BotStuck.BotIsStuck;

            if (!stuck && FinalBotUnstuckTimer != 0f)
            {
                FinalBotUnstuckTimer = 0f;
            }

            if (stuck && BotUnstuckTimerDecision < Time.time)
            {
                if (FinalBotUnstuckTimer == 0f)
                {
                    FinalBotUnstuckTimer = Time.time + 10f;
                }

                BotUnstuckTimerDecision = Time.time + 5f;

                var current = this.CurrentSoloDecision;
                if (FinalBotUnstuckTimer < Time.time && SAIN.HasEnemy)
                {
                    Decision = SoloDecision.UnstuckDogFight;
                    return true;
                }
                if (current == SoloDecision.Search || current == SoloDecision.UnstuckSearch)
                {
                    Decision = SoloDecision.UnstuckMoveToCover;
                    return true;
                }
                if (current == SoloDecision.WalkToCover || current == SoloDecision.UnstuckMoveToCover)
                {
                    Decision = SoloDecision.UnstuckSearch;
                    return true;
                }
            }
            return false;
        }

        private float BotUnstuckTimerDecision = 0f;
        private float FinalBotUnstuckTimer = 0f;
    }
}