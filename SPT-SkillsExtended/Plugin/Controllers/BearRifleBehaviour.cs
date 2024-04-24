using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkillsExtended.Controllers
{
    internal class BearRifleBehaviour : MonoBehaviour
    {
        public static bool isSubscribed = false;

        public Dictionary<string, int> weaponInstanceIds = [];

        private SkillManager _skillManager => Utils.GetActiveSkillManager();

        private ISession _session => Plugin.Session;

        private GameWorld _gameWorld => Singleton<GameWorld>.Instance;

        private int _bearAKLevel => _session.Profile.Skills.BearAksystems.Level;

        private WeaponSkillData _bearSkillData => Plugin.SkillData.BearRifleSkill;

        private float _ergoBonusBear => _skillManager.BearAksystems.IsEliteLevel
            ? _bearAKLevel * _bearSkillData.ErgoMod + _bearSkillData.ErgoModElite
            : _bearAKLevel * _bearSkillData.ErgoMod;

        private float _recoilBonusBear => _skillManager.BearAksystems.IsEliteLevel
            ? _bearAKLevel * _bearSkillData.RecoilReduction + _bearSkillData.RecoilReductionElite
            : _bearAKLevel * _bearSkillData.RecoilReduction;

        private IEnumerable<Item> _bearWeapons => _session.Profile.Inventory.AllRealPlayerItems
            .Where(x => _bearSkillData.Weapons.Contains(x.TemplateId));

        // Store an object containing the weapons original stats.
        private Dictionary<string, OrigWeaponValues> _originalWeaponValues = [];

        private void Update()
        {
            SetupSkillManager();

            if (_skillManager == null) { return; }

            // Only run this behavior if we are BEAR, or the player has completed the USEC skill
            if (Plugin.Session?.Profile?.Side == EPlayerSide.Bear || _skillManager.UsecArsystems.IsEliteLevel)
            {
                StaticManager.Instance.StartCoroutine(UpdateWeapons(_bearWeapons, _ergoBonusBear, _recoilBonusBear, _bearAKLevel));
            }
        }

        private void SetupSkillManager()
        {
            if (_gameWorld && !isSubscribed)
            {
                if (_gameWorld.MainPlayer == null || _gameWorld?.MainPlayer?.Location == "hideout")
                {
                    return;
                }

                if ((_gameWorld.MainPlayer.Side == EPlayerSide.Bear && !_skillManager.BearAksystems.IsEliteLevel)
                    || (_skillManager.UsecArsystems.IsEliteLevel && !_skillManager.BearAksystems.IsEliteLevel)
                    || Plugin.SkillData.DisableEliteRequirements)
                {
                    _skillManager.OnMasteringExperienceChanged += ApplyBearAKXp;
                    Plugin.Log.LogDebug("BEAR AK XP ENABLED.");
                }

                isSubscribed = true;
                return;
            }
        }

        private void ApplyBearAKXp(MasterSkill action)
        {
            var items = _session.Profile.InventoryInfo.GetItemsInSlots([EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon])
                .Where(x => x != null && (_bearSkillData.Weapons.Contains(x.TemplateId))).Any();

            // TODO: This is bugged, it will allow xp even if its not the active weapon.
            if (items)
            {
                _skillManager.BearAksystems.Current += _bearSkillData.WeaponProfXp * SEConfig.bearWeaponSpeedMult.Value;

                Plugin.Log.LogDebug($"BEAR AK {_bearSkillData.WeaponProfXp * SEConfig.bearWeaponSpeedMult.Value} XP Gained.");
                return;
            }

            Plugin.Log.LogDebug("Invalid weapon for XP");
        }

        private IEnumerator UpdateWeapons(IEnumerable<Item> items, float ergoBonus, float recoilReduction, int level)
        {
            foreach (var item in items)
            {
                if (item is Weapon weap)
                {
                    // Store the weapons original values
                    if (!_originalWeaponValues.ContainsKey(item.TemplateId))
                    {
                        var origVals = new OrigWeaponValues
                        {
                            ergo = weap.Template.Ergonomics,
                            weaponUp = weap.Template.RecoilForceUp,
                            weaponBack = weap.Template.RecoilForceBack
                        };

                        Plugin.Log.LogDebug($"original {weap.LocalizedName()} ergo: {weap.Template.Ergonomics}, up {weap.Template.RecoilForceUp}, back {weap.Template.RecoilForceBack}");

                        _originalWeaponValues.Add(item.TemplateId, origVals);
                    }

                    //Skip instances of the weapon that are already adjusted at this level.
                    if (weaponInstanceIds.ContainsKey(item.Id))
                    {
                        if (weaponInstanceIds[item.Id] == level)
                        {
                            continue;
                        }
                        else
                        {
                            weaponInstanceIds.Remove(item.Id);
                        }
                    }

                    weap.Template.Ergonomics = _originalWeaponValues[item.TemplateId].ergo * (1 + ergoBonus);
                    weap.Template.RecoilForceUp = _originalWeaponValues[item.TemplateId].weaponUp * (1 - recoilReduction);
                    weap.Template.RecoilForceBack = _originalWeaponValues[item.TemplateId].weaponBack * (1 - recoilReduction);

                    Plugin.Log.LogDebug($"New {weap.LocalizedName()} ergo: {weap.Template.Ergonomics}, up {weap.Template.RecoilForceUp}, back {weap.Template.RecoilForceBack}");

                    weaponInstanceIds.Add(item.Id, level);
                }
            }

            yield break;
        }
    }
}