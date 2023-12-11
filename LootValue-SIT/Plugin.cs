using System;
using System.Reflection;
using EFT;
using EFT.UI.DragAndDrop;
using EFT.InventoryLogic;
using EFT.UI;
//using Aki.Reflection.Patching;
//using Aki.Reflection.Utils;
//using CurrencyUtil = GClass2334;
using CurrencyUtil = GClass2385;
using static LootValue.Globals;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Newtonsoft.Json;
using static UnityEngine.EventSystems.EventTrigger;
using static System.Collections.Specialized.BitVector32;
using System.Threading.Tasks;

using StayInTarkov;
using TraderClass = Trader;
using InventoryControllerClass = InventoryController;
using GClass1716 = GClass1766;
using GClass1711 = GClass1761;
using TraderAssortmentControllerClass = Assortment;

/// https://hub.sp-tarkov.com/files/file/1606-lootvalue/
/// https://github.com/IhanaMies/LootValue

namespace LootValue
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class LootValueMod : BaseUnityPlugin
    {
        // BepinEx
        public const string pluginGuid = "IhanaMies.LootValue";
        public const string pluginName = "LootValue";
        public const string pluginVersion = "1.2.2";

        private void Awake()
        {
            Config.SaveOnConfigSet = true;

            logger = Logger;

            SetupConfig();

            new LootValueTraderPatch().Enable();
            new LootValueShowTooltipPatch().Enable();
            new LootValueGridItemOnPointerEnterPatch().Enable();
            new LootValueGridItemOnPointerExitPatch().Enable();
            new LootValueItemViewOnClickPatch().Enable();

            Config.SettingChanged += Config_SettingChanged;
        }

        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            ConfigEntryBase entry = e.ChangedSetting;

            logger.LogInfo($"Settings changed - {entry.Definition.Section}:{entry.Definition.Key}");

            if (entry.Definition.Key == "Custom colours")
            {
                if (UseCustomColours.Value)
                {
                    logger.LogInfo($"Read colors");
                    SlotColoring.ReadColors(CustomColours.Value);
                }
            }

            if (entry.Definition.Key == "Custom colours" || entry.Definition.Key == "Use custom colours")
            {
                if (UseCustomColours.Value)
                {
                    SlotColoring.ReadColors(CustomColours.Value);
                }
                else
                {
                    SlotColoring.UseDefaultColors();
                }
            }
        }

        internal static ConfigEntry<bool> UseCustomColours;
        internal static ConfigEntry<string> CustomColours;
        internal static ConfigEntry<bool> EnableQuickSell;
        internal static ConfigEntry<bool> EnableFleaQuickSell;
        internal static ConfigEntry<bool> OneButtonQuickSell;
        internal static ConfigEntry<bool> OneButtonQuickSellFlea;

        internal static ConfigEntry<bool> OnlyShowTotalValue;
        internal static ConfigEntry<bool> ShowFleaPriceBeforeAccess;
        internal static ConfigEntry<bool> IgnoreFleaMaxOfferCount;

        private void SetupConfig()
        {
            OneButtonQuickSell = Config.Bind("Quick Sell", "One button quick sell", false);
            OneButtonQuickSellFlea = Config.Bind("Quick Sell", "One button quick only. Sell FIR item to trader if flea orders are full", false);
            OnlyShowTotalValue = Config.Bind("Quick Sell", "Only show total value", false);
            EnableQuickSell = Config.Bind("Quick Sell", "Enable quick sell", true, "Hold Left Alt + Left Shift while left clicking an item to quick sell either to flea (if enabled) or trader which ever has better value");
            EnableFleaQuickSell = Config.Bind("Quick Sell", "Enable flea quick sell", true);
            ShowFleaPriceBeforeAccess = Config.Bind("Flea", "Show flea price before access", false);
            IgnoreFleaMaxOfferCount = Config.Bind("Flea", "Ignore flea max offer count", false);

            UseCustomColours = Config.Bind("Colours", "Use custom colours", false);
            CustomColours = Config.Bind("Colours", "Custom colours", "[5000:#ff0000],[10000:#ffff00],[:#ffffff]",
@"Colouring bound is marked as [int:hexcolor] e.q. [lower than this value : will be this hexcolor]
The values should incremental from lower to higher and last value should be valueless.
For example [5000:#ff0000],[10000:#ffff00],[:#ffffff] means three different bounds.
Anything under 5000 rubles, will be red.
Anything under 10000 rubles, will be yellow.
The third is marked as the ultimate color. Anything over 10000 rubles would be white.
"
            );

            if (UseCustomColours.Value)
                SlotColoring.ReadColors(CustomColours.Value);
        }
    }

    internal class LootValueTraderPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TraderClass).GetConstructors()[0];

        [PatchPostfix]
        private static void PatchPostfix(ref TraderClass __instance)
        {
            __instance.UpdateSupplyData();
        }
    }

    internal static class PlayerExtensions
    {
        private static readonly FieldInfo InventoryControllerField =
            typeof(Player).GetField("_inventoryController", BindingFlags.NonPublic | BindingFlags.Instance);

        public static InventoryControllerClass GetInventoryController(this Player player) =>
            InventoryControllerField.GetValue(player) as InventoryControllerClass;
    }

    internal static class Globals
    {
        public static bool isStashItemHovered = false;
        //public static ISession Session => ClientAppUtils.GetMainApp().GetClientBackEndSession();
        public static ISession Session => StayInTarkovHelperConstants.GetMainApp().GetClientBackEndSession();
        public static ManualLogSource logger { get; set; }
        public static Item hoveredItem;

        public static TraderOffer GetBestTraderOffer(Item item)
        {
            if (!Session.Profile.Examined(item))
                return null;

            switch (item.Owner?.OwnerType)
            {
                case EOwnerType.RagFair:
                case EOwnerType.Trader:
                    if (item.StackObjectsCount > 1 || item.UnlimitedCount)
                    {
                        item = item.CloneItem();
                        item.StackObjectsCount = 1;
                        item.UnlimitedCount = false;
                    }
                    break;
            }

            TraderOffer highestOffer = null;

            if (item is Weapon weapon)
            {
                // Get price of all mods + base price
                List<Item> itemsToPrice = new List<Item> { new Item(weapon.Id, weapon.Template) };
                foreach (Mod m in weapon.Mods)
                {
                    itemsToPrice.Add(new Item(m.Id, m.Template));
                }

                // Get highest price of total weapon build
                foreach (TraderClass trader in Session.Traders)
                {
                    if (!trader.Info.Available || trader.Info.Disabled || !trader.Info.Unlocked)
                        continue;

                    //Skip COOP Traders & Anastasia
                    if (trader.Id.Contains("usecTrader") || trader.Id.Contains("bearTrader") || trader.Id.Contains("coopTrader") || trader.Id.Contains("Anastasia"))
                        continue;

                    TraderOffer curPrice = null;

                    foreach (Item mod in itemsToPrice)
                    {
                        if (GetTraderOffer(mod, trader) is TraderOffer offer)
                        {
                            if (curPrice == null)
                            {
                                curPrice = offer;
                            }
                            else
                            {
                                curPrice.Price += offer.Price;
                            }
                        }
                    }

                    if (curPrice == null)
                    {
                        continue;
                    }

                    // Update highest offer
                    if (highestOffer == null)
                    {
                        highestOffer = curPrice;
                    }
                    else
                    {
                        if (curPrice.Price > highestOffer.Price)
                        {
                            highestOffer = curPrice;
                        }
                    }
                }
            }
            else
            {
                foreach (TraderClass trader in Session.Traders)
                {
                    if (!trader.Info.Available || trader.Info.Disabled || !trader.Info.Unlocked)
                        continue;

                    //Skip COOP Traders & Anastasia
                    if (trader.Id.Contains("usecTrader") || trader.Id.Contains("bearTrader") || trader.Id.Contains("coopTrader") || trader.Id.Contains("Anastasia"))
                        continue;

                    if (GetTraderOffer(item, trader) is TraderOffer offer)
                        if (highestOffer == null || offer.Price > highestOffer.Price)
                            highestOffer = offer;
                }
            }

            return highestOffer;
        }

        public class FleaPriceRequest
        {
            public string templateId;
            public FleaPriceRequest(string templateId) => this.templateId = templateId;
        }

        private static TraderOffer GetTraderOffer(Item item, TraderClass trader)
        {
            var result = trader.GetUserItemPrice(item);
            if (result == null)
                return null;

            return new TraderOffer(
                trader.Id,
                trader.LocalizedName,
                result.Value.Amount,
                CurrencyUtil.GetCurrencyCharById(result.Value.CurrencyId),
                trader.GetSupplyData().CurrencyCourses[result.Value.CurrencyId],
                item.StackObjectsCount
            );
        }

        public sealed class TraderOffer
        {
            public string TraderId;
            public string TraderName;
            public int Price;
            public string Currency;
            public double Course;
            public int Count;

            public TraderOffer(string traderId, string traderName, int price, string currency, double course, int count)
            {
                TraderId = traderId;
                TraderName = traderName;
                Price = price;
                Currency = currency;
                Course = course;
                Count = count;
            }
        }
    }

    public class ItemShowTooltipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GridItemView).GetMethod("ShowTooltip", BindingFlags.Instance | BindingFlags.NonPublic);

        [PatchPrefix]
        static void Prefix(GridItemView __instance)
        {
            if (__instance.Item != null)
                hoveredItem = __instance.Item;
        }
    }

    internal class LootValueGridItemOnPointerEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GridItemView).GetMethod("OnPointerEnter", BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        static void Prefix(GridItemView __instance, PointerEventData eventData)
        {
            if (__instance.Item != null)
            {
                hoveredItem = __instance.Item;
                Globals.isStashItemHovered = true;
            }
        }
    }

    internal class LootValueGridItemOnPointerExitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GridItemView).GetMethod("OnPointerExit", BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        static void Prefix(GridItemView __instance, PointerEventData eventData)
        {
            Globals.isStashItemHovered = false;
            hoveredItem = null;
        }
    }

    public class SellItemToTraderRequest
    {
        public string ItemId;
        public string TraderId;
        public int Price;

        public SellItemToTraderRequest(string itemId, string traderId, int price)
        {
            this.ItemId = itemId;
            this.TraderId = traderId;
            this.Price = price;
        }
    }

    internal class LootValueItemViewOnClickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GridItemView).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);

        private static HashSet<string> itemSells = new HashSet<string>();

        [PatchPrefix]
        static async void Prefix(GridItemView __instance, PointerEventData.InputButton button, Vector2 position, bool doubleClick)
        {
            Item item = __instance.Item;

            if (itemSells.Contains(item.Id))
                return;

            itemSells.Add(item.Id);

            if (LootValueMod.EnableQuickSell.Value && !GClass1766.InRaid && item != null)
            {
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt))
                {
                    //One button quicksell
                    if (LootValueMod.OneButtonQuickSell.Value)
                    {
                        if (button == PointerEventData.InputButton.Left)
                        {
                            TraderOffer bestTraderOffer = GetBestTraderOffer(item);
                            double? fleaPrice = null;

                            if (item.MarkedAsSpawnedInSession)
                                fleaPrice = FleaPriceCache.FetchPrice(item.TemplateId);

                            if (bestTraderOffer != null)
                            {
                                if (fleaPrice.HasValue && fleaPrice.Value > bestTraderOffer.Price)
                                {
                                    if (!HasFleaSlotToSell(item))
                                    {
                                        if (LootValueMod.OneButtonQuickSellFlea.Value)
                                        {
                                            NotificationManagerClass.DisplayWarningNotification("Maximum number of flea offers reached. Sell to trader");

                                            using (TraderClass traderClass = Globals.Session.GetTrader(bestTraderOffer.TraderId))
                                            {
                                                if (traderClass.CurrentAssortment == null)
                                                    await traderClass.RefreshAssortment(true, true);

                                                TraderAssortmentControllerClass tacc = traderClass.CurrentAssortment;
                                                tacc.PrepareToSell(item, new LocationInGrid(2, 3, ItemRotation.Horizontal));
                                                tacc.Sell();
                                            }
                                        }
                                        else
                                        {
                                            NotificationManagerClass.DisplayWarningNotification("Maximum number of flea offers reached");
                                        }

                                        itemSells.Remove(item.Id);
                                        return;
                                    }

                                    GClass1711[] gs = {
                                        new GClass1711 { count = fleaPrice.Value - 1, _tpl = "5449016a4bdc2d6f028b456f" }
                                    };

                                    Session.RagFair.AddOffer(false, new string[1] { item.Id }, gs, null);

                                    if (!HasFleaSlotToSell(item))
                                        NotificationManagerClass.DisplayWarningNotification("Maximum number of flea offers reached");
                                }
                                else
                                {
                                    using (TraderClass traderClass = Globals.Session.GetTrader(bestTraderOffer.TraderId))
                                    {
                                        if (traderClass.CurrentAssortment == null)
                                            await traderClass.RefreshAssortment(true, true);

                                        TraderAssortmentControllerClass tacc = traderClass.CurrentAssortment;
                                        tacc.PrepareToSell(item, new LocationInGrid(2, 3, ItemRotation.Horizontal));
                                        tacc.Sell();
                                    }
                                }
                            }
                        }
                    }
                    else //Two button quicksell
                    {
                        if (button == PointerEventData.InputButton.Left)
                        {
                            await SellToTrader(item);
                        }
                        else if (button == PointerEventData.InputButton.Right)
                        {
                            SellToFlea(item);
                        }
                    }
                }
            }

            itemSells.Remove(item.Id);
        }

        static async Task SellToTrader(Item item)
        {
            try
            {
                TraderOffer bestTraderOffer = GetBestTraderOffer(item);

                if (bestTraderOffer != null)
                {
                    using (TraderClass traderClass = Globals.Session.GetTrader(bestTraderOffer.TraderId))
                    {
                        if (traderClass.CurrentAssortment == null)
                            await traderClass.RefreshAssortment(true, true);

                        TraderAssortmentControllerClass tacc = traderClass.CurrentAssortment;
                        tacc.PrepareToSell(item, new LocationInGrid(2, 3, ItemRotation.Horizontal));
                        tacc.Sell();
                    }
                }

                itemSells.Remove(item.Id);
            }
            catch (Exception ex)
            {
                itemSells.Remove(item.Id);

                logger.LogDebug($"Something fucked up: {ex.Message}");
                logger.LogDebug($"{ex.InnerException.Message}");
            }
        }

        static bool HasFleaSlotToSell(Item item)
        {
            return LootValueMod.IgnoreFleaMaxOfferCount.Value || Session.RagFair.MyOffersCount < Session.RagFair.GetMaxOffersCount(Session.RagFair.MyRating);
        }

        static void SellToFlea(Item item)
        {
            if (!item.MarkedAsSpawnedInSession || !Session.RagFair.Available)
                return;

            double? fleaPrice = FleaPriceCache.FetchPrice(item.TemplateId);

            if (!HasFleaSlotToSell(item))
            {
                NotificationManagerClass.DisplayWarningNotification("Maximum number of flea offers reached");
                return;
            }

            if (Session.RagFair.Available && fleaPrice.HasValue)
            {
                GClass1711[] gs = {
                    new GClass1711 { count = fleaPrice.Value - 1, _tpl = "5449016a4bdc2d6f028b456f" }
                };

                Session.RagFair.AddOffer(false, new string[1] { item.Id }, gs, null);
            }
        }
    }

    internal class LootValueShowTooltipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(SimpleTooltip).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        private static void Prefix(ref string text, ref float delay, SimpleTooltip __instance)
        {
            delay = 0;

            bool isFleaEligible = false;
            double lowestFleaOffer = 0;

            if (hoveredItem != null)
            {
                TraderOffer bestTraderOffer = GetBestTraderOffer(hoveredItem);

                //For weapons we want to fetch each mods flea price, if eligible
                if (hoveredItem is Weapon weapon)
                {
                    double totalFleaPrice = 0;

                    foreach (Mod mod in weapon.Mods)
                    {
                        if (mod.MarkedAsSpawnedInSession)
                        {
                            double? fleaPrice = FleaPriceCache.FetchPrice(mod.TemplateId);

                            if (fleaPrice.HasValue)
                            {
                                isFleaEligible = true;
                                totalFleaPrice += fleaPrice.Value * mod.StackObjectsCount;
                            }
                        }
                    }

                    // Add base weapon price
                    if (weapon.MarkedAsSpawnedInSession)
                    {
                        double? fleaPrice = FleaPriceCache.FetchPrice(weapon.TemplateId);

                        if (fleaPrice.HasValue)
                        {
                            isFleaEligible = true;
                            totalFleaPrice += fleaPrice.Value;
                        }
                    }

                    if (totalFleaPrice > 0)
                        lowestFleaOffer = totalFleaPrice;
                }
                else if (hoveredItem.MarkedAsSpawnedInSession)
                {
                    double? fleaPrice = FleaPriceCache.FetchPrice(hoveredItem.TemplateId);

                    if (fleaPrice.HasValue)
                    {
                        isFleaEligible = true;
                        lowestFleaOffer = fleaPrice.Value * hoveredItem.StackObjectsCount;
                    }
                }

                int fleaPricePerSlot = 0, traderPricePerSlot = 0;

                var size = hoveredItem.CalculateCellSize();
                int slots = size.X * size.Y;

                if (isFleaEligible)
                    fleaPricePerSlot = (int)Math.Round(lowestFleaOffer / slots);

                if (bestTraderOffer != null)
                {
                    double totalTraderPrice = bestTraderOffer.Price;
                    traderPricePerSlot = (int)Math.Round(totalTraderPrice / slots);

                    SetText(traderPricePerSlot, fleaPricePerSlot, totalTraderPrice, slots, ref text, bestTraderOffer.TraderName);
                }

                if (isFleaEligible)
                    SetText(fleaPricePerSlot, traderPricePerSlot, lowestFleaOffer, slots, ref text, "Flea");

                //hoveredItem = null;
            }
        }

        private static void SetText(int valuePerSlotA, int valuePerSlotB, double totalValue, int slots, ref string text, string buyer)
        {
            string perSlotColor = SlotColoring.GetColorFromValuePerSlots(valuePerSlotA);
            string highlightText;

            if (valuePerSlotA > valuePerSlotB)
                highlightText = $"<color=#ffffff>{buyer}</color>";
            else
                highlightText = buyer;

            if (LootValueMod.OnlyShowTotalValue.Value)
            {
                text += $"<br>{highlightText}: <color={perSlotColor}>{totalValue.FormatNumber()}</color>";
            }
            else
            {
                text += $"<br>{highlightText}: <color={perSlotColor}>{valuePerSlotA.FormatNumber()}</color>";

                if (slots > 1)
                    text += $" Total: {totalValue.FormatNumber()}";
            }
        }
    }
}
