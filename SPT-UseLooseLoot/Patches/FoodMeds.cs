using System;
using System.Reflection;
using System.Linq;
using System.Collections;

using Aki.Reflection.Patching;
using Aki.Reflection.Utils;

using EFT.InventoryLogic;
using EFT;

using HarmonyLib;

namespace Gaylatea
{
    namespace UseLooseLoot
    {
        /// <summary>
        /// Changes loose food and med items so that they can be used without
        /// needing to take them into your inventory first.
        /// </summary>
        class MakeFoodMedsUsablePatch : ModulePatch
        {
            private static Type _playerActionClassType;
            private static Type _menuClassType;
            private static Type _menuItemClassType;
            private static Type _stringLocalizeType;

            private static FieldInfo _menuItemNameField;
            private static FieldInfo _menuItemActionField;
            private static FieldInfo _menuActionsField;

            private static MethodInfo _localizedMethod;

            protected override MethodBase GetTargetMethod()
            {
                _playerActionClassType = PatchConstants.EftTypes.Single(x => x.GetMethods().Where(method => method.Name == "GetAvailableActions").Count() > 0);
                _menuClassType = PatchConstants.EftTypes.Single(x => x.GetMethod("SelectNextAction") != null);
                _menuItemClassType = PatchConstants.EftTypes.Single(x => x.GetField("TargetName") != null && x.GetField("Disabled") != null);
                _stringLocalizeType = PatchConstants.EftTypes.Single(x => x.GetMethod("LocalizeAreaName") != null);

                _menuItemNameField = AccessTools.Field(_menuItemClassType, "Name");
                _menuItemActionField = AccessTools.Field(_menuItemClassType, "Action");
                _menuActionsField = AccessTools.Field(_menuClassType, "Actions");

                _localizedMethod = AccessTools.Method(_stringLocalizeType, "Localized", new Type[] { typeof(string), typeof(string) });

                // Find the method to hook to by its parameter names, instead of method name, incase BSG adds more methods
                return AccessTools.GetDeclaredMethods(_playerActionClassType).FirstOrDefault(IsTargetMethod);
            }

            private static bool IsTargetMethod(MethodInfo mi)
            {
                var parameters = mi.GetParameters();
                return parameters.Length > 3
                    && parameters[0].Name == "owner"
                    && parameters[1].Name == "rootItem"
                    && parameters[2].Name == "lootItemOwner";
            }

            [PatchPostfix]
            public static void PatchPostfix(ref object __result, Item rootItem, GamePlayerOwner owner)
            {
                if(!(rootItem is MedsClass) && !(rootItem is FoodClass)) {
                    return;
                }

                // We can access a List<Type> as a generic list using IList
                IList menuItems = _menuActionsField.GetValue(__result) as IList;

                var actionHandler = new FoodMedUser
                {
                    owner = owner,
                    item = rootItem
                };

                // We can create an instance of a compile time unknown type using Activator
                object searchMenuItem = Activator.CreateInstance(_menuItemClassType, new object[] { });

                // And then use FieldInfo objects to populate its
                _menuItemNameField.SetValue(searchMenuItem, _localizedMethod.Invoke(null, new object[] { "Use", null }));
                _menuItemActionField.SetValue(searchMenuItem, new Action(actionHandler.UseAll));

                // And because our list is generic, we can just add into it like normal
                menuItems.Add(searchMenuItem);
            }
        }

        class FoodMedUser {
            public GamePlayerOwner owner;
            public Item item;

            public void UseAll() {
                this.owner.Player.HealthController.ApplyItem(this.item, EBodyPart.Common);
            }
        }
    }
}