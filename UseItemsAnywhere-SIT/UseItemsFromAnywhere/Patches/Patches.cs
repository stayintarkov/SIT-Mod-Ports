using StayInTarkov;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace UseItemsFromAnywhere
{
    public class UseItemsFromAnywhere
    {
        private static string[] items =
        {
            "62178c4d4ecf221597654e3d", // Red Flare
            "624c0b3340357b5f566e8766", // Yellow Flare
            "6217726288ed9f0845317459", // green Flare
            "62178be9d0050232da3485d9"  // white Flare
        };

        public class IsAtBindablePlace : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(InventoryControllerClass).GetMethod("IsAtBindablePlace", BindingFlags.Public | BindingFlags.Instance);

            [PatchPostfix]
            private static void Postfix(InventoryControllerClass __instance, ref bool __result, ref Item item)
            {
                Inventory inventory =
                    (Inventory)AccessTools.Property(typeof(InventoryControllerClass), "Inventory").GetValue(__instance);

                if (item is Weapon || item is GrenadeClass || item is MedsClass
                    || item is FoodClass || item is GItem9 || item is GGItem15
                    || item is RecodableItemClass || item.GetItemComponent<KnifeComponent>() != null
                    || items.Contains(item.TemplateId))
                {
                    if (inventory.Equipment.Contains(item))
                    {
                        __result = true;
                        return;
                    }
                    else
                    {
#if DEBUG
                        ConsoleScreen.Log($"{item.TemplateId} is not in the players inventory");
#endif
                        __result = false;
                        return;
                    }
                }
                else
                {
#if DEBUG
                    ConsoleScreen.Log($"Item {item.TemplateId} not of bindable type");
#endif
                    return;
                }
            }
        }

        public class IsAtReachablePlace : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(InventoryControllerClass).GetMethod("IsAtReachablePlace", BindingFlags.Public | BindingFlags.Instance);


            [PatchPostfix]
            private static void Postfix(InventoryControllerClass __instance, ref bool __result, ref Item item)
            {
                Inventory inventory =
                    (Inventory)AccessTools.Property(typeof(InventoryControllerClass), "Inventory").GetValue(__instance); 

                if (inventory.Equipment.Contains(item))
                {
                    __result = true;
                    return;
                }
                else
                {
#if DEBUG
                        ConsoleScreen.Log($"{item.TemplateId} is not in the players inventory");
#endif
                    __result = false;
                    return;
                }
            }
        }

        public class UnbindItemPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(InventoryControllerClass).GetMethod("UnbindItem", BindingFlags.Public | BindingFlags.Instance);


            [PatchPostfix]
            private static void Postfix(InventoryControllerClass __instance, ref bool __result, ref Item item)
            {
                Inventory inventory =
                    (Inventory)AccessTools.Property(typeof(InventoryControllerClass), "Inventory").GetValue(__instance);

                if (inventory.Equipment.Contains(item))
                {
                    __result = true;
                    return;
                }
                else
                {
#if DEBUG
                    ConsoleScreen.Log($"{item.TemplateId} is not in the players inventory");
#endif
                    __result = false;
                    return;
                }
            }
        }
    }
}