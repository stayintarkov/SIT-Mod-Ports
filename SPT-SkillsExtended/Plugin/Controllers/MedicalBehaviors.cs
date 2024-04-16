using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static SkillsExtended.Helpers.Constants;

namespace SkillsExtended.Controllers
{
    public class MedicalBehavior : MonoBehaviour
    {
        public sealed class MedKitValues : GInterface302
        {
            public int MaxHpResource { set; get; }
            public float HpResourceRate { set; get; }
        }

        public sealed class HealthEffectValues : GInterface296
        {
            public float UseTime { set; get; }

            public KeyValuePair<EBodyPart, float>[] BodyPartTimeMults { set; get; }

            public Dictionary<EHealthFactorType, CutPiece> HealthEffects { set; get; }

            public Dictionary<EDamageEffectType, DamageEffect> DamageEffects { set; get; }

            public string StimulatorBuffs { set; get; }
        }

        private Dictionary<string, MedKitValues> _originalMedKitValues = new Dictionary<string, MedKitValues>();
        private Dictionary<string, HealthEffectValues> _originalHealthEffectValues = new Dictionary<string, HealthEffectValues>();

        private GameWorld gameWorld { get => Singleton<GameWorld>.Instance; }

        private Player player { get => gameWorld.MainPlayer; }

        private SkillManager _skillManager => Utils.SetActiveSkillManager();

        // Store the instance ID of the item and the level its bonus resource is set to.
        public Dictionary<string, int> firstAidInstanceIDs = new Dictionary<string, int>();

        public Dictionary<string, int> fieldMedicineInstanceIDs = new Dictionary<string, int>();

        // Store a dictionary of bodyparts to prevent the user from spam exploiting the leveling system.
        // Bodypart, Last time healed
        private Dictionary<EBodyPart, DateTime> _firstAidBodypartCahce = new Dictionary<EBodyPart, DateTime>();

        private Dictionary<EBodyPart, DateTime> _fieldMedicineBodyPartCache = new Dictionary<EBodyPart, DateTime>();

        private float FaPmcSpeedBonus => _skillManager.FirstAid.IsEliteLevel 
            ? 1f - (_skillManager.FirstAid.Level * MEDICAL_SPEED_BONUS) - MEDICAL_SPEED_BONUS_ELITE
            : 1f - (_skillManager.FirstAid.Level * MEDICAL_SPEED_BONUS);

        private float FaHpBonus => _skillManager.FirstAid.IsEliteLevel 
            ? _skillManager.FirstAid.Level * MEDKIT_HP_BONUS + MEDKIT_HP_BONUS_ELITE 
            : _skillManager.FirstAid.Level * MEDKIT_HP_BONUS;

        private float FmPmcSpeedBonus => _skillManager.FirstAid.IsEliteLevel 
            ? 1f - (_skillManager.FirstAid.Level * MEDICAL_SPEED_BONUS) - MEDICAL_SPEED_BONUS_ELITE
            : 1f - (_skillManager.FirstAid.Level * MEDICAL_SPEED_BONUS);

        private void Awake()
        {
            new DoMedEffectPatch().Enable();
            new SetItemInHands().Enable();
        }

        private void Update()
        {
            if (gameWorld?.MainPlayer == null)
            {
                _fieldMedicineBodyPartCache.Clear();
                _firstAidBodypartCahce.Clear();
            }

            // Dont continue if skill manager is null
            //if (_skillManager == null) { return; }

            StaticManager.Instance.StartCoroutine(MedicalItemUpdate());
        }

        public void ApplyFirstAidExp(EBodyPart bodypart)
        {
            float xpGain = 1.5f;

            if (!CanGainXPForLimb(_firstAidBodypartCahce, bodypart)) { return; }

            _skillManager.FirstAid.SetCurrent(_skillManager.FirstAid.Current + (xpGain * SEConfig.firstAidSpeedMult.Value), true);

            Plugin.Log.LogDebug($"Skill: {_skillManager.FirstAid.Id} Side: {player.Side} Gained: {xpGain * SEConfig.firstAidSpeedMult.Value} exp.");
        }

        public void ApplyFieldMedicineExp(EBodyPart bodypart)
        {
            float xpGain = 2.5f;

            // If we recently healed this limb, return
            if (!CanGainXPForLimb(_fieldMedicineBodyPartCache, bodypart)) { return; }

            _skillManager.FieldMedicine.SetCurrent(_skillManager.FieldMedicine.Current + (xpGain * SEConfig.fieldMedicineSpeedMult.Value), true);

            Plugin.Log.LogDebug($"Skill: {_skillManager.FieldMedicine.Id} Side: {player.Side} Gained: {xpGain * SEConfig.fieldMedicineSpeedMult.Value} exp.");
        }

