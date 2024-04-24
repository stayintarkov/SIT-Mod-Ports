using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkillsExtended.Controllers
{
    public class FirstAidBehaviour : MonoBehaviour
    {
        private Dictionary<string, MedKitValues> _originalMedKitValues = [];

        private SkillManager _skillManager => Utils.GetActiveSkillManager();

        private MedicalSkillData _skillData => Plugin.SkillData.MedicalSkills;

        // Store the instance ID of the item and the level its bonus resource is set to.
        public Dictionary<string, int> firstAidInstanceIDs = [];

        // Store a dictionary of bodyparts to prevent the user from spam exploiting the leveling
        // system. Bodypart, Last time healed
        private Dictionary<EBodyPart, DateTime> _firstAidBodypartCahce = [];

        private Dictionary<string, HealthEffectValues> _originalHealthEffectValues = [];

        private float FaPmcSpeedBonus => _skillManager.FirstAid.IsEliteLevel
            ? 1f - (_skillManager.FirstAid.Level * _skillData.MedicalSpeedBonus) - _skillData.MedicalSpeedBonusElite
            : 1f - (_skillManager.FirstAid.Level * _skillData.MedicalSpeedBonus);

        private float FaHpBonus => _skillManager.FirstAid.IsEliteLevel
            ? _skillManager.FirstAid.Level * _skillData.MedkitHpBonus + _skillData.MedkitHpBonusElite
            : _skillManager.FirstAid.Level * _skillData.MedkitHpBonus;

        private void Update()
        {
            if (Plugin.Items == null)
            {
                return;
            }

            if (Plugin.GameWorld?.MainPlayer == null)
            {
                _firstAidBodypartCahce.Clear();
            }

            StaticManager.Instance.StartCoroutine(FirstAidUpdate());
        }

        public void ApplyFirstAidExp(EBodyPart bodypart)
        {
            float xpGain = 1.5f;

            if (!Utils.CanGainXPForLimb(_firstAidBodypartCahce, bodypart)) { return; }

            _skillManager.FirstAid.SetCurrent(_skillManager.FirstAid.Current + (xpGain * SEConfig.firstAidSpeedMult.Value), true);

            Plugin.Log.LogDebug($"Skill: {_skillManager.FirstAid.Id} Side: {Plugin.Player.Side} Gained: {xpGain * SEConfig.firstAidSpeedMult.Value} exp.");
        }

        private void ApplyFirstAidSpeedBonus(Item item)
        {
            float bonus = FaPmcSpeedBonus;

            if (firstAidInstanceIDs.ContainsKey(item.Id)) { return; }

            if (item is MedsClass meds)
            {
                if (!_originalHealthEffectValues.ContainsKey(item.TemplateId))
                {
                    var origValues = new HealthEffectValues
                    {
                        UseTime = meds.HealthEffectsComponent.UseTime,
                        BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults,
                        HealthEffects = meds.HealthEffectsComponent.HealthEffects,
                        DamageEffects = meds.HealthEffectsComponent.DamageEffects,
                        StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs
                    };

                    _originalHealthEffectValues.Add(item.TemplateId, origValues);
                }

                GInterface296 newGInterface = new HealthEffectValues
                {
                    UseTime = _originalHealthEffectValues[meds.TemplateId].UseTime * bonus,
                    BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults,
                    HealthEffects = meds.HealthEffectsComponent.HealthEffects,
                    DamageEffects = meds.HealthEffectsComponent.DamageEffects,
                    StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs
                };

                var healthEffectComp = AccessTools.Field(typeof(MedsClass), "HealthEffectsComponent").GetValue(meds);
                AccessTools.Field(typeof(HealthEffectsComponent), "ginterface296_0").SetValue(healthEffectComp, newGInterface);

                Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {_originalHealthEffectValues[meds.TemplateId].UseTime * bonus} seconds");
            }
        }

        private void ApplyFirstAidHPBonus(Item item)
        {
            // Dont apply HP bonuses with realism med changes enabled
            if (Plugin.RealismConfig.med_changes) { return; }

            if (firstAidInstanceIDs.ContainsKey(item.Id)) { return; }

            if (item is MedsClass meds)
            {
                Meds2Class medKitInterface;

                // Add the original medkit template to the original dictionary
                if (!_originalMedKitValues.ContainsKey(item.TemplateId))
                {
                    var origMedValues = new MedKitValues
                    {
                        MaxHpResource = meds.MedKitComponent.MaxHpResource,
                        HpResourceRate = meds.MedKitComponent.HpResourceRate
                    };

                    _originalMedKitValues.Add(item.TemplateId, origMedValues);
                }

                int maxHpResource = Mathf.FloorToInt(_originalMedKitValues[item.TemplateId].MaxHpResource * (1 + FaHpBonus));
                if (meds.TemplateId == "590c657e86f77412b013051d")
                {
                    maxHpResource = Mathf.Clamp(maxHpResource, 1800, 2750);
                }

                medKitInterface = new MedKitValues
                {
                    MaxHpResource = maxHpResource,
                    HpResourceRate = meds.MedKitComponent.HpResourceRate
                };

                Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {medKitInterface.MaxHpResource} HP");

                var currentResouce = meds.MedKitComponent.HpResource;
                var currentMaxResouce = meds.MedKitComponent.MaxHpResource;

                // Only change the current resource if the item is unused.
                if (currentResouce == currentMaxResouce)
                {
                    meds.MedKitComponent.HpResource = medKitInterface.MaxHpResource;
                }

                var medComp = AccessTools.Field(typeof(MedsClass), "MedKitComponent").GetValue(meds);
                AccessTools.Field(typeof(MedKitComponent), "ginterface302_0").SetValue(medComp, medKitInterface);
            }
        }

        private IEnumerator FirstAidUpdate()
        {
            var items = Plugin.Items.Where(x => x is MedsClass);

            if (items == null) { yield break; }

            foreach (var item in items)
            {
                // Skip if we already set this first aid item.
                if (firstAidInstanceIDs.ContainsKey(item.Id))
                {
                    int previouslySet = firstAidInstanceIDs[item.Id];

                    if (previouslySet == _skillManager.FirstAid.Level)
                    {
                        continue;
                    }
                    else
                    {
                        firstAidInstanceIDs.Remove(item.Id);
                    }
                }

                // Apply first aid speed bonus to items
                if (_skillData.FaItemList.Contains(item.TemplateId))
                {
                    ApplyFirstAidSpeedBonus(item);
                    ApplyFirstAidHPBonus(item);
                    firstAidInstanceIDs.Add(item.Id, _skillManager.FirstAid.Level);
                }
            }

            yield break;
        }
    }
}