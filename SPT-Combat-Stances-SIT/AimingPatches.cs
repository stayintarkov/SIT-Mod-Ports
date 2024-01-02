using StayInTarkov;
using CombatStances;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using InventoryItemHandler = ItemMovementHandler; 


namespace CombatStances
{

    public static class AimController
    {
        private static bool hasSetActiveAimADS = false;
        private static bool wasToggled = false;
        private static bool hasSetCanAds = false;

        private static bool checkProtruding(ProtrudableComponent x)
        {
            return x.IsProtruding();
        }

        //bsg's bullshit
        private static bool IsAllowedADSWithFS(Weapon weapon, Player.FirearmController fc)
        {
            if (weapon.CompactHandling)
            {
                bool stockIsDeployed = false;
                IEnumerable<ProtrudableComponent> foldableStockComponents = Enumerable.Empty<ProtrudableComponent>();
                FoldableComponent foldableComponent;
                if (InventoryItemHandler.CanFold(weapon, out foldableComponent))
                {
                    if (foldableComponent.FoldedSlot == null)
                    {
                        stockIsDeployed |= !foldableComponent.Folded;
                    }
                    else if (foldableComponent.FoldedSlot.ContainedItem != null)
                    {
                        foldableStockComponents = Enumerable.ToArray<ProtrudableComponent>(foldableComponent.FoldedSlot.ContainedItem.GetItemComponentsInChildren<ProtrudableComponent>(true));
                        bool stockIsProtruding;
                        if (!foldableComponent.Folded)
                        {
                            stockIsProtruding = Enumerable.Any<ProtrudableComponent>(foldableStockComponents, new Func<ProtrudableComponent, bool>(checkProtruding));
                        }
                        else
                        {
                            stockIsProtruding = false;
                        }
                        stockIsDeployed = (stockIsDeployed || stockIsProtruding);
                    }
                }
                IEnumerable<ProtrudableComponent> stocks = Enumerable.Except<ProtrudableComponent>(weapon.GetItemComponentsInChildren<ProtrudableComponent>(true), foldableStockComponents);
                stockIsDeployed |= Enumerable.Any<ProtrudableComponent>(stocks, new Func<ProtrudableComponent, bool>(checkProtruding));
                return !stockIsDeployed;
            }

            if (weapon.WeapClass == "pistol")
            {
                return true;
            }

            return false;
        }

        public static void ADSCheck(Player player, EFT.Player.FirearmController fc)
        {
            if (!player.IsAI && fc.Item != null)
            {
                bool isAiming = (bool)AccessTools.Field(typeof(EFT.Player.FirearmController), "_isAiming").GetValue(fc);
                FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                ThermalVisionComponent thermComponent = player.ThermalVisionObserver.Component;
                bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                bool thermalIsOn = thermComponent != null && (thermComponent.Togglable == null || thermComponent.Togglable.On);
                bool isAllowedADSFS = IsAllowedADSWithFS(fc.Item, fc);
                bool visionDeviceBlocksADS = Plugin.EnableNVGPatch.Value && (Plugin.HasOptic && (nvgIsOn || thermalIsOn));
                if (visionDeviceBlocksADS || (Plugin.EnableFSPatch.Value && (fsIsON && !isAllowedADSFS)))
                {
                    if (!hasSetCanAds)
                    {
                        if (isAiming)
                        {
                            fc.ToggleAim();
                        }
                        Plugin.IsAllowedADS = false;
                        hasSetCanAds = true;
                    }
                }
                else
                {
                    Plugin.IsAllowedADS = true;
                    hasSetCanAds = false;
                }

                if (StanceController.IsActiveAiming && !hasSetActiveAimADS)
                {
                    player.MovementContext.SetAimingSlowdown(true, 0.33f);
                    hasSetActiveAimADS = true;
                }
                else if (!StanceController.IsActiveAiming && hasSetActiveAimADS)
                {
                    player.MovementContext.SetAimingSlowdown(false, 0.33f);
                    if (isAiming)
                    {
                        player.MovementContext.SetAimingSlowdown(true, 0.33f);
                    }

                    hasSetActiveAimADS = false;
                }

                if (isAiming)
                {
                    StanceController.IsPatrolStance = false;
                }

                if (!wasToggled && (fsIsON || nvgIsOn))
                {
                    wasToggled = true;
                }
                if (wasToggled == true && (!fsIsON && !nvgIsOn))
                {
                    StanceController.WasActiveAim = false;
                    if (Plugin.ToggleActiveAim.Value)
                    {
                        StanceController.StanceBlender.Target = 0f;
                        StanceController.IsActiveAiming = false;
                    }
                    wasToggled = false;
                }

                if (player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire)
                {
                    Plugin.IsAiming = isAiming;
                    StanceController.PistolIsColliding = false;
                }
                else if (fc.Item.WeapClass == "pistol")
                {
                    StanceController.PistolIsColliding = true;
                }

            }
        }
    }

    public class SetAimingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("set_IsAiming", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance, bool value, ref bool ____isAiming)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
            if (__instance.Item.WeapClass == "pistol")
            {
                player.Physical.Aim((!____isAiming || !(player.MovementContext.StationaryWeapon == null)) ? 0f : __instance.ErgonomicWeight * 0.2f);
            }
        }
    }

    public class ToggleAimPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("ToggleAim", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
            if ((Plugin.EnableFSPatch.Value || Plugin.EnableNVGPatch.Value) && !player.IsAI)
            {
                StanceController.CanResetAimDrain = true;

                return Plugin.IsAllowedADS;
            }
            return true;
        }
    }
}