using System;

namespace Ravenfield.AiTick;

/// <summary>
/// Contains deterministic, engine-independent rules for bounding overwatch.
/// </summary>
public static class BoundingOverwatchPolicy
{
    public const float DefaultBoundDistance = 12f;
    public const float DefaultPhaseTimeout = 6f;
    public const int HardMinimumSquadSize = 4;

    public static bool IsEligible(
        bool enabled,
        bool patchAvailable,
        bool aiLeader,
        bool attackOrder,
        bool playerIssuedOrder,
        bool hasVehicle,
        bool hasScriptedPath,
        int liveInfantryCount,
        int minimumSquadSize)
    {
        var requiredSize = Math.Max(HardMinimumSquadSize, minimumSquadSize);
        return enabled
            && patchAvailable
            && aiLeader
            && attackOrder
            && !playerIssuedOrder
            && !hasVehicle
            && !hasScriptedPath
            && liveInfantryCount >= requiredSize;
    }

    public static bool IsManeuverMember(int squadMemberIndex, int maneuverParity)
    {
        return squadMemberIndex >= 0
            && (maneuverParity == 0 || maneuverParity == 1)
            && squadMemberIndex % 2 == maneuverParity;
    }

    public static bool ShouldSwap(
        float now,
        float phaseStartedAt,
        float phaseTimeoutSeconds,
        int maneuverCount,
        int readyManeuverCount)
    {
        return maneuverCount > 0 && readyManeuverCount >= maneuverCount;
    }

    public static bool HasTimedOut(
        float now,
        float phaseStartedAt,
        float phaseTimeoutSeconds,
        int maneuverCount,
        int readyManeuverCount)
    {
        return maneuverCount > 0
            && readyManeuverCount < maneuverCount
            && now - phaseStartedAt >= phaseTimeoutSeconds;
    }

    public static bool CanDispatchMover(int pendingMoverCount, bool dispatchedThisTick)
    {
        return pendingMoverCount > 0 && !dispatchedThisTick;
    }

    public static float ResolveBoundDistance(float configured)
    {
        if (float.IsNaN(configured))
        {
            return DefaultBoundDistance;
        }

        return Math.Max(6f, Math.Min(20f, configured));
    }

    public static float ResolvePhaseTimeout(float configured)
    {
        if (float.IsNaN(configured))
        {
            return DefaultPhaseTimeout;
        }

        return Math.Max(3f, Math.Min(12f, configured));
    }

    public static int AdvanceParity(int currentParity)
    {
        return currentParity == 0 ? 1 : 0;
    }

    public static float ResolveLateralOffset(
        int squadMemberIndex,
        int maneuverParity,
        float spacingMeters)
    {
        if (!IsManeuverMember(squadMemberIndex, maneuverParity)
            || float.IsNaN(spacingMeters))
        {
            return 0f;
        }

        var rankWithinElement = squadMemberIndex / 2 + 1;
        var magnitude = Math.Min(8f, rankWithinElement * Math.Abs(spacingMeters));
        return maneuverParity == 0 ? -magnitude : magnitude;
    }
}
