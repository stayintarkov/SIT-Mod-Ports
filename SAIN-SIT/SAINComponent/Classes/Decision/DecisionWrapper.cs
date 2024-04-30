using System;
using System.Collections.Generic;
using static SAIN.SAINComponent.Classes.Decision.DecisionWrapper;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class DecisionWrapper : SAINComponentAbstract, ISAINClass
    {
        public DecisionWrapper(SAINComponentClass sain) : base(sain)
        {
            Main = new DecisionTypeWrapper<SoloDecision>(sain);
            Squad = new DecisionTypeWrapper<SquadDecision>(sain);
            Self = new DecisionTypeWrapper<SelfDecision>(sain);
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public DecisionTypeWrapper<SoloDecision> Main { get; private set; }
        public DecisionTypeWrapper<SquadDecision> Squad { get; private set; }
        public DecisionTypeWrapper<SelfDecision> Self { get; private set; }

        public class DecisionTypeWrapper<T> where T : Enum
        {
            public DecisionTypeWrapper(SAINComponentClass sain)
            {
                SAIN = sain;
                Type = typeof(T);
            }

            private readonly SAINComponentClass SAIN;
            private Type Type;

            public T Current
            {
                get
                {
                    if (Type == Solo)
                    {
                        return (T)(object)SAIN.Decision.CurrentSoloDecision;
                    }
                    else if (Type == Squad)
                    {
                        return (T)(object)SAIN.Decision.CurrentSquadDecision;
                    }
                    else if (Type == Self)
                    {
                        return (T)(object)SAIN.Decision.CurrentSelfDecision;
                    }
                    Logger.LogError($"Could not find Current Decision of Type {Type}");
                    return default;
                }
            }

            public T Last
            {
                get
                {
                    if (Type == Solo)
                    {
                        return (T)(object)SAIN.Decision.OldSoloDecision;
                    }
                    else if (Type == Squad)
                    {
                        return (T)(object)SAIN.Decision.OldSquadDecision;
                    }
                    else if (Type == Self)
                    {
                        return (T)(object)SAIN.Decision.OldSelfDecision;
                    }
                    Logger.LogError($"Could not find Last Decision of Type {Type}");
                    return default;
                }
            }

            private static readonly Type Solo = typeof(SoloDecision);
            private static readonly Type Squad = typeof(SquadDecision);
            private static readonly Type Self = typeof(SelfDecision);

        }
    }
}