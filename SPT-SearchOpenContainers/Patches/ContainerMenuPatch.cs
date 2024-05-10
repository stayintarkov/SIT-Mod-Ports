using Aki.Reflection.Patching;
using EFT;
using System;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Utils;
using HarmonyLib;
using EFT.Interactive;
using System.Collections;
using UnityEngine;

namespace DrakiaXYZ.SearchOpenContainers.Patches
{
    internal class ContainerMenuPatch : ModulePatch
    {
        private static Type _playerActionClassType;
        private static Type _menuClassType;
        private static Type _menuItemClassType;
        private static Type _interactionTypeClassType;
        private static Type _stringLocalizeType;

        private static FieldInfo _menuItemNameField;
        private static FieldInfo _menuItemActionField;
        private static FieldInfo _menuActionsField;

        private static MethodInfo _containerSearchMethod;
        private static MethodInfo _playerInteractContainerMethod;
        private static MethodInfo _localizedMethod;

        private static object interactionTypeInstance;

        protected override MethodBase GetTargetMethod()
        {
            // Setup the class type references we'll need
            _playerActionClassType = PatchConstants.EftTypes.Single(x => x.GetMethods().Where(method => method.Name == "GetAvailableActions").Count() > 0); // GClass1826
            _menuClassType = PatchConstants.EftTypes.Single(x => x.GetMethod("SelectNextAction") != null); // GClass2888
            _menuItemClassType = PatchConstants.EftTypes.Single(x => x.GetField("TargetName") != null && x.GetField("Disabled") != null); // GClass2887
            _interactionTypeClassType = PatchConstants.EftTypes.Single(x => x.GetField("InteractionType") != null && x.BaseType == typeof(object)); // GClass2843
            _stringLocalizeType = PatchConstants.EftTypes.Single(x => x.GetMethod("LocalizeAreaName") != null);

            // Get field accessors we'll need
            _menuItemNameField = AccessTools.Field(_menuItemClassType, "Name");
            _menuItemActionField = AccessTools.Field(_menuItemClassType, "Action");
            _menuActionsField = AccessTools.Field(_menuClassType, "Actions");

            // Get method info we'll need
            _containerSearchMethod = AccessTools.GetDeclaredMethods(_playerActionClassType).FirstOrDefault(mi =>
            {
                var parameters = mi.GetParameters();
                return parameters.Length == 4
                    && parameters[0].Name == "owner"
                    && parameters[1].Name == "callback"
                    && parameters[2].Name == "lootableContainer"
                    && parameters[3].Name == "initialDistance";
            });

            _playerInteractContainerMethod = AccessTools.GetDeclaredMethods(typeof(Player)).FirstOrDefault(mi =>
            {
                var parameters = mi.GetParameters();
                return parameters.Length == 2
                    && parameters[0].Name == "door"
                    && parameters[1].Name == "interactionResult";
            });

            _localizedMethod = AccessTools.Method(_stringLocalizeType, "Localized", new Type[] { typeof(string), typeof(string) });

            // We only need one instance of the InteractionType class, so create it now
            interactionTypeInstance = Activator.CreateInstance(_interactionTypeClassType, new object[] { EInteractionType.Open });

            return AccessTools.GetDeclaredMethods(_playerActionClassType).FirstOrDefault(mi =>
            {
                var parameters = mi.GetParameters();
                return parameters.Length == 2
                    && parameters[0].Name == "owner"
                    && parameters[1].Name == "container";
            });
        }

        [PatchPostfix]
        public static void PatchPostfix(ref object __result, GamePlayerOwner owner, LootableContainer container)
        {
            // If the container is open, add "Search" to the top of the menu
            if (__result != null && container.DoorState == EDoorState.Open)
            {
                // We can access a List<Type> as a generic list using IList
                IList menuItems = _menuActionsField.GetValue(__result) as IList;

                var actionHandler = new SearchActionHandler
                {
                    owner = owner,
                    container = container,
                    initialDistance = Vector3.Distance(owner.Player.Transform.position, container.transform.position)
                };

                // We can create an instance of a compile time unknown type using Activator
                object searchMenuItem = Activator.CreateInstance(_menuItemClassType, new object[] { });

                // And then use FieldInfo objects to populate its
                _menuItemNameField.SetValue(searchMenuItem, _localizedMethod.Invoke(null, new object[] { "Search", null }));
                _menuItemActionField.SetValue(searchMenuItem, new Action(actionHandler.StartOpenContainer));

                // And because our list is generic, we can just insert into it like normal
                menuItems.Insert(0, searchMenuItem);
            }
        }

        internal class SearchActionHandler
        {
            public GamePlayerOwner owner;
            public LootableContainer container;
            public float initialDistance;

            public void StartOpenContainer()
            {
                // First tell the player object we're interacting with the container
                _playerInteractContainerMethod.Invoke(owner.Player, new object[] { container, interactionTypeInstance });

                // Then set the players interact callback to our OpenContainerCallback
                owner.Player.SetCallbackForInteraction(new Action<Action>(OpenContainerCallback));

                // Trigger the callback we just set
                owner.Player.TryInteractionCallback(container);
            }

            public void OpenContainerCallback(Action callback)
            {
                _containerSearchMethod.Invoke(null, new object[] { owner, callback, container, initialDistance });
            }
        }
    }
}
