using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using SkillsExtended.Helpers;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using static EFT.SkillManager;

using static SkillsExtended.Helpers.Constants;

namespace SkillsExtended.Patches
{
    internal class SkillManagerConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SkillManager).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(EPlayerSide) }, null);

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

            AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.UsecArsystems, false);
            AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.BearAksystems, false);
            AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.UsecTactics, true);
            AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.BearRawpower, true);
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
                AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.FirstAid, false);
                AccessTools.Field(typeof(Skill), "Locked").SetValue(__instance.FieldMedicine, false);
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

        [PatchPostfix]
        public static void Postfix(SimpleTooltip __instance, ref string text)
        {
            string firstAid = @"\bFirstAidDescriptionPattern\b";
            string fieldMedicine = @"\bFieldMedicineDescriptionPattern\b";
            
            string usecARSystems = @"\bUsecArsystemsDescription\b";
            string usecTactics = @"\bUsecTacticsDescription\b";

            string bearAKSystems = @"\bBearAksystemsDescription\b";
            string bearRawpower = @"\bBearRawpowerDescription\b";

            if (Regex.IsMatch(text, firstAid))
            {
                var firstAidSkill = Plugin.Session.Profile.Skills.FirstAid;

                float speedBonus = firstAidSkill.IsEliteLevel
                    ? (firstAidSkill.Level * MEDICAL_SPEED_BONUS) - MEDICAL_SPEED_BONUS_ELITE
                    : (firstAidSkill.Level * MEDICAL_SPEED_BONUS);

                float hpBonus = firstAidSkill.IsEliteLevel
                    ? firstAidSkill.Level * MEDKIT_HP_BONUS + MEDKIT_HP_BONUS_ELITE
                    : firstAidSkill.Level * MEDKIT_HP_BONUS;

                __instance.SetText(SkillDescriptions.FirstAidDescription(speedBonus, hpBonus));
            }

            if (Regex.IsMatch(text, fieldMedicine))
            {
                var fieldMedicineSkill = Plugin.Session.Profile.Skills.FieldMedicine;

                float speedBonus = fieldMedicineSkill.IsEliteLevel
                    ? (fieldMedicineSkill.Level * MEDICAL_SPEED_BONUS) - MEDICAL_SPEED_BONUS_ELITE
                    : (fieldMedicineSkill.Level * MEDICAL_SPEED_BONUS);

                __instance.SetText(SkillDescriptions.FieldMedicineDescription(speedBonus));
            }

            if (Regex.IsMatch(text, usecARSystems))
            {
                var usecSystems = Plugin.Session.Profile.Skills.UsecArsystems;

                float ergoBonus = usecSystems.IsEliteLevel
                    ? usecSystems.Level * ERGO_MOD + ERGO_MOD_ELITE
                    : usecSystems.Level * ERGO_MOD;

                float recoilReduction = usecSystems.IsEliteLevel
                    ? usecSystems.Level * RECOIL_REDUCTION + RECOIL_REDUCTION_ELITE
                    : usecSystems.Level * RECOIL_REDUCTION;

                __instance.SetText(SkillDescriptions.UsecArSystemsDescription(ergoBonus, recoilReduction));
            }

            if (Regex.IsMatch(text, bearAKSystems))
            {
                var bearSystems = Plugin.Session.Profile.Skills.BearAksystems;

                float ergoBonus = bearSystems.IsEliteLevel
                    ? bearSystems.Level * ERGO_MOD + ERGO_MOD_ELITE
                    : bearSystems.Level * ERGO_MOD;

                float recoilReduction = bearSystems.IsEliteLevel
                    ? bearSystems.Level * RECOIL_REDUCTION + RECOIL_REDUCTION_ELITE
                    : bearSystems.Level * RECOIL_REDUCTION;

                __instance.SetText(SkillDescriptions.BearAkSystemsDescription(ergoBonus, recoilReduction));
            }

            if (Regex.IsMatch(text, usecTactics))
            {
                var usecTacticsSkill = Plugin.Session.Profile.Skills.UsecTactics;

                float inertiaReduction = usecTacticsSkill.IsEliteLevel
                    ? usecTacticsSkill.Level * USEC_INERTIA_RED_BONUS + USEC_INERTIA_RED_BONUS_ELITE
                    : usecTacticsSkill.Level * USEC_INERTIA_RED_BONUS;

                float aimPunchReduction = usecTacticsSkill.IsEliteLevel
                    ? usecTacticsSkill.Level * USEC_AIMPUNCH_RED_BONUS + USEC_AIMPUNCH_RED_BONUS_ELITE
                    : usecTacticsSkill.Level * USEC_AIMPUNCH_RED_BONUS;

                __instance.SetText(SkillDescriptions.UsecTacticsDescription(inertiaReduction, aimPunchReduction));
            }

            if (Regex.IsMatch(text, bearRawpower))
            {
                var bearRawpowerSkill = Plugin.Session.Profile.Skills.BearRawpower;

                float hpBonus = bearRawpowerSkill.IsEliteLevel
                    ? bearRawpowerSkill.Level * BEAR_POWER_HP_BONUS + BEAR_POWER_HP_BONUS_ELITE
                    : bearRawpowerSkill.Level * BEAR_POWER_HP_BONUS;

                float carryWeightBonus = bearRawpowerSkill.IsEliteLevel
                    ? bearRawpowerSkill.Level * BEAR_POWER_CARRY_BONUS + BEAR_POWER_CARRY_BONUS_ELITE
                    : bearRawpowerSkill.Level * BEAR_POWER_CARRY_BONUS;

                __instance.SetText(SkillDescriptions.BearRawpowerDescription(hpBonus, carryWeightBonus));
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
                if (SEConfig.disableEliteRequirement.Value)
                {
                    return true;
                }

                // Skip original method and dont show skill
                return false;
            }

            /*
            // Usec Tactics
            if (skill.Id == ESkillId.UsecTactics && side == EPlayerSide.Bear)
            {
                // Skip original method and dont show skill
                return false;
            }
            */

            // Bear AK systems
            if (skill.Id == ESkillId.BearAksystems && side == EPlayerSide.Usec && !skills.UsecArsystems.IsEliteLevel)
            {
                if (SEConfig.disableEliteRequirement.Value)
                {
                    return true;
                }

                // Skip original method and dont show skill
                return false;
            }

            /*
            // Bear Raw Power
            if (skill.Id == ESkillId.BearRawpower && side == EPlayerSide.Usec)
            {
                // Skip original method and dont show skill
                return false;
            }
            */

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
                Plugin.MedicalScript.fieldMedicineInstanceIDs.Clear();
                Plugin.MedicalScript.firstAidInstanceIDs.Clear();
                Plugin.WeaponsScript.weaponInstanceIds.Clear();
                Utils.CheckServerModExists();
            }
        }
    }
}