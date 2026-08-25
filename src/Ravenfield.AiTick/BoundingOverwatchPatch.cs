using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Ravenfield.Trigger;
using UnityEngine;

namespace Ravenfield.AiTick;

/// <summary>
/// Splits eligible attacking squads into alternating support and maneuver
/// elements. It uses Ravenfield's cover reservations, pathfinder, and weapon AI.
/// </summary>
internal static class BoundingOverwatchPatch
{
    internal static bool Enabled = true;
    internal static bool Available;
    internal static int MinimumSquadSize = BoundingOverwatchPolicy.HardMinimumSquadSize;
    internal static float BoundDistance = BoundingOverwatchPolicy.DefaultBoundDistance;
    internal static float PhaseTimeout = BoundingOverwatchPolicy.DefaultPhaseTimeout;

    private const float TickPeriod = 1f;
    private const float StopBoundingDistance = 18f;
    private const float ArrivalDistance = 3f;
    private const float LateralSpacing = 2f;

    private static readonly ConditionalWeakTable<Squad, SquadState> States = new();
    private static readonly HashSet<Squad> ActiveSquads = new();
    private static readonly HashSet<AiActorController> OwnedActors = new();
    private static MethodInfo? issueMovement;
    private static BepInEx.Logging.ManualLogSource? log;
    private static bool loggedFirstActivation;
    private static bool loggedFirstSwap;
    private static bool loggedFirstTimeout;
    private static bool loggedResumeFailure;

    private sealed class SquadState
    {
        internal bool Active;
        internal float NextTick;
        internal float PhaseStartedAt;
        internal int ManeuverParity = 1;
        internal int OrderId;
        internal int MemberFingerprint;
        internal Vector3 Objective;
        internal Vector3 ThreatPosition;
        internal readonly List<AiActorController> Infantry = new();
        internal readonly List<AiActorController> PendingMovers = new();
        internal readonly HashSet<AiActorController> DispatchedMovers = new();
        internal readonly Dictionary<AiActorController, Vector3> Destinations = new();
        internal readonly HashSet<AiActorController> Owned = new();
        internal readonly HashSet<AiActorController> PluginCovers = new();
    }

