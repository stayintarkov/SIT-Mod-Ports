using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkillsExtended.Controllers
{
    public class WeaponProficiencyBehaviors : MonoBehaviour
    {
        private class OrigWeaponValues
        {
            public float ergo;
            public float weaponUp;
            public float weaponBack;
        }

        public Dictionary<string, int> weaponInstanceIds = new Dictionary<string, int>();

        private SkillManager _skillManager => Utils.SetActiveSkillManager();

        private ISession _session => Plugin.Session;

        private GameWorld _gameWorld { get => Singleton<GameWorld>.Instance; }

        private int _usecARLevel => _session.Profile.Skills.UsecArsystems.Level;
        
        private int _bearAKLevel => _session.Profile.Skills.BearAksystems.Level;
        
       
        
        private float _ergoBonusUsec => _skillManager.UsecArsystems.IsEliteLevel 
            ? _usecARLevel * Constants.ERGO_MOD + Constants.ERGO_MOD_ELITE 
            : _usecARLevel * Constants.ERGO_MOD;
        
        private float _recoilBonusUsec => _skillManager.UsecArsystems.IsEliteLevel 
            ? _usecARLevel * Constants.RECOIL_REDUCTION + Constants.RECOIL_REDUCTION_ELITE 
            : _usecARLevel * Constants.RECOIL_REDUCTION;

        private float _ergoBonusBear => _skillManager.BearAksystems.IsEliteLevel
            ? _bearAKLevel * Constants.ERGO_MOD + Constants.ERGO_MOD_ELITE
            : _bearAKLevel * Constants.ERGO_MOD;

        private float _recoilBonusBear => _skillManager.BearAksystems.IsEliteLevel
            ? _bearAKLevel * Constants.RECOIL_REDUCTION + Constants.RECOIL_REDUCTION_ELITE
            : _bearAKLevel * Constants.RECOIL_REDUCTION;

        // Store an object containing the weapons original stats.
        private Dictionary<string, OrigWeaponValues> _originalWeaponValues = new Dictionary<string, OrigWeaponValues>();

        private List<string> _customUsecWeapons = new List<string>();
        private List<string> _customBearWeapons = new List<string>();

        private IEnumerable<Item> _usecWeapons => _session.Profile.Inventory.AllRealPlayerItems
            .Where(x => Constants.USEC_WEAPON_LIST.Contains(x.TemplateId) || _customUsecWeapons.Contains(x.TemplateId));

        private IEnumerable<Item> _bearWeapons => _session.Profile.Inventory.AllRealPlayerItems
            .Where(x => Constants.BEAR_WEAPON_LIST.Contains(x.TemplateId) || _customBearWeapons.Contains(x.TemplateId));

        public static bool isSubscribed = false;

        private void Awake()
        {
            GetCustomWeapons();
        }

        private void Update()
        {
            SetupSkillManager();

            if (_skillManager == null) { return; }

            // Only run this behavior if we are USEC, or the player has completed the BEAR skill
            if (Plugin.Session?.Profile?.Side == EPlayerSide.Usec || _skillManager.BearAksystems.IsEliteLevel)
            { 
                StaticManager.Instance.StartCoroutine(UpdateWeapons(_usecWeapons, _ergoBonusUsec, _recoilBonusUsec, _usecARLevel));
            }

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
                
                if ((_gameWorld.MainPlayer.Side == EPlayerSide.Usec && !_skillManager.UsecArsystems.IsEliteLevel) 
                    || (_skillManager.BearAksystems.IsEliteLevel && !_skillManager.UsecArsystems.IsEliteLevel)
                    || SEConfig.disableEliteRequirement.Value)
                {
                    _skillManager.OnMasteringExperienceChanged += ApplyUsecARXp;           
                    Plugin.Log.LogDebug("USEC AR XP ENABLED.");
                }

                if ((_gameWorld.MainPlayer.Side == EPlayerSide.Bear && !_skillManager.BearAksystems.IsEliteLevel) 
                    || (_skillManager.UsecArsystems.IsEliteLevel && !_skillManager.BearAksystems.IsEliteLevel)
                    || SEConfig.disableEliteRequirement.Value)
                {
                    _skillManager.OnMasteringExperienceChanged += ApplyBearAKXp;
                    Plugin.Log.LogDebug("BEAR AK XP ENABLED.");
                }
                
                isSubscribed = true;
                return;
            }
        }

        private void ApplyUsecARXp(MasterSkill action)
        {
            var items = _session.Profile.InventoryInfo.GetItemsInSlots(new EquipmentSlot[] { EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon })
                .Where(x => x != null && (Constants.USEC_WEAPON_LIST.Contains(x.TemplateId) || _customUsecWeapons.Contains(x.TemplateId))).Any();
           
            if (items)
            {
                _skillManager.UsecArsystems.Current += Constants.WEAPON_PROF_XP * SEConfig.usecWeaponSpeedMult.Value;

                Plugin.Log.LogDebug($"USEC AR {Constants.WEAPON_PROF_XP * SEConfig.usecWeaponSpeedMult.Value} XP Gained.");
                return;
            }
            
            Plugin.Log.LogDebug("Invalid weapon for XP");
        }

        private void ApplyBearAKXp(MasterSkill action)
        {
            var items = _session.Profile.InventoryInfo.GetItemsInSlots(new EquipmentSlot[] { EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon })
                .Where(x => x != null && (Constants.BEAR_WEAPON_LIST.Contains(x.TemplateId) || _customBearWeapons.Contains(x.TemplateId))).Any();

            if (items)
            {
                _skillManager.BearAksystems.Current += Constants.WEAPON_PROF_XP * SEConfig.bearWeaponSpeedMult.Value;

                Plugin.Log.LogDebug($"BEAR AK {Constants.WEAPON_PROF_XP * SEConfig.bearWeaponSpeedMult.Value} XP Gained.");
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
                        var origVals = new OrigWeaponValues();

                        origVals.ergo = weap.Template.Ergonomics;
                        origVals.weaponUp = weap.Template.RecoilForceUp;
                        origVals.weaponBack = weap.Template.RecoilForceBack;

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

        private void GetCustomWeapons()
        {
            _customUsecWeapons = Utils.Get<List<string>>("/skillsExtended/GetCustomWeaponsUsec");
            _customBearWeapons = Utils.Get<List<string>>("/skillsExtended/GetCustomWeaponsBear");

            if (_customUsecWeapons == null)
            {
                // Add a bogus entry to prevent a null condition
                _customUsecWeapons = new List<string>
                {
                    ""
                };
            }

            if (_customBearWeapons == null)
            {
                // Add a bogus entry to prevent a null condition
                _customBearWeapons = new List<string>
                {
                    ""
                };
            }
        }
    }
}