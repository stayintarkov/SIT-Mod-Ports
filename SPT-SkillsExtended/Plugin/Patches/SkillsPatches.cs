using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using static EFT.SkillManager;

namespace SkillsExtended.Patches
{
    internal class SkillManagerConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SkillManager).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [typeof(EPlayerSide)], null);

        [PatchPostfix]
        public static void Postfix(SkillManager __instance, ref Skill[] ___DisplayList, ref Skill[] ___Skills,
            ref Skill ___UsecArsystems, ref Skill ___BearAksystems, ref Skill ___UsecTactics, ref Skill ___BearRawpower)
        {
            int insertIndex = 12;

            ___UsecArsystems = new Skill(__instance, ESkillId.UsecArsystems, ESkillClass.Special, Array.Empty<SkillAction>(), Array.Empty<AbstractBuff>());
            ___BearAksystems = new Skill(__instance, ESkillId.BearAksystems, ESkillClass.Special, Array.Empty<SkillAction>(), Array.Empty<AbstractBuff>());

            ___UsecTactics = new Skill(__instance, ESkillId.UsecTactics, ESkillClass.Special, Array.Empty<SkillAction>(), Array.Empty<AbstractBuff>());
            ___BearRawpower = new Skill(__instance, ESkillId.BearRawpower, ESkillClass.Special, Array.Empty<SkillAction>(), Array.Empty<AbstractBuff>());

            var newDisplayList = new Skill[___DisplayList.Length + 4];

            Array.Copy(___DisplayList, newDisplayList, insertIndex);

            newDisplayList[12] = ___UsecArsystems;
            newDisplayList[12 + 1] = ___BearAksystems;

            newDisplayList[12 + 2] = ___UsecTactics;
            newDisplayList[12 + 3] = ___BearRawpower;

            Array.Copy(___DisplayList, insertIndex, newDisplayList, insertIndex + 4, ___DisplayList.Length - insertIndex);

            ___DisplayList = newDisplayList;

            Array.Resize(ref ___Skills, ___Skills.Length + 4);

            ___Skills[___Skills.Length - 1] = ___UsecArsystems;
            ___Skills[___Skills.Length - 2] = ___BearAksystems;

            ___Skills[___Skills.Length - 3] = ___UsecTactics;
            ___Skills[___Skills.Length - 4] = ___BearRawpower;

            // If the skill is not enabled, lock it
            AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.UsecArsystems,
                !Plugin.SkillData.UsecRifleSkill.Enabled);

            AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.BearAksystems,
                !Plugin.SkillData.BearRifleSkill.Enabled);

            AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.UsecTactics,
                !Plugin.SkillData.UsecTacticsSkill.Enabled);

            AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.BearRawpower,
                !Plugin.SkillData.BearRawPowerSkill.Enabled);
        }
    }

    internal class EnableSkillsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SkillManager).GetMethod("method_3", BindingFlags.Public | BindingFlags.Instance);

        [PatchPostfix]
        public static void Postfix(SkillManager __instance)
        {
            try
            {
                // If the skill is not enabled, lock it
                AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.FirstAid,
                    !Plugin.SkillData.MedicalSkills.EnableFirstAid);

                AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.FieldMedicine,
                    !Plugin.SkillData.MedicalSkills.EnableFieldMedicine);

                AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.Lockpicking,
                    !Plugin.SkillData.LockPickingSkill.Enabled);
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug(e);
            }
        }
    }

    internal class SimpleToolTipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SimpleTooltip).GetMethods().SingleCustom(x => x.Name == "Show" && x.GetParameters().Length == 5);

        private static SkillDataResponse _skillData => Plugin.SkillData;

        [PatchPostfix]
        public static void Postfix(SimpleTooltip __instance, ref string text)
        {
            string firstAid = @"\bFirstAidDescriptionPattern\b";
            string fieldMedicine = @"\bFieldMedicineDescriptionPattern\b";

            string usecARSystems = @"\bUsecArsystemsDescription\b";
            string usecTactics = @"\bUsecTacticsDescription\b";

            string lockPicking = @"\bLock picking skill\b";

            string bearAKSystems = @"\bBearAksystemsDescription\b";
            string bearRawpower = @"\bBearRawpowerDescription\b";

            if (Regex.IsMatch(text, firstAid))
            {
                var firstAidSkill = Plugin.Session.Profile.Skills.FirstAid;

                float speedBonus = firstAidSkill.IsEliteLevel
                    ? (firstAidSkill.Level * _skillData.MedicalSkills.MedicalSpeedBonus) - _skillData.MedicalSkills.MedicalSpeedBonusElite
                    : (firstAidSkill.Level * _skillData.MedicalSkills.MedicalSpeedBonus);

                float hpBonus = firstAidSkill.IsEliteLevel
                    ? firstAidSkill.Level * _skillData.MedicalSkills.MedkitHpBonus + _skillData.MedicalSkills.MedkitHpBonusElite
                    : firstAidSkill.Level * _skillData.MedicalSkills.MedkitHpBonus;

                __instance.SetText(SkillDescriptions.FirstAidDescription(speedBonus, hpBonus));
            }

            if (Regex.IsMatch(text, fieldMedicine))
            {
                var fieldMedicineSkill = Plugin.Session.Profile.Skills.FieldMedicine;

                float speedBonus = fieldMedicineSkill.IsEliteLevel
                    ? (fieldMedicineSkill.Level * _skillData.MedicalSkills.MedicalSpeedBonus) - _skillData.MedicalSkills.MedicalSpeedBonusElite
                    : (fieldMedicineSkill.Level * _skillData.MedicalSkills.MedicalSpeedBonus);

                __instance.SetText(SkillDescriptions.FieldMedicineDescription(speedBonus));
            }

            if (Regex.IsMatch(text, usecARSystems))
            {
                var usecSystems = Plugin.Session.Profile.Skills.UsecArsystems;

                float ergoBonus = usecSystems.IsEliteLevel
                    ? usecSystems.Level * _skillData.UsecRifleSkill.ErgoMod + _skillData.UsecRifleSkill.ErgoModElite
                    : usecSystems.Level * _skillData.UsecRifleSkill.ErgoMod;

                float recoilReduction = usecSystems.IsEliteLevel
                    ? usecSystems.Level * _skillData.UsecRifleSkill.RecoilReduction + _skillData.UsecRifleSkill.RecoilReductionElite
                    : usecSystems.Level * _skillData.UsecRifleSkill.RecoilReduction;

                __instance.SetText(SkillDescriptions.UsecArSystemsDescription(ergoBonus, recoilReduction));
            }

            if (Regex.IsMatch(text, bearAKSystems))
            {
                var bearSystems = Plugin.Session.Profile.Skills.BearAksystems;

                float ergoBonus = bearSystems.IsEliteLevel
                    ? bearSystems.Level * _skillData.BearRifleSkill.ErgoMod + _skillData.BearRifleSkill.ErgoModElite
                    : bearSystems.Level * _skillData.BearRifleSkill.ErgoMod;

                float recoilReduction = bearSystems.IsEliteLevel
                    ? bearSystems.Level * _skillData.BearRifleSkill.RecoilReduction + _skillData.BearRifleSkill.RecoilReductionElite
                    : bearSystems.Level * _skillData.BearRifleSkill.RecoilReduction;

                __instance.SetText(SkillDescriptions.BearAkSystemsDescription(ergoBonus, recoilReduction));
            }

            if (Regex.IsMatch(text, lockPicking))
            {
                var lockPickingSkill = Plugin.Session.Profile.Skills.Lockpicking;

                float timeReduction = lockPickingSkill.IsEliteLevel
                    ? lockPickingSkill.Level * _skillData.LockPickingSkill.TimeReduction + _skillData.LockPickingSkill.TimeReductionElite
                    : lockPickingSkill.Level * _skillData.LockPickingSkill.TimeReduction;

                __instance.SetText(SkillDescriptions.LockPickingDescription(timeReduction));
            }

            if (Regex.IsMatch(text, usecTactics))
            {
                var usecTacticsSkill = Plugin.Session.Profile.Skills.UsecTactics;

                float inertiaReduction = usecTacticsSkill.IsEliteLevel
                    ? usecTacticsSkill.Level * _skillData.UsecTacticsSkill.InertiaRedBonus + _skillData.UsecTacticsSkill.InertiaRedBonusElite
                    : usecTacticsSkill.Level * _skillData.UsecTacticsSkill.InertiaRedBonus;

                __instance.SetText(SkillDescriptions.UsecTacticsDescription(inertiaReduction));
            }

            if (Regex.IsMatch(text, bearRawpower))
            {
                var bearRawpowerSkill = Plugin.Session.Profile.Skills.BearRawpower;

                float hpBonus = bearRawpowerSkill.IsEliteLevel
                    ? bearRawpowerSkill.Level * _skillData.BearRawPowerSkill.HPBonus + _skillData.BearRawPowerSkill.HPBonusElite
                    : bearRawpowerSkill.Level * _skillData.BearRawPowerSkill.HPBonus;

                __instance.SetText(SkillDescriptions.BearRawpowerDescription(hpBonus));
            }
        }
    }

    internal class SkillPanelDisablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SkillPanel).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);

        [PatchPrefix]
        public static bool Prefix(Skill skill)
        {
            var skills = Plugin.Session.Profile.Skills;
            var side = Plugin.Session.Profile.Side;

            if (skill.Locked)
            {
                // Skip original method and dont show skill
                return false;
            }

            // Usec AR systems
            if (skill.Id == ESkillId.UsecArsystems && side == EPlayerSide.Bear && !skills.BearAksystems.IsEliteLevel)
            {
                if (Plugin.SkillData.DisableEliteRequirements)
                {
                    return true;
                }

                // Skip original method and dont show skill
                return false;
            }

            // Usec Tactics
            if (skill.Id == ESkillId.UsecTactics && side == EPlayerSide.Bear)
            {
                // Skip original method and dont show skill
                return false;
            }

            // Bear AK systems
            if (skill.Id == ESkillId.BearAksystems && side == EPlayerSide.Usec && !skills.UsecArsystems.IsEliteLevel)
            {
                if (Plugin.SkillData.DisableEliteRequirements)
                {
                    return true;
                }

                // Skip original method and dont show skill
                return false;
            }

            // Bear Raw Power
            if (skill.Id == ESkillId.BearRawpower && side == EPlayerSide.Usec)
            {
                // Skip original method and dont show skill
                return false;
            }

            // Show the skill
            return true;
        }
    }

    internal class SkillPanelNamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SkillPanel).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);

        [PatchPostfix]
        public static void Postfix(SkillPanel __instance, Skill skill)
        {
            if (skill.Id == ESkillId.UsecArsystems)
            {
                TextMeshProUGUI name = (TextMeshProUGUI)AccessTools.Field(typeof(SkillPanel), "_name").GetValue(__instance);
                name.text = "USEC rifle and carbine proficiency";
            }

            if (skill.Id == ESkillId.UsecTactics)
            {
                TextMeshProUGUI name = (TextMeshProUGUI)AccessTools.Field(typeof(SkillPanel), "_name").GetValue(__instance);
                name.text = "USEC Tactics";
            }

            if (skill.Id == ESkillId.BearAksystems)
            {
                TextMeshProUGUI name = (TextMeshProUGUI)AccessTools.Field(typeof(SkillPanel), "_name").GetValue(__instance);
                name.text = "BEAR rifle and carbine proficiency";
            }

            if (skill.Id == ESkillId.BearRawpower)
            {
                TextMeshProUGUI name = (TextMeshProUGUI)AccessTools.Field(typeof(SkillPanel), "_name").GetValue(__instance);
                name.text = "BEAR Raw Power";
            }
        }
    }

    internal class OnScreenChangePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(MenuTaskBar).GetMethod("OnScreenChanged");

        [PatchPrefix]
        public static void Prefix(EEftScreenType eftScreenType)
        {
            if (eftScreenType == EEftScreenType.Inventory)
            {
                if (Plugin.SkillData.MedicalSkills.EnableFieldMedicine)
                {
                    Plugin.FieldMedicineScript.fieldMedicineInstanceIDs.Clear();
                }

                if (Plugin.SkillData.MedicalSkills.EnableFirstAid)
                {
                    Plugin.FirstAidScript.firstAidInstanceIDs.Clear();
                }

                if (Plugin.SkillData.UsecRifleSkill.Enabled)
                {
                    Plugin.UsecRifleScript.weaponInstanceIds.Clear();
                }

                if (Plugin.SkillData.BearRifleSkill.Enabled)
                {
                    Plugin.BearRifleScript.weaponInstanceIds.Clear();
                }

                Utils.CheckServerModExists();
            }
        }
    }
}