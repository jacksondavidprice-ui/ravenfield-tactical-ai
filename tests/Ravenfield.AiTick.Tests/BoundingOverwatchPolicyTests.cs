using System.Reflection;

namespace Ravenfield.AiTick.Tests;

public sealed class BoundingOverwatchPolicyTests
{
    [Fact]
    public void EligibleSquadPassesEverySafetyGate()
    {
        Assert.True(IsEligible(
            enabled: true,
            patchAvailable: true,
            aiLeader: true,
            attackOrder: true,
            playerIssuedOrder: false,
            hasVehicle: false,
            hasScriptedPath: false,
            liveInfantryCount: 4,
            minimumSquadSize: 4));
    }

    [Theory]
    [InlineData(false, true, true, true, false, false, false, 4, 4)]
    [InlineData(true, false, true, true, false, false, false, 4, 4)]
    [InlineData(true, true, false, true, false, false, false, 4, 4)]
    [InlineData(true, true, true, false, false, false, false, 4, 4)]
    [InlineData(true, true, true, true, true, false, false, 4, 4)]
    [InlineData(true, true, true, true, false, true, false, 4, 4)]
    [InlineData(true, true, true, true, false, false, true, 4, 4)]
    [InlineData(true, true, true, true, false, false, false, 3, 4)]
    [InlineData(true, true, true, true, false, false, false, 3, 2)]
    [InlineData(true, true, true, true, false, false, false, 4, 5)]
    public void IneligibleSquadFailsAnyUnsafeGate(
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
        Assert.False(IsEligible(
            enabled,
            patchAvailable,
            aiLeader,
            attackOrder,
            playerIssuedOrder,
            hasVehicle,
            hasScriptedPath,
            liveInfantryCount,
            minimumSquadSize));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(2, 0, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 1, true)]
    [InlineData(3, 1, true)]
    [InlineData(-1, 0, false)]
    [InlineData(-1, 1, false)]
    public void ManeuverMembershipUsesDeterministicEvenOddTeams(
        int squadMemberIndex,
        int maneuverParity,
        bool expected)
    {
        Assert.Equal(expected, IsManeuverMember(squadMemberIndex, maneuverParity));
    }

    [Theory]
    [InlineData(5f, 0f, 6f, 2, 1, false)]
    [InlineData(5f, 0f, 6f, 2, 2, true)]
    [InlineData(6f, 0f, 6f, 2, 0, false)]
    [InlineData(100f, 0f, 6f, 0, 0, false)]
    public void PhaseSwapsOnlyWhenEveryMoverIsReady(
        float now,
        float phaseStartedAt,
        float phaseTimeoutSeconds,
        int maneuverCount,
        int readyManeuverCount,
        bool expected)
    {
        Assert.Equal(
            expected,
            ShouldSwap(
                now,
                phaseStartedAt,
                phaseTimeoutSeconds,
                maneuverCount,
                readyManeuverCount));
    }

    [Theory]
    [InlineData(5f, 0f, 6f, 2, 0, false)]
    [InlineData(6f, 0f, 6f, 2, 0, true)]
    [InlineData(6f, 0f, 6f, 2, 1, true)]
    [InlineData(100f, 0f, 6f, 0, 0, false)]
    [InlineData(100f, 0f, 6f, 2, 2, false)]
    public void TimeoutIsReportedOnlyForAnIncompleteActiveManeuver(
        float now,
        float phaseStartedAt,
        float timeout,
        int moverCount,
        int readyCount,
        bool expected)
    {
        Assert.Equal(
            expected,
            HasTimedOut(now, phaseStartedAt, timeout, moverCount, readyCount));
    }

    [Theory]
    [InlineData(1, false, true)]
    [InlineData(2, false, true)]
    [InlineData(1, true, false)]
    [InlineData(0, false, false)]
    [InlineData(-1, false, false)]
    public void AtMostOnePendingMoverCanBeDispatchedPerTick(
        int pendingMoverCount,
        bool dispatchedThisTick,
        bool expected)
    {
        Assert.Equal(
            expected,
            CanDispatchMover(pendingMoverCount, dispatchedThisTick));
    }

    [Theory]
    [InlineData(float.NaN, 12f)]
    [InlineData(2f, 6f)]
    [InlineData(6f, 6f)]
    [InlineData(14f, 14f)]
    [InlineData(20f, 20f)]
    [InlineData(25f, 20f)]
    public void BoundDistanceUsesDefaultAndSafeLimits(float configured, float expected)
    {
        Assert.Equal(expected, ResolveBoundDistance(configured));
    }

