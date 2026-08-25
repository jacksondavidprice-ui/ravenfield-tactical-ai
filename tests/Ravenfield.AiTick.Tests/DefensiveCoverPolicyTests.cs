using System.Reflection;

namespace Ravenfield.AiTick.Tests;

public sealed class DefensiveCoverPolicyTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void ExtendedCoverGateFollowsConfiguredTruthTable(
        bool enabled,
        bool vanillaIsInCqcZone,
        bool expected)
    {
        Assert.Equal(expected, UseExtendedCoverGate(enabled, vanillaIsInCqcZone));
    }

    [Theory]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, true)]
    [InlineData(true, true, false, true)]
    public void UnavailablePatchCannotExtendTheVanillaGate(
        bool patchAvailable,
        bool enabled,
        bool vanillaIsInCqcZone,
        bool expected)
    {
        Assert.Equal(
            expected,
            UseExtendedCoverGate(patchAvailable, enabled, vanillaIsInCqcZone));
    }

    private static bool UseExtendedCoverGate(bool enabled, bool vanillaIsInCqcZone)
    {
        var policyType = typeof(DefensiveCoverPolicyTests).Assembly.GetType(
            "Ravenfield.AiTick.DefensiveCoverPolicy");
        Assert.NotNull(policyType);

        var method = policyType.GetMethod(
            "UseExtendedCoverGate",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(bool), typeof(bool)],
            modifiers: null);
        Assert.NotNull(method);

        return Assert.IsType<bool>(method.Invoke(null, [enabled, vanillaIsInCqcZone]));
    }

    private static bool UseExtendedCoverGate(
        bool patchAvailable,
        bool enabled,
        bool vanillaIsInCqcZone)
    {
        var policyType = typeof(DefensiveCoverPolicyTests).Assembly.GetType(
            "Ravenfield.AiTick.DefensiveCoverPolicy");
        Assert.NotNull(policyType);

        var method = policyType.GetMethod(
            "UseExtendedCoverGate",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(bool), typeof(bool), typeof(bool)],
            modifiers: null);
        Assert.NotNull(method);

        return Assert.IsType<bool>(
            method.Invoke(null, [patchAvailable, enabled, vanillaIsInCqcZone]));
    }
}
