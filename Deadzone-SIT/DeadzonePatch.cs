using HarmonyLib;
using UnityEngine;
using EFT;
using EFT.Animations;

namespace DeadzoneMod;

public class Deadzone
{
    static Quaternion MakeQuaternionDelta(Quaternion from, Quaternion to)
        => to * Quaternion.Inverse(from);

    static void SetRotationWrapped(ref float yaw, ref float pitch)
    {
        // I prefer using (-180; 180) euler angle range over (0; 360)
        // However, wrapping the angles is easier with (0; 360), so temporarily cast it
        if (yaw < 0) yaw += 360;
        if (pitch < 0) pitch += 360;

        pitch %= 360;
        yaw %= 360;

        // Now cast it back
        if (yaw > 180) yaw -= 360;
        if (pitch > 180) pitch -= 360;
    }

    static void SetRotationClamped(ref float yaw, ref float pitch, float maxAngle)
    {
        Vector2 clampedVector
            = Vector2.ClampMagnitude(
                new Vector2(yaw, pitch),
                maxAngle
            );

        yaw = clampedVector.x;
        pitch = clampedVector.y;
    }

    static readonly System.Diagnostics.Stopwatch aimWatch = new();
    static float GetDeltaTime()
    {
        float deltaTime = aimWatch.Elapsed.Milliseconds / 1000f;
        aimWatch.Reset();
        aimWatch.Start();

        return deltaTime;
    }

    static float aimSmoothed = 0f;

    static void UpdateAimSmoothed(ProceduralWeaponAnimation animationInstance)
    {
        float deltaTime = GetDeltaTime();

        // TODO: use aiming time
        // Maybe it can be extracted from ProceduralWeaponAnimation?
        aimSmoothed = Mathf.Lerp(aimSmoothed, animationInstance.IsAiming ? 1f : 0f, deltaTime * 6f);
    }

    public static float cumulativePitch = 0f;
    public static float cumulativeYaw = 0f;

    static Vector2 lastYawPitch;

    static void UpdateDeadzoneRotation(Vector2 currentYawPitch, WeaponSettings settings)
    {
        Quaternion lastRotation = Quaternion.Euler(lastYawPitch.x, lastYawPitch.y, 0);
        Quaternion currentRotation = Quaternion.Euler(currentYawPitch.x, currentYawPitch.y, 0);

        lastYawPitch = currentYawPitch;

        // all euler angles should go to hell
        lastRotation = Quaternion.SlerpUnclamped(currentRotation, lastRotation, settings.Sensitivity.Value);

        Vector3 delta = MakeQuaternionDelta(lastRotation, currentRotation).eulerAngles;

        cumulativeYaw += delta.x;
        cumulativePitch += delta.y;

        SetRotationWrapped(ref cumulativeYaw, ref cumulativePitch);

        SetRotationClamped(ref cumulativeYaw, ref cumulativePitch, settings.MaxAngle.Value);
    }

    static void ApplyDeadzone(ProceduralWeaponAnimation animationInstance, WeaponSettings settings)
    {
        float aimMultiplier = 1f - ((1f - settings.AimMultiplier.Value) * aimSmoothed);

        Transform weaponRootAnim = animationInstance.HandsContainer.WeaponRootAnim;

        if (weaponRootAnim == null) return;

        weaponRootAnim.LocalRotateAround(
            Vector3.up * settings.Position.Value,
            new Vector3(
                cumulativePitch * aimMultiplier,
                0,
                cumulativeYaw * aimMultiplier
            )
        );

        // Not doing this messes up pivot for all offsets after this
        weaponRootAnim.LocalRotateAround(
            Vector3.up * -settings.Position.Value,
            Vector3.zero
        );
    }

    public static void PatchedUpdate(Player player, ProceduralWeaponAnimation weaponAnimation)
    {
        Vector2 currentYawPitch = new(player.MovementContext.Yaw, player.MovementContext.Pitch);

        WeaponSettings settings;

        settings = Plugin.Settings.WeaponSettings.fallback;

        UpdateDeadzoneRotation(currentYawPitch, settings);

        UpdateAimSmoothed(weaponAnimation);

        ApplyDeadzone(weaponAnimation, settings);
    }
}

#pragma warning disable IDE0051
public class DeadzonePatch
{

    static public void Enable()
        => Harmony.CreateAndPatchAll(typeof(DeadzonePatch));

    [HarmonyPatch(typeof(Player), "VisualPass")]
    [HarmonyPrefix]
    static void FindLocalProceduralWeaponAnimation(Player __instance)
    {
        if (!Plugin.Enabled) return;

        if (!__instance.IsYourPlayer) return;

        localPlayer = __instance;
        localWeaponAnimation = __instance.ProceduralWeaponAnimation;
    }

    public static Player localPlayer;
    public static ProceduralWeaponAnimation localWeaponAnimation;

    [HarmonyPatch(typeof(ProceduralWeaponAnimation), "AvoidObstacles")]
    [HarmonyPostfix]
    static void WeaponAnimationPatch(ProceduralWeaponAnimation __instance)
    {
        if (!Plugin.Enabled) return;

        if (localPlayer == null) return;
        if (__instance != localWeaponAnimation) return;

        Deadzone.PatchedUpdate(localPlayer, localWeaponAnimation);
    }
}
