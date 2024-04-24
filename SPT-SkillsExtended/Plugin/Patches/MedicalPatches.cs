using Aki.Reflection.Patching;
using EFT;
using EFT.InventoryLogic;
using System.Linq;
using System.Reflection;

namespace SkillsExtended.Patches
{
    internal class DoMedEffectPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethods().First(m =>
                m.Name == "SetInHands" && m.GetParameters()[0].Name == "meds");
        }

        [PatchPrefix]
        public static void Prefix(Player __instance, MedsClass meds, EBodyPart bodyPart)
        {
            // Dont give xp for surgery
            if (meds.TemplateId == "5d02778e86f774203e7dedbe" || meds.TemplateId == "5d02797c86f774203f38e30a")
            {
                return;
            }

            if (!__instance.IsYourPlayer)
            {
                return;
            }

            if (Plugin.SkillData.MedicalSkills.FmItemList.Contains(meds.TemplateId) && Plugin.SkillData.MedicalSkills.EnableFieldMedicine)
            {
                Plugin.FieldMedicineScript.ApplyFieldMedicineExp(bodyPart);
                Plugin.Log.LogDebug("Field Medicine Effect");
                return;
            }

            if (Plugin.SkillData.MedicalSkills.FaItemList.Contains(meds.TemplateId) && Plugin.SkillData.MedicalSkills.EnableFirstAid)
            {
                Plugin.FirstAidScript.ApplyFirstAidExp(bodyPart);
            }
        }
    }

    internal class SetItemInHands : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("TryProceed", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        public static void Postfix(Player __instance, Item item)
        {
            // Dont give xp for surgery
            if (item.TemplateId == "5d02778e86f774203e7dedbe" || item.TemplateId == "5d02797c86f774203f38e30a")
            {
                return;
            }

            if (!__instance.IsYourPlayer)
            {
                return;
            }

            if (Plugin.SkillData.MedicalSkills.FmItemList.Contains(item.TemplateId) && Plugin.SkillData.MedicalSkills.EnableFieldMedicine)
            {
                Plugin.FieldMedicineScript.ApplyFieldMedicineExp(EBodyPart.Common);
                return;
            }

            if (Plugin.SkillData.MedicalSkills.FaItemList.Contains(item.TemplateId) && Plugin.SkillData.MedicalSkills.EnableFirstAid)
            {
                Plugin.FirstAidScript.ApplyFirstAidExp(EBodyPart.Common);
            }
        }
    }
}