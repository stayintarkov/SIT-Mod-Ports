using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System.Text;
using SAIN.SAINComponent;
using SAIN.Layers.Combat.Solo.Cover;
using System.Collections.Generic;

namespace SAIN.Layers.Combat.Solo
{
    internal class CombatSoloLayer : SAINLayer
    {
        public CombatSoloLayer(BotOwner bot, int priority) : base(bot, priority, Name)
        {
        }

        public static readonly string Name = BuildLayerName<CombatSoloLayer>();

        public override Action GetNextAction()
        {
            SoloDecision Decision = CurrentDecision;
            var SelfDecision = SAIN.Decision.CurrentSelfDecision;
            LastActionDecision = Decision;
            switch (Decision)
            {
                case SoloDecision.MoveToEngage:
                    return new Action(typeof(MoveToEngageAction), $"{Decision}");

                case SoloDecision.RushEnemy:
                    return new Action(typeof(RushEnemyAction), $"{Decision}");

                case SoloDecision.ThrowGrenade:
                    return new Action(typeof(ThrowGrenadeAction), $"{Decision}");

                case SoloDecision.ShiftCover:
                    return new Action(typeof(ShiftCoverAction), $"{Decision}");

                case SoloDecision.RunToCover:
                    if (SAIN.Cover.CoverPoints.Count > 0)
                    {
                        return new Action(typeof(RunToCoverAction), $"{Decision}");
                    }
                    else
                    {
                        NoCoverUseDogFight = true;
                        return new Action(typeof(DogFightAction), $"{Decision} : No Cover Found Yet! Using Dogfight");
                    }

                case SoloDecision.Retreat:
                    if (SAIN.Cover.CoverPoints.Count > 0)
                    {
                        return new Action(typeof(RunToCoverAction), $"{Decision} + {SelfDecision}");
                    }
                    else
                    {
                        NoCoverUseDogFight = true;
                        return new Action(typeof(DogFightAction), $"{Decision} : No Cover Found Yet! Using Dogfight");
                    }

                case SoloDecision.WalkToCover:
                case SoloDecision.UnstuckMoveToCover:
                    if (SAIN.Cover.CoverPoints.Count > 0)
                    {
                        return new Action(typeof(WalkToCoverAction), $"{Decision}");
                    }
                    else
                    {
                        NoCoverUseDogFight = true;
                        return new Action(typeof(DogFightAction), $"{Decision} : No Cover Found Yet! Using Dogfight");
                    }

                case SoloDecision.DogFight:
                case SoloDecision.UnstuckDogFight:
                    return new Action(typeof(DogFightAction), $"{Decision}");

                case SoloDecision.StandAndShoot:
                    return new Action(typeof(StandAndShootAction), $"{Decision}");

                case SoloDecision.HoldInCover:
                    if (SelfDecision != SelfDecision.None)
                    {
                        return new Action(typeof(HoldinCoverAction), $"{Decision} + {SelfDecision}");
                    }
                    else
                    {
                        return new Action(typeof(HoldinCoverAction), $"{Decision}");
                    }

                case SoloDecision.Shoot:
                    return new Action(typeof(ShootAction), $"{Decision}");

                case SoloDecision.Search:
                case SoloDecision.UnstuckSearch:
                    return new Action(typeof(SearchAction), $"{Decision}");

                case SoloDecision.Investigate:
                    return new Action(typeof(InvestigateAction), $"{Decision}");

                default:
                    return new Action(typeof(StandAndShootAction), $"DEFAULT! {Decision}");
            }
        }

        bool NoCoverUseDogFight;

        public override bool IsActive()
        {
            if (SAIN == null) return false;

            return CurrentDecision != SoloDecision.None;
        }

        public override bool IsCurrentActionEnding()
        {
            if (SAIN == null) return true;

            if (NoCoverUseDogFight && SAIN.Cover.CoverPoints.Count > 0)
            {
                NoCoverUseDogFight = false;
                return true;
            }

            return CurrentDecision != LastActionDecision;
        }

        private SoloDecision LastActionDecision = SoloDecision.None;
        public SoloDecision CurrentDecision => SAIN.Memory.Decisions.Main.Current;
    }
}