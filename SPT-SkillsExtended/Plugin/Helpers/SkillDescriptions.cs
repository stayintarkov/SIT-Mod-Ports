using SkillsExtended.Models;

namespace SkillsExtended.Helpers
{
    public static class SkillDescriptions
    {
        private static SkillDataResponse _skillData => Plugin.SkillData;

        public static string FirstAidDescription(float speedBonus, float hpBonus)
        {
            string medicalHpString = !Plugin.RealismConfig.med_changes
                ? $"\n\n Increases the HP resource of medical items by {_skillData.MedicalSkills.MedkitHpBonus * 100}% per level." +
                  $"\n\n Elite bonus: {_skillData.MedicalSkills.MedkitHpBonusElite * 100}%." +
                  $"\n\n Current speed bonus: <color=#54C1FFFF>{speedBonus * 100}%</color> " +
                  $"\n\n Current bonus HP: <color=#54C1FFFF>{hpBonus * 100}%</color>"

                : $"\n\n Current speed bonus: <color=#54C1FFFF>{speedBonus * 100}%</color> ";

            return $"First aid skills make use of first aid kits quicker and more effective." +
                    $"\n\n Increases the speed of healing items by {_skillData.MedicalSkills.MedicalSpeedBonus * 100}% per level. " +
                    $"\n\n Elite bonus: {_skillData.MedicalSkills.MedicalSpeedBonusElite * 100}% " +
                    medicalHpString;
        }

        public static string FieldMedicineDescription(float speedBonus)
        {
            return $"Field Medicine increases your skill at applying wound dressings. " +
                    $"\n\n Increases the speed of splints, bandages, and heavy bleed items {_skillData.MedicalSkills.MedicalSpeedBonus * 100}% per level." +
                    $"\n\n Elite bonus: {_skillData.MedicalSkills.MedicalSpeedBonusElite * 100}% " +
                    $"\n\n Current speed bonus: <color=#54C1FFFF>{speedBonus * 100}%</color>";
        }

        public static string UsecArSystemsDescription(float ergoBonus, float recoilReduction)
        {
            return $"As a USEC PMC, you excel in the use of NATO assault rifles and carbines." +
                    $"\n\nInceases ergonomics by {_skillData.UsecRifleSkill.ErgoMod * 100}% per level on NATO assault rifles and carbines. " +
                    $"\n{_skillData.UsecRifleSkill.ErgoModElite * 100}% Elite bonus" +
                    $"\n\nReduces vertical and horizontal recoil by {_skillData.UsecRifleSkill.RecoilReduction * 100}% per level. " +
                    $"\n{_skillData.UsecRifleSkill.RecoilReductionElite * 100}% Elite bonus" +
                    $"\nCurrent ergonomics bonus: <color=#54C1FFFF>{ergoBonus * 100}%</color>" +
                    $"\nCurrent recoil bonuses: <color=#54C1FFFF>{recoilReduction * 100}%</color>";
        }

        public static string BearAkSystemsDescription(float ergoBonus, float recoilReduction)
        {
            return $"As a BEAR PMC, you excel in the use of Russian assault rifles and carbines." +
                    $"\n\nInceases ergonomics by {_skillData.BearRifleSkill.ErgoMod * 100}% per level on Russian assault rifles and carbines." +
                    $"\n {_skillData.BearRifleSkill.ErgoModElite * 100}% Elite bonus" +
                    $"\n\nReduces vertical and horizontal recoil by {_skillData.BearRifleSkill.RecoilReduction * 100}% per level. " +
                    $"\n {_skillData.BearRifleSkill.RecoilReductionElite * 100}% Elite bonus" +
                    $"\n\nCurrent ergonomics bonus: <color=#54C1FFFF>{ergoBonus * 100}%</color>" +
                    $"\nCurrent recoil bonuses: <color=#54C1FFFF>{recoilReduction * 100}%</color>";
        }

        public static string LockPickingDescription(float timeReduction)
        {
            return "Picking locks requires patience and skill. As you gain levels, the chance for locks to be picked increases. Some locks are harder to pick than others." +
                $"\n\nReduces time it takes to pick locks by {_skillData.LockPickingSkill.TimeReduction * 100}%" +
                $"\n {_skillData.LockPickingSkill.TimeReductionElite * 100}% Elite bonus" +
                $"\n\nCurrent time reduction bonus: <color=#54C1FFFF>{timeReduction * 100}%</color>";
        }

        public static string UsecTacticsDescription(float inertiaReduction)
        {
            return $"Master the art of swift, calculated engagements with USEC's Tactical Prowess. " +
                    $"This skill minimizes inertia, allowing you to move seamlessly between cover and execute precise maneuvers. " +
                    $"\n\nReduces Intertia by {_skillData.UsecTacticsSkill.InertiaRedBonus * 100}% per level" +
                    $"\n{_skillData.UsecTacticsSkill.InertiaRedBonusElite * 100}% Elite bonus" +
                    $"\n\nCurrent inertia bonus: <color=#54C1FFFF>{inertiaReduction * 100}%</color>";
        }

        public static string BearRawpowerDescription(float hpBonus)
        {
            return $"This skill amplifies your survivability by boosting your overall health, " +
                $"turning you into a resilient force on the battlefield." +
                $"\n\nIncreases health by {_skillData.BearRawPowerSkill.HPBonus * 100}% per level" +
                $"\n{_skillData.BearRawPowerSkill.HPBonusElite * 100}% Elite bonus" +
                $"\n\nCurrent health bonus: <color=#54C1FFFF>{hpBonus * 100}%</color>";
        }
    }
}