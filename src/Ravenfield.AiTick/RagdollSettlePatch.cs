using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Ravenfield.AiTick;

/// <summary>
/// Freezes dead ragdolls once they have settled so PhysX is not simulating
/// hundreds of bone bodies until the actor is reused.
/// </summary>
internal static class RagdollSettlePatch
{
    internal static bool Enabled = true;
    internal static float FreezeSpeed = RagdollSettlePolicy.DefaultFreezeSpeed;

    private static FieldInfo? actorDead;
    private static FieldInfo? actorRagdoll;
    private static FieldInfo? hipRigidbody;
    private static FieldInfo? deathTimestamp;
    private static FieldInfo? ragdollBodies;
    private static readonly HashSet<int> frozen = new HashSet<int>();
    private static BepInEx.Logging.ManualLogSource? log;
    private static bool loggedFirst;

    internal static void Initialize(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
    {
        log = logger;
        if (!Enabled)
        {
            logger.LogInfo("Ragdoll settle freeze is off.");
            return;
        }

        var actor = AccessTools.TypeByName("Actor");
        var raggy = AccessTools.TypeByName("ActiveRaggy");
        if (actor is null || raggy is null)
        {
            logger.LogError("Actor/ActiveRaggy not found. Ragdoll settle freeze disabled.");
            return;
        }

        actorDead = AccessTools.Field(actor, "dead");
        actorRagdoll = AccessTools.Field(actor, "ragdoll");
        hipRigidbody = AccessTools.Field(actor, "hipRigidbody");
        deathTimestamp = AccessTools.Field(actor, "deathTimestamp");
        ragdollBodies = AccessTools.Field(raggy, "rigidbodies");
        var fixedUpdate = AccessTools.Method(actor, "FixedUpdate");
        var ragdoll = AccessTools.Method(raggy, "Ragdoll", new[] { typeof(Vector3) });
        if (actorDead is null || actorRagdoll is null || hipRigidbody is null || deathTimestamp is null
            || ragdollBodies is null || fixedUpdate is null || ragdoll is null)
        {
            logger.LogError("Ragdoll settle fields/methods missing. Freeze disabled.");
            return;
        }

        harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(typeof(RagdollSettlePatch), nameof(FixedUpdatePostfix)));
        harmony.Patch(ragdoll, prefix: new HarmonyMethod(typeof(RagdollSettlePatch), nameof(RagdollPrefix)));
        logger.LogInfo($"Ragdoll settle freeze: speed<={FreezeSpeed:0.###}");
    }

    private static void RagdollPrefix(object __instance)
    {
        try
        {
            frozen.Remove(((Object)__instance).GetInstanceID());
            SetFrozen((Rigidbody[])ragdollBodies!.GetValue(__instance), false);
        }
        catch (System.Exception ex)
        {
            log?.LogWarning("Failed to unfreeze a ragdoll: " + ex.Message);
        }
    }

    private static void FixedUpdatePostfix(object __instance)
    {
        try
        {
            if (!(bool)actorDead!.GetValue(__instance))
            {
                return;
            }

            var ragdoll = actorRagdoll!.GetValue(__instance);
            if (ragdoll is null)
            {
                return;
            }

            var ragdollId = ((Object)ragdoll).GetInstanceID();
            if (frozen.Contains(ragdollId))
            {
                return;
            }

            var hip = hipRigidbody!.GetValue(__instance) as Rigidbody;
            if (hip == null)
            {
                return;
            }

            var bodies = (Rigidbody[])ragdollBodies!.GetValue(ragdoll);
            var active = bodies != null && bodies.Length > 0 && bodies[0] != null && bodies[0].gameObject.activeInHierarchy;
            var secondsDead = Time.time - (float)deathTimestamp!.GetValue(__instance);
            if (!RagdollSettlePolicy.ShouldFreeze(
                    true,
                    false,
                    active,
                    secondsDead,
                    RagdollSettlePolicy.DefaultMinSecondsDead,
                    hip.velocity.magnitude,
                    FreezeSpeed))
            {
                return;
            }

            SetFrozen(bodies, true);
            frozen.Add(ragdollId);
            if (!loggedFirst)
            {
                loggedFirst = true;
                log?.LogInfo("First dead ragdoll frozen after settle. Later freezes are silent.");
            }
        }
        catch (System.Exception)
        {
            // Actor teardown during scene unload.
        }
    }

    private static void SetFrozen(Rigidbody[]? bodies, bool freeze)
    {
        if (bodies is null)
        {
            return;
        }

        for (var i = 0; i < bodies.Length; i++)
        {
            var body = bodies[i];
            if (body == null)
            {
                continue;
            }

            if (freeze)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = freeze;
            body.detectCollisions = !freeze;
        }
    }
}