        private void ApplyFirstAidSpeedBonus(Item item)
        {
            float bonus = FaPmcSpeedBonus;

            if (firstAidInstanceIDs.ContainsKey(item.Id)) { return; }

            if (item is MedsClass meds)
            {
                if (!_originalHealthEffectValues.ContainsKey(item.TemplateId))
                {
                    var origValues = new HealthEffectValues();

                    origValues.UseTime = meds.HealthEffectsComponent.UseTime;
                    origValues.BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults;
                    origValues.HealthEffects = meds.HealthEffectsComponent.HealthEffects;
                    origValues.DamageEffects = meds.HealthEffectsComponent.DamageEffects;
                    origValues.StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs;

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
                AccessTools.Field(typeof(HealthEffectsComponent), "ginterface292_0").SetValue(healthEffectComp, newGInterface);

                Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {_originalHealthEffectValues[meds.TemplateId].UseTime * bonus} seconds");
            }
        }

        private void ApplyFirstAidHPBonus(Item item)
        {
            if (firstAidInstanceIDs.ContainsKey(item.Id)) { return; }

            if (item is MedsClass meds)
            {
                GInterface302 medKitInterface;

                // Add the original medkit template to the original dictionary
                if (!_originalMedKitValues.ContainsKey(item.TemplateId))
                {
                    var origMedValues = new MedKitValues();

                    origMedValues.MaxHpResource = meds.MedKitComponent.MaxHpResource;
                    origMedValues.HpResourceRate = meds.MedKitComponent.HpResourceRate;

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
                AccessTools.Field(typeof(MedKitComponent), "ginterface298_0").SetValue(medComp, medKitInterface);
            }
        }

        private void ApplyFieldMedicineSpeedBonus(Item item)
        {
            float bonus = FmPmcSpeedBonus;

            if (item is MedsClass meds)
            {
                if (!_originalHealthEffectValues.ContainsKey(item.TemplateId))
                {
                    var origValues = new HealthEffectValues();

                    origValues.UseTime = meds.HealthEffectsComponent.UseTime;
                    origValues.BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults;
                    origValues.HealthEffects = meds.HealthEffectsComponent.HealthEffects;
                    origValues.DamageEffects = meds.HealthEffectsComponent.DamageEffects;
                    origValues.StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs;

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
                AccessTools.Field(typeof(HealthEffectsComponent), "ginterface292_0").SetValue(healthEffectComp, newGInterface);

                Plugin.Log.LogDebug($"Field Medicine: Set instance {item.Id} of type {item.TemplateId} to {_originalHealthEffectValues[meds.TemplateId].UseTime * bonus} seconds");
            }
        }

        private IEnumerator MedicalItemUpdate()
        {
            var items = Plugin.Session?.Profile?.Inventory?.AllRealPlayerItems?.Where(x => x is MedsClass);

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

                // Apply first aid speed bonus to items
                if (FIRST_AID_ITEM_LIST.Contains(item.TemplateId))
                {
                    ApplyFirstAidSpeedBonus(item);
                    ApplyFirstAidHPBonus(item);
                    firstAidInstanceIDs.Add(item.Id, _skillManager.FirstAid.Level);
                }

                // Apply Field medicine speed bonus to items
                if (FIELD_MEDICINE_ITEM_LIST.Contains(item.TemplateId))
                {
                    ApplyFieldMedicineSpeedBonus(item);
                    fieldMedicineInstanceIDs.Add(item.Id, _skillManager.FieldMedicine.Level);
                }
            }

            yield break;
        }

        private bool CanGainXPForLimb(Dictionary<EBodyPart, DateTime> dict, EBodyPart bodypart)
        {
            if (!dict.ContainsKey(bodypart))
            {
                dict.Add(bodypart, DateTime.Now);
                return true;
            }
            else
            {
                TimeSpan elapsed = DateTime.Now - dict[bodypart];

                if (elapsed.TotalSeconds >= SEConfig.medicalSkillCoolDownTime.Value)
                {
                    dict.Remove(bodypart);
                    return true;
                }

                Plugin.Log.LogDebug($"Time until next available xp: {SEConfig.medicalSkillCoolDownTime.Value - elapsed.TotalSeconds} seconds");
                return false;
            }
        }
    }
}