    [Theory]
    [InlineData(float.NaN, 6f)]
    [InlineData(1f, 3f)]
    [InlineData(3f, 3f)]
    [InlineData(8f, 8f)]
    [InlineData(12f, 12f)]
    [InlineData(20f, 12f)]
    public void PhaseTimeoutUsesDefaultAndSafeLimits(float configured, float expected)
    {
        Assert.Equal(expected, ResolvePhaseTimeout(configured));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 0)]
    [InlineData(2, 0)]
    public void ParityAlternatesAndInvalidValuesResetToEven(int currentParity, int expected)
    {
        Assert.Equal(expected, AdvanceParity(currentParity));
    }

    [Theory]
    [InlineData(0, 0, 2f, -2f)]
    [InlineData(2, 0, 2f, -4f)]
    [InlineData(1, 1, 2f, 2f)]
    [InlineData(3, 1, 2f, 4f)]
    public void LateralOffsetUsesSignedOneBasedWithinParityRankTimesSpacing(
        int squadMemberIndex,
        int maneuverParity,
        float spacingMeters,
        float expected)
    {
        Assert.Equal(
            expected,
            ResolveLateralOffset(squadMemberIndex, maneuverParity, spacingMeters));
    }

    [Theory]
    [InlineData(-1, 0, 2f, 0f)]
    [InlineData(0, -1, 2f, 0f)]
    [InlineData(0, 2, 2f, 0f)]
    public void LateralOffsetRejectsNegativeIndicesAndInvalidParity(
        int squadMemberIndex,
        int maneuverParity,
        float spacingMeters,
        float expected)
    {
        Assert.Equal(
            expected,
            ResolveLateralOffset(squadMemberIndex, maneuverParity, spacingMeters));
    }

    [Theory]
    [InlineData(10, 0, 2f, -8f)]
    [InlineData(11, 1, 2f, 8f)]
    public void LateralOffsetCapsEachSideAtEightMeters(
        int squadMemberIndex,
        int maneuverParity,
        float spacingMeters,
        float expected)
    {
        Assert.Equal(
            expected,
            ResolveLateralOffset(squadMemberIndex, maneuverParity, spacingMeters));
    }

    private static bool IsEligible(
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
        return Invoke<bool>(
            "IsEligible",
            [
                typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                typeof(bool), typeof(bool), typeof(int), typeof(int),
            ],
            [
                enabled, patchAvailable, aiLeader, attackOrder, playerIssuedOrder,
                hasVehicle, hasScriptedPath, liveInfantryCount, minimumSquadSize,
            ]);
    }

    private static bool IsManeuverMember(int squadMemberIndex, int maneuverParity)
    {
        return Invoke<bool>(
            "IsManeuverMember",
            [typeof(int), typeof(int)],
            [squadMemberIndex, maneuverParity]);
    }

    private static bool ShouldSwap(
        float now,
        float phaseStartedAt,
        float phaseTimeoutSeconds,
        int maneuverCount,
        int readyManeuverCount)
    {
        return Invoke<bool>(
            "ShouldSwap",
            [typeof(float), typeof(float), typeof(float), typeof(int), typeof(int)],
            [now, phaseStartedAt, phaseTimeoutSeconds, maneuverCount, readyManeuverCount]);
    }

    private static bool HasTimedOut(
        float now,
        float phaseStartedAt,
        float timeout,
        int moverCount,
        int readyCount)
    {
        return Invoke<bool>(
            "HasTimedOut",
            [typeof(float), typeof(float), typeof(float), typeof(int), typeof(int)],
            [now, phaseStartedAt, timeout, moverCount, readyCount]);
    }

    private static bool CanDispatchMover(int pendingMoverCount, bool dispatchedThisTick)
    {
        return Invoke<bool>(
            "CanDispatchMover",
            [typeof(int), typeof(bool)],
            [pendingMoverCount, dispatchedThisTick]);
    }

    private static float ResolveBoundDistance(float configured)
    {
        return Invoke<float>(
            "ResolveBoundDistance",
            [typeof(float)],
            [configured]);
    }

    private static float ResolvePhaseTimeout(float configured)
    {
        return Invoke<float>(
            "ResolvePhaseTimeout",
            [typeof(float)],
            [configured]);
    }

    private static int AdvanceParity(int currentParity)
    {
        return Invoke<int>(
            "AdvanceParity",
            [typeof(int)],
            [currentParity]);
    }

    private static float ResolveLateralOffset(
        int squadMemberIndex,
        int maneuverParity,
        float spacingMeters)
    {
        return Invoke<float>(
            "ResolveLateralOffset",
            [typeof(int), typeof(int), typeof(float)],
            [squadMemberIndex, maneuverParity, spacingMeters]);
    }

    private static T Invoke<T>(string methodName, Type[] parameterTypes, object[] arguments)
    {
        var policyType = typeof(BoundingOverwatchPolicyTests).Assembly.GetType(
            "Ravenfield.AiTick.BoundingOverwatchPolicy");
        Assert.NotNull(policyType);

        var method = policyType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: parameterTypes,
            modifiers: null);
        Assert.NotNull(method);

        return Assert.IsType<T>(method.Invoke(null, arguments));
    }
}
