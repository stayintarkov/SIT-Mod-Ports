using EFT;
using EFT.NextObservedPlayer;
using System;

namespace SAIN.SAINComponent.BaseClasses
{
    public abstract class PersonBaseClass
    {
        public PersonBaseClass(IAIDetails iPlayer)
        {
            IAIDetails = iPlayer;
        }

        public IAIDetails IAIDetails { get; private set; }
        public bool PlayerNull => IAIDetails == null;
        public Player Player => IAIDetails as Player;
    }
}