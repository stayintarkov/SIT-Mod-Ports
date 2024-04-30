using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Info;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngineInternal;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class GroupTalk : SAINBase, ISAINClass
    {
        public GroupTalk(SAINComponentClass bot) : base(bot)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (!SAIN.BotActive || SAIN.GameIsEnding || !SAIN.Talk.CanTalk || !BotSquad.BotInGroup)
            {
                if (Subscribed)
                {
                    Dispose();
                }
                return;
            }

            if (!SAIN.Info.FileSettings.Mind.SquadTalk)
            {
                return;
            }

            if (!Subscribed)
            {
                Subscribe();
            }

            if (TalkTimer < Time.time)
            {
                TalkTimer = Time.time + 0.33f;
                FriendIsClose = AreFriendsClose();
                if (FriendIsClose)
                {
                    if (!TalkHurt())
                    {
                        if (!TalkCurrentAction())
                        {
                            if (SAIN.Enemy != null)
                            {
                                TalkEnemyLocation();
                            }
                        }
                    }

                    if (SAIN.Enemy != null && SAIN.Squad.IAmLeader)
                    {
                        UpdateLeaderCommand();
                    }
                }
            }
        }

        private void EnemyConversation(EPhraseTrigger trigger, ETagStatus status, Player player)
        {
            if (player == null)
            {
                return;
            }
            if (SAIN.HasEnemy || !FriendIsClose)
            {
                return;
            }
            if (!BotOwner.BotsGroup.IsPlayerEnemy(player))
            {
                return;
            }
            if ((player.Position - SAIN.Position).sqrMagnitude > 50f * 50f)
            {
                return;
            }
            if (EFTMath.RandomBool(33))
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.OnEnemyConversation, ETagStatus.Aware, Random.Range(0.33f, 0.66f));
                SAIN.EnemyController.GetEnemy(player.ProfileId)?.SetHeardStatus(true, player.Position);
            }
        }

        public void TalkEnemySniper()
        {
            if (FriendIsClose)
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.SniperPhrase, ETagStatus.Combat, UnityEngine.Random.Range(0.5f, 1f));
            }
        }

        public void Dispose()
        {
            if (Subscribed)
            {
                SAIN.Squad.SquadInfo.MemberKilled -= FriendlyDown;
                SAINPlugin.BotController.PlayerTalk -= EnemyConversation;
                BotOwner.BotsGroup.OnReportEnemy -= Contact;
                BotOwner.DeadBodyWork.OnStartLookToBody -= LootStuff;
                BotOwner.BotsGroup.OnEnemyRemove -= EnemyDown;
                Subscribed = false;
            }
        }

        private void Subscribe()
        {
            if (!Subscribed)
            {
                Subscribed = true;

                SAIN.Squad.SquadInfo.MemberKilled += FriendlyDown;
                SAINPlugin.BotController.PlayerTalk += EnemyConversation;
                BotOwner.BotsGroup.OnReportEnemy += Contact;
                BotOwner.DeadBodyWork.OnStartLookToBody += LootStuff;
                BotOwner.BotsGroup.OnEnemyRemove += EnemyDown;
            }
        }

        private void EnemyDown(IPlayer person)
        {
            if (!FriendIsClose || !PersonIsClose(person))
            {
                return;
            }
            if (EFTMath.RandomBool(60))
            {
                if (!SAIN.Squad.IAmLeader)
                {
                    float randomTime = UnityEngine.Random.Range(0.33f, 1f);
                    SAIN.Talk.TalkAfterDelay(EPhraseTrigger.EnemyDown, ETagStatus.Aware, randomTime);
                    SAIN.Squad.SquadInfo?.LeaderComponent?.Talk?.TalkAfterDelay(EPhraseTrigger.GoodWork, ETagStatus.Aware, randomTime + 0.5f);
                }
            }
        }

        private bool PersonIsClose(IPlayer player)
        {
            return player != null && BotOwner != null && (player.Position - BotOwner.Position).magnitude < 30f;
        }

        public bool FriendIsClose;

        private const float LeaderFreq = 1f;
        private const float TalkFreq = 0.5f;
        private const float FriendTooFar = 30f;
        private const float FriendTooClose = 5f;
        private const float EnemyTooClose = 5f;

        private void FriendlyDown(IPlayer player, DamageInfo damage, float time)
        {
            if (BotOwner.IsDead || BotOwner.BotState != EBotState.Active)
            {
                return;
            }
            if (!FriendIsClose || !PersonIsClose(player))
            {
                return;
            }
            if (EFTMath.RandomBool(60))
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.OnFriendlyDown, ETagStatus.Combat, UnityEngine.Random.Range(0.33f, 1f));
            }
        }

        private float FirstContactTimer = 0f;
        private const float FirstContactFreq = 5f;

        private void Contact(IPlayer person, Vector3 enemyPos, Vector3 weaponRootLast, EEnemyPartVisibleType isVisibleOnlyBySense)
        {
            if (BotOwner.IsDead || BotOwner.BotState != EBotState.Active)
            {
                return;
            }
            if (!FriendIsClose)
            {
                return;
            }

            if (FirstContactTimer < Time.time)
            {
            }
        }

        private void LootStuff(float num)
        {
            if (BotOwner.IsDead || BotOwner.BotState != EBotState.Active)
            {
                return;
            }
            if (!FriendIsClose)
            {
                return;
            }

            var trigger = LootPhrases.PickRandom();

            SAIN.Talk.Say(trigger, null, true);
        }

        private readonly List<EPhraseTrigger> LootPhrases = new List<EPhraseTrigger> { EPhraseTrigger.LootBody, EPhraseTrigger.LootContainer, EPhraseTrigger.LootGeneric, EPhraseTrigger.LootKey, EPhraseTrigger.LootMoney, EPhraseTrigger.LootNothing, EPhraseTrigger.LootWeapon, EPhraseTrigger.OnLoot };

        private bool AreFriendsClose()
        {
            foreach (var member in SAIN.Squad.Members.Values)
            {
                if (member.Player != null && member.Player.ProfileId != Player.ProfileId && member.BotIsAlive && (member.Position - BotOwner.Position).sqrMagnitude < (20f * 20f))
                {
                    return true;
                }
            }
            return false;
        }

        private void AllMembersSay(EPhraseTrigger trigger, ETagStatus mask, float delay = 1.5f, float chance = 100f)
        {
            foreach (var member in BotSquad.Members.Values)
            {
                if (member?.BotIsAlive == true && SAIN.Squad.LeaderComponent != null && !member.Squad.IAmLeader && member.Squad.DistanceToSquadLeader <= 20f)
                {
                    if (EFTMath.RandomBool(chance))
                    {
                        member.Talk.TalkAfterDelay(trigger, mask, delay * UnityEngine.Random.Range(0.75f, 2f));
                    }
                }
            }
        }

        private void UpdateLeaderCommand()
        {
            if (LeaderComponent != null)
            {
                if (BotSquad.IAmLeader && LeaderTimer < Time.time)
                {
                    LeaderTimer = Time.time + Randomized * SAIN.Info.FileSettings.Mind.SquadLeadTalkFreq;

                    if (!CheckIfLeaderShouldCommand())
                    {
                        if (CheckFriendliesTimer < Time.time 
                            && CheckFriendlyLocation(out var trigger))
                        {
                            CheckFriendliesTimer = Time.time + SAIN.Info.FileSettings.Mind.SquadLeadTalkFreq * 5f;

                            SAIN.Talk.Say(trigger);
                            AllMembersSay(EPhraseTrigger.Roger, ETagStatus.Aware, Random.Range(1f, 3f), 50f);
                        }
                    }
                }
            }
        }

        private float CheckFriendliesTimer = 0f;

        private bool TalkHurt()
        {
            if (HurtTalkTimer < Time.time)
            {
                var trigger = EPhraseTrigger.PhraseNone;
                HurtTalkTimer = Time.time + SAIN.Info.FileSettings.Mind.SquadMemberTalkFreq * 5f * Random.Range(0.66f, 1.33f);

                if (SAIN.HasEnemy && SAIN.Enemy.RealDistance < 20f)
                {
                    return false;
                }

                var health = SAIN.Memory.HealthStatus;
                switch (health)
                {
                    case ETagStatus.Injured:
                        if (EFTMath.RandomBool(25))
                        {
                            trigger = EFTMath.RandomBool() ? EPhraseTrigger.HurtMedium : EPhraseTrigger.HurtLight;
                        }
                        break;

                    case ETagStatus.BadlyInjured:
                        trigger = EPhraseTrigger.HurtHeavy; break;
                    case ETagStatus.Dying:
                        trigger = EPhraseTrigger.HurtNearDeath; break;
                    default:
                        trigger = EPhraseTrigger.PhraseNone; break;
                }

                if (trigger != EPhraseTrigger.PhraseNone)
                {
                    SAIN.Talk.Say(trigger);
                    return true;
                }
            }
            return false;
        }

        public bool TalkRetreat => SAIN.Enemy?.IsVisible == true && SAIN.Decision.RetreatDecisions.Contains(SAIN.Memory.Decisions.Main.Current);

        public bool TalkCurrentAction()
        {
            EPhraseTrigger trigger;

            if (TalkRetreat)
            {
                trigger = EPhraseTrigger.NeedHelp;
            }
            else if (SAIN.Suppression.IsSuppressed)
            {
                trigger = EPhraseTrigger.UnderFire;
            }
            else if (!HearNoise(out trigger, out var mask))
            {
                if (SAIN.Enemy != null)
                {
                    if (!TalkBotDecision(out trigger, out mask) && BotOwner.Memory.IsUnderFire)
                    {
                        trigger = EPhraseTrigger.NeedHelp;
                    }
                }
            }

            if (trigger != EPhraseTrigger.PhraseNone)
            {
                SAIN.Talk.Say(trigger, null, true);
                return true;
            }
            return false;
        }

        private bool HearNoise(out EPhraseTrigger trigger, out ETagStatus mask)
        {
            trigger = EPhraseTrigger.PhraseNone;
            mask = ETagStatus.Aware;

            if (SAIN.Enemy != null)
            {
                return false;
            }

            var hear = BotOwner.BotsGroup.YoungestPlace(BotOwner, 50f, true);

            if (hear != null)
            {
                if (hear.CheckingPlayer != null && hear.CheckingPlayer.ProfileId != BotOwner.ProfileId)
                {
                    return false;
                }
                if (!hear.IsDanger)
                {
                    if (hear.CreatedTime + 0.5f < Time.time && hear.CreatedTime + 1f > Time.time)
                    {
                        trigger = EPhraseTrigger.NoisePhrase;
                        mask = ETagStatus.Aware;
                    }
                }
            }

            return trigger != EPhraseTrigger.PhraseNone;
        }

        private bool TalkBotDecision(out EPhraseTrigger trigger, out ETagStatus mask)
        {
            mask = ETagStatus.Combat;
            switch (SAIN.Memory.Decisions.Self.Current)
            {
                case SelfDecision.Reload:
                    trigger = EPhraseTrigger.OnWeaponReload;
                    break;

                case SelfDecision.RunAway:
                    trigger = EPhraseTrigger.OnYourOwn;
                    break;

                case SelfDecision.FirstAid:
                case SelfDecision.Stims:
                    trigger = EPhraseTrigger.CoverMe;
                    break;

                default:
                    trigger = EPhraseTrigger.PhraseNone;
                    break;
            }

            return trigger != EPhraseTrigger.PhraseNone;
        }

        public bool CheckIfLeaderShouldCommand()
        {
            if (CommandSayTimer < Time.time)
            {
                var mySquadDecision = SAIN.Memory.Decisions.Squad.Current;
                var myCurrentDecision = SAIN.Memory.Decisions.Main.Current;

                CommandSayTimer = Time.time + SAIN.Info.FileSettings.Mind.SquadLeadTalkFreq;
                var commandTrigger = EPhraseTrigger.PhraseNone;
                var trigger = EPhraseTrigger.PhraseNone;
                var gesture = EGesture.None;

                if (SAIN.Squad.SquadInfo?.MemberHasDecision(SquadDecision.Suppress) == true)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.Suppress;
                    trigger = EPhraseTrigger.Roger;
                }
                else if (mySquadDecision == SquadDecision.Search)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.FollowMe;
                    trigger = EPhraseTrigger.Going;
                }
                else if (SAIN.Squad.MemberIsFallingBack)
                {
                    gesture = EGesture.ComeToMe;
                    commandTrigger = EFTMath.RandomBool() ? EPhraseTrigger.GetInCover : EPhraseTrigger.GetBack;
                    trigger = EPhraseTrigger.PhraseNone;
                }
                else if (BotOwner.DoorOpener.Interacting && EFTMath.RandomBool(33f))
                {
                    commandTrigger = EPhraseTrigger.OpenDoor;
                    trigger = EPhraseTrigger.Roger;
                }
                else if (myCurrentDecision == SoloDecision.RunAway)
                {
                    commandTrigger = EPhraseTrigger.OnYourOwn;
                    trigger = EFTMath.RandomBool() ? EPhraseTrigger.Repeat : EPhraseTrigger.Stop;
                }
                else if (SAIN.Squad.SquadInfo?.MemberIsRegrouping == true)
                {
                    gesture = EGesture.ComeToMe;
                    commandTrigger = EPhraseTrigger.Regroup;
                    trigger = EPhraseTrigger.Roger;
                }
                else if (mySquadDecision == SquadDecision.Help)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.Gogogo;
                    trigger = EPhraseTrigger.Going;
                }
                else if (myCurrentDecision == SoloDecision.HoldInCover)
                {
                    gesture = EGesture.Stop;
                    commandTrigger = EPhraseTrigger.HoldPosition;
                    trigger = EPhraseTrigger.Roger;
                }

                if (commandTrigger != EPhraseTrigger.PhraseNone)
                {
                    if (gesture != EGesture.None && SAIN.Squad.VisibleMembers.Count > 0 && SAIN.Enemy?.IsVisible == false)
                    {
                        Player.HandsController.ShowGesture(gesture);
                    }
                    if (SAIN.Squad.VisibleMembers.Count / (float)SAIN.Squad.Members.Count < 0.5f)
                    {
                        SAIN.Talk.Say(commandTrigger);
                        AllMembersSay(trigger, ETagStatus.Aware, Random.Range(0.75f, 1.5f), 35f);
                    }
                    return true;
                }
            }

            return false;
        }

        private float EnemyPosTimer = 0f;

        public bool TalkEnemyLocation()
        {
            if (EnemyPosTimer < Time.time)
            {
                EnemyPosTimer = Time.time + 1f;
                var trigger = EPhraseTrigger.PhraseNone;
                var mask = ETagStatus.Aware;

                var enemy = SAIN.Enemy;
                if (SAIN.Enemy.IsVisible && enemy.EnemyLookingAtMe)
                {
                    if (enemy.EnemyLookingAtMe && EFTMath.RandomBool())
                    {
                        mask = ETagStatus.Combat;
                        bool injured = !SAIN.Memory.Healthy && !SAIN.Memory.Injured;
                        trigger = injured ? EPhraseTrigger.NeedHelp : EPhraseTrigger.OnRepeatedContact;
                    }
                    else
                    {
                        EnemyDirectionCheck(enemy.EnemyPosition, out trigger, out mask);
                    }
                }

                if (trigger == EPhraseTrigger.PhraseNone && enemy.Seen)
                {
                    if (enemy.TimeSinceSeen > 60f && _trySayRatTimer < Time.time)
                    {
                        _trySayRatTimer = Time.time + 60f * Random.Range(0.5f, 1.5f);

                        if (EFTMath.RandomBool(33))
                        {
                            trigger = EPhraseTrigger.Rat;
                        }
                    }
                    else if (enemy.TimeSinceSeen > 20f && _trySayLostContactTimer < Time.time)
                    {
                        _trySayLostContactTimer = Time.time + 60f * Random.Range(0.5f, 1.5f);

                        if (EFTMath.RandomBool(45))
                        {
                            trigger = EFTMath.RandomBool() ? EPhraseTrigger.OnLostVisual : EPhraseTrigger.LostVisual;
                        }
                    }
                }

                if (trigger != EPhraseTrigger.PhraseNone)
                {
                    SAIN.Talk.Say(trigger, mask, true);
                    return true;
                }
            }

            return false;
        }

        private bool EnemyDirectionCheck(Vector3 enemyPosition, out EPhraseTrigger trigger, out ETagStatus mask)
        {
            // Check Behind
            if (IsEnemyInDirection(enemyPosition, 180f, AngleToDot(75f)))
            {
                mask = ETagStatus.Aware;
                trigger = EPhraseTrigger.OnSix;
                return true;
            }

            // Check Left Flank
            if (IsEnemyInDirection(enemyPosition, -90f, AngleToDot(33f)))
            {
                mask = ETagStatus.Aware;
                trigger = EPhraseTrigger.LeftFlank;
                return true;
            }

            // Check Right Flank
            if (IsEnemyInDirection(enemyPosition, 90f, AngleToDot(33f)))
            {
                mask = ETagStatus.Aware;
                trigger = EPhraseTrigger.RightFlank;
                return true;
            }

            // Check Front
            if (IsEnemyInDirection(enemyPosition, 0f, AngleToDot(33f)))
            {
                mask = ETagStatus.Combat;
                trigger = EPhraseTrigger.InTheFront;
                return true;
            }

            trigger = EPhraseTrigger.PhraseNone;
            mask = ETagStatus.Unaware;
            return false;
        }

        private float AngleToRadians(float angle)
        {
            return (angle * (Mathf.PI)) / 180;
        }

        private float AngleToDot(float angle)
        {
            return Mathf.Cos(AngleToRadians(angle));
        }

        private bool CheckFriendlyLocation(out EPhraseTrigger trigger)
        {
            trigger = EPhraseTrigger.PhraseNone;

            int tooClose = 0;
            int total = 0;

            foreach (var member in SAIN.Squad.Members.Values)
            {
                if (member == null) continue;

                total++;
                if ((member.Position - SAIN.Position).sqrMagnitude <= FriendTooClose * FriendTooClose)
                {
                    tooClose++;
                }
            }

            float tooCloseRatio = (float)tooClose / (float)total;

            if (tooCloseRatio > 0.5f)
            {
                trigger = EPhraseTrigger.Spreadout;
            }
            else if (SAIN.Squad.SquadInfo?.MemberIsRegrouping == true)
            {
                trigger = EPhraseTrigger.Regroup;
            }

            return trigger != EPhraseTrigger.PhraseNone;
        }

        private bool IsEnemyInDirection(Vector3 enemyPosition, float angle, float threshold)
        {
            Vector3 enemyDirectionFromBot = enemyPosition - BotOwner.Transform.position;

            Vector3 enemyDirectionNormalized = enemyDirectionFromBot.normalized;
            Vector3 botLookDirectionNormalized = Player.MovementContext.PlayerRealForward.normalized;

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * botLookDirectionNormalized;

            return Vector3.Dot(enemyDirectionNormalized, direction) > threshold;
        }

        private bool SayRatCheck()
        {
            if (SAIN.Enemy != null)
            {
                if (SAIN.Enemy.TimeSinceSeen > 45f && SAIN.Enemy.Seen && _trySayRatTimer < Time.time)
                {
                    _trySayRatTimer = Time.time + 60f * Random.Range(0.75f, 1.25f);

                    if (EFTMath.RandomBool(33))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public SAINBotTalkClass LeaderComponent => SAIN.Squad.LeaderComponent?.Talk;
        private float Randomized => Random.Range(0.75f, 1.25f);
        private SAINSquadClass BotSquad => SAIN.Squad;

        private float CommandSayTimer = 0f;
        private float LeaderTimer = 0f;
        private float TalkTimer = 0f;
        private float HurtTalkTimer = 0f;
        private float _trySayRatTimer = 0f;
        private float _trySayLostContactTimer = 0f;
        private bool Subscribed = false;
    }
}