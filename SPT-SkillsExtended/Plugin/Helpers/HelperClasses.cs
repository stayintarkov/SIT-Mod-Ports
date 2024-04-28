using EFT.HealthSystem;
using EFT.InventoryLogic;
using System.Collections.Generic;

namespace SkillsExtended.Helpers
{
    public sealed class MedKitValues : Meds2Class
    {
        public int MaxHpResource2 { set; get; }
        public float HpResourceRate2 { set; get; }
    }

    public sealed class HealthEffectValues : GInterface296
    {
        public float UseTime { set; get; }

        public KeyValuePair<EBodyPart, float>[] BodyPartTimeMults { set; get; }

        public Dictionary<EHealthFactorType, CutPiece> HealthEffects { set; get; }

        public Dictionary<EDamageEffectType, DamageEffect> DamageEffects { set; get; }

        public string StimulatorBuffs { set; get; }
    }

    public struct OrigWeaponValues
    {
        public float ergo;
        public float weaponUp;
        public float weaponBack;
    }
}