using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using IcyClawz.CustomInteractions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SIT.Tarkov.Core;

namespace TraderModding
{
    internal static class PlayerExtensions
    {
        private static readonly FieldInfo InventoryControllerField =
            typeof(Player).GetField("_inventoryController", BindingFlags.NonPublic | BindingFlags.Instance);
        public static InventoryController GetInventoryController(this Player player) =>
            InventoryControllerField.GetValue(player) as InventoryController;
    }

    internal sealed class CustomInteractionsProvider : IItemCustomInteractionsProvider
    {
        internal static StaticIcons StaticIcons => EFTHardSettings.Instance.StaticIcons;
        public IEnumerable<CustomInteraction> GetCustomInteractions(ItemUiContext uiContext, EItemViewType viewType, Item item)
        {  
            var isInRaid = PatchConstants.EftTypes.Single(x => x.GetField("IsNetworkGame") != null).GetField("InRaid").GetValue(null);
            
            if (viewType != EItemViewType.Inventory || (bool)isInRaid)
            {
                yield break;
            }
            {
                var component = item.GetItemComponent<FireModeComponent>();
                if (component != null)
                {
                    yield return new CustomInteraction
                    {
                        Caption = () => "TRADER MODDING",
                        Icon = () => StaticIcons.GetAttributeIcon(EItemAttributeId.RaidModdable),
                        Enabled = () => true,
                        Action = () =>
                        {
                            // Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
                            ComponentUtils.TraderModding(uiContext, item);
                            // uiContext.RedrawContextMenus(new[] { item.TemplateId });
                        }
                    };
                }
            }
        }

    }

    internal static class ComponentUtils
    {
       public static void TraderModding(ItemUiContext uiContext, Item weapon)
        {
            Globals.isTraderModding = true;
            // We grab data from server of all loyalty unlocked mods from traders that aren't a barter offer.
            Globals.traderMods = TraderModdingUtils.GetData();
            // add the parts from the current gun to the traderMods list because spaghetti :D
            TraderModdingUtils.AddGunParts(weapon);

            uiContext.EditBuild((Weapon)weapon);
        }
    }



}
