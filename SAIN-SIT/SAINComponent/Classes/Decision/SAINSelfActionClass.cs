using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SAINSelfActionClass : SAINBase, ISAINClass
    {
        public SAINSelfActionClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        private float _handsBusyTimer;

        public void Update()
        {
            RefreshMeds();

            if (!UsingMeds && Player != null)
            {
                if (SAIN.Memory.Decisions.Self.Current == SelfDecision.Reload)
                {
                    TryReload();
                    return;
                }
                if (_handsBusyTimer < Time.time)
                {
                    var handsController = Player.HandsController;
                    if (handsController.IsInInteractionStrictCheck())
                    {
                        _handsBusyTimer = Time.time + 1f;
                        return;
                    }

                    switch (SAIN.Memory.Decisions.Self.Current)
                    {
                        case SelfDecision.Reload:
                            TryReload();
                            break;

                        case SelfDecision.Surgery:
                            DoSurgery();
                            break;

                        case SelfDecision.FirstAid:
                            DoFirstAid();
                            break;

                        case SelfDecision.Stims:
                            DoStims();
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private void RefreshMeds()
        {
            if (_refreshMedsTimer < Time.time)
            {
                _refreshMedsTimer = Time.time + 5f;
                BotOwner.Medecine.RefreshCurMeds();
            }
        }

        private float _refreshMedsTimer;

        public void Dispose()
        {
        }

        private bool UsingMeds => BotOwner.Medecine?.Using == true;

        public void DoFirstAid()
        {
            var heal = BotOwner.Medecine.FirstAid;
            if (HealTimer < Time.time && heal.ShallStartUse())
            {
                HealTimer = Time.time + 5f;
                heal.TryApplyToCurrentPart();
            }
        }

        public void DoSurgery()
        {
            var surgery = BotOwner.Medecine.SurgicalKit;
            if (HealTimer < Time.time && !BotOwner.Mover.IsMoving && SAIN.Cover.BotIsAtCoverInUse() && surgery.ShallStartUse())
            {
                HealTimer = Time.time + 5f;
                surgery.ApplyToCurrentPart();
            }
        }

        public void DoStims()
        {
            var stims = BotOwner.Medecine.Stimulators;
            if (HealTimer < Time.time && stims.CanUseNow())
            {
                HealTimer = Time.time + 5f;
                try { stims.TryApply(); }
                catch { }
            }
        }

        private bool HaveStimsToHelp()
        {
            return false;
        }

        private void Refresh()
        {
            Player getPlayer = Player;
            MedicalItems.Clear();

            // getPlayer._inventoryController.GetAcceptableItemsNonAlloc<MedsClass>(anySlots, MedicalItems, null);
            foreach (var item in MedicalItems)
            {
            }
        }

        public static readonly EquipmentSlot[] anySlots = new EquipmentSlot[]
        {
            EquipmentSlot.Pockets,
            EquipmentSlot.TacticalVest,
            EquipmentSlot.Backpack,
            EquipmentSlot.SecuredContainer,
        };

        private readonly List<MedsClass> MedicalItems = new List<MedsClass>();

        public void TryReload()
        {
            try
            {
                BotOwner.WeaponManager.Reload.TryReload();
                if (BotOwner.WeaponManager.Reload.NoAmmoForReloadCached)
                {
                    BotOwner.WeaponManager.Reload.TryFillMagazines();
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        public void BotCancelReload()
        {
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                BotOwner.WeaponManager.Reload.TryStopReload();
            }
        }

        private float StimTimer = 0f;
        private float HealTimer = 0f;
    }
}
