using EFT;
using Interpolation;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static GClass738;

namespace SAIN.BotController.Classes
{
    public class Squad
    {
        public Squad()
        {
            CheckSquadTimer = Time.time + 10f;
        }

        public string GetId()
        {
            if (Id.IsNullOrEmpty())
            {
                return GUID;
            }
            else
            {
                return Id;
            }
        }

        public bool SquadIsSuppressEnemy(string profileId, out SAINComponentClass suppressingMember)
        {
            foreach (var member in Members)
            {
                SAINEnemy enemy = member.Value?.Enemy;
                if (enemy?.EnemyPlayer != null && enemy.EnemyPlayer.ProfileId == profileId && enemy.EnemyIsSuppressed)
                {
                    suppressingMember = member.Value;
                    return true;
                }
            }
            suppressingMember = null;
            return false;
        }

        public List<PlaceForCheck> GroupPlacesForCheck => EFTBotGroup?.PlacesForCheck;

        public bool IsPointTooCloseToLastPlaceForCheck(Vector3 position)
        {
            PlaceForCheck mostRecentPlace = null;
            if (GroupPlacesForCheck != null && GroupPlacesForCheck.Count > 0)
            {
                mostRecentPlace = GroupPlacesForCheck[GroupPlacesForCheck.Count - 1];

                if (mostRecentPlace != null && (position - mostRecentPlace.Position).sqrMagnitude < 2)
                {
                    return true;
                }
            }
            return false;
        }

        public enum ESearchPointType
        {
            Hearing,
            Flashlight,
        }

        public void AddPointToSearch(Vector3 position, float soundPower, BotOwner botOwner, AISoundType soundType, Vector3 originalPosition, IPlayer player, ESearchPointType searchType = ESearchPointType.Hearing)
        {
            if (EFTBotGroup == null)
            {
                EFTBotGroup = botOwner.BotsGroup;
                Logger.LogError("Botsgroup null");
            }
            if (GroupPlacesForCheck == null)
            {
                Logger.LogError("PlacesForCheck null");
                return;
            }

            if (searchType  == ESearchPointType.Hearing)
            {
                try
                {
                    SetVisibleAndHeard(player, position);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }

            bool isDanger = soundType == AISoundType.step ? false : true;
            PlaceForCheckType checkType = isDanger ? PlaceForCheckType.danger : PlaceForCheckType.simple;
            AddNewPlaceForCheck(botOwner, position, checkType, player);
        }

        public readonly Dictionary<IPlayer, PlaceForCheck> PlayerPlaceChecks = new Dictionary<IPlayer, PlaceForCheck>();

        private void SetVisibleAndHeard(IPlayer player, Vector3 position)
        {
            const float SoundAggroDist = 75f;

            bool playerIsHuman = player.IsAI == false;

            if (player != null && Members != null)
            {
                foreach (var member in Members)
                {
                    if (member.Value == null)
                    {
                        continue;
                    }
                    SAINEnemy sainEnemy = member.Value?.EnemyController?.CheckAddEnemy(player);
                    sainEnemy?.SetHeardStatus(true, position);

                    if (playerIsHuman)
                    {
                        member.Value.Cover.ForceCoverFinderState(true, 30f);
                    }

                    if (sainEnemy != null 
                        && member.Value?.Info?.Profile?.IsPMC == true)
                    {
                        float sqrMagnitude = (player.Position - position).sqrMagnitude;
                        if (sqrMagnitude < SoundAggroDist * SoundAggroDist)
                        {
                            BotOwner botOwner = member.Value.BotOwner;
                            if (botOwner != null)
                            {
                                EnemyInfo goalEnemy = botOwner.Memory?.GoalEnemy;
                                if (goalEnemy == null 
                                    && sainEnemy.EnemyInfo != null 
                                    && !sainEnemy.EnemyInfo.HaveSeen)
                                {
                                    // By default bots won't set a goal enemy until actually seen.
                                    // We need PMCs to do this purely on audio, and it seems like this is the simplest way to do so.
                                    // SAIN won't allow them to shoot even if they are set visible here momentarily, so it should be harmless.
                                    sainEnemy.EnemyInfo.SetVisible(true);
                                    botOwner.Memory.GoalEnemy = goalEnemy;
                                    sainEnemy.EnemyInfo.SetVisible(false);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddNewPlaceForCheck(BotOwner botOwner, Vector3 position, PlaceForCheckType checkType, IPlayer player)
        {
            const float navSampleDist = 5f;
            const float dontLerpDist = 50f;

            if (FindNavMesh(position, out Vector3 hitPosition, navSampleDist))
            {
                // Too many places were being sent to a bot, causing confused behavior.
                // This way I'm tying 1 placeforcheck to each player and updating it based on new info.
                PlaceForCheck oldPlace = null;
                bool placeUpdated = false;
                if (PlayerPlaceChecks.ContainsKey(player))
                {
                    oldPlace = PlayerPlaceChecks[player];
                    if (oldPlace != null 
                        && (oldPlace.BasePoint - position).sqrMagnitude <= dontLerpDist * dontLerpDist)
                    {
                        Vector3 averagePosition = averagePosition = Vector3.Lerp(oldPlace.BasePoint, hitPosition, 0.5f);

                        if (FindNavMesh(averagePosition, out hitPosition, navSampleDist) 
                            && CanPathToPoint(hitPosition, botOwner) == NavMeshPathStatus.PathComplete)
                        {
                            //bool isOldPlaceActive = botOwner.Memory.GoalTarget.GoalTarget == oldPlace;

                            PlaceForCheck replacementPlace = new PlaceForCheck(hitPosition, checkType);

                            if (GroupPlacesForCheck.Contains(oldPlace))
                            {
                                GroupPlacesForCheck.Remove(oldPlace);
                            }

                            GroupPlacesForCheck.Add(replacementPlace);
                            PlayerPlaceChecks[player] = replacementPlace;
                            CalcGoalForBot(botOwner);
                            placeUpdated = true;
                        }
                    }
                }

                if (!placeUpdated 
                    && CanPathToPoint(hitPosition, botOwner) == NavMeshPathStatus.PathComplete)
                {
                    PlaceForCheck newPlace = new PlaceForCheck(position, checkType);
                    GroupPlacesForCheck.Add(newPlace);
                    AddOrUpdatePlaceForPlayer(newPlace, player);
                    CalcGoalForBot(botOwner);
                }
            }
        }

        private bool FindNavMesh(Vector3 position, out Vector3 hitPosition, float navSampleDist = 5f)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, navSampleDist, -1))
            {
                hitPosition = hit.position;
                return true;
            }
            Vector3 rayEnd = position + (Vector3.down * navSampleDist * navSampleDist);
            if (NavMesh.Raycast(position, rayEnd, out NavMeshHit hit2, NavMesh.AllAreas))
            {
                hitPosition = hit2.position;
                return true;
            }
            hitPosition = Vector3.zero;
            return false;
        }

        private NavMeshPathStatus CanPathToPoint(Vector3 point, BotOwner botOwner)
        {
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(botOwner.Position, point, -1, path);
            return path.status;
        }

        private void CalcGoalForBot(BotOwner botOwner)
        {
            try
            {
                if (!botOwner.Memory.GoalTarget.HavePlaceTarget() && botOwner.Memory.GoalEnemy == null)
                {
                    botOwner.BotsGroup.CalcGoalForBot(botOwner);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void AddOrUpdatePlaceForPlayer(PlaceForCheck place, IPlayer player)
        {
            if (PlayerPlaceChecks.ContainsKey(player))
            {
                PlayerPlaceChecks[player] = place;
            }
            else
            {
                PlayerPlaceChecks.Add(player, place);
            }
        }

        private BotsGroup EFTBotGroup;

        public string Id { get; private set; } = string.Empty;

        public readonly string GUID = Guid.NewGuid().ToString("N");

        public bool SquadReady { get; private set; }

        public Action<IPlayer, DamageInfo, float> LeaderKilled { get; set; }
        public Action<IPlayer, DamageInfo, float> MemberKilled { get; set; }

        public Action<SAINComponentClass, float> NewLeaderFound { get; set; }

        public bool LeaderIsDeadorNull => LeaderComponent?.Player == null || LeaderComponent?.Player?.HealthController.IsAlive == false;

        public float TimeThatLeaderDied { get; private set; }

        public const float FindLeaderAfterKilledCooldown = 60f;

        public SAINComponentClass LeaderComponent { get; private set; }
        public string LeaderId { get; private set; }

        public float LeaderPowerLevel { get; private set; }

        public bool MemberIsFallingBack
        {
            get
            {
                return MemberHasDecision(SoloDecision.Retreat, SoloDecision.RunAway, SoloDecision.RunToCover);
            }
        }

        public bool MemberIsRegrouping
        {
            get
            {
                return MemberHasDecision(SquadDecision.Regroup);
            }
        }

        public bool MemberHasDecision(params SoloDecision[] decisionsToCheck)
        {
            foreach (var member in Members)
            {
                if (member.Value != null)
                {
                    var memberDecision = member.Value.Decision.CurrentSoloDecision;
                    foreach (var decision in decisionsToCheck)
                    {
                        if (decision == memberDecision)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool MemberHasDecision(params SquadDecision[] decisionsToCheck)
        {
            foreach (var member in Members)
            {
                if (member.Value != null)
                {
                    var memberDecision = member.Value.Decision.CurrentSquadDecision;
                    foreach (var decision in decisionsToCheck)
                    {
                        if (decision == memberDecision)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool MemberHasDecision(params SelfDecision[] decisionsToCheck)
        {
            foreach (var member in Members)
            {
                if (member.Value != null)
                {
                    var memberDecision = member.Value.Decision.CurrentSelfDecision;
                    foreach (var decision in decisionsToCheck)
                    {
                        if (decision == memberDecision)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public float SquadPowerLevel
        {
            get
            {
                float result = 0f;
                foreach (var memberInfo in MemberInfos.Values)
                {
                    if (memberInfo.SAIN != null && memberInfo.SAIN.IsDead == false)
                    {
                        result += memberInfo.PowerLevel;
                    }
                }
                return result;
            }
        }

        public void Update()
        {
            // After 10 seconds since squad is originally created,
            // find a squad leader and activate the squad to give time for all bots to spawn in
            // since it can be staggered over a few seconds.
            if (!SquadReady && CheckSquadTimer < Time.time && Members.Count > 0)
            {
                FindSquadLeader(); 
                SquadReady = true;
                // Timer before starting to recheck
                RecheckSquadTimer = Time.time + 10f;
            }

            // Check happens once the squad is originally "activated" and created
            // Wait until all members are out of combat to find a squad leader, or 60 seconds have passed to find a new squad leader is they are KIA
            if (SquadReady)
            {
                if (RecheckSquadTimer < Time.time && LeaderIsDeadorNull)
                {
                    RecheckSquadTimer = Time.time + 3f;

                    if (TimeThatLeaderDied < Time.time + FindLeaderAfterKilledCooldown)
                    {
                        FindSquadLeader();
                    }
                    else
                    {
                        bool outOfCombat = true;
                        foreach (var member in MemberInfos.Values)
                        {
                            if (member.HasEnemy == true)
                            {
                                outOfCombat = false;
                                break;
                            }
                        }
                        if (outOfCombat)
                        {
                            FindSquadLeader();
                        }
                    }
                }
            }
        }

        private float RecheckSquadTimer;
        private float CheckSquadTimer;

        private void MemberWasKilled(Player player, IPlayer lastAggressor, DamageInfo lastDamageInfo, EBodyPart lastBodyPart)
        {
            if (SAINPlugin.DebugMode)
            {
                Logger.LogInfo(
                    $"Member [{player?.Profile.Nickname}] " +
                    $"was killed for Squad: [{Id}] " +
                    $"by [{lastAggressor?.Profile.Nickname}] " +
                    $"at Time: [{Time.time}] " +
                    $"by damage type: [{lastDamageInfo.DamageType}] " +
                    $"to Body part: [{lastBodyPart}]"
                    );
            }

            MemberKilled?.Invoke(lastAggressor, lastDamageInfo, Time.time);

            if (MemberInfos.TryGetValue(player?.ProfileId, out var member) 
                && member != null)
            {
                // If this killed Member is the squad leader then
                if (member.ProfileId == LeaderId)
                {
                    if (SAINPlugin.DebugMode)
                        Logger.LogInfo($"Leader [{player?.Profile.Nickname}] was killed for Squad: [{Id}]");

                    LeaderKilled?.Invoke(lastAggressor, lastDamageInfo, Time.time);
                    TimeThatLeaderDied = Time.time;
                    LeaderComponent = null;
                }
            }

            RemoveMember(player?.ProfileId);
        }

        public void MemberExtracted(SAINComponentClass sain)
        {
            if (SAINPlugin.DebugMode)
                Logger.LogInfo($"Leader [{sain?.Player?.Profile.Nickname}] Extracted for Squad: [{Id}]");
            RemoveMember(sain?.ProfileId);
        }

        private void FindSquadLeader()
        {
            float power = 0f;
            SAINComponentClass leadComponent = null;

            // Iterate through each memberInfo memberInfo in friendly group to see who has the highest power level or if any are bosses
            foreach (var memberInfo in MemberInfos.Values)
            {
                if (memberInfo.SAIN == null || memberInfo.SAIN.IsDead) continue;

                // If this memberInfo is a boss type, they are the squad leader
                bool isBoss = memberInfo.SAIN.Info.Profile.IsBoss;
                // or If this memberInfo has a higher power level than the last one we checked, they are the squad leader
                if (isBoss || memberInfo.PowerLevel > power)
                {
                    power = memberInfo.PowerLevel;
                    leadComponent = memberInfo.SAIN;

                    if (isBoss)
                    {
                        break;
                    }
                }
            }

            if (leadComponent != null)
            {
                AssignSquadLeader(leadComponent);
            }
        }

        private void AssignSquadLeader(SAINComponentClass sain)
        {
            if (sain?.Player == null)
            {
                Logger.LogError($"Tried to Assign Null SAIN Component or Player for Squad [{Id}], skipping");
                return;
            }

            LeaderComponent = sain;
            LeaderPowerLevel = sain.Info.Profile.PowerLevel;
            LeaderId = sain.Player?.ProfileId;

            NewLeaderFound?.Invoke(sain, Time.time);

            if (SAINPlugin.DebugMode)
            {
                Logger.LogInfo(
                    $" Found New Leader. Name [{sain.BotOwner?.Profile?.Nickname}]" +
                    $" for Squad: [{Id}]" +
                    $" at Time: [{Time.time}]" +
                    $" Group Size: [{Members.Count}]"
                    );
            }
        }

        public void AddMember(SAINComponentClass sain)
        {
            // Make sure nothing is null as a safety check.
            if (sain?.Player != null && sain.BotOwner != null)
            {
                // Make sure this profile ID doesn't already exist for whatever reason
                if (!Members.ContainsKey(sain.ProfileId))
                {
                    // If this is the first member, add their side to the start of their ID for easier identifcation during debug
                    if (Members.Count == 0)
                    {
                        EFTBotGroup = sain.BotOwner.BotsGroup;
                        Id = sain.Player.Profile.Side.ToString() + "_" + GUID;
                    }

                    var memberInfo = new MemberInfo(sain);
                    MemberInfos.Add(sain.ProfileId, memberInfo);
                    Members.Add(sain.ProfileId, sain);

                    // if this new member is a boss, set them to leader automatically
                    if (sain.Info.Profile.IsBoss)
                    {
                        AssignSquadLeader(sain);
                    }
                    // If this new memberInfo has a higher power level than the existing squad leader, set them as the new squad leader if they aren't a boss type
                    else if (LeaderComponent != null && sain.Info.Profile.PowerLevel > LeaderPowerLevel && !LeaderComponent.Info.Profile.IsBoss)
                    {
                        AssignSquadLeader(sain);
                    }

                    // Subscribe when this member is killed
                    sain.Player.OnPlayerDead += MemberWasKilled;
                }
            }
        }

        public void RemoveMember(SAINComponentClass sain)
        {
            RemoveMember(sain?.ProfileId);
        }

        public void RemoveMember(string id)
        {
            if (Members.ContainsKey(id))
            {
                Members.Remove(id);
            }
            if (MemberInfos.TryGetValue(id, out var memberInfo))
            {
                Player player = memberInfo.SAIN?.Player;
                if (player != null)
                {
                    player.OnPlayerDead -= MemberWasKilled;
                }
                memberInfo.Dispose();
                MemberInfos.Remove(id);
            }
        }

        public readonly Dictionary<string, SAINComponentClass> Members = new Dictionary<string, SAINComponentClass>();
        public readonly Dictionary<string, MemberInfo> MemberInfos = new Dictionary<string, MemberInfo>();
    }
}
