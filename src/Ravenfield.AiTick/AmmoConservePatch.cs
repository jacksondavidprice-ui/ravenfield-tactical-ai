using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Ravenfield.AiTick;

/// <summary>
/// Stops AI automatic weapons from spraying at range. Full auto inside CQB,
/// short bursts at mid range, singles further out.
/// </summary>
internal static class AmmoConservePatch
{
    internal static bool Enabled = true;
    internal static float CqbRange = AmmoConservePolicy.VanillaCqbRange;
    internal static float MidRange = AmmoConservePolicy.DefaultMidRange;
    internal static int MidBurst = AmmoConservePolicy.DefaultMidBurst;
    internal static int LongBurst = AmmoConservePolicy.DefaultLongBurst;
    internal static float MidPause = AmmoConservePolicy.DefaultMidPause;
    internal static float LongPause = AmmoConservePolicy.DefaultLongPause;

    private struct BurstState
    {
        public int shots;
        public float lastShot;
        public float pauseUntil;
    }

    private static readonly Dictionary<int, BurstState> states = new Dictionary<int, BurstState>();
    private static MethodInfo? getUser;
    private static MethodInfo? actorPosition;
    private static FieldInfo? actorAi;
    private static FieldInfo? closestEnemy;
    private static FieldInfo? actorController;
    private static FieldInfo? aiTarget;
    private static FieldInfo? weaponConfig;
    private static FieldInfo? configAuto;

    internal static void Initialize(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
    {
        if (!Enabled)
        {
            logger.LogInfo("Bot ammo conserve is off.");
            return;
        }

        var weapon = AccessTools.TypeByName("Weapon");
        var actor = AccessTools.TypeByName("Actor");
        var ai = AccessTools.TypeByName("AiActorController");
        var config = AccessTools.TypeByName("Weapon+Configuration")
            ?? AccessTools.Inner(weapon, "Configuration");
        if (weapon is null || actor is null || config is null)
        {
            logger.LogError("Weapon/Actor not found. Ammo conserve disabled.");
            return;
        }

        getUser = AccessTools.PropertyGetter(weapon, "user") ?? AccessTools.Method(weapon, "get_user");
        actorPosition = AccessTools.Method(actor, "Position");
        actorAi = AccessTools.Field(actor, "aiControlled");
        closestEnemy = AccessTools.Field(actor, "closestEnemyDistance");
        actorController = AccessTools.Field(actor, "controller");
        aiTarget = ai is null ? null : AccessTools.Field(ai, "target");
        weaponConfig = AccessTools.Field(weapon, "configuration");
        configAuto = AccessTools.Field(config, "auto");
        var canFire = AccessTools.Method(weapon, "CanFire");
        var shoot = AccessTools.Method(weapon, "Shoot", new[] { typeof(Vector3), typeof(bool) })
            ?? AccessTools.Method(weapon, "Shoot");
        if (getUser is null || actorAi is null || closestEnemy is null || weaponConfig is null
            || configAuto is null || canFire is null || shoot is null)
        {
            logger.LogError("Ammo conserve fields/methods missing. Disabled.");
            return;
        }

        harmony.Patch(canFire, postfix: new HarmonyMethod(typeof(AmmoConservePatch), nameof(CanFirePostfix)));
        harmony.Patch(shoot, postfix: new HarmonyMethod(typeof(AmmoConservePatch), nameof(ShootPostfix)));
        logger.LogInfo(
            $"Bot ammo conserve: cqb={CqbRange:0.#}m mid={MidRange:0.#}m " +
            $"burst={MidBurst}/{LongBurst} pause={MidPause:0.##}/{LongPause:0.##}s");
    }

    private static void CanFirePostfix(object __instance, ref bool __result)
    {
        if (!__result || !Enabled)
        {
            return;
        }

        try
        {
            if (!TryRange(__instance, out var id, out var distance, out var burst))
            {
                return;
            }

            if (burst <= 0)
            {
                return;
            }

            if (!states.TryGetValue(id, out var state))
            {
                return;
            }

            if (!AmmoConservePolicy.CanFireNow(Time.time, state.pauseUntil))
            {
                __result = false;
            }
        }
        catch (System.Exception)
        {
        }
    }

    private static void ShootPostfix(object __instance, bool __result)
    {
        if (!__result || !Enabled)
        {
            return;
        }

        try
        {
            if (!TryRange(__instance, out var id, out var distance, out var burst))
            {
                return;
            }

            states.TryGetValue(id, out var state);
            var pause = distance <= MidRange ? MidPause : LongPause;
            AmmoConservePolicy.OnShot(
                ref state.shots,
                ref state.lastShot,
                ref state.pauseUntil,
                Time.time,
                burst,
                pause,
                AmmoConservePolicy.DefaultEngagementReset);
            states[id] = state;
        }
        catch (System.Exception)
        {
        }
    }

    private static bool TryRange(object weapon, out int id, out float distance, out int burst)
    {
        id = 0;
        distance = 0f;
        burst = 0;
        var config = weaponConfig!.GetValue(weapon);
        if (config is null || !(bool)configAuto!.GetValue(config))
        {
            return false;
        }

        var user = getUser!.Invoke(weapon, null);
        if (user is null || !(bool)actorAi!.GetValue(user))
        {
            return false;
        }

        distance = (float)closestEnemy!.GetValue(user);
        if (aiTarget != null && actorController != null && actorPosition != null)
        {
            var controller = actorController.GetValue(user);
            if (controller != null)
            {
                var target = aiTarget.GetValue(controller);
                if (target != null)
                {
                    var from = (Vector3)actorPosition.Invoke(user, null);
                    var to = (Vector3)actorPosition.Invoke(target, null);
                    distance = Vector3.Distance(from, to);
                }
            }
        }

        burst = AmmoConservePolicy.BurstSize(distance, CqbRange, MidRange, MidBurst, LongBurst);
        id = ((Object)weapon).GetInstanceID();
        return true;
    }
}
