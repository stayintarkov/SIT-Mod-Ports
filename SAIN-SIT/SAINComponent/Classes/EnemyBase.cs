using EFT;
using SAIN.SAINComponent.BaseClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public abstract class EnemyBase
    {
        public EnemyBase(SAINEnemy enemy)
        {
            Enemy = enemy;
        }

        public SAINComponentClass SAIN => Enemy.SAIN;
        public EnemyInfo EnemyInfo => Enemy.EnemyInfo;
        public Player EnemyPlayer => Enemy.EnemyPlayer;
        public IPlayer EnemyIPlayer => Enemy.EnemyPerson.IPlayer;
        public BotOwner BotOwner => Enemy.BotOwner;
        public SAINEnemy Enemy { get; private set; }
        public SAINPersonClass EnemyPerson => Enemy.EnemyPerson;
        public SAINPersonTransformClass EnemyTransform => Enemy.EnemyTransform;
        public Vector3 EnemyPosition => Enemy.EnemyPosition;
        public Vector3 EnemyDirection => Enemy.EnemyDirection;
        public bool IsCurrentEnemy => Enemy.IsCurrentEnemy;
    }
}