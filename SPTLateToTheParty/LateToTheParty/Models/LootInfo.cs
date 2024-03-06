using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;

namespace LateToTheParty.Models
{
    public enum ELootType
    {
        Invalid = 0,
        Loose = 1,
        Static = 2,
    }

    public class LootInfo
    {
        public PathAccessibilityData PathData = new PathAccessibilityData();
        public ELootType LootType { get; } = ELootType.Invalid;
        public bool IsDestroyed { get; set; } = false;
        public bool IsInPlayerInventory { get; set; } = false;
        public TraderControllerClass TraderController { get; set; } = null;
        public Transform Transform { get; set; } = null;
        public double DistanceToNearestSpawnPoint { get; set; } = 0;
        public double RaidETWhenFound { get; set; } = -999;
        public double RaidETWhenDestroyed { get; set; } = -999;
        public bool CanDestroy { get; set; } = false;
        public Item ParentItem { get; set; } = null;
        public WorldInteractiveObject ParentContainer = null;
        public WorldInteractiveObject NearbyInteractiveObject { get; set; } = null;

        public LootInfo(ELootType lootType)
        {
            LootType = lootType;
        }

        public LootInfo(ELootType lootType, TraderControllerClass traderController, Transform transform) : this(lootType)
        {
            TraderController = traderController;
            Transform = transform;
        }

        public LootInfo(ELootType lootType, TraderControllerClass traderController, Transform transform, double distanceToNearestSpawnPoint) : this(lootType, traderController, transform)
        {
            DistanceToNearestSpawnPoint = distanceToNearestSpawnPoint;
        }

        public LootInfo(ELootType lootType, TraderControllerClass traderController, Transform transform, double distanceToNearestSpawnPoint, double raidET) : this(lootType, traderController, transform, distanceToNearestSpawnPoint)
        {
            RaidETWhenFound = raidET;
        }
    }
}