    internal static void Initialize(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
    {
        Available = false;
        log = logger;

        var update = AccessTools.Method(typeof(Squad), nameof(Squad.Update), Type.EmptyTypes);
        var updateOrders = AccessTools.Method(typeof(Squad), "UpdateOrders", Type.EmptyTypes);
        issueMovement = AccessTools.Method(typeof(Squad), "IssueMovement", Type.EmptyTypes);
        var allowCombat = AccessTools.Method(
            typeof(AiActorController),
            nameof(AiActorController.AllowCombatOverrideMovement),
            Type.EmptyTypes);
        var issueSegment = AccessTools.Method(
            typeof(Squad),
            "IssueMovePathSegment",
            new[] { typeof(Squad.MovePathSegment) });
        var assignOrder = AccessTools.Method(typeof(Squad), nameof(Squad.AssignOrder), new[] { typeof(Order) });
        var dropMember = AccessTools.Method(
            typeof(Squad),
            nameof(Squad.DropMember),
            new[] { typeof(ActorController) });
        var startPathGroup = AccessTools.Method(
            typeof(Squad),
            nameof(Squad.StartPathGroup),
            new[] { typeof(ScriptedPathGroup), typeof(bool) });
        var enterVehicle = AccessTools.Method(
            typeof(Squad),
            nameof(Squad.EnterVehicle),
            new[] { typeof(Vehicle) });
        var moveTo = AccessTools.Method(typeof(Squad), nameof(Squad.MoveTo), new[] { typeof(Vector3) });
        var disband = AccessTools.Method(typeof(Squad), "Disband", Type.EmptyTypes);
        var scriptedPath = AccessTools.Method(
            typeof(AiActorController),
            nameof(AiActorController.InstantiateScriptedPathSeeker),
            Type.EmptyTypes);

        var required = new Dictionary<string, MethodInfo?>
        {
            ["Squad.Update()"] = update,
            ["Squad.UpdateOrders()"] = updateOrders,
            ["Squad.IssueMovement()"] = issueMovement,
            ["AiActorController.AllowCombatOverrideMovement()"] = allowCombat,
            ["Squad.IssueMovePathSegment(MovePathSegment)"] = issueSegment,
            ["Squad.AssignOrder(Order)"] = assignOrder,
            ["Squad.DropMember(ActorController)"] = dropMember,
            ["Squad.StartPathGroup(ScriptedPathGroup,bool)"] = startPathGroup,
            ["Squad.EnterVehicle(Vehicle)"] = enterVehicle,
            ["Squad.MoveTo(Vector3)"] = moveTo,
            ["Squad.Disband()"] = disband,
            ["AiActorController.InstantiateScriptedPathSeeker()"] = scriptedPath,
        };
        foreach (var pair in required)
        {
            if (pair.Value is null)
            {
                logger.LogError(pair.Key + " was not found. Bounding overwatch is unavailable.");
                return;
            }
        }

        try
        {
            harmony.Patch(
                update!,
                postfix: new HarmonyMethod(typeof(BoundingOverwatchPatch), nameof(SquadUpdatePostfix)));
            harmony.Patch(
                updateOrders!,
                prefix: new HarmonyMethod(typeof(BoundingOverwatchPatch), nameof(UpdateOrdersPrefix)));
            harmony.Patch(
                issueMovement!,
                prefix: new HarmonyMethod(typeof(BoundingOverwatchPatch), nameof(IssueMovementPrefix)));
            harmony.Patch(
                allowCombat!,
                postfix: new HarmonyMethod(typeof(BoundingOverwatchPatch), nameof(AllowCombatPostfix)));
            harmony.Patch(
                issueSegment!,
                prefix: new HarmonyMethod(typeof(BoundingOverwatchPatch), nameof(IssueMoveSegmentPrefix)));

            var releasePrefix = new HarmonyMethod(typeof(BoundingOverwatchPatch), nameof(ExternalSquadActionPrefix));
            harmony.Patch(assignOrder!, prefix: releasePrefix);
            harmony.Patch(dropMember!, prefix: releasePrefix);
            harmony.Patch(startPathGroup!, prefix: releasePrefix);
            harmony.Patch(enterVehicle!, prefix: releasePrefix);
            harmony.Patch(moveTo!, prefix: releasePrefix);
            harmony.Patch(disband!, prefix: releasePrefix);
            harmony.Patch(
                scriptedPath!,
                prefix: new HarmonyMethod(typeof(BoundingOverwatchPatch), nameof(ScriptedPathPrefix)));

            Available = true;
            logger.LogInfo(Enabled
                ? "Bounding overwatch is on. Eligible attack squads use alternating support and maneuver elements."
                : "Bounding overwatch is off. Ravenfield squad movement is unchanged.");
        }
        catch (Exception ex)
        {
            Available = false;
            logger.LogError("Could not patch bounding overwatch. Vanilla squad movement is unchanged. " + ex.Message);
        }
    }

    internal static void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (enabled || ActiveSquads.Count == 0)
        {
            return;
        }

        var active = new List<Squad>(ActiveSquads);
        foreach (var squad in active)
        {
            if (squad != null && States.TryGetValue(squad, out var state))
            {
                Release(squad, state, resumeVanilla: true);
            }
        }
    }

    private static void SquadUpdatePostfix(Squad __instance)
    {
        if (__instance == null || __instance.disbanded)
        {
            return;
        }

        var state = States.GetValue(__instance, _ => new SquadState());
        if (Time.time < state.NextTick)
        {
            return;
        }

        state.NextTick = Time.time + TickPeriod;
        CollectEligibleInfantry(
            __instance,
            state,
            out var hasForeignOverride,
            out var hasIndividualScriptedPath,
            out var memberFingerprint);
        var order = __instance.order;
        var eligible = BoundingOverwatchPolicy.IsEligible(
            Enabled,
            Available,
            !__instance.HasPlayerLeader(),
            order is not null && order.type == Order.OrderType.Attack,
            order is not null && order.isIssuedByPlayer,
            __instance.squadVehicle is not null || __instance.IsEnteringSquadVehicle(),
            __instance.HasActiveScriptedPathGroup() || hasIndividualScriptedPath,
            state.Infantry.Count,
            MinimumSquadSize)
            && !hasForeignOverride;

        if (!eligible || order is null)
        {
            Release(__instance, state, resumeVanilla: true);
            return;
        }

        var objective = order.ResolveCurrentTargetPosition();
        var leader = __instance.Leader();
        if (leader == null
            || leader.actor == null
            || FlatDistance(leader.actor.Position(), objective) <= StopBoundingDistance)
        {
            Release(__instance, state, resumeVanilla: true);
            return;
        }

        if (!state.Active
            || state.OrderId != order.uniqueID
            || state.MemberFingerprint != memberFingerprint)
        {
            Release(__instance, state, resumeVanilla: false);
            state.Active = true;
            state.ManeuverParity = 1;
            state.OrderId = order.uniqueID;
            state.MemberFingerprint = memberFingerprint;
            ActiveSquads.Add(__instance);
            StartPhase(__instance, state, objective);
            DispatchNextMover(state);
            if (!loggedFirstActivation)
            {
                loggedFirstActivation = true;
                log?.LogInfo(
                    $"Bounding overwatch activated: squad={__instance.number}, infantry={state.Infantry.Count}, " +
                    $"supportParity=0, maneuverParity={state.ManeuverParity}.");
            }

            return;
        }

        if (BoundingOverwatchPolicy.CanDispatchMover(state.PendingMovers.Count, dispatchedThisTick: false))
        {
            DispatchNextMover(state);
            return;
        }

        CountManeuverReadiness(state, out var maneuverCount, out var readyCount);
        if (BoundingOverwatchPolicy.ShouldSwap(
                Time.time,
                state.PhaseStartedAt,
                PhaseTimeout,
                maneuverCount,
                readyCount))
        {
            state.ManeuverParity = BoundingOverwatchPolicy.AdvanceParity(state.ManeuverParity);
            StartPhase(__instance, state, objective);
            DispatchNextMover(state);
            if (!loggedFirstSwap)
            {
                loggedFirstSwap = true;
                log?.LogInfo(
                    $"Bounding overwatch swapped elements: squad={__instance.number}, " +
                    $"maneuverParity={state.ManeuverParity}.");
            }

            return;
        }

        if (BoundingOverwatchPolicy.HasTimedOut(
                Time.time,
                state.PhaseStartedAt,
                PhaseTimeout,
                maneuverCount,
                readyCount))
        {
            if (!loggedFirstTimeout)
            {
                loggedFirstTimeout = true;
                log?.LogWarning(
                    $"Bounding overwatch released a failed bound: squad={__instance.number}, " +
                    $"ready={readyCount}/{maneuverCount}.");
            }

            Release(__instance, state, resumeVanilla: true);
        }
    }

    private static void CollectEligibleInfantry(
        Squad squad,
        SquadState state,
        out bool hasForeignOverride,
        out bool hasIndividualScriptedPath,
        out int memberFingerprint)
    {
        state.Infantry.Clear();
        hasForeignOverride = false;
        hasIndividualScriptedPath = false;
        memberFingerprint = 17;
        foreach (var ai in squad.aiMembers)
        {
            if (ai == null
                || ai.actor == null
                || ai.actor.dead
                || ai.actor.fallenOver
                || ai.actor.IsSeated()
                || ai.actor.parachuteDeployed
                || ai.IsEnteringVehicle()
                || ai.HasTargetVehicle())
            {
                continue;
            }

            if (ai.IsFollowingScriptedPath())
            {
                hasIndividualScriptedPath = true;
            }

            if (ai.isDefaultMovementOverridden && !state.Owned.Contains(ai))
            {
                hasForeignOverride = true;
            }

            state.Infantry.Add(ai);
            unchecked
            {
                memberFingerprint = memberFingerprint * 31 + ai.actor.GetInstanceID();
            }
        }
    }

    private static void StartPhase(Squad squad, SquadState state, Vector3 objective)
    {
        state.PendingMovers.Clear();
        state.DispatchedMovers.Clear();
        state.Destinations.Clear();
        state.Objective = objective;
        var target = squad.GetTarget();
        state.ThreatPosition = target == null ? objective : target.Position();
        state.PhaseStartedAt = float.PositiveInfinity;

        foreach (var ai in state.Infantry)
        {
            TakeOwnership(state, ai);
            ai.OverrideDefaultMovement();
            if (BoundingOverwatchPolicy.IsManeuverMember(ai.squadMemberIndex, state.ManeuverParity))
            {
                ai.CancelPath(isMovementOverride: true);
                state.PendingMovers.Add(ai);
            }
            else
            {
                if (!ai.IsMovingToCover())
                {
                    ai.CancelPath(isMovementOverride: true);
                }

                state.Destinations[ai] = ai.actor.Position();
            }
        }
    }

    private static void DispatchNextMover(SquadState state)
    {
        if (!BoundingOverwatchPolicy.CanDispatchMover(state.PendingMovers.Count, dispatchedThisTick: false))
        {
            return;
        }

        var ai = state.PendingMovers[0];
        state.PendingMovers.RemoveAt(0);
        IssueBound(ai, state, state.Objective, state.ThreatPosition);
        state.DispatchedMovers.Add(ai);
        if (state.PendingMovers.Count == 0)
        {
            state.PhaseStartedAt = Time.time;
        }
    }

