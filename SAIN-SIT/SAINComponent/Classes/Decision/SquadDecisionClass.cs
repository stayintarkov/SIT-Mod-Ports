using BepInEx.Logging;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Info;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SquadDecisionClass : SAINBase, ISAINClass
    {
        public SquadDecisionClass(SAINComponentClass sain) : base(sain)
        {
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

        private SAINSquadClass Squad => SAIN.Squad;

        float SquaDDecision_DontDoSquadDecision_EnemySeenRecentTime = 3f;

        public bool GetDecision(out SquadDecision Decision)
        {
            Decision = SquadDecision.None;
            if (!Squad.BotInGroup || Squad.LeaderComponent?.IsDead == true)
            {
                return false;
            }
            if (SAIN.Enemy?.IsVisible == true || SAIN.Enemy?.TimeSinceSeen < SquaDDecision_DontDoSquadDecision_EnemySeenRecentTime)
            {
                return false;
            }

            if (EnemyDecision(out Decision))
            {
                return true;
            }
            if (StartRegroup())
            {
                Decision = SquadDecision.Regroup;
                return true;
            }

            return false;
        }

        float SquaDecision_RadioCom_MaxDistSq = 1200f;
        float SquadDecision_MyEnemySeenRecentTime = 5f;

        private bool EnemyDecision(out SquadDecision Decision)
        {
            Decision = SquadDecision.None;
            foreach (var member in SAIN.Squad.Members.Values)
            {
                if (member == null || member.BotOwner == BotOwner || member.BotOwner.IsDead)
                {
                    continue;
                }
                if (!HasRadioComms && (SAIN.Transform.Position - member.Transform.Position).sqrMagnitude > SquaDecision_RadioCom_MaxDistSq)
                {
                    continue;
                }
                var myEnemy = SAIN.Enemy;
                if (myEnemy != null && member.HasEnemy)
                {
                    if (myEnemy.EnemyIPlayer == member.Enemy.EnemyIPlayer)
                    {
                        if (StartSuppression(member))
                        {
                            Decision = SquadDecision.Suppress;
                            return true;
                        }
                        if (myEnemy.IsVisible || myEnemy.TimeSinceSeen < SquadDecision_MyEnemySeenRecentTime)
                        {
                            return false;
                        }
                        if (StartGroupSearch(member))
                        {
                            Decision = SquadDecision.Search;
                            return true;
                        }
                        if (StartHelp(member))
                        {
                            Decision = SquadDecision.Help;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool HasRadioComms => SAIN.Equipment.HasEarPiece;

        float SquadDecision_SuppressFriendlyDistStart = 30f;
        float SquadDecision_SuppressFriendlyDistEnd = 50f;

        private bool StartSuppression(SAINComponentClass member)
        {
            bool memberRetreat = member.Memory.Decisions.Main.Current == SoloDecision.Retreat;
            float memberDistance = (member.Transform.Position - BotOwner.Position).magnitude;
            float ammo = SAIN.Decision.SelfActionDecisions.AmmoRatio;
            if (memberRetreat && memberDistance < SquadDecision_SuppressFriendlyDistStart && ammo > 0.5f)
            {
                return true;
            }
            if (SAIN.Memory.Decisions.Squad.Current == SquadDecision.Suppress && !EndSuppresion(memberDistance, memberRetreat, ammo))
            {
                return true;
            }
            return false;
        }

        private bool EndSuppresion(float memberDistance, bool memberRetreat, float ammoRatio)
        {
            if (!memberRetreat || memberDistance >= SquadDecision_SuppressFriendlyDistEnd || ammoRatio <= 0.1f)
            {
                return true;
            }
            return false;
        }

        private bool StartGroupSearch(SAINComponentClass member)
        {
            bool squadSearching = member.Memory.Decisions.Main.Current == SoloDecision.Search || member.Decision.CurrentSquadDecision == SquadDecision.Search;
            if (squadSearching)
            {
                return true;
            }
            return false;
        }

        float SquadDecision_StartHelpFriendDist = 15f;
        float SquadDecision_EndHelpFriendDist = 25f;
        float SquadDecision_EndHelp_FriendsEnemySeenRecentTime = 5f;

        private bool StartHelp(SAINComponentClass member)
        {
            float distance = member.Enemy.PathDistance;
            bool visible = member.Enemy.IsVisible;
            if (distance < SquadDecision_StartHelpFriendDist && visible)
            {
                return true;
            }
            if (SAIN.Memory.Decisions.Squad.Current == SquadDecision.Help && !EndHelp(member, distance))
            {
                return true;
            }
            return false;
        }

        private bool EndHelp(SAINComponentClass member, float distance)
        {
            if (distance > SquadDecision_EndHelpFriendDist || member.Enemy.TimeSinceSeen > SquadDecision_EndHelp_FriendsEnemySeenRecentTime)
            {
                return true;
            }
            return false;
        }

        float SquadDecision_Regroup_NoEnemy_StartDist = 125f;
        float SquadDecision_Regroup_NoEnemy_EndDistance = 50f;
        float SquadDecision_Regroup_Enemy_StartDist = 50f;
        float SquadDecision_Regroup_Enemy_EndDistance = 15f;
        float SquadDecision_Regroup_EnemySeenRecentTime = 60f;

        public bool StartRegroup()
        {
            var squad = SAIN.Squad;
            if (squad.IAmLeader)
            {
                return false;
            }

            float maxDist = SquadDecision_Regroup_NoEnemy_StartDist;
            float minDist = SquadDecision_Regroup_NoEnemy_EndDistance;

            var enemy = SAIN.Enemy;
            if (enemy != null)
            {
                if (enemy.IsVisible || (enemy.Seen && enemy.TimeSinceSeen < SquadDecision_Regroup_EnemySeenRecentTime))
                {
                    return false;
                }
                maxDist = SquadDecision_Regroup_Enemy_StartDist;
                minDist = SquadDecision_Regroup_Enemy_EndDistance;
            }

            var lead = squad.LeaderComponent;
            if (lead != null)
            {
                Vector3 BotPos = BotOwner.Position;
                Vector3 leadPos = lead.Transform.Position;
                Vector3 directionToLead = leadPos - BotPos;
                float leadDistance = directionToLead.magnitude;
                if (enemy != null)
                {
                    Vector3 EnemyPos = enemy.EnemyPosition;
                    Vector3 directionToEnemy = EnemyPos - BotPos;
                    float EnemyDistance = directionToEnemy.magnitude;
                    if (EnemyDistance < leadDistance)
                    {
                        if (EnemyDistance < 30f && Vector3.Dot(directionToEnemy.normalized, directionToLead.normalized) > 0.25f)
                        {
                            return false;
                        }
                    }
                }
                if (SAIN.Memory.Decisions.Squad.Current == SquadDecision.Regroup)
                {
                    return leadDistance > minDist;
                }
                else
                {
                    return leadDistance > maxDist;
                }
            }
            return false;
        }
    }
}