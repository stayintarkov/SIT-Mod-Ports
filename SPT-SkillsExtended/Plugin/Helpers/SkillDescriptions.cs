using static SkillsExtended.Helpers.Constants;

namespace SkillsExtended.Helpers
{
    public static class SkillDescriptions
    {
        public static string FirstAidDescription(float speedBonus, float hpBonus)
        {
            return $"First aid skills make use of first aid kits quicker and more effective." +
                    $"\n\n Increases the speed of healing items by {MEDICAL_SPEED_BONUS * 100}% per level. " +
                    $"\n\n Elite bonus: {MEDICAL_SPEED_BONUS_ELITE * 100}% " +
                    $"\n\n Increases the HP resource of medical items by {MEDKIT_HP_BONUS * 100}% per level." +
                    $"\n\n Elite bonus: {MEDKIT_HP_BONUS_ELITE * 100}%." +
                    $"\n\n Current speed bonus: <color=#54C1FFFF>{speedBonus * 100}%</color> " +
                    $"\n\n Current bonus HP: <color=#54C1FFFF>{hpBonus * 100}%</color>";
        }

        public static string FieldMedicineDescription(float speedBonus)
        {
            return $"Field Medicine increases your skill at applying wound dressings. " +
                    $"\n\n Increases the speed of splints, bandages, and heavy bleed items {MEDICAL_SPEED_BONUS * 100}% per level." +
                    $"\n\n Elite bonus: {MEDICAL_SPEED_BONUS_ELITE * 100}% " +
                    $"\n\n Current speed bonus: <color=#54C1FFFF>{speedBonus * 100}%</color>";
        }

        public static string UsecArSystemsDescription(float ergoBonus, float recoilReduction)
        {
            return $"As a USEC PMC, you excel in the use of NATO assault rifles and carbines." +
                    $"\n\nInceases ergonomics by {ERGO_MOD * 100}% per level on NATO assault rifles and carbines. " +
                    $"\n{RECOIL_REDUCTION_ELITE * 100}% Elite bonus" +
                    $"\n\nReduces vertical and horizontal recoil by {RECOIL_REDUCTION * 100}% per level. " +
                    $"\n{RECOIL_REDUCTION_ELITE * 100}% Elite bonus" +
                    $"\nCurrent ergonomics bonus: <color=#54C1FFFF>{ergoBonus * 100}%</color>" +
                    $"\nCurrent recoil bonuses: <color=#54C1FFFF>{recoilReduction * 100}%</color>";
        }

        public static string BearAkSystemsDescription(float ergoBonus, float recoilReduction)
        {
            return $"As a BEAR PMC, you excel in the use of Russian assault rifles and carbines." +
                    $"\n\nInceases ergonomics by {ERGO_MOD * 100}% per level on Russian assault rifles and carbines." +
                    $"\n {RECOIL_REDUCTION_ELITE * 100}% Elite bonus" +
                    $"\n\nReduces vertical and horizontal recoil by {RECOIL_REDUCTION * 100}% per level. " +
                    $"\n {RECOIL_REDUCTION_ELITE * 100}% Elite bonus" +
                    $"\n\nCurrent ergonomics bonus: <color=#54C1FFFF>{ergoBonus * 100}%</color>" +
                    $"\nCurrent recoil bonuses: <color=#54C1FFFF>{recoilReduction * 100}%</color>";
        }

        public static string UsecTacticsDescription(float inertiaReduction, float aimPunchReduction)
        {
            return $"Master the art of swift, calculated engagements with USEC's Tactical Prowess. " +
                    $"This skill minimizes inertia, allowing you to move seamlessly between cover and execute precise maneuvers. " +
                    $"Additionally, it reduces aimpunch, granting you superior control over your aim even in the heat of battle." +
                    $"\n\nReduces Intertia by {USEC_INERTIA_RED_BONUS * 100}% per level" +
                    $"\n{USEC_INERTIA_RED_BONUS_ELITE * 100}% Elite bonus" +
                    $"\n\nReduces aim punch by {USEC_AIMPUNCH_RED_BONUS * 100}% per level. " +
                    $"\n{USEC_AIMPUNCH_RED_BONUS_ELITE * 100}% Elite bonus" +
                    $"\n\nCurrent inertia bonus: <color=#54C1FFFF>{inertiaReduction * 100}%</color>" +
                    $"\nCurrent aimpunch bonus: <color=#54C1FFFF>{aimPunchReduction * 100}%</color>";
        }

        public static string BearRawpowerDescription(float hpBonus, float carryWeightBonus)
        {
            return $"Unleash the untamed strength within with BEAR Raw Power. This skill amplifies your survivability by boosting your overall health, " +
                $"turning you into a resilient force on the battlefield. Moreover, " +
                $"revel in the enhanced physical prowess as your carry weight capacity expands, " +
                $"allowing you to wield heavier armaments and gear. Harness the true essence of BEAR's raw power for unparalleled endurance and dominance in the face of adversity." +
                $"\n\nIncreases health by {BEAR_POWER_HP_BONUS * 100}% per level" +
                $"\n{BEAR_POWER_HP_BONUS_ELITE * 100}% Elite bonus" +
                $"\n\nIncreases carry weight by {BEAR_POWER_CARRY_BONUS * 100}% per level. " +
                $"\n{BEAR_POWER_CARRY_BONUS_ELITE * 100}% Elite bonus" +
                $"\n\nCurrent health bonus: <color=#54C1FFFF>{hpBonus * 100}%</color>" +
                $"\nCurrent weight bonus: <color=#54C1FFFF>{carryWeightBonus * 100}%</color>";
        }
    }
}
