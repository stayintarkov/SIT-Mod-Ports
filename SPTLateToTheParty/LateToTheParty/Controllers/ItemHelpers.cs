using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public static class ItemHelpers
    {
        private static Dictionary<string, Item> allItems = new Dictionary<string, Item>();

        public static IEnumerable<Item> FindAllItemsInContainer(this Item container, bool includeSelf = false)
        {
            List<Item> allContainedItems = container.GetAllItems().ToList();
            
            if (!includeSelf)
            {
                allContainedItems.Remove(container);
            }

            foreach (Item item in allContainedItems.ToArray())
            {
                allContainedItems.AddRange(item.FindAllItemsInContainer(false));
            }

            return allContainedItems.Distinct();
        }

        public static IEnumerable<Item> FindAllItemsInContainers(this IEnumerable<Item> containers, bool includeSelf = false)
        {
            List<Item> allItems = new List<Item>();
            foreach (Item container in containers)
            {
                allItems.AddRange(container.FindAllItemsInContainer(includeSelf));
            }
            return allItems.Distinct();
        }

        public static IEnumerable<Item> FindAllRelatedItems(this IEnumerable<Item> items)
        {
            List<Item> allItems = new List<Item>();
            foreach (Item item in items)
            {
                Item parentItem = item.GetAllParentItemsAndSelf().Last();
                allItems.AddRange(parentItem.GetAllItems().Reverse());
            }
            return allItems.Distinct();
        }

        public static int GetItemSlots(this Item item)
        {
            int itemSlots = 0;
            if (ConfigController.LootRanking?.Items?.ContainsKey(item.TemplateId) == true)
            {
                itemSlots = (int)ConfigController.LootRanking.Items[item.TemplateId].Size;
            }
            else
            {
                var itemSize = item.CalculateCellSize();
                itemSlots = itemSize.X * itemSize.Y;
            }

            return itemSlots;
        }

        public static IEnumerable<string> GetSecureContainerIDs()
        {
            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
            if (itemFactory == null)
            {
                return Enumerable.Empty<string>();
            }

            // Find all possible secure containers
            List<string> secureContainerIDs = new List<string>();
            foreach (Item item in itemFactory.CreateAllItemsEver())
            {
                if (!(item.Template is SecureContainerTemplateClass))
                {
                    continue;
                }

                if (!(item.Template as SecureContainerTemplateClass).isSecured)
                {
                    continue;
                }

                secureContainerIDs.Add(item.TemplateId);
            }

            return secureContainerIDs;
        }

        public static Dictionary<string, Item> GetAllItems()
        {
            if (allItems.Count > 0)
            {
                return allItems;
            }

            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
            if (itemFactory == null)
            {
                return allItems;
            }

            foreach(Item item in itemFactory.CreateAllItemsEver())
            {
                allItems.Add(item.TemplateId, item);
            }

            LoggingController.LogInfo("Created dictionary of " + allItems.Count + " items");
            return allItems;
        }
    }
}
