using EFT.InventoryLogic;
using SAIN.Helpers;
using System;
using UnityEngine;

using TemplateIdToObjectMappingsClass = GClass2752;

namespace SAIN
{
    public class SAINWeaponInfo
    {
        public SAINWeaponInfo(Weapon weapon)
        {
            Weapon = weapon;
            WeaponClass = TryGetWeaponClass(weapon);
            AmmoCaliber = TryGetAmmoCaliber(weapon);
            TryCalculate(true);
        }

        private void Log()
        {
            if (SAINPlugin.DebugMode)
            {
                Logger.LogWarning(
                    $"Found Weapon Info: " +
                    $"Weapon: [{Weapon.ShortName}] " +
                    $"Weapon Class: [{WeaponClass}] " +
                    $"Ammo Caliber: [{AmmoCaliber}] " +
                    $"Calculated Audible Range: [{CalculatedAudibleRange}] " +
                    $"Base Audible Range: [{BaseAudibleRange}] " +
                    $"Muzzle Loudness: [{MuzzleLoudness}] " +
                    $"Muzzle Loudness Realism: [{MuzzleLoudnessRealism}] " +
                    $"Speed Factor: [{SpeedFactor}] " +
                    $"Subsonic: [{Subsonic}] " +
                    $"Has Red Dot? [{HasRedDot}] " +
                    $"Has Optic? [{HasOptic}] " +
                    $"Has Suppressor? [{HasSuppressor}]");
            }
        }

        public bool TryCalculate(bool skipTimer = false)
        {
            if (Weapon == null)
            {
                return false;
            }

            if (!skipTimer && lastCalcTime + calcFreq > Time.time)
            {
                return false;
            }
            lastCalcTime = Time.time;

            float realismLoudness = 0;
            MuzzleLoudness = 0;
            HasRedDot = false;
            HasOptic = false;
            HasSuppressor = false;

            foreach (var mod in Weapon.Mods)
            {
                // Checks if this weapon has an optic or suppressor for AI decision making
                CheckModForSuppresorAndSights(mod);

                // Calculate loudness
                if (!ModDetection.RealismLoaded)
                {
                    MuzzleLoudness += mod.Template.Loudness;
                }
                else
                {
                    // For RealismMod: if the muzzle device has a silencer attached to it then it shouldn't contribute to the loudness stat.
                    Item containedItem = null;
                    if (mod.Slots.Length > 0)
                    {
                        containedItem = mod.Slots[0].ContainedItem;
                    }
                    if (containedItem == null
                        || (containedItem is Mod modItem && IsModSuppressor(modItem, out var suppressor)))
                    {
                        realismLoudness += mod.Template.Loudness;
                    }
                }
            }

            if (ModDetection.RealismLoaded)
            {
                MuzzleLoudnessRealism = (realismLoudness / 200) + 1f;
                CalculatedAudibleRange = BaseAudibleRange * SuppressorModifier * MuzzleLoudnessRealism;
            }
            else
            {
                CalculatedAudibleRange = BaseAudibleRange * SuppressorModifier + MuzzleLoudness;
            }

            if (CalculatedAudibleRange != lastCalculatedRange)
            {
                lastCalculatedRange = CalculatedAudibleRange;
                Log();
            }

            return true;
        }

        private const float calcFreq = 1f;
        private float lastCalcTime;

        public readonly Weapon Weapon;
        public readonly IWeaponClass WeaponClass;
        public readonly ICaliber AmmoCaliber;

        public float CalculatedAudibleRange { get; private set; }
        private float lastCalculatedRange;

        public AISoundType AISoundType => HasSuppressor ? AISoundType.silencedGun : AISoundType.gun;

        public float BaseAudibleRange
        {
            get
            {
                if (SAINPlugin.LoadedPreset?.GlobalSettings?.Hearing?.HearingDistances.TryGetValue(AmmoCaliber, out var range) == true)
                {
                    return range;
                }
                Logger.LogError($"Cannot find base audible range for Caliber: [{AmmoCaliber}]");
                return 150f;
            }
        }

        public float MuzzleLoudness { get; private set; }

        public float MuzzleLoudnessRealism { get; private set; }

        public float SuppressorModifier
        {
            get
            {
                float supmod = 1f;
                bool suppressed = HasSuppressor;

                if (suppressed && Subsonic)
                {
                    supmod *= SAINPlugin.LoadedPreset.GlobalSettings.Hearing.SubsonicModifier;
                }
                else if (suppressed)
                {
                    supmod *= SAINPlugin.LoadedPreset.GlobalSettings.Hearing.SuppressorModifier;
                }
                return supmod;
            }
        }

        public float SpeedFactor => 2f - Weapon.SpeedFactor;

        private const float SuperSonicSpeed = 343.2f;

        public bool Subsonic
        {
            get
            {
                if (Weapon == null)
                {
                    return false;
                }
                return Weapon.CurrentAmmoTemplate.InitialSpeed * SpeedFactor < SuperSonicSpeed;
            }
        }

        public bool HasRedDot { get; private set; }

        public bool HasOptic { get; private set; }

        public bool HasSuppressor { get; private set; }

        public static bool IsModSuppressor(Mod mod, out Item suppressor)
        {
            suppressor = null;
            if (mod.Slots.Length > 0)
            {
                Item item = mod.Slots[0].ContainedItem;
                if (item != null && mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[SuppressorTypeId])
                {
                    suppressor = item;
                }
            }
            return suppressor != null;
        }

        private void CheckModForSuppresorAndSights(Mod mod)
        {
            if (mod != null)
            {
                Type modType = mod.GetType();
                if (!HasSuppressor && IsSilencer(modType))
                {
                    HasSuppressor = true;
                }
                else if (!sightFound)
                {
                    if (IsOptic(modType))
                    {
                        HasOptic = true;
                    }
                    else if (IsRedDot(modType))
                    {
                        HasRedDot = true;
                    }
                }
            }
        }

        private bool sightFound => HasRedDot || HasOptic;

        private static bool IsSilencer(Type modType)
        {
            return modType == TemplateIdToObjectMappingsClass.TypeTable[SuppressorTypeId];
        }

        private static bool IsOptic(Type modType)
        {
            return CheckTemplates(modType, AssaultScopeTypeId, OpticScopeTypeId, SpecialScopeTypeId);
        }

        private static bool IsRedDot(Type modType)
        {
            return CheckTemplates(modType, CollimatorTypeId, CompactCollimatorTypeId);
        }

        private static bool CheckTemplates(Type modType, params string[] templateIDs)
        {
            for (int i = 0; i < templateIDs.Length; i++)
            {
                if (CheckTemplateType(modType, templateIDs[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckTemplateType(Type modType, string id)
        {
            if (TemplateIdToObjectMappingsClass.TypeTable.TryGetValue(id, out Type result))
            {
                if (result == modType)
                {
                    return true;
                }
            }
            if (TemplateIdToObjectMappingsClass.TemplateTypeTable.TryGetValue(id, out result))
            {
                if (result == modType)
                {
                    return true;
                }
            }
            return false;
        }

        public static readonly string SuppressorTypeId = "550aa4cd4bdc2dd8348b456c";
        public static readonly string CollimatorTypeId = "55818ad54bdc2ddc698b4569";
        public static readonly string CompactCollimatorTypeId = "55818acf4bdc2dde698b456b";
        public static readonly string AssaultScopeTypeId = "55818add4bdc2d5b648b456f";
        public static readonly string OpticScopeTypeId = "55818ae44bdc2dde698b456c";
        public static readonly string SpecialScopeTypeId = "55818aeb4bdc2ddc698b456a";

        private static IWeaponClass TryGetWeaponClass(Weapon weapon)
        {
            IWeaponClass WeaponClass = EnumValues.TryParse<IWeaponClass>(weapon.Template.weapClass);
            if (WeaponClass == default)
            {
                WeaponClass = EnumValues.TryParse<IWeaponClass>(weapon.WeapClass);
            }
            return WeaponClass;
        }

        private static ICaliber TryGetAmmoCaliber(Weapon weapon)
        {
            ICaliber caliber = EnumValues.TryParse<ICaliber>(weapon.Template.ammoCaliber);
            if (caliber == default)
            {
                caliber = EnumValues.TryParse<ICaliber>(weapon.AmmoCaliber);
            }
            return caliber;
        }
    }
}
