using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Interpolation;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;
using static RootMotion.FinalIK.InteractionTrigger;

using BotEventHandler = GClass603;

namespace SAIN
{
    public class PlayerWeaponInfoContainer
    {
        public PlayerWeaponInfoContainer(Player player)
        {
            Player = player;
            Slots.Add(EquipmentSlot.FirstPrimaryWeapon, null);
            Slots.Add(EquipmentSlot.SecondPrimaryWeapon, null);
            Slots.Add(EquipmentSlot.Holster, null);
        }

        public void ClearCache()
        {
            Weapons.Clear();
            Slots.Clear();
        }

        public void PlayAISound(float range, AISoundType soundType)
        {
            if (Player?.AIData != null && nextShootTime < Time.time)
            {
                float timeAdd;
                if (!Player.AIData.IsAI)
                {
                    timeAdd = 0.1f;
                }
                else
                {
                    timeAdd = 1f;
                }
                if (Singleton<BotEventHandler>.Instantiated)
                {
                    nextShootTime = Time.time + timeAdd;
                    Singleton<BotEventHandler>.Instance.PlaySound(Player, Player.WeaponRoot.position, range, soundType);
                }
            }
        }

        private float nextShootTime;

        public void CheckForNewWeapons()
        {
            var firearmController = Player.HandsController as FirearmController;
            GetWeaponInfo(firearmController?.Item);
            UpdateSlots();
        }

        public void UpdateSlots()
        {
            var primary = Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.FirstPrimaryWeapon)?.ContainedItem;
            if (primary != null && primary is Weapon primaryWeapon)
            {
                Slots[EquipmentSlot.FirstPrimaryWeapon] = GetWeaponInfo(primaryWeapon);
            }

            var secondary = Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.SecondPrimaryWeapon)?.ContainedItem;
            if (secondary != null && secondary is Weapon secondaryWeapon)
            {
                Slots[EquipmentSlot.SecondPrimaryWeapon] = GetWeaponInfo(secondaryWeapon);
            }

            var holster = Player.Inventory?.Equipment?.GetSlot(EquipmentSlot.Holster)?.ContainedItem;
            if (holster != null && holster is Weapon holsterWeapon)
            {
                Slots[EquipmentSlot.Holster] = GetWeaponInfo(holsterWeapon);
            }
        }

        public SAINWeaponInfo GetWeaponInfo(Weapon weapon)
        {
            if (weapon == null)
            {
                return null;
            }
            if (!Weapons.ContainsKey(weapon))
            {
                Weapons.Add(weapon, new SAINWeaponInfo(weapon));
            }
            return Weapons[weapon];
        }


        public readonly Player Player;
        public SAINWeaponInfo Primary => Slots[EquipmentSlot.FirstPrimaryWeapon];
        public SAINWeaponInfo Secondary => Slots[EquipmentSlot.SecondPrimaryWeapon];
        public SAINWeaponInfo Holster => Slots[EquipmentSlot.Holster];

        public Dictionary<Weapon, SAINWeaponInfo> Weapons = new Dictionary<Weapon, SAINWeaponInfo>();
        public Dictionary<EquipmentSlot, SAINWeaponInfo> Slots = new Dictionary<EquipmentSlot, SAINWeaponInfo>();
    }
}
