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
    public class UsecRifleBehaviour : MonoBehaviour
    {
        public Dictionary<string, int> weaponInstanceIds = [];

        private SkillManager _skillManager => Utils.GetActiveSkillManager();

        private ISession _session => Plugin.Session;

        private GameWorld _gameWorld => Singleton<GameWorld>.Instance;

        private int _usecARLevel => _session.Profile.Skills.UsecArsystems.Level;

        private WeaponSkillData _usecSkillData => Plugin.SkillData.UsecRifleSkill;

        private float _ergoBonusUsec => _skillManager.UsecArsystems.IsEliteLevel
            ? _usecARLevel * _usecSkillData.ErgoMod + _usecSkillData.ErgoModElite
            : _usecARLevel * _usecSkillData.ErgoMod;

        private float _recoilBonusUsec => _skillManager.UsecArsystems.IsEliteLevel
            ? _usecARLevel * _usecSkillData.RecoilReduction + _usecSkillData.RecoilReductionElite
            : _usecARLevel * _usecSkillData.RecoilReduction;

        // Store an object containing the weapons original stats.
        private Dictionary<string, OrigWeaponValues> _originalWeaponValues = [];

        private IEnumerable<Item> _usecWeapons => _session.Profile.Inventory.AllRealPlayerItems
            .Where(x => _usecSkillData.Weapons.Contains(x.TemplateId));

        public static bool isSubscribed = false;

        private void Update()
        {
            SetupSkillManager();

            if (_skillManager == null) { return; }

            // Only run this behavior if we are USEC, or the player has completed the BEAR skill
            if (Plugin.Session?.Profile?.Side == EPlayerSide.Usec || _skillManager.BearAksystems.IsEliteLevel)
            {
                StaticManager.Instance.StartCoroutine(UpdateWeapons(_usecWeapons, _ergoBonusUsec, _recoilBonusUsec, _usecARLevel));
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

                if ((_gameWorld.MainPlayer.Side == EPlayerSide.Usec && !_skillManager.UsecArsystems.IsEliteLevel)
                    || (_skillManager.BearAksystems.IsEliteLevel && !_skillManager.UsecArsystems.IsEliteLevel)
                    || Plugin.SkillData.DisableEliteRequirements)
                {
                    _skillManager.OnMasteringExperienceChanged += ApplyUsecARXp;
                    Plugin.Log.LogDebug("USEC AR XP ENABLED.");
                }

                isSubscribed = true;
                return;
            }
        }

        private void ApplyUsecARXp(MasterSkill action)
        {
            var items = _session.Profile.InventoryInfo.GetItemsInSlots([EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon])
                .Where(x => x != null && (_usecSkillData.Weapons.Contains(x.TemplateId))).Any();

            // TODO: This is bugged, it will allow xp even if its not the active weapon.
            if (items)
            {
                _skillManager.UsecArsystems.Current += _usecSkillData.WeaponProfXp * SEConfig.usecWeaponSpeedMult.Value;

                Plugin.Log.LogDebug($"USEC AR {_usecSkillData.WeaponProfXp * SEConfig.usecWeaponSpeedMult.Value} XP Gained.");
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