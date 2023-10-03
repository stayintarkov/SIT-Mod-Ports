using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Screens;
using EFT.UI.WeaponModding;
using HarmonyLib;
using System.Reflection;
using System.Linq;
using UnityEngine;
using SIT.Tarkov.Core;


namespace TraderModding
{
    public class ModsHidePatch : ModulePatch
    {
        // This patch returns false on the method that populates mods in the slot view container. pretty
        // spaghetti but it works

        protected override MethodBase GetTargetMethod()
        {
            // Better than "method_x", which can be changed at any time.
            return AccessTools.GetDeclaredMethods(typeof(DropDownMenu)).Single(m =>
            {
                var paramList = m.GetParameters();
                // Searching our desired method through parameters
                // method_?(GClass???? sourceContext, Item item, TraderControllerClass itemController, Transform container)
                return (paramList.Length == 4 && paramList[0].Name == "sourceContext" && paramList[3].Name == "container");
            });
        }

        [PatchPrefix]
        static bool Prefix(Item item, ref RectTransform container)
        {
            if (Globals.isTraderModding && Globals.traderMods.Length > 0)
            {
                if (Globals.traderMods.All(mod => mod != item.TemplateId))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class ScreenChangePatch : ModulePatch
    {
        // A patch that stops trader modding logic if player goes to inventory, menu or hideout.

        // TODO eventually add a way for player to be able to go to character tab in
        // TaskBarMenu without clearing trader modding.

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuTaskBar).GetMethod("OnScreenChanged", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        static void Prefix(EEftScreenType eftScreenType)
        {
            if (eftScreenType == EEftScreenType.Inventory || eftScreenType == EEftScreenType.MainMenu || eftScreenType == EEftScreenType.Hideout)
            {
                TraderModdingUtils.EndTraderModding();
            }
        }
    }

}
