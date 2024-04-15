using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using DoorBreach;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace ShootTheDoor
{
    internal class DoorBreachComponent : MonoBehaviour
    { //also all stolen from drakiaxyz
        public static ManualLogSource Logger { get; private set; } = BepInEx.Logging.Logger.CreateLogSource("ShootTheDoor");

        private DoorBreachComponent()
        {
            Logger.LogInfo($"This is ShootTheDoor {DoorBreachPlugin.PluginVarsion}");
        }

        public void Awake()
        {
            foreach (var openableObj in getAllOpenableObjects())
            {
                var correctStates = new[] { EDoorState.Shut, EDoorState.Locked, EDoorState.Open };
                if (!correctStates.Contains(openableObj.DoorState))
                {
                    Logger.LogDebug($"Unexpected obj state: {openableObj.DoorState} ({openableObj.ToString()})");
                    continue;
                }

                // Skip open doors
                if (openableObj.DoorState == EDoorState.Open)
                {
                    continue;
                }

                if (!openableObj.Operatable)
                {
                    Logger.LogDebug($"Obj is not operable ({openableObj.ToString()})");
                    continue;
                }

                if (openableObj.gameObject.layer != DoorBreachPlugin.interactiveLayer)
                {
                    Logger.LogDebug($"Obj is not in Interactive layer ({openableObj.ToString()})");
                    continue;
                }

                var hitpoints = openableObj.gameObject.GetOrAddComponent<Hitpoints>();
                hitpoints.hitpoints = DoorBreachPlugin.ObjectHP.Value;
                openableObj.OnEnable();
            }

        }

        private List<WorldInteractiveObject> getAllOpenableObjects()
        {
            List<WorldInteractiveObject> openableObjects = new List<WorldInteractiveObject> { };
            foreach (var door in FindObjectsOfType<Door>())
            {
                openableObjects.Add(door);
            }
            foreach (var container in FindObjectsOfType<LootableContainer>())
            {
                openableObjects.Add(container);
            }
            foreach (var carTrunc in FindObjectsOfType<Trunk>())
            {
                openableObjects.Add(carTrunc);
            }
            return openableObjects;
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<DoorBreachComponent>();
            }
        }
    }

    //create hitpoints unityengine.component
    internal class Hitpoints : MonoBehaviour
    {
        public float hitpoints;
    }

    internal class ApplyHit : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BallisticCollider).GetMethod(nameof(BallisticCollider.ApplyHit));


        [PatchPostfix]
        public static void PatchPostFix(DamageInfo damageInfo, ShotId shotID)
        {
            try
            {
                if (damageInfo.Player == null)
                {
                    return;
                }

                if (new[] { EFT.NetworkPackets.EHitType.Lamp, EFT.NetworkPackets.EHitType.Window }.Contains(damageInfo.HittedBallisticCollider.HitType))
                {
                    return;
                }

                BallisticCollider collider = damageInfo.HittedBallisticCollider;

                var hitpoints = collider.GetComponentInParent<Hitpoints>();
                if (hitpoints == null)
                {
                    return;
                }

                var interactableObj = getInteractableObj(collider);
                if (interactableObj == null)
                {
                    DoorBreachComponent.Logger.LogError("Unexpected null interactableObj");
                    return;
                }

                // I'm not sure we wan't to shoot-close open doors, so Open state is not suitable
                var suitableStates = new[] { EDoorState.Shut, EDoorState.Locked };
                if (!suitableStates.Contains(interactableObj.DoorState))
                {
                    DoorBreachComponent.Logger.LogDebug($"Skip {interactableObj.DoorState} state");
                    return;
                }

                DoorBreachComponent.Logger.LogDebug("Start processing correct shoot");

                var newDamage = getNewDamage(damageInfo);
                DoorBreachComponent.Logger.LogDebug($"Reducing income damage: {damageInfo.Damage} -> {newDamage}");
                DoorBreachComponent.Logger.LogDebug($"Obj hitpoints: {hitpoints.hitpoints} -> {hitpoints.hitpoints - newDamage}");
                hitpoints.hitpoints -= newDamage;

                if (hitpoints.hitpoints <= 0)
                {
                    // Now door is broken and we want to remove locked state, in case you want to close it by-hand later
                    if (interactableObj.DoorState == EDoorState.Locked)
                    {
                        interactableObj.DoorState = EDoorState.Shut;
                    }

                    // Using specific interraction for doors
                    EInteractionType interraction = (interactableObj as Door != null) ? EInteractionType.Breach : EInteractionType.Open;

                    var player = damageInfo.Player.AIData.Player;
                    player.CurrentManagedState.ExecuteDoorInteraction(interactableObj, new InteractionResult(interraction), null, player);
                }

                DoorBreachComponent.Logger.LogDebug("End processing correct shoot\n");
            }
            catch (Exception ex)
            {
                DoorBreachComponent.Logger.LogError($"Ex: {ex.ToString()}");
            }
        }

        private static WorldInteractiveObject getInteractableObj(BallisticCollider collider)
        {
            Door door = collider.GetComponentInParent<Door>();
            if (door != null)
            {
                return door;
            }

            var carTrunk = collider.GetComponentInParent<Trunk>();
            if (carTrunk != null)
            {
                return carTrunk;
            }

            var lootContainer = collider.GetComponentInParent<LootableContainer>();
            if (lootContainer != null)
            {
                return lootContainer;
            }

            return null;
        }

        private static float getNewDamage(DamageInfo damageInfo)
        {
            var materialType = damageInfo.HittedBallisticCollider.TypeOfMaterial;

            var damageAgainstObjects = getBaseDamageAgainstObjects(damageInfo);

            var materialMult = getMaterialDamageMult(materialType);
            var lockHitMult = getLockHitDamageMult(damageInfo);
            var meeleDmgMult = getMeeleDamageMult(damageInfo);
            var ammoDamageMult = getAmmoDamageMult(damageInfo);
            var totalMult = materialMult * lockHitMult * meeleDmgMult * ammoDamageMult;
            var newDamage = damageAgainstObjects * totalMult;

            DoorBreachComponent.Logger.LogDebug($"{damageAgainstObjects}(damageAgainstObjects) * {materialMult}(materailMult for {materialType})" +
                $"* {lockHitMult}(lockHitMult) * {meeleDmgMult}(meeleDmgMult) * {ammoDamageMult}(ammoDamageMult) == {newDamage}");

            return newDamage;
        }

        private static float getBaseDamageAgainstObjects(DamageInfo damageInfo)
        {
            var bulletTemplate = Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId] as AmmoTemplate;
            if (bulletTemplate == null)
            {
                return damageInfo.Damage;
            }

            var id = bulletTemplate._id;
            if (!id.Contains(DoorBreachPlugin.LOCKPICK_AMMO_TAG))
            {
                return damageInfo.Damage;
            }

            // Change base damage value for specific ammo
            if (id.Contains(DoorBreachPlugin.TIER1_LOCKPICK_TAG))
            {
                return DoorBreachPlugin.Tier1LockpickAmmoBaseDamage.Value;
            }
            else if (id.Contains(DoorBreachPlugin.TIER2_LOCKPICK_TAG))
            {
                return DoorBreachPlugin.Tier2LockpickAmmoBaseDamage.Value;
            }
            else if (id.Contains(DoorBreachPlugin.TIER3_LOCKPICK_TAG))
            {
                return DoorBreachPlugin.Tier3LockpickAmmoBaseDamage.Value;
            }
            else
            {
                return DoorBreachPlugin.Tier0LockpickAmmoBaseDamage.Value;
            }
        }

        private static float getMaterialDamageMult(MaterialType material)
        {
            switch (material)
            {
                case MaterialType.WoodThin:
                    return 1 / DoorBreachPlugin.ThinWoodProtectionMult.Value;
                case MaterialType.Plastic:
                    return 1 / DoorBreachPlugin.PlasticProtectionMult.Value;
                case MaterialType.WoodThick:
                    return 1 / DoorBreachPlugin.ThickWoodProtectionMult.Value;
                case MaterialType.MetalThin:
                    return 1 / DoorBreachPlugin.ThinMetalProtectionMult.Value;
                case MaterialType.MetalThick:
                    return 1 / DoorBreachPlugin.ThickMetalProtectionMult.Value;
                default:
                    DoorBreachComponent.Logger.LogWarning($"Unknown material: {material}");
                    return 1;
            }
        }

        private static float getLockHitDamageMult(DamageInfo damageInfo)
        {
            return isLockHit(damageInfo) ? DoorBreachPlugin.LockHitDmgMult.Value : DoorBreachPlugin.NonLockHitDmgMult.Value;
        }

        private static float getMeeleDamageMult(DamageInfo damageInfo)
        {
            return damageInfo.DamageType == EDamageType.Melee ? DoorBreachPlugin.MeeleWeaponDamageMult.Value : 1F;
        }

        private static float getAmmoDamageMult(DamageInfo damageInfo)
        {
            var bulletTemplate = Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId] as AmmoTemplate;
            if (bulletTemplate == null)
            {
                return 1F;
            }

            var id = bulletTemplate._id;
            if (!id.Contains(DoorBreachPlugin.LOCKPICK_AMMO_TAG))
            {
                return 1F;
            }

            if (id.Contains(DoorBreachPlugin.TIER1_LOCKPICK_TAG))
            {
                return DoorBreachPlugin.Tier1LockpickAmmoDamageMult.Value;
            }
            else if (id.Contains(DoorBreachPlugin.TIER2_LOCKPICK_TAG))
            {
                return DoorBreachPlugin.Tier2LockpickAmmoDamageMult.Value;
            }
            else if (id.Contains(DoorBreachPlugin.TIER3_LOCKPICK_TAG))
            {
                return DoorBreachPlugin.Tier3LockpickAmmoDamageMult.Value;
            }
            else
            {
                return DoorBreachPlugin.Tier0LockpickAmmoDamageMult.Value;
            }
        }

        private static bool isLockHit(DamageInfo damageInfo)
        {
            Collider col = damageInfo.HitCollider;

            var door = col.GetComponentInParent<Door>();
            if (door != null && door.GetComponentInChildren<DoorHandle>() != null)
            {
                //DoorBreachComponent.Logger.LogDebug("BB: doorhandle exists so checking if hit");
                Vector3 localHitPoint = col.transform.InverseTransformPoint(damageInfo.HitPoint);
                DoorHandle doorHandle = door.GetComponentInChildren<DoorHandle>();
                Vector3 doorHandleLocalPos = doorHandle.transform.localPosition;
                float distanceToHandle = Vector3.Distance(localHitPoint, doorHandleLocalPos);
                return distanceToHandle < 0.25f;
            }

            var trunk = col.GetComponentInParent<Trunk>();
            if (trunk != null && trunk.GetComponentInChildren<DoorHandle>() != null)
            {
                var gameobj = trunk.gameObject;

                var carLockObj = gameobj.transform.Find("CarLock_Hand").gameObject;
                var lockObj = carLockObj.transform.Find("Lock").gameObject;

                float distanceToLock = Vector3.Distance(damageInfo.HitPoint, lockObj.transform.position);
                return distanceToLock < 0.25f;
            }

            var container = col.GetComponentInParent<LootableContainer>();
            //if doorhandle exists and is hit
            if (container != null && container.GetComponentInChildren<DoorHandle>() != null)
            {
                //DoorBreachComponent.Logger.LogDebug("BB: doorhandle exists so checking if hit");
                var gameobj = container.gameObject;

                //find child game object Lock from gameobj
                var lockObj = gameobj.transform.Find("Lock").gameObject;

                float distanceToLock = Vector3.Distance(damageInfo.HitPoint, lockObj.transform.position);
                //DoorBreachComponent.Logger.LogError("Distance to lock: " + distanceToLock);
                return distanceToLock < 0.25f;
            }

            return false;
        }
    }
}
