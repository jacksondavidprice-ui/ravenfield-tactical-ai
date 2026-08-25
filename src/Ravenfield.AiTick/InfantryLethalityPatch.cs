using HarmonyLib;
using UnityEngine;

namespace Ravenfield.AiTick;

/// <summary>
/// Raises accepted direct firearm hits to the target's current health. Ravenfield
/// still owns damage rejection, callbacks, scoring, effects, and death handling.
/// </summary>
internal static class InfantryLethalityPatch
{
    internal static bool Enabled = true;
    internal static bool AffectPlayer = true;
    internal static float HandgunNonFatalBeyondMeters = LethalityPolicy.DefaultHandgunNonFatalBeyondMeters;
    internal static bool Available;

    private static BepInEx.Logging.ManualLogSource? log;
    private static bool loggedFirstHit;
    private static bool loggedFailure;

    internal static void Initialize(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
    {
        Available = false;
        log = logger;
        var damage = AccessTools.Method(typeof(Actor), nameof(Actor.Damage), new[] { typeof(DamageInfo) });
        if (damage is null)
        {
            logger.LogError("Actor.Damage was not found. Infantry one-hit lethality is unavailable.");
            return;
        }

        try
        {
            harmony.Patch(
                damage,
                prefix: new HarmonyMethod(typeof(InfantryLethalityPatch), nameof(DamagePrefix)));
            Available = true;
            logger.LogInfo(Enabled
                ? "Infantry one-hit lethality is on. Ordinary direct firearm hits are lethal."
                : "Infantry one-hit lethality is off.");
        }
        catch (System.Exception ex)
        {
            logger.LogError("Could not patch infantry lethality. Vanilla damage is unchanged. " + ex.Message);
        }
    }

    private static void DamagePrefix(Actor __instance, ref DamageInfo info)
    {
        var original = info;
        try
        {
            var entry = info.sourceWeaponEntry;
            if (__instance is null || entry is null)
            {
                return;
            }

            var sourceActor = info.sourceActor;
            var distance = sourceActor != null
                ? Vector3.Distance(sourceActor.Position(), info.point)
                : float.PositiveInfinity;
            var isTrueHandgun = entry.mainRole == Weapon.WeaponRole.Handgun
                && entry.slot == WeaponManager.WeaponSlot.Secondary;
            var isMountedWeapon = info.sourceWeapon is MountedWeapon;
            if (!LethalityPolicy.ShouldForceLethal(
                    Enabled && Available,
                    info.type == DamageInfo.DamageSourceType.Projectile,
                    info.isSplashDamage,
                    targetIsInfantry: true,
                    __instance.aiControlled,
                    AffectPlayer,
                    __instance.hasHeroArmor,
                    (int)entry.mainRole,
                    distance,
                    HandgunNonFatalBeyondMeters,
                    isTrueHandgun,
                    isMountedWeapon))
            {
                return;
            }

            info.healthDamage = Mathf.Max(info.healthDamage, __instance.health);
            if (!loggedFirstHit)
            {
                loggedFirstHit = true;
                log?.LogInfo(
                    $"One-hit infantry damage applied: role={entry.mainRole}, targetAi={__instance.aiControlled}, " +
                    $"distance={distance:0.#}m.");
            }
        }
        catch (System.Exception ex)
        {
            info = original;
            if (!loggedFailure)
            {
                loggedFailure = true;
                log?.LogWarning("Infantry lethality skipped one hit and preserved vanilla damage. " + ex.Message);
            }
        }
    }
}
