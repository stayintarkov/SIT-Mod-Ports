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
    internal class FieldMedicineBehaviour : MonoBehaviour
    {
        private SkillManager _skillManager => Utils.GetActiveSkillManager();

        private MedicalSkillData _skillData => Plugin.SkillData.MedicalSkills;

        private float FmPmcSpeedBonus => _skillManager.FirstAid.IsEliteLevel
            ? 1f - (_skillManager.FirstAid.Level * _skillData.MedicalSpeedBonus) - _skillData.MedicalSpeedBonusElite
            : 1f - (_skillManager.FirstAid.Level * _skillData.MedicalSpeedBonus);

        private Dictionary<string, HealthEffectValues> _originalHealthEffectValues = [];

        private Dictionary<EBodyPart, DateTime> _fieldMedicineBodyPartCache = [];

        public Dictionary<string, int> fieldMedicineInstanceIDs = [];

        private void Update()
        {
            if (Plugin.Items == null)
            {
                return;
            }

            if (Plugin.GameWorld?.MainPlayer == null)
            {
                _fieldMedicineBodyPartCache.Clear();
            }

            StaticManager.Instance.StartCoroutine(FieldMedicineUpdate());
        }

        public void ApplyFieldMedicineExp(EBodyPart bodypart)
        {
            float xpGain = 2.5f;

            // If we recently healed this limb, return
            if (!Utils.CanGainXPForLimb(_fieldMedicineBodyPartCache, bodypart)) { return; }

            _skillManager.FieldMedicine.SetCurrent(_skillManager.FieldMedicine.Current + (xpGain * SEConfig.fieldMedicineSpeedMult.Value), true);

            Plugin.Log.LogDebug($"Skill: {_skillManager.FieldMedicine.Id} Side: {Plugin.Player.Side} Gained: {xpGain * SEConfig.fieldMedicineSpeedMult.Value} exp.");
        }

        private void ApplyFieldMedicineSpeedBonus(Item item)
        {
            float bonus = FmPmcSpeedBonus;

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

                Plugin.Log.LogDebug($"Field Medicine: Set instance {item.Id} of type {item.TemplateId} to {_originalHealthEffectValues[meds.TemplateId].UseTime * bonus} seconds");
            }
        }

        private IEnumerator FieldMedicineUpdate()
        {
            var items = Plugin.Items.Where(x => x is MedsClass);

            if (items == null) { yield break; }

            foreach (var item in items)
            {
                // Skip if we already set this field medicine item.
                if (fieldMedicineInstanceIDs.ContainsKey(item.Id))
                {
                    int previouslySet = fieldMedicineInstanceIDs[item.Id];

                    if (previouslySet == _skillManager.FieldMedicine.Level)
                    {
                        continue;
                    }
                    else
                    {
                        fieldMedicineInstanceIDs.Remove(item.Id);
                    }
                }

                // Apply Field medicine speed bonus to items
                if (_skillData.FmItemList.Contains(item.TemplateId))
                {
                    ApplyFieldMedicineSpeedBonus(item);
                    fieldMedicineInstanceIDs.Add(item.Id, _skillManager.FieldMedicine.Level);
                }
            }

            yield break;
        }
    }
}