using StayInTarkov;
using System.Reflection;
using BepInEx.Logging;
using EFT.InventoryLogic;

// This patch helps you find the classes needed for EquipmentTypes.cs
// It will log the class of the item to console when you move it in inventory
namespace LootingBots.Patch
{
    public class GetItemClassFromInspect : ModulePatch
    {
       private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("LootingBots DEBUG:");
        
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemMovementHandler).GetMethod(
                "Move",
                BindingFlags.Public | BindingFlags.Static
            );
        }

        [PatchPrefix]
        private static bool PatchPreFix(Item item)
        {
            logger.LogInfo($"Item Class: {item.GetType().Name}");
            return true;
        }
    }
}