    private static void IssueBound(
        AiActorController ai,
        SquadState state,
        Vector3 objective,
        Vector3 threatPosition)
    {
        var origin = ai.actor.Position();
        var advance = objective - origin;
        advance.y = 0f;
        var remaining = advance.magnitude;
        if (remaining < 0.01f)
        {
            state.Destinations[ai] = origin;
            return;
        }

        var direction = advance / remaining;
        var step = Mathf.Min(BoundDistance, Mathf.Max(0f, remaining - StopBoundingDistance));
        if (step < 1f)
        {
            state.Destinations[ai] = origin;
            return;
        }

        var right = new Vector3(direction.z, 0f, -direction.x);
        var lateralOffset = BoundingOverwatchPolicy.ResolveLateralOffset(
            ai.squadMemberIndex,
            state.ManeuverParity,
            LateralSpacing);
        var probe = origin + direction * step + right * lateralOffset;
        var threatDirection = threatPosition - probe;
        threatDirection.y = 0f;

        ai.ReleaseDefaultMovementOverride();
        var cover = CoverManager.instance == null
            ? null
            : CoverManager.instance.GetCoverPositionAgainstDirection(probe, threatDirection);
        if (cover != null)
        {
            var coverAdvance = cover.position - origin;
            coverAdvance.y = 0f;
            var madeForwardProgress = Vector3.Dot(coverAdvance, direction) >= step * 0.35f;
            var withinLocalSearch = coverAdvance.magnitude <= step + 10f;
            if (madeForwardProgress && withinLocalSearch && ai.EnterCover(cover))
            {
                ai.OverrideDefaultMovement();
                state.PluginCovers.Add(ai);
                state.Destinations[ai] = cover.position;
                return;
            }
        }

        ai.LeaveCover();
        state.PluginCovers.Remove(ai);
        ai.OverrideDefaultMovement();
        ai.Goto(probe, isMovementOverride: true);
        state.Destinations[ai] = probe;
    }

    private static void CountManeuverReadiness(
        SquadState state,
        out int maneuverCount,
        out int readyCount)
    {
        maneuverCount = state.DispatchedMovers.Count;
        readyCount = 0;
        foreach (var ai in state.DispatchedMovers)
        {
            if (ai != null
                && (ai.IsInCover()
                    || (state.Destinations.TryGetValue(ai, out var destination)
                        && FlatDistance(ai.actor.Position(), destination) <= ArrivalDistance)))
            {
                readyCount++;
            }
        }
    }

    private static void TakeOwnership(SquadState state, AiActorController ai)
    {
        state.Owned.Add(ai);
        OwnedActors.Add(ai);
    }

    private static void ExternalSquadActionPrefix(Squad __instance)
    {
        if (__instance != null && States.TryGetValue(__instance, out var state))
        {
            Release(__instance, state, resumeVanilla: false);
        }
    }

    private static void ScriptedPathPrefix(AiActorController __instance)
    {
        var squad = __instance == null ? null : __instance.squad;
        if (squad != null && States.TryGetValue(squad, out var state))
        {
            Release(squad, state, resumeVanilla: false);
        }
    }

    private static bool UpdateOrdersPrefix(Squad __instance)
    {
        return __instance == null || !ActiveSquads.Contains(__instance);
    }

    private static bool IssueMoveSegmentPrefix(Squad __instance)
    {
        return __instance == null || !ActiveSquads.Contains(__instance);
    }

    private static bool IssueMovementPrefix(Squad __instance)
    {
        return __instance == null || !ActiveSquads.Contains(__instance);
    }

    private static void AllowCombatPostfix(AiActorController __instance, ref bool __result)
    {
        if (__instance != null && OwnedActors.Contains(__instance))
        {
            __result = false;
        }
    }

    private static void Release(Squad squad, SquadState state, bool resumeVanilla)
    {
        if (!state.Active && state.Owned.Count == 0)
        {
            return;
        }

        ActiveSquads.Remove(squad);
        foreach (var ai in state.Owned)
        {
            OwnedActors.Remove(ai);
            if (ai == null)
            {
                continue;
            }

            ai.CancelPath(isMovementOverride: true);
            ai.ReleaseDefaultMovementOverride();
            if (state.PluginCovers.Contains(ai))
            {
                ai.LeaveCover();
            }
        }

        state.Owned.Clear();
        state.PluginCovers.Clear();
        state.PendingMovers.Clear();
        state.DispatchedMovers.Clear();
        state.Destinations.Clear();
        state.Active = false;

        if (!resumeVanilla || squad == null || squad.disbanded || squad.order is null || issueMovement is null)
        {
            return;
        }

        try
        {
            issueMovement.Invoke(squad, null);
        }
        catch (Exception ex)
        {
            if (!loggedResumeFailure)
            {
                loggedResumeFailure = true;
                log?.LogWarning("Could not resume one squad after bounding overwatch. " + ex.Message);
            }
        }
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
