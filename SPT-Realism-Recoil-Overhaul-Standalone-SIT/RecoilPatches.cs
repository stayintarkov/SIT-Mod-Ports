using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using EFT.Animations;
using PlayerInterface = IFirearms;
using AimingSettings = Config4.AimingConfiguration;
using StayInTarkov;
using StayInTarkov.Coop;
using System.Threading.Tasks;

namespace RecoilStandalone
{
    public class RecoilCoopPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(StayInTarkovPlugin).Assembly.GetType("StayInTarkov.Coop.CoopGame").GetMethod("vmethod_2", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void WaitForCoopGame(Task<LocalPlayer> task)
        {
            task.Wait();

            LocalPlayer localPlayer = task.Result;

            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                Utils.ClientPlayer = localPlayer.GetPlayer;
                if (Utils.ClientPlayer != null)
                {
                    Logger.LogMessage("RecoilOverhaul: Found CoopPlayer");
                    Utils.WeaponReady = true;
                }
            }
        }

        [PatchPostfix]
        private static void PatchPostFix(Task<LocalPlayer> __result)
        {
            Task.Run(() => WaitForCoopGame(__result));
        }
    }

    public class RecoilLocalPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalPlayer).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Task<LocalPlayer> __result)
        {
            LocalPlayer localPlayer = __result.Result;
            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                Utils.ClientPlayer = localPlayer.GetPlayer;
                if (Utils.ClientPlayer != null)
                {
                    Logger.LogMessage("RecoilOverhaul: Found LocalPlayer");
                    Utils.WeaponReady = true;
                }            
            }
        }
    }

    public class RecoilOverhaulHideoutInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HideoutPlayerOwner).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(HideoutPlayerOwner __instance)
        {
            if (__instance != null)
            {
                Utils.ClientPlayer = __instance.HideoutPlayer.GetPlayer;
                {
                    if (Utils.ClientPlayer != null)
                    {
                        Logger.LogMessage("RecoilOverhaul: Found HideoutPlayer");
                    }
                }
            }
        }
    }

    public class ApplyComplexRotationPatch : ModulePatch
    {
        private static FieldInfo weapRotationField;
        private static FieldInfo currentRotationField;
        private static FieldInfo blindfireStrength;
        private static FieldInfo blindfireRotationField;
        private static PropertyInfo overlappingBlindfireField;

        protected override MethodBase GetTargetMethod()
        {
            blindfireStrength = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_blindfireStrength");
            blindfireRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_blindFireRotation");
            weapRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_temporaryRotation");
            currentRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_scopeRotation");
            overlappingBlindfireField = AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3");

            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ApplyComplexRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        private static Vector3 currentRecoil = Vector3.zero;
        private static Vector3 targetRecoil = Vector3.zero;

        [PatchPostfix]
        private static void Postfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {
            if (!Plugin.CombatStancesIsPresent)
            {
                PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_firearmAnimationData").GetValue(__instance);
                if (playerInterface != null && playerInterface.Weapon != null)
                {
                    Weapon weapon = playerInterface.Weapon;
                    Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                    if (player != null && player.IsYourPlayer)
                    {
                        float pitch = (float)blindfireStrength.GetValue(__instance);
                        float overlappingBlindfire = (float)overlappingBlindfireField.GetValue(__instance);
                        Vector3 blindFireRotation = (Vector3)blindfireRotationField.GetValue(__instance);
                        Quaternion currentRotation = (Quaternion)currentRotationField.GetValue(__instance);
                        Vector3 weaponWorldPos = __instance.HandsContainer.WeaponRootAnim.position;

                        Quaternion weapRotation = (Quaternion)weapRotationField.GetValue(__instance);
                        Quaternion rhs = Quaternion.Euler(pitch * overlappingBlindfire * blindFireRotation);

                        RecoilController.DoCantedRecoil(ref targetRecoil, ref currentRecoil, ref weapRotation);
                        __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weaponWorldPos, weapRotation * rhs * currentRotation);
                    }
                }
            }
        }
    }

    public class RotatePatch : ModulePatch
    {
        private static FieldInfo movementContextField;
        private static FieldInfo playerField;

        private static Vector2 recordedRotation = Vector3.zero;
        private static Vector2 targetRotation = Vector3.zero;
        private static bool hasReset = false;
        private static float timer = 0.0f;
        private static float resetTime = 0.5f;

        protected override MethodBase GetTargetMethod()
        {
            movementContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
            playerField = AccessTools.Field(typeof(MovementContext), "_player");

            return typeof(MovementState).GetMethod("Rotate", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void resetTimer(Vector2 target, Vector2 current)
        {
            timer += Time.deltaTime;

            bool doHybridReset = (Plugin.EnableHybridRecoil.Value && !Plugin.HasStock) || (Plugin.EnableHybridRecoil.Value && Plugin.HybridForAll.Value);
            if ((doHybridReset && timer >= resetTime && target == current) || (!doHybridReset && (timer >= resetTime || target == current)))
            {
                hasReset = true;
            }
        }

        [PatchPrefix]
        private static void Prefix(MovementState __instance, ref Vector2 deltaRotation, bool ignoreClamp)
        {
            MovementContext movementContext = (MovementContext)movementContextField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(movementContext);

            if (player.IsYourPlayer)
            {
                float fpsFactor = 144f / (1f / Time.unscaledDeltaTime);

                //restet is enabled && if hybrid for all is NOT enabled || if hybrid is eanbled + for all is false + is pistol or folded stock/stockless
                bool hybridBlocksReset = Plugin.EnableHybridRecoil.Value && !Plugin.HasStock && !Plugin.EnableHybridReset.Value;
                bool canResetVert = Plugin.ResetVertical.Value && !hybridBlocksReset;
                bool canResetHorz = Plugin.ResetHorizontal.Value && !hybridBlocksReset;

                if (Plugin.ShotCount > Plugin.PrevShotCount)
                {
                    float controlFactor = Plugin.ShotCount <= 2f ? Plugin.PlayerControlMulti.Value * 3 : Plugin.PlayerControlMulti.Value;
                    Plugin.PlayerControl += Mathf.Abs(deltaRotation.y) * controlFactor;

                    hasReset = false;
                    timer = 0f;

                    EFT.Player.FirearmController fc = player.HandsController as EFT.Player.FirearmController;
                    float shotCountFactor = Mathf.Min(Plugin.ShotCount * 0.4f, 1.75f);
                    float angle = ((90f - Plugin.RecoilAngle) / 50f);
                    float dispersion = Mathf.Max(Plugin.TotalDispersion * 2.75f * Plugin.RecoilDispersionFactor.Value * shotCountFactor * fpsFactor, 0f);
                    float dispSpeedFactor = 2f - Plugin.RecoilDetla;
                    float dispersionSpeed = Math.Max(Time.time * Plugin.RecoilDispersionSpeed.Value * dispSpeedFactor, 0.1f);

                    float xRotation = 0f;
                    float yRotation = 0f;

                    //S pattern
                    if (!Plugin.IsVector)
                    {
                        xRotation = Mathf.Lerp(-dispersion, dispersion, Mathf.PingPong(dispersionSpeed, 1f)) + angle;
                        yRotation = Mathf.Min(-Plugin.TotalVRecoil * Plugin.RecoilClimbFactor.Value * shotCountFactor * fpsFactor, 0f);
                    }
                    else 
                    {
                        float recoilAmount = Plugin.TotalVRecoil * Plugin.RecoilClimbFactor.Value * shotCountFactor * fpsFactor;
                        dispersion = Mathf.Max(Plugin.TotalDispersion * Plugin.RecoilDispersionFactor.Value * shotCountFactor * fpsFactor, 0f);
                        xRotation = (float)Math.Round(Mathf.Lerp(-dispersion, dispersion, Mathf.PingPong(Time.time * 8f, 1f)), 3);
                        yRotation = (float)Math.Round(Mathf.Lerp(-recoilAmount, recoilAmount, Mathf.PingPong(Time.time * 4f, 1f)), 3);
                    }

                    targetRotation = movementContext.Rotation + new Vector2(xRotation, yRotation);

                    if ((canResetVert && (movementContext.Rotation.y > recordedRotation.y + 2f || deltaRotation.y <= -1f)) || (canResetHorz && Mathf.Abs(deltaRotation.x) >= 1f))
                    {
                        recordedRotation = movementContext.Rotation;
                    }

                }
                else if (!hasReset && !Plugin.IsFiring)
                {
                    float resetSpeed = Plugin.TotalConvergence * Plugin.ResetSpeed.Value;

                    bool xIsBelowThreshold = Mathf.Abs(deltaRotation.x) <= Plugin.ResetSensitivity.Value;
                    bool yIsBelowThreshold = Mathf.Abs(deltaRotation.y) <= Plugin.ResetSensitivity.Value;

                    Vector2 resetTarget = movementContext.Rotation;

                    if (canResetVert && canResetHorz && xIsBelowThreshold && yIsBelowThreshold)
                    {
                        resetTarget = new Vector2(recordedRotation.x, recordedRotation.y);
                        movementContext.Rotation = Vector2.Lerp(movementContext.Rotation, new Vector2(recordedRotation.x, recordedRotation.y), resetSpeed);
                    }
                    else if (canResetHorz && xIsBelowThreshold)
                    {
                        resetTarget = new Vector2(recordedRotation.x, movementContext.Rotation.y);
                        movementContext.Rotation = Vector2.Lerp(movementContext.Rotation, new Vector2(recordedRotation.x, movementContext.Rotation.y), resetSpeed);
                    }
                    else if (canResetVert && yIsBelowThreshold)
                    {
                        resetTarget = new Vector2(movementContext.Rotation.x, recordedRotation.y);
                        movementContext.Rotation = Vector2.Lerp(movementContext.Rotation, new Vector2(movementContext.Rotation.x, recordedRotation.y), resetSpeed);
                    }
                    else
                    {
                        resetTarget = movementContext.Rotation;
                        recordedRotation = movementContext.Rotation;
                    }

                    resetTimer(resetTarget, movementContext.Rotation);
                }
                else if (!Plugin.IsFiring)
                {
                    if (Mathf.Abs(deltaRotation.y) > 0.1f)
                    {
                        Plugin.PlayerControl += Mathf.Abs(deltaRotation.y) * Plugin.PlayerControlMulti.Value;
                    }
                    else 
                    {
                        Plugin.PlayerControl = 0f;
                    }

                    recordedRotation = movementContext.Rotation;
                }
                if (Plugin.IsFiring)
                {
                    if (targetRotation.y <= recordedRotation.y - Plugin.RecoilClimbLimit.Value)
                    {
                        targetRotation.y = movementContext.Rotation.y;
                    }

                    movementContext.Rotation = Vector2.Lerp(movementContext.Rotation, targetRotation, Plugin.RecoilSmoothness.Value);
                }

                if (Plugin.ShotCount == Plugin.PrevShotCount)
                {
                    Plugin.PlayerControl = Mathf.Lerp(Plugin.PlayerControl, 0f, 0.05f);
                }
            }
        }
    }

    public class PlayerLateUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
        }

    
        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (Utils.CheckIsReady() && __instance.IsYourPlayer)
            {
                float mountingSwayBonus = Plugin.IsMounting ? Plugin.MountingSwayBonus : Plugin.BracingSwayBonus;
                float mountingRecoilBonus = Plugin.IsMounting ? Plugin.MountingRecoilBonus : Plugin.BracingRecoilBonus;
                bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

                __instance.ProceduralWeaponAnimation.CrankRecoil = Plugin.EnableCrank.Value;
                __instance.ProceduralWeaponAnimation.Shootingg.Intensity = Plugin.RecoilIntensity.Value * mountingRecoilBonus;

                float swayIntensity = 1f;
                if (Plugin.IsFiring && isMoving && Plugin.IsAiming)
                {
                    swayIntensity = Plugin.SwayIntensity.Value * mountingSwayBonus * 0.01f;
                }
                else
                {
                    swayIntensity = Plugin.SwayIntensity.Value * mountingSwayBonus;
                }
                __instance.ProceduralWeaponAnimation.Breath.Intensity = swayIntensity * Plugin.BreathIntensity;
                __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = swayIntensity * swayIntensity;

                if (Plugin.IsFiring)
                {
                    RecoilController.SetRecoilParams(__instance.ProceduralWeaponAnimation, __instance.HandsController.Item as Weapon, isMoving);
                }
                else if (!Plugin.CombatStancesIsPresent) 
                {
                    __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = 0.45f;
                }

                if (Utils.WeaponReady && __instance?.ProceduralWeaponAnimation != null && __instance.ProceduralWeaponAnimation?.CurrentScope != null)
                {
                    Plugin.HasOptic = __instance.ProceduralWeaponAnimation.CurrentScope.IsOptic ? true : false;
                }
                else 
                {
                    Plugin.HasOptic = false;
                }
            }
        }
    }

    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_firearmAnimationData").GetValue(__instance);
            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    bool hasStockMod = false;
                    foreach (Mod mod in weapon.Mods) 
                    {
                        if (mod is GClass2540) 
                        {
                            hasStockMod = true;
                        }
                    }

                    Plugin.HasStock = !__instance._shouldMoveWeaponCloser && hasStockMod;
                    Plugin.HandsIntensity = __instance.HandsContainer.HandsRotation.InputIntensity;
                    Plugin.BreathIntensity = __instance.Breath.Intensity;
                    float baseConvergence = weapon.Template.Convergence;
                    float classMulti = RecoilController.GetConvergenceMulti(weapon);

                    float convBaseValue = baseConvergence * classMulti; 
                    float convergenceMulti = Plugin.EnableHybridRecoil.Value && !Plugin.HasStock  ? Plugin.ConvergenceMulti.Value / 1.85f : Plugin.EnableHybridRecoil.Value && Plugin.HybridForAll.Value ? Plugin.ConvergenceMulti.Value / 1.4f : Plugin.ConvergenceMulti.Value;
                    Plugin.TotalConvergence = Mathf.Min((float)Math.Round(convBaseValue * convergenceMulti, 2), 30f);
                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.TotalConvergence;
                    Plugin.RecoilAngle = RecoilController.GetRecoilAngle(weapon);
                    Plugin.IsVector = weapon.TemplateId == "5fb64bc92b1b027b1f50bcf2" || weapon.TemplateId == "5fc3f2d5900b1d5091531e57";
                }
            }
        }
    }

    public class UpdateSwayFactorsPatch : ModulePatch
    {
        private static FieldInfo playerInterfaceField;

        protected override MethodBase GetTargetMethod()
        {
            playerInterfaceField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_firearmAnimationData");

            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateSwayFactors", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            PlayerInterface playerInterface = (PlayerInterface)playerInterfaceField.GetValue(__instance);
            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Utils.ClientPlayer;
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    float swayStrn = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_swayStrength").GetValue(__instance);
                    __instance.MotionReact.SwayFactors = new Vector3(swayStrn, __instance.IsAiming ? (swayStrn * 0.3f) : swayStrn, swayStrn) * Plugin.SwayIntensity.Value;
                } 
            }
        }
    }

    public class GetAimingPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player");

            return typeof(EFT.Player.FirearmController).GetMethod("get_IsAiming", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance, ref bool ____isAiming)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                Plugin.IsAiming = ____isAiming;
            }
        }
    }

    public class ProcessPatch : ModulePatch
    {
        private static FieldInfo iWeaponField;
        private static FieldInfo weaponClassField;
        private static FieldInfo intensityFactorsField;
        private static FieldInfo buffInfoField;

        protected override MethodBase GetTargetMethod()
        {
            iWeaponField = AccessTools.Field(typeof(ShotEffector), "_weapon");
            weaponClassField = AccessTools.Field(typeof(ShotEffector), "_mainWeaponInHands");
            intensityFactorsField = AccessTools.Field(typeof(ShotEffector), "_separateIntensityFactors");
            buffInfoField = AccessTools.Field(typeof(ShotEffector), "_buffs");

            return typeof(ShotEffector).GetMethod("Process");
        }

        [PatchPrefix]
        public static bool Prefix(ref ShotEffector __instance, float str = 1f)
        {
            IWeapon iWeapon = (IWeapon)iWeaponField.GetValue(__instance);
            if (iWeapon.Item.Owner.ID == Utils.ClientPlayer.ProfileId)
            {
                Weapon weaponClass = (Weapon)weaponClassField.GetValue(__instance);
                Vector3 separateIntensityFactors = (Vector3)intensityFactorsField.GetValue(__instance);
                SkillManager.BuffInfo buffInfo = (SkillManager.BuffInfo)AccessTools.Field(typeof(ShotEffector), "_buffs").GetValue(__instance);

                float classVMulti = RecoilController.GetVRecoilMulti(weaponClass);
                float classCamMulti = RecoilController.GetCamRecoilMulti(weaponClass);
                float mountingRecoilBonus = Plugin.IsMounting ? Plugin.MountingRecoilBonus : Plugin.BracingRecoilBonus;

                float cameraRecoil = weaponClass.Template.CameraRecoil * Plugin.CamMulti.Value * str * classCamMulti;
                Plugin.TotalCameraRecoil = cameraRecoil * mountingRecoilBonus;


            
                float angle = Mathf.LerpAngle(weaponClass.Template.RecoilAngle, 90f, buffInfo.RecoilSupression.y);
                float factoredDispersion = (weaponClass.Template.RecolDispersion / (1f + buffInfo.RecoilSupression.y)) * Plugin.DispMulti.Value * mountingRecoilBonus * (1f + weaponClass.RecoilDelta);
                Plugin.TotalDispersion = factoredDispersion;
                __instance.RecoilDegree = new Vector2(angle - factoredDispersion, angle + factoredDispersion);
                __instance.RecoilRadian = __instance.RecoilDegree * 0.017453292f;

                __instance.ShotVals[3].Intensity = cameraRecoil;
                __instance.ShotVals[4].Intensity = -cameraRecoil;

                float fovFactor = (Singleton<SettingsManager>.Instance.Game.Settings.FieldOfView / 70f);
                float opticLimit = Plugin.IsAiming && Plugin.HasOptic ? 10f * fovFactor : 15f * fovFactor;
                float hRecoil = Mathf.Min(25f, Random.Range(__instance.RecoilStrengthZ.x, __instance.RecoilStrengthZ.y) * str * Plugin.HorzMulti.Value) * mountingRecoilBonus;
                Plugin.TotalHRecoil = hRecoil;
                hRecoil = Mathf.Min(hRecoil * fovFactor, opticLimit);

                float recoilRadian = Random.Range(__instance.RecoilRadian.x, __instance.RecoilRadian.y);
                float vertRecoil = Random.Range(__instance.RecoilStrengthXy.x, __instance.RecoilStrengthXy.y) * str * Plugin.VertMulti.Value * classVMulti * mountingRecoilBonus;
                __instance.RecoilDirection = new Vector3(-Mathf.Sin(recoilRadian) * vertRecoil * separateIntensityFactors.x, Mathf.Cos(recoilRadian) * vertRecoil * separateIntensityFactors.y, hRecoil * separateIntensityFactors.z) * __instance.Intensity;
                
                Plugin.TotalVRecoil = vertRecoil;
    
                Vector2 heatDirection = (iWeapon != null) ? iWeapon.MalfState.OverheatBarrelMoveDir : Vector2.zero;
                float heatFactor = (iWeapon != null) ? iWeapon.MalfState.OverheatBarrelMoveMult : 0f;
                float totalRecoilFactor = (__instance.RecoilRadian.x + __instance.RecoilRadian.y) / 2f * ((__instance.RecoilStrengthXy.x + __instance.RecoilStrengthXy.y) / 2f) * heatFactor;
                __instance.RecoilDirection.x = __instance.RecoilDirection.x + heatDirection.x * totalRecoilFactor;
                __instance.RecoilDirection.y = __instance.RecoilDirection.y + heatDirection.y * totalRecoilFactor;
                ShotEffector.ShotVal[] shotVals = __instance.ShotVals;
                
                for (int i = 0; i < shotVals.Length; i++)
                {
                    shotVals[i].Process(__instance.RecoilDirection);
                }

                Plugin.WiggleTimer = 0f;
                Plugin.FiringTimer = 0f;
                Plugin.IsFiring = true;
                Plugin.IsFiringWiggle = true;
                Plugin.ShotCount++;

                return false;
            }
            return true;
        }
    }
    public class ShootPatch : ModulePatch
    {
        private static FieldInfo playerInterfaceField;

        protected override MethodBase GetTargetMethod()
        {
            playerInterfaceField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_firearmAnimationData");

            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("Shoot");
        }

        [PatchPostfix]
        public static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            PlayerInterface playerInterface = (PlayerInterface)playerInterfaceField.GetValue(__instance);

            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
                {
                    bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
                    RecoilController.SetRecoilParams(__instance, weapon, isMoving);
                }
            }
        }
    }

    public class SetCurveParametersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.RecoilSpring).GetMethod("SetCurveParameters");
        }
        [PatchPostfix]
        public static void PatchPostfix(EFT.Animations.RecoilSpring __instance)
        {
            float[] _originalKeyValues = (float[])AccessTools.Field(typeof(EFT.Animations.RecoilSpring), "_originalKeyValues").GetValue(__instance);

            float value = __instance.ReturnSpeedCurve[0].value;
            for (int i = 1; i < _originalKeyValues.Length; i++)
            {
                Keyframe key = __instance.ReturnSpeedCurve[i];
                key.value = value + _originalKeyValues[i] * Plugin.ConvergenceSpeedCurve.Value;
                __instance.ReturnSpeedCurve.RemoveKey(i);
                __instance.ReturnSpeedCurve.AddKey(key);
            }
        }
    }

    public class BreathProcessPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BreathEffector).GetMethod("Process", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(BreathEffector __instance, float deltaTime, float ____breathIntensity, float ____shakeIntensity, float ____breathFrequency,
        float ____cameraSensetivity, Vector2 ____baseHipRandomAmplitudes, Spring ____recoilRotationSpring, Spring ____handsRotationSpring, AnimationCurve ____lackOfOxygenStrength, GClass2089[] ____processors)
        {
            float amplGain = Mathf.Sqrt(__instance.AmplitudeGain.Value);
            __instance.HipXRandom.Amplitude = Mathf.Clamp(____baseHipRandomAmplitudes.x + amplGain, 0f, 3f);
            __instance.HipZRandom.Amplitude = Mathf.Clamp(____baseHipRandomAmplitudes.y + amplGain, 0f, 3f);
            __instance.HipXRandom.Hardness = (__instance.HipZRandom.Hardness = __instance.Hardness.Value);
            ____shakeIntensity = 1f;
            bool isInjured = __instance.TremorOn || __instance.Fracture;
            float intensityHolder = 1f;

            if (__instance.Physical.HoldingBreath)
            {
                ____breathIntensity = 0.15f;
                ____shakeIntensity = 0.15f;
            }
            else if (Time.time < __instance.StiffUntill)
            {
                float intensity = Mathf.Clamp(-__instance.StiffUntill + Time.time + 1f, isInjured ? 0.5f : 0.3f, 1f);
                ____breathIntensity = intensity * __instance.Intensity;
                ____shakeIntensity = intensity;
                intensityHolder = intensity;
            }
            else
            {
                float t = ____lackOfOxygenStrength.Evaluate(__instance.OxygenLevel);
                float b = __instance.IsAiming ? 0.75f : 1f;
                ____breathIntensity = Mathf.Clamp(Mathf.Lerp(4f, b, t), 1f, 1.5f) * __instance.Intensity;
                ____breathFrequency = Mathf.Clamp(Mathf.Lerp(4f, 1f, t), 1f, 2.5f) * deltaTime;
                ____cameraSensetivity = Mathf.Lerp(2f, 0f, t) * __instance.Intensity;
            }
            GClass721<float> staminaLevel = __instance.StaminaLevel;
            __instance.YRandom.Amplitude = __instance.BreathParams.AmplitudeCurve.Evaluate(staminaLevel);
            float stamFactor = __instance.BreathParams.Delay.Evaluate(staminaLevel);
            __instance.XRandom.MinMaxDelay = (__instance.YRandom.MinMaxDelay = new Vector2(stamFactor / 2f, stamFactor));
            __instance.YRandom.Hardness = __instance.BreathParams.Hardness.Evaluate(staminaLevel);
            float randomY = __instance.YRandom.GetValue(deltaTime);
            float randomX = __instance.XRandom.GetValue(deltaTime);
            ____handsRotationSpring.AddAcceleration(new Vector3(Mathf.Max(0f, -randomY) * (1f - staminaLevel) * 2f, randomY, randomX) * (____shakeIntensity * __instance.Intensity));
            Vector3 breathVector = Vector3.zero;
            if (isInjured)
            {
                float tremorSpeed = __instance.TremorOn ? deltaTime : (deltaTime / 2f);
                tremorSpeed *= intensityHolder;
                float tremorXRandom = __instance.TremorXRandom.GetValue(tremorSpeed);
                float tremorYRandom = __instance.TremorYRandom.GetValue(tremorSpeed);
                float tremorZRnadom = __instance.TremorZRandom.GetValue(tremorSpeed);
                if (__instance.Fracture && !__instance.IsAiming)
                {
                    tremorXRandom += Mathf.Max(0f, randomY) * Mathf.Lerp(1f, 100f / __instance.EnergyFractureLimit, staminaLevel);
                }
                breathVector = new Vector3(tremorXRandom, tremorYRandom, tremorZRnadom) * __instance.Intensity;
            }
            else if (!__instance.IsAiming)
            {
                breathVector = new Vector3(__instance.HipXRandom.GetValue(deltaTime), 0f, __instance.HipZRandom.GetValue(deltaTime)) * (__instance.Intensity * __instance.HipPenalty);
            }

            if (Vector3.SqrMagnitude(breathVector - ____recoilRotationSpring.Zero) > 0.01f)
            {
                ____recoilRotationSpring.Zero = Vector3.Lerp(____recoilRotationSpring.Zero, breathVector, 0.1f);
            }
            else
            {
                ____recoilRotationSpring.Zero = breathVector;
            }
            ____processors[0].ProcessRaw(____breathFrequency, Plugin.BreathIntensity * 0.15f);
            ____processors[1].ProcessRaw(____breathFrequency, Plugin.BreathIntensity * 0.15f * ____cameraSensetivity);
            return false;
        }
    }
}

