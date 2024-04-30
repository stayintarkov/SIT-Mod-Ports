using BepInEx.Logging;
using EFT;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;
using SAIN.Helpers;
using SAIN.SAINComponent;
using EFT.Utilities;
using EFT.Ballistics;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class SAINBotTalkClass : SAINBase, ISAINClass
    {
        public SAINBotTalkClass(SAINComponentClass sain) : base(sain)
        {
            PhraseObjectsAdd(PersonalPhraseDict);

            GroupTalk = new GroupTalk(sain);
            EnemyTalk = new EnemyTalk(sain);
            TimeUntilCanTalk = Time.time + UnityEngine.Random.Range(1f, 2f);
        }

        public void Init()
        {
            if (Player != null)
            {
                Player.OnDamageReceived += GetHit;
            }
        }

        private void GetHit(float damage, EBodyPart bodyPart, EDamageType type, float damageReducedByArmor, MaterialType special = MaterialType.None)
        {
            if (Player == null || BotOwner == null || SAIN == null)
            {
                return;
            }
            EPhraseTrigger trigger = EPhraseTrigger.OnBeingHurt | EPhraseTrigger.Hit;
            ETagStatus mask = ETagStatus.Combat | ETagStatus.Aware | ETagStatus.Unaware;
            SendSayCommand(trigger, mask);
        }

        private float TimeUntilCanTalk;

        public void Update()
        {
            if (BotOwner == null || SAIN == null) return;

            if (CanTalk && TimeUntilCanTalk < Time.time)
            {
                GroupTalk.Update();

                EnemyTalk.Update();

                BotTalkPackage TalkPack = null;

                if (TalkAfterDelayTimer < Time.time && TalkDelayPack != null)
                {
                    TalkPack = TalkDelayPack;
                    TalkDelayPack = null;
                }
                else if (TalkPriorityTimer < Time.time && NormalBotTalk != null)
                {
                    TalkPack = NormalBotTalk;
                    NormalBotTalk = null;
                    TalkPriorityActive = false;
                }

                if (allTalkDelay < Time.time && TalkPack != null)
                {
                    allTalkDelay = Time.time + SAIN.Info.FileSettings.Mind.TalkFrequency;
                    if (TalkPack.phraseInfo.Phrase == EPhraseTrigger.Roger || TalkPack.phraseInfo.Phrase == EPhraseTrigger.Negative)
                    {
                        if (SAIN.Squad.VisibleMembers != null && SAIN.Squad.LeaderComponent != null && SAIN.Squad.VisibleMembers.Contains(SAIN.Squad.LeaderComponent) && SAIN.Enemy?.IsVisible == false)
                        {
                            if (TalkPack.phraseInfo.Phrase == EPhraseTrigger.Roger)
                            {
                                Player.HandsController.ShowGesture(EGesture.Good);
                            }
                            else
                            {
                                Player.HandsController.ShowGesture(EGesture.Bad);
                            }
                            return;
                        }
                    }
                    SendSayCommand(TalkPack);
                }
            }
        }

        public void Dispose()
        {
            if (Player != null)
            {
                Player.OnDamageReceived -= GetHit;
            }
            PersonalPhraseDict.Clear();
        }

        public bool CanTalk => SAIN.Info.FileSettings.Mind.CanTalk;

        public void Say(EPhraseTrigger phrase, ETagStatus? additionalMask = null, bool withGroupDelay = false)
        {
            if ((CanTalk && TimeUntilCanTalk < Time.time) 
                || phrase == EPhraseTrigger.OnDeath 
                || phrase == EPhraseTrigger.OnAgony 
                || phrase == EPhraseTrigger.OnBeingHurt)
            {
                if (withGroupDelay && !BotOwner.BotsGroup.GroupTalk.CanSay(BotOwner, phrase))
                {
                    return;
                }

                CheckPhrase(phrase, SetETagMask(additionalMask));
            }
        }

        private float TalkAfterDelayTimer = 0f;
        private BotTalkPackage TalkDelayPack;

        public void TalkAfterDelay(EPhraseTrigger phrase, ETagStatus mask, float delay)
        {
            if ((CanTalk && TimeUntilCanTalk < Time.time)
                || phrase == EPhraseTrigger.OnDeath
                || phrase == EPhraseTrigger.OnAgony
                || phrase == EPhraseTrigger.OnBeingHurt)
            {
                if (!PersonalPhraseDict.ContainsKey(phrase))
                {
                    Logger.LogWarning($"Phrase: [{phrase}] Not in Dictionary, adding it manually.");
                    PersonalPhraseDict.Add(phrase, new PhraseInfo(phrase, 10, 5f));
                }
                var talk = new BotTalkPackage(PersonalPhraseDict[phrase], mask);
                TalkDelayPack = CheckPriority(talk, TalkDelayPack, out bool changeTalk);
                if (changeTalk)
                {
                    TalkAfterDelayTimer = Time.time + delay;
                }
            }
        }

        public EnemyTalk EnemyTalk { get; private set; }
        public GroupTalk GroupTalk { get; private set; }

        private void SendSayCommand(EPhraseTrigger type, ETagStatus mask)
        {
            BotOwner.BotsGroup.GroupTalk.PhraseSad(BotOwner, type);
            Player.Say(type, false, 0f, mask);
            PersonalPhraseDict[type].TimeLastSaid = Time.time;
            if (SAINPlugin.DebugMode)
            {
                //Logger.LogDebug(type);
            }
        }

        private void SendSayCommand(BotTalkPackage talkPackage)
        {
            SendSayCommand(talkPackage.phraseInfo.Phrase, talkPackage.Mask);
        }

        private ETagStatus SetETagMask(ETagStatus? additionaMask = null)
        {
            ETagStatus etagStatus;
            if (BotOwner.BotsGroup.MembersCount > 1)
            {
                etagStatus = ETagStatus.Coop;
            }
            else
            {
                etagStatus = ETagStatus.Solo;
            }

            if (BotOwner.Memory.IsUnderFire)
            {
                etagStatus |= ETagStatus.Combat;
            }
            else if (SAIN.Enemy != null)
            {
                if (SAIN.Enemy.Seen && SAIN.Enemy.TimeSinceSeen < 30f)
                {
                    etagStatus |= ETagStatus.Combat;
                }
                else
                {
                    etagStatus |= ETagStatus.Aware;
                }

                switch (SAIN.Enemy.EnemyIPlayer.Side)
                {
                    case EPlayerSide.Usec:
                        etagStatus |= ETagStatus.Usec;
                        break;

                    case EPlayerSide.Bear:
                        etagStatus |= ETagStatus.Bear;
                        break;

                    case EPlayerSide.Savage:
                        etagStatus |= ETagStatus.Scav;
                        break;
                }
            }
            else if (BotOwner.Memory.GoalTarget.HavePlaceTarget())
            {
                etagStatus |= ETagStatus.Aware;
            }
            else
            {
                etagStatus |= ETagStatus.Unaware;
            }

            if (additionaMask != null)
            {
                etagStatus |= additionaMask.Value;
            }

            return etagStatus;
        }

        private void CheckPhrase(EPhraseTrigger phrase, ETagStatus mask)
        {
            if (phrase == EPhraseTrigger.OnDeath || phrase == EPhraseTrigger.OnAgony || phrase == EPhraseTrigger.OnBeingHurt)
            {
                SendSayCommand(phrase, mask);
                return;
            }

            if (!PersonalPhraseDict.ContainsKey(phrase))
            {
                Logger.LogWarning($"Phrase: [{phrase}] Not in Dictionary, adding it manually.");
                PersonalPhraseDict.Add(phrase, new PhraseInfo(phrase, 10, 5f));
            }

            if (PersonalPhraseDict.ContainsKey(phrase))
            {
                var phraseInfo = PersonalPhraseDict[phrase];
                if (phraseInfo.TimeLastSaid + phraseInfo.TimeDelay < Time.time)
                {
                    var data = new BotTalkPackage(phraseInfo, mask);

                    NormalBotTalk = CheckPriority(data, NormalBotTalk);

                    if (!TalkPriorityActive)
                    {
                        TalkPriorityActive = true;
                        TalkPriorityTimer = Time.time + 0.15f;
                    }
                }
            }
        }

        private BotTalkPackage CheckPriority(BotTalkPackage newTalk, BotTalkPackage oldTalk)
        {
            if (oldTalk == null)
            {
                return newTalk;
            }
            if (newTalk == null)
            {
                return oldTalk;
            }

            int newPriority = newTalk.phraseInfo.Priority;
            int oldPriority = oldTalk.phraseInfo.Priority;

            bool ChangeTalk = oldPriority < newPriority;

            return ChangeTalk ? newTalk : oldTalk;
        }

        private BotTalkPackage CheckPriority(BotTalkPackage newTalk, BotTalkPackage oldTalk, out bool ChangeTalk)
        {
            if (oldTalk == null)
            {
                ChangeTalk = true;
                return newTalk;
            }
            if (newTalk == null)
            {
                ChangeTalk = false;
                return oldTalk;
            }

            int newPriority = newTalk.phraseInfo.Priority;
            int oldPriority = oldTalk.phraseInfo.Priority;

            ChangeTalk = oldPriority < newPriority;

            return ChangeTalk ? newTalk : oldTalk;
        }

        static void PhraseObjectsAdd(Dictionary<EPhraseTrigger, PhraseInfo> dictionary)
        {
            AddPhrase(EPhraseTrigger.OnGoodWork, 1, 60f, dictionary);
            AddPhrase(EPhraseTrigger.OnBreath, 3, 15f, dictionary);
            AddPhrase(EPhraseTrigger.EnemyHit, 4, 3f, dictionary);
            AddPhrase(EPhraseTrigger.Rat, 5, 120f, dictionary);
            AddPhrase(EPhraseTrigger.OnMutter, 6, 20f, dictionary);
            AddPhrase(EPhraseTrigger.OnEnemyDown, 7, 10f, dictionary);
            AddPhrase(EPhraseTrigger.OnEnemyConversation, 8, 30f, dictionary);
            AddPhrase(EPhraseTrigger.GoForward, 9, 40f, dictionary);
            AddPhrase(EPhraseTrigger.Gogogo, 10, 40f, dictionary);
            AddPhrase(EPhraseTrigger.Going, 11, 60f, dictionary);
            AddPhrase(EPhraseTrigger.OnFight, 12, 5f, dictionary);
            AddPhrase(EPhraseTrigger.OnEnemyShot, 13, 3f, dictionary);
            AddPhrase(EPhraseTrigger.OnLostVisual, 14, 10f, dictionary);
            AddPhrase(EPhraseTrigger.OnRepeatedContact, 15, 5f, dictionary);
            AddPhrase(EPhraseTrigger.OnFirstContact, 16, 5f, dictionary);
            AddPhrase(EPhraseTrigger.OnBeingHurtDissapoinment, 17, 35f, dictionary);
            AddPhrase(EPhraseTrigger.StartHeal, 18, 75f, dictionary);
            AddPhrase(EPhraseTrigger.HurtLight, 19, 60f, dictionary);
            AddPhrase(EPhraseTrigger.OnWeaponReload, 20, 10f, dictionary);
            AddPhrase(EPhraseTrigger.OnOutOfAmmo, 21, 15f, dictionary);
            AddPhrase(EPhraseTrigger.HurtMedium, 22, 60f, dictionary);
            AddPhrase(EPhraseTrigger.HurtHeavy, 23, 30f, dictionary);
            AddPhrase(EPhraseTrigger.LegBroken, 24, 30f, dictionary);
            AddPhrase(EPhraseTrigger.HandBroken, 25, 30f, dictionary);
            AddPhrase(EPhraseTrigger.HurtNearDeath, 26, 20f, dictionary);
            AddPhrase(EPhraseTrigger.OnFriendlyDown, 27, 10f, dictionary);
            AddPhrase(EPhraseTrigger.FriendlyFire, 28, 2f, dictionary);
            AddPhrase(EPhraseTrigger.NeedHelp, 29, 30f, dictionary);
            AddPhrase(EPhraseTrigger.GetInCover, 30, 40f, dictionary);
            AddPhrase(EPhraseTrigger.LeftFlank, 31, 5f, dictionary);
            AddPhrase(EPhraseTrigger.RightFlank, 32, 5f, dictionary);
            AddPhrase(EPhraseTrigger.NeedWeapon, 33, 15f, dictionary);
            AddPhrase(EPhraseTrigger.WeaponBroken, 34, 15f, dictionary);
            AddPhrase(EPhraseTrigger.OnGrenade, 35, 10f, dictionary);
            AddPhrase(EPhraseTrigger.OnEnemyGrenade, 36, 10f, dictionary);
            AddPhrase(EPhraseTrigger.Stop, 37, 1f, dictionary);
            AddPhrase(EPhraseTrigger.OnBeingHurt, 38, 1f, dictionary);
            AddPhrase(EPhraseTrigger.OnAgony, 39, 1f, dictionary);
            AddPhrase(EPhraseTrigger.OnDeath, 40, 1f, dictionary);
            AddPhrase(EPhraseTrigger.Regroup, 10, 80f, dictionary);
            AddPhrase(EPhraseTrigger.OnSix, 15, 10f, dictionary);
            AddPhrase(EPhraseTrigger.InTheFront, 15, 20f, dictionary);
            AddPhrase(EPhraseTrigger.FollowMe, 15, 45f, dictionary);
            AddPhrase(EPhraseTrigger.HoldPosition, 6, 60f, dictionary);
            AddPhrase(EPhraseTrigger.Suppress, 20, 15f, dictionary);
            AddPhrase(EPhraseTrigger.Roger, 10, 30f, dictionary);
            AddPhrase(EPhraseTrigger.Negative, 10, 30f, dictionary);
            AddPhrase(EPhraseTrigger.PhraseNone, 1, 1f, dictionary);
            AddPhrase(EPhraseTrigger.Attention, 25, 30f, dictionary);
            AddPhrase(EPhraseTrigger.OnYourOwn, 25, 15f, dictionary);
            AddPhrase(EPhraseTrigger.Repeat, 25, 30f, dictionary);
            AddPhrase(EPhraseTrigger.CoverMe, 25, 45f, dictionary);
            AddPhrase(EPhraseTrigger.NoisePhrase, 5, 120f, dictionary);
            AddPhrase(EPhraseTrigger.UnderFire, 34, 5f, dictionary);
            AddPhrase(EPhraseTrigger.MumblePhrase, 10, 35f, dictionary);
            AddPhrase(EPhraseTrigger.GetBack, 10, 45f, dictionary);
            AddPhrase(EPhraseTrigger.LootBody, 5, 30f, dictionary);
            AddPhrase(EPhraseTrigger.LootContainer, 5, 30f, dictionary);
            AddPhrase(EPhraseTrigger.LootGeneric, 5, 30f, dictionary);
            AddPhrase(EPhraseTrigger.LootKey, 5, 30f, dictionary);
            AddPhrase(EPhraseTrigger.LootMoney, 5, 30f, dictionary);
            AddPhrase(EPhraseTrigger.LootNothing, 5, 30f, dictionary);
            AddPhrase(EPhraseTrigger.LootWeapon, 5, 30f, dictionary);
            AddPhrase(EPhraseTrigger.OnLoot, 5, 30f, dictionary);

            foreach (EPhraseTrigger value in System.Enum.GetValues(typeof(EPhraseTrigger)))
            {
                AddPhrase(value, 10, 5f, dictionary);
            }
        }

        static void AddPhrase(EPhraseTrigger phrase, int priority, float timeDelay, Dictionary<EPhraseTrigger, PhraseInfo> dictionary)
        {
            if (!dictionary.ContainsKey(phrase))
            {
                dictionary.Add(phrase, new PhraseInfo(phrase, priority, timeDelay));
            }
        }

        private BotTalkPackage NormalBotTalk;

        private bool TalkPriorityActive = false;

        private float TalkPriorityTimer = 0f;

        private float allTalkDelay = 0f;

        private readonly Dictionary<EPhraseTrigger, PhraseInfo> PersonalPhraseDict = new Dictionary<EPhraseTrigger, PhraseInfo>();
    }

    public class BotTalkPackage
    {
        public BotTalkPackage(PhraseInfo phrase, ETagStatus mask)
        {
            phraseInfo = phrase;
            Mask = mask;
            TimeCreated = Time.time;
        }

        public PhraseInfo phraseInfo;

        public ETagStatus Mask;

        public float TimeCreated { get; private set; }
    }

    public class PhraseInfo
    {
        public PhraseInfo(EPhraseTrigger trigger, int priority, float timeDelay)
        {
            Phrase = trigger;
            Priority = priority;
            TimeDelay = timeDelay;
        }

        public EPhraseTrigger Phrase { get; private set; }
        public int Priority { get; private set; }
        public float TimeDelay { get; set; }

        public float TimeLastSaid = 0f;
    }
}