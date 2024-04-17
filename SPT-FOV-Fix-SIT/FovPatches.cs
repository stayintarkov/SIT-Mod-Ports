using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.InputSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static EFT.Player;
using FCSubClass = EFT.Player.FirearmController.GClass1609;
using IActiveWeapon = GInterface322;
using ScopeStatesStruct = ScopeStates;
using SightComptInterface = GInterface307;
using WeaponState = WeaponEffectsManager;
using InputClass = GHandsInputTranslator1;


namespace FOVFix
{

    public class KeyInputPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InputClass).GetMethod("TranslateCommand", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(InputClass __instance, ECommand command)
        {
            if (Plugin.AllowToggleZoom.Value && command == ECommand.ChangeScopeMagnification && Plugin.EnableVariableZoom.Value && !Plugin.IsFixedMag && Plugin.IsOptic && (!Plugin.CanToggle || Plugin.CanToggleButNotFixed) && Plugin.IsAiming)
            {
                Plugin.ToggledMagnification = !Plugin.ToggledMagnification;
                float zoom = 
                    Plugin.CurrentZoom >= Plugin.MinZoom && Plugin.CurrentZoom <= Plugin.MaxZoom / 2 ? Plugin.MaxZoom :
                    Plugin.CurrentZoom <= Plugin.MaxZoom && Plugin.CurrentZoom > Plugin.MaxZoom / 2 ? Plugin.MinZoom :
                    Plugin.CurrentZoom;
                Plugin.HandleZoomInput(zoom, true);
            }

            return true;
        }
    }

    public class CalculateScaleValueByFovPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("CalculateScaleValueByFov");
        }

        [PatchPrefix]
        public static bool Prefix(ref float ____ribcageScaleCompensated)
        {
            ____ribcageScaleCompensated = Plugin.FovScale.Value;
            return false;
        }
    }

    public class OperationSetScopeModePatch : ModulePatch
    {
        private static FieldInfo fAnimatorField;
        private static FieldInfo weaponStateField;

        protected override MethodBase GetTargetMethod()
        {
            fAnimatorField = AccessTools.Field(typeof(FCSubClass), "firearmsAnimator_0");
            weaponStateField = AccessTools.Field(typeof(FCSubClass), "gclass1668_0");

            return typeof(FCSubClass).GetMethod("SetScopeMode", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(FCSubClass __instance, ScopeStatesStruct[] scopeStates)
        {
            if (__instance.CanChangeScopeStates(scopeStates))
            {
                if (Plugin.CanToggle || !Plugin.IsOptic)
                {
                    FirearmsAnimator fAnimator = (FirearmsAnimator)fAnimatorField.GetValue(__instance);
                    fAnimator.ModToggleTrigger();
                }
                WeaponState weaponState = (WeaponState)weaponStateField.GetValue(__instance);
                weaponState.UpdateScopesMode();
            }
            return false;
        }
    }

    public class ChangeAimingModePatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("ChangeAimingMode", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPrefix(Player.FirearmController __instance)
        {
            Plugin.ChangeSight = true;
        }
    }

    public class SetScopeModePatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo sighCompField;

        private static bool canToggle = false;
        private static bool isFixedMag = false;
        private static bool isOptic = false; 
/*        private static bool isCurrentlyFucky = false;*/
        private static bool canToggleButNotFixed = false;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            sighCompField = AccessTools.Field(typeof(EFT.InventoryLogic.SightComponent), "_template");

            return typeof(Player.FirearmController).GetMethod("SetScopeMode", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);
            ProceduralWeaponAnimation pwa = player.ProceduralWeaponAnimation;
            Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;

            if (Plugin.IsOptic || currentAimingMod.TemplateId == "5c07dd120db834001c39092d" || currentAimingMod.TemplateId == "5c0a2cec0db834001b7ce47d") 
            {
                Plugin.ChangeSight = true;


                isOptic = pwa.CurrentScope.IsOptic;
                SightComponent sightComp = player.ProceduralWeaponAnimation.CurrentAimingMod;
                SightModClass sightModClass = currentAimingMod as SightModClass;
                SightComptInterface zooms = (SightComptInterface)sighCompField.GetValue(sightModClass.Sight);

                canToggle = currentAimingMod.Template.ToolModdable;
                isFixedMag = currentAimingMod.Template.HasShoulderContact;
                canToggleButNotFixed = canToggle && !isFixedMag;

                float minZoom = 1f;
                float maxZoom = 1f;

                if (isFixedMag)
                {
                    minZoom = zooms.Zooms[0][0];
                    maxZoom = minZoom;
                }
                else if (canToggleButNotFixed && zooms.Zooms[0].Length > 2)
                {
                    minZoom = zooms.Zooms[0][0];
                    maxZoom = zooms.Zooms[0][2];
                }
                else
                {
                    minZoom = zooms.Zooms[0][0];
                    maxZoom = zooms.Zooms[0][1];
                }

                /*                isCurrentlyFucky = (minZoom < 2 && sightComp.SelectedScopeIndex == 0 && sightComp.SelectedScopeMode == 0 && !isFixedMag && !canToggle);
                */

                bool isSamVudu = currentAimingMod.TemplateId == "5b3b99475acfc432ff4dcbee" && Plugin.SamSwatVudu.Value;
                if ((!canToggle && !Plugin.IsFucky && !isSamVudu) || (isSamVudu && !Plugin.DidToggleForFirstPlane))
                {
                    return false;
                }
                return true;
            }
            return true;
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);
            ProceduralWeaponAnimation pwa = player.ProceduralWeaponAnimation;
            Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;
            if (isOptic || currentAimingMod.TemplateId == "5c07dd120db834001c39092d" || currentAimingMod.TemplateId == "5c0a2cec0db834001b7ce47d")
            {
                if (!canToggle)
                {
                    return;
                }

                if (isFixedMag)
                {
                    float currentToggle = player.ProceduralWeaponAnimation.CurrentAimingMod.GetCurrentOpticZoom();
                    Plugin.CurrentZoom = currentToggle;
                    Plugin.ZoomScope(currentToggle);
                }
            }
        }
    }



    public class IsAimingPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo sightComptField;

        private static bool hasSetFov = false;
        private static float adsTimer = 0f;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            sightComptField = AccessTools.Field(typeof(EFT.InventoryLogic.SightComponent), "_template");

            return typeof(Player.FirearmController).GetMethod("get_IsAiming", BindingFlags.Instance | BindingFlags.Public);
        }

        private static Item getContainedItem (Slot x)
        {
            return x.ContainedItem;
        }

        private static string getSightComp(SightComponent x)
        {
            return x.Item.Name;
        }

        private static bool hasScopeAimBone(SightComponent sightComp, Player player)
        {
            List<ProceduralWeaponAnimation.SightNBone> scopeAimTransforms = player.ProceduralWeaponAnimation.ScopeAimTransforms;
            for (int i = 0; i < scopeAimTransforms.Count; i++)
            {
                if (scopeAimTransforms[i].Mod != null && scopeAimTransforms[i].Mod.Equals(sightComp))
                {
                    return true;
                }
            }
            return false;
        }

        private static ScopeStatesStruct[] getScopeModeFullList(Weapon weapon, Player player)
        {
            //you can thank BSG for this monstrosity 
            IEnumerable<SightComponent> sightEnumerable = Enumerable.OrderBy<SightComponent, string>(Enumerable.Select<Slot, Item>(weapon.AllSlots, new Func<Slot, Item>(getContainedItem)).GetComponents<SightComponent>(), new Func<SightComponent, string>(getSightComp));
            List<ScopeStatesStruct> sightStructList = new List<ScopeStatesStruct>();
            int aimIndex = weapon.AimIndex.Value;
            int index = 0;
            foreach (SightComponent sightComponent in sightEnumerable) 
            {
                if (hasScopeAimBone(sightComponent, player)) 
                {
                    for (int i = 0; i < sightComponent.ScopesCount; i++)
                    {
                        int sightMode = (sightComponent.ScopesSelectedModes.Length != sightComponent.ScopesCount) ? 0 : sightComponent.ScopesSelectedModes[i];
                        int scopeCalibrationIndex = (sightComponent.ScopesCurrentCalibPointIndexes.Length != sightComponent.ScopesCount) ? 0 : sightComponent.ScopesCurrentCalibPointIndexes[i];
                        sightStructList.Add(new ScopeStatesStruct
                        {
                            Id = sightComponent.Item.Id,
                            ScopeIndexInsideSight = i,
                            ScopeMode = ((index == aimIndex) ? (sightMode + 1) : sightMode),
                            ScopeCalibrationIndex = scopeCalibrationIndex
                        });
                        index++;
                    }
                }
            }
            return sightStructList.ToArray();
        }

        private static ScopeStatesStruct[] doVuduZoom(Weapon weapon, Player player)
        {
            //you can thank BSG for this monstrosity 
            IEnumerable<SightComponent> sightEnumerable = Enumerable.OrderBy<SightComponent, string>(Enumerable.Select<Slot, Item>(weapon.AllSlots, new Func<Slot, Item>(getContainedItem)).GetComponents<SightComponent>(), new Func<SightComponent, string>(getSightComp));
            List<ScopeStatesStruct> sightStructList = new List<ScopeStatesStruct>();
            int aimIndex = weapon.AimIndex.Value;
            foreach (SightComponent sightComponent in sightEnumerable)
            {
                if (hasScopeAimBone(sightComponent, player))
                {
                    for (int i = 0; i < sightComponent.ScopesCount; i++)
                    {
                        int index = Plugin.CurrentZoom == 1f ? (int)Plugin.CurrentZoom - 1 : Plugin.CurrentZoom == 1.5f ? 1 : (int)Plugin.CurrentZoom;

                        sightStructList.Add(new ScopeStatesStruct
                        {
                            Id = sightComponent.Item.Id,
                            ScopeIndexInsideSight = 0,
                            ScopeMode = index,
                            ScopeCalibrationIndex = index
                        });
                    }
                }
            }
            return sightStructList.ToArray();
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, bool __result)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
            {
                Plugin.IsAiming = __result;

                if (Plugin.EnableVariableZoom.Value && Plugin.IsAiming && (!hasSetFov || Plugin.ChangeSight || (Plugin.DidToggleForFirstPlane && Plugin.SamSwatVudu.Value && Plugin.CurrentScopeTempID == "5b3b99475acfc432ff4dcbee")))
                {
                    Plugin.ChangeSight = false;
                    ProceduralWeaponAnimation pwa = player.ProceduralWeaponAnimation;
                    if (pwa.CurrentScope.IsOptic)
                    {
                        Plugin.IsOptic = true;
                        adsTimer += Time.deltaTime;
                        if (adsTimer >= 0.5f)
                        {
                            hasSetFov = true;
                            Mod currentAimingMod = (pwa.CurrentAimingMod != null) ? pwa.CurrentAimingMod.Item as Mod : null;
                            SightModClass sightModClass = currentAimingMod as SightModClass;
                            SightComponent sightComp = player.ProceduralWeaponAnimation.CurrentAimingMod;
                            SightComptInterface sightCompInter = (SightComptInterface)sightComptField.GetValue(sightModClass.Sight);
                            Plugin.IsFixedMag = currentAimingMod.Template.HasShoulderContact;
                            Plugin.CanToggle = Plugin.AllowReticleToggle.Value ? true :  currentAimingMod.Template.ToolModdable;
                            Plugin.CanToggleButNotFixed = Plugin.CanToggle && !Plugin.IsFixedMag;
                            float minZoom = 1f;
                            float maxZoom = 1f;

                            if (Plugin.IsFixedMag)
                            {
                                minZoom = sightCompInter.Zooms[0][0];
                                maxZoom = minZoom;
                            }
                            else if (currentAimingMod.TemplateId == "5b3b99475acfc432ff4dcbee" && Plugin.SamSwatVudu.Value) 
                            {
                                minZoom = sightCompInter.Zooms[0][0];
                                maxZoom = sightCompInter.Zooms[0][6];
                            }
                            else if (Plugin.CanToggleButNotFixed && sightCompInter.Zooms[0].Length > 2)
                            {
                                minZoom = sightCompInter.Zooms[0][0];
                                maxZoom = sightCompInter.Zooms[0][2];
                            }
                            else if (sightCompInter.Zooms[0][0] > sightCompInter.Zooms[0][1])
                            {
                                maxZoom = sightCompInter.Zooms[0][0];
                                minZoom = sightCompInter.Zooms[0][1];
                            }
                            else
                            {
                                minZoom = sightCompInter.Zooms[0][0];
                                maxZoom = sightCompInter.Zooms[0][1];
                            }

                            Plugin.IsFucky = (minZoom < 2 && sightComp.SelectedScopeIndex == 0 && sightComp.SelectedScopeMode == 0 && !Plugin.IsFixedMag && !Plugin.CanToggle && currentAimingMod.TemplateId != "5b2388675acfc4771e1be0be");
                            bool isSamVudu = currentAimingMod.TemplateId == "5b3b99475acfc432ff4dcbee" && Plugin.SamSwatVudu.Value;
                            if (Plugin.IsFucky && !isSamVudu)
                            {
                                __instance.SetScopeMode(getScopeModeFullList(__instance.Item, player));
                            }

                            if (Plugin.DidToggleForFirstPlane && Plugin.SamSwatVudu.Value && currentAimingMod.TemplateId == "5b3b99475acfc432ff4dcbee")
                            {
                                __instance.SetScopeMode(doVuduZoom(__instance.Item, player));
                                Plugin.DidToggleForFirstPlane = false;
                            }

                            Plugin.MinZoom = minZoom;
                            Plugin.MaxZoom = maxZoom;

                            Plugin.CurrentWeapInstanceID = __instance.Item.Id.ToString();
                            Plugin.CurrentScopeInstanceID = pwa.CurrentAimingMod.Item.Id.ToString();

                            bool weapExists = true;
                            bool scopeExists = false;
                            float rememberedZoom = minZoom;

                            if (!Plugin.WeaponScopeValues.ContainsKey(Plugin.CurrentWeapInstanceID))
                            {
                                weapExists = false;
                                Plugin.WeaponScopeValues[Plugin.CurrentWeapInstanceID] = new List<Dictionary<string, float>>();
                            }

                            List<Dictionary<string, float>> scopes = Plugin.WeaponScopeValues[Plugin.CurrentWeapInstanceID];
                            foreach (Dictionary<string, float> scopeDict in scopes)
                            {
                                if (scopeDict.ContainsKey(Plugin.CurrentScopeInstanceID))
                                {
                                    rememberedZoom = scopeDict[Plugin.CurrentScopeInstanceID];
                                    scopeExists = true;
                                    break;
                                }
                            }

                            if (!scopeExists)
                            {
                                Dictionary<string, float> newScope = new Dictionary<string, float>
                                {
                                  { Plugin.CurrentScopeInstanceID, minZoom }
                                };
                                Plugin.WeaponScopeValues[Plugin.CurrentWeapInstanceID].Add(newScope);
                            }

                            bool isElcan = Plugin.IsFixedMag && Plugin.CanToggle;
                            if (!isElcan && (Plugin.IsFixedMag || !weapExists || !scopeExists))
                            {
                                Plugin.CurrentZoom = minZoom;
                                Plugin.ZoomScope(minZoom);
                            }

                            if (weapExists && scopeExists)
                            {
                                Plugin.CurrentZoom = rememberedZoom;
                                Plugin.ZoomScope(rememberedZoom);
                            }

                            if (isElcan)
                            {
                                float currentToggle = player.ProceduralWeaponAnimation.CurrentAimingMod.GetCurrentOpticZoom();
                                Plugin.CurrentZoom = currentToggle;
                                Plugin.ZoomScope(currentToggle);
                            }
                        }
                    }
                    else
                    {
                        Plugin.CurrentZoom = 1f;
                    }
                }
                else if (!Plugin.IsAiming)
                {
                    adsTimer = 0f;
                    hasSetFov = false;
                }
            }
        }
    }


    public class FreeLookPatch : ModulePatch
    {
        private static FieldInfo resetLookField;
        private static FieldInfo mouseLookControlField;
        private static FieldInfo isResettingLookField;
        private static FieldInfo setResetedLookNextFrameField;
        private static FieldInfo isLookingField;
        private static FieldInfo horizontalField;
        private static FieldInfo verticalField;

        protected override MethodBase GetTargetMethod()
        {
            resetLookField = AccessTools.Field(typeof(Player), "_resetLook");
            mouseLookControlField = AccessTools.Field(typeof(Player), "_mouseLookControl");
            isResettingLookField = AccessTools.Field(typeof(Player), "_isResettingLook");
            setResetedLookNextFrameField = AccessTools.Field(typeof(Player), "_setResetedLookNextFrame");
            isLookingField = AccessTools.Field(typeof(Player), "_isLooking");
            horizontalField = AccessTools.Field(typeof(Player), "_horizontal");
            verticalField = AccessTools.Field(typeof(Player), "_vertical");

            return typeof(Player).GetMethod("Look", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance, float deltaLookY, float deltaLookX, bool withReturn = true)
        {
            Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
            if (fc == null) return true;

            bool _resetLook = (bool)resetLookField.GetValue(__instance);
            bool mouseLookControl = (bool)mouseLookControlField.GetValue(__instance);
            bool isResettingLook = (bool)isResettingLookField.GetValue(__instance);
            bool _setResetedLookNextFrame = (bool)setResetedLookNextFrameField.GetValue(__instance);
            bool isLooking = (bool)isLookingField.GetValue(__instance);

            float _horizontal = (float)horizontalField.GetValue(__instance);
            float _vertical = (float)verticalField.GetValue(__instance);

            bool isAiming = __instance.HandsController != null && __instance.HandsController.IsAiming && !__instance.IsAI;
            EFTHardSettings instance = EFTHardSettings.Instance;
            Vector2 horizontalLimit = new Vector2(-50f, 50f);
            Vector2 mouse_LOOK_VERTICAL_LIMIT = instance.MOUSE_LOOK_VERTICAL_LIMIT;
            if (isAiming)
            {
                horizontalLimit *= instance.MOUSE_LOOK_LIMIT_IN_AIMING_COEF;
            }
            Vector3 eulerAngles = __instance.ProceduralWeaponAnimation.HandsContainer.CameraTransform.eulerAngles;
            if (eulerAngles.x >= 50f && eulerAngles.x <= 90f && __instance.MovementContext.IsSprintEnabled)
            {
                mouse_LOOK_VERTICAL_LIMIT.y = 0f;
            }
            horizontalField.SetValue(__instance, Mathf.Clamp(_horizontal - deltaLookY, horizontalLimit.x, horizontalLimit.y));
            verticalField.SetValue(__instance, Mathf.Clamp(_vertical + deltaLookX, mouse_LOOK_VERTICAL_LIMIT.x, mouse_LOOK_VERTICAL_LIMIT.y));
            float x2 = (_vertical > 0f) ? (_vertical * (1f - _horizontal / horizontalLimit.y * (_horizontal / horizontalLimit.y))) : _vertical;
            if (_setResetedLookNextFrame)
            {
                isResettingLookField.SetValue(__instance, false);
                setResetedLookNextFrameField.SetValue(__instance, false);
            }
            if (_resetLook)
            {
                mouseLookControlField.SetValue(__instance, false);
                resetLookField.SetValue(__instance, false);
                isResettingLookField.SetValue(__instance, true);
                deltaLookY = 0f;
                deltaLookX = 0f;
            }
            if (Math.Abs(deltaLookY) >= 1E-45f && Math.Abs(deltaLookX) >= 1E-45f)
            {
                mouseLookControlField.SetValue(__instance, true);
            }
            if (!mouseLookControl && withReturn)
            {
                if (Mathf.Abs(_horizontal) > 0.01f)
                {
                    horizontalField.SetValue(__instance, Mathf.Lerp(_horizontal, 0f, Time.deltaTime * 15f));
                }
                else
                {
                    horizontalField.SetValue(__instance, 0f);
                }
                if (Mathf.Abs(_vertical) > 0.01f)
                {
                    verticalField.SetValue(__instance, Mathf.Lerp(_vertical, 0f, Time.deltaTime * 15f));
                }
                else
                {
                    verticalField.SetValue(__instance, 0f);
                }
            }
            if (!isResettingLook && _horizontal != 0f && _vertical != 0f)
            {
                isLookingField.SetValue(__instance, true);
            }
            else
            {
                isLookingField.SetValue(__instance, false);
            }
            if (_horizontal == 0f && _vertical == 0f)
            {
                setResetedLookNextFrameField.SetValue(__instance, true);
            }
            __instance.HeadRotation = new Vector3(x2, _horizontal, 0f);
            __instance.ProceduralWeaponAnimation.SetHeadRotation(__instance.HeadRotation);
            return false;
        }
    }


    public class LerpCameraPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("LerpCamera", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Animations.ProceduralWeaponAnimation __instance, float dt, float ____overweightAimingMultiplier, float ____aimingSpeed, float ____aimSwayStrength, Player.ValueBlender ____aimSwayBlender, Vector3 ____aimSwayDirection, Vector3 ____headRotationVec, Vector3 ____vCameraTarget, Player.ValueBlenderDelay ____tacticalReload, Quaternion ____cameraIdenity, Quaternion ____rotationOffset)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null) return true;
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && firearmController.Weapon != null)
            {
                float Single_1 = Singleton<SettingsManager>.Instance.Game.Settings.HeadBobbing;
                float camZ = __instance.IsAiming && !Plugin.IsOptic && firearmController.Weapon.WeapClass == "pistol" ? ____vCameraTarget.z - Plugin.PistolOffset.Value : __instance.IsAiming && !Plugin.IsOptic ? ____vCameraTarget.z - Plugin.NonOpticOffset.Value : __instance.IsAiming && Plugin.IsOptic ? ____vCameraTarget.z - Plugin.OpticPosOffset.Value : ____vCameraTarget.z;
                Vector3 localPosition = __instance.HandsContainer.CameraTransform.localPosition;
                Vector2 a = new Vector2(localPosition.x, localPosition.y);
                Vector2 b = new Vector2(____vCameraTarget.x, ____vCameraTarget.y);
                float aimFactor = __instance.IsAiming ? (____aimingSpeed * __instance.CameraSmoothBlender.Value * ____overweightAimingMultiplier) : Plugin.CameraSmoothOut.Value;
                Vector2 targetPosition = Vector2.Lerp(a, b, dt * aimFactor);
                float zPos = localPosition.z;
                float smoothTime = Plugin.IsOptic ? Plugin.OpticSmoothTime.Value * dt : firearmController.Weapon.WeapClass == "pistol" ? Plugin.PistolSmoothTime.Value * dt : Plugin.CameraSmoothTime.Value * dt;
                float yPos = __instance.IsAiming ? (1f + __instance.HandsContainer.HandsPosition.GetRelative().y * 100f + __instance.TurnAway.Position.y * 10f) : Plugin.CameraSmoothOut.Value;
                zPos = Mathf.Lerp(zPos, camZ, smoothTime * yPos);
                Vector3 newLocalPosition = new Vector3(targetPosition.x, targetPosition.y, zPos) + __instance.HandsContainer.CameraPosition.GetRelative();
                if (____aimSwayStrength > 0f)
                {
                    float blendValue = ____aimSwayBlender.Value;
                    if (__instance.IsAiming && blendValue > 0f)
                    {
                        __instance.HandsContainer.SwaySpring.ApplyVelocity(____aimSwayDirection * blendValue);
                    }
                }
                __instance.HandsContainer.CameraTransform.localPosition = newLocalPosition;
                Quaternion animatedRotation = __instance.HandsContainer.CameraAnimatedFP.localRotation * __instance.HandsContainer.CameraAnimatedTP.localRotation;
                __instance.HandsContainer.CameraTransform.localRotation = Quaternion.Lerp(____cameraIdenity, animatedRotation, Single_1 * (1f - ____tacticalReload.Value)) * Quaternion.Euler(__instance.HandsContainer.CameraRotation.Get() + ____headRotationVec) * ____rotationOffset;
                __instance.method_19(dt);
                __instance.HandsContainer.CameraTransform.localEulerAngles += __instance.Shootingg.CurrentRecoilEffect.GetCameraRotationRecoil();

                return false;
            }
            return true;
        }
    }

    public class PwaWeaponParamsPatch : ModulePatch
    {
        private static FieldInfo isAimingField;
        private static PropertyInfo baseFOVField;
        private static PropertyInfo aimIndexField;
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            isAimingField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_isAiming");
            aimIndexField = AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "AimIndex");
            baseFOVField = AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_2");

            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("method_23", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            float baseFOV = (float)baseFOVField.GetValue(__instance);
            int aimIndex = (int)aimIndexField.GetValue(__instance);
            bool isAiming = (bool)isAimingField.GetValue(__instance);

            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }

            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && firearmController.Weapon != null)
            {
                if (player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    if (__instance.PointOfView == EPointOfView.FirstPerson)
                    {
                        if (!__instance.Sprint && aimIndex < __instance.ScopeAimTransforms.Count)
                        {
                            float zoom = 1;
                            if (player.ProceduralWeaponAnimation.CurrentAimingMod != null)
                            {
                                zoom = player.ProceduralWeaponAnimation.CurrentAimingMod.GetCurrentOpticZoom();
                                Plugin.CurrentScopeTempID = player.ProceduralWeaponAnimation.CurrentAimingMod.Item.TemplateId;
                            }
                            bool isOptic = __instance.CurrentScope.IsOptic;
                            Plugin.IsOptic = isOptic;
                            float zoomMulti = !isOptic ? Plugin.NonOpticFOVMulti.Value : Plugin.EnableVariableZoom.Value ? Utils.GetADSFoVMulti(Plugin.CurrentZoom) : Utils.GetADSFoVMulti(zoom);
                            float sightFOV = baseFOV * zoomMulti * Plugin.GlobalADSMulti.Value;
                            float fov = __instance.IsAiming ? sightFOV : baseFOV;
                            if (Plugin.ShouldDoZoom)
                            {
                                float zoomFactor = isOptic && isAiming ? Plugin.OpticExtraZoom.Value : Plugin.NonOpticExtraZoom.Value;
                                float zoomedFOV = fov * zoomFactor;
                                FPSCamera.Instance.SetFov(zoomedFOV, 1f, true);
                                return;
                            }
                            FPSCamera.Instance.SetFov(fov, 1f, !isAiming);
                        }
                    }
                }
            }
            else if (player.IsYourPlayer)
            {
                if (__instance.PointOfView == EPointOfView.FirstPerson)
                {

                    if (!__instance.Sprint && aimIndex < __instance.ScopeAimTransforms.Count)
                    {
                        float sightFOV = baseFOV * Plugin.RangeFinderADSMulti.Value * Plugin.GlobalADSMulti.Value;
                        float fov = __instance.IsAiming ? sightFOV : baseFOV;

                        FPSCamera.Instance.SetFov(fov, 1f, !isAiming);
                    }
                }
            }
        }
    }
    public class OpticSightAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.CameraControl.OpticSight).GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.CameraControl.OpticSight __instance)
        {

            __instance.TemplateCamera.gameObject.SetActive(false);
            if (__instance.name != "DONE")
            {
                if (Plugin.TrueOneX.Value == true && __instance.TemplateCamera.fieldOfView >= 24)
                {
                    return false;
                }
                __instance.TemplateCamera.fieldOfView *= Plugin.GlobalOpticFOVMulti.Value;
                __instance.name = "DONE";
            }
            return false;
        }
    }

    public class OnWeaponParametersChangedPatch : ModulePatch
    {
        private static FieldInfo weaponField;

        protected override MethodBase GetTargetMethod()
        {
            weaponField = AccessTools.Field(typeof(ShotEffector), "_weapon");
            return typeof(ShotEffector).GetMethod("OnWeaponParametersChanged", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ShotEffector __instance)
        {
            IWeapon _weapon = (IWeapon)weaponField.GetValue(__instance);
            if (_weapon.Item.Owner.ID.StartsWith("pmc") || _weapon.Item.Owner.ID.StartsWith("scav"))
            {
                Plugin.HasRAPTAR = false;

                if (!_weapon.IsUnderbarrelWeapon)
                {
                    Weapon weap = _weapon.Item as Weapon;
                    IEnumerable<Mod> weapMods = weap.Mods;
                    foreach (Mod mod in weapMods)
                    {
                        if (mod.TemplateId == "61605d88ffa6e502ac5e7eeb")
                        {
                            Plugin.HasRAPTAR = true;
                        }
                    }
                }

            }
        }
    }


    public class TacticalRangeFinderControllerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TacticalRangeFinderController).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {

            if (Plugin.HasRAPTAR == false)
            {
                FPSCamera.Instance.OpticCameraManager.Camera.fieldOfView = Plugin.RangeFinderFOV.Value;
            }

        }
    }


    //better to do it in method_17Patch, as this method also sets FOV in general.
    /*    public class SetFovPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FPSCamera).GetMethod("SetFov", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPrefix]
            private static bool Prefix(FPSCamera __instance, ref float x, float time, Coroutine ___coroutine_0, bool applyFovOnCamera = true)
            {

                var _method_4 = AccessTools.Method(typeof(FPSCamera), "method_4");
                float fov = x * Plugin.globalADSMulti.Value;

                if (___coroutine_0 != null)
                {
                    StaticManager.KillCoroutine(___coroutine_0);
                }
                if (__instance.Camera == null)
                {
                    return false;
                }
                IEnumerator meth4Enumer = (IEnumerator)_method_4.Invoke(__instance, new object[] { fov, time });
                AccessTools.Property(typeof(FPSCamera), "ApplyDovFovOnCamera").SetValue(__instance, applyFovOnCamera);
                ___coroutine_0 = StaticManager.BeginCoroutine(meth4Enumer);
                return false;
            }
        }*/

}
