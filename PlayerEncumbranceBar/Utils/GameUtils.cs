using System;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace PlayerEncumbranceBar.Utils
{
    // this static helper class solely exists to try to remove GClassXXXX and assorted other references
    public static class GameUtils
    {
        // reflection
        private static Type _profileInterface = typeof(ISession).GetInterfaces().First(i =>
            {
                var properties = i.GetProperties();
                return properties.Length == 2 &&
                       properties.Any(p => p.Name == "Profile");
            });

        private static PropertyInfo _sessionProfileProperty = AccessTools.Property(_profileInterface, "Profile");
        private static PropertyInfo _sessionProfileOfPetProperty = AccessTools.Property(_profileInterface, "ProfileOfPet");

        private static FieldInfo _skillManagerStrengthBuffEliteField = AccessTools.Field(typeof(SkillManager), "StrengthBuffElite");

        private static FieldInfo _inventoryTotalWeightEliteSkillField = AccessTools.Field(typeof(Inventory), "TotalWeightEliteSkill");
        private static FieldInfo _inventoryTotalWeightField = AccessTools.Field(typeof(Inventory), "TotalWeight");

        private static PropertyInfo _floatWrapperValueProperty = AccessTools.Property(_inventoryTotalWeightField.FieldType, "Value");
        private static FieldInfo _boolWrapperValueField = AccessTools.Field(_skillManagerStrengthBuffEliteField.FieldType, "Value");

        private static FieldInfo _backendConfigSettingsStaminaField = AccessTools.Field(typeof(BackendConfigSettingsClass), "Stamina");
        private static FieldInfo _staminaWalkOverweightLimitsField = AccessTools.Field(_backendConfigSettingsStaminaField.FieldType, "WalkOverweightLimits");
        private static FieldInfo _staminaBaseOverweightLimitsField = AccessTools.Field(_backendConfigSettingsStaminaField.FieldType, "BaseOverweightLimits");

        public static ISession Session => ClientAppUtils.GetMainApp().GetClientBackEndSession();
        public static Profile PlayerProfile => _sessionProfileProperty.GetValue(Session) as Profile;
        public static Profile ScavProfile => _sessionProfileOfPetProperty.GetValue(Session) as Profile;
        public static Inventory Inventory => PlayerProfile.Inventory;
        public static SkillManager Skills => PlayerProfile.Skills;

        public static float GetPlayerCurrentWeight()
        {
            var profile = PlayerProfile;

            // get local game to check if player is scav
            var game = Singleton<AbstractGame>.Instance;
            if (game != null && game is LocalGame)
            {
                var localGame = game as LocalGame;
                if (localGame?.PlayerOwner?.Player?.Profile != null)
                {
                    profile = localGame.PlayerOwner.Player.Profile;
                }
            }

            var inventory = profile.Inventory;
            var skills = profile.Skills;

            var totalWeightEliteSkillWrapper = _inventoryTotalWeightEliteSkillField.GetValue(inventory);
            var totalWeightEliteSkill = (float)_floatWrapperValueProperty.GetValue(totalWeightEliteSkillWrapper);

            var totalWeightWrapper = _inventoryTotalWeightField.GetValue(inventory);
            var totalWeight = (float)_floatWrapperValueProperty.GetValue(totalWeightWrapper);

            var strengthBuffEliteBuff = _skillManagerStrengthBuffEliteField.GetValue(skills);
            var hasEliteBuff = (bool)_boolWrapperValueField.GetValue(strengthBuffEliteBuff);

            return hasEliteBuff ? totalWeightEliteSkill : totalWeight;
        }

        public static Vector2 GetBaseOverweightLimits()
        {
            var backendSettings = Singleton<BackendConfigSettingsClass>.Instance;
            var stamina = _backendConfigSettingsStaminaField.GetValue(backendSettings);

            return (Vector2)_staminaBaseOverweightLimitsField.GetValue(stamina);
        }

        public static Vector2 GetWalkOverweightLimits()
        {
            var backendSettings = Singleton<BackendConfigSettingsClass>.Instance;
            var stamina = _backendConfigSettingsStaminaField.GetValue(backendSettings);

            return (Vector2)_staminaWalkOverweightLimitsField.GetValue(stamina);
        }

        public static RectTransform GetRectTransform(this GameObject gameObject)
        {
            return gameObject.transform as RectTransform;
        }

        public static RectTransform GetRectTransform(this Component component)
        {
            return component.transform as RectTransform;
        }

        public static void ResetTransform(this GameObject gameObject)
        {
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
		    gameObject.transform.localScale = Vector3.one;
        }
    }
}
