using Ravenfield.AiTick;

namespace Ravenfield.AiTick.Tests;

public sealed class AiTickBudgetTests
{
    [Fact]
    public void ZeroOrNegativeMaxMeansNoCap()
    {
        Assert.Equal(int.MaxValue, AiTickBudget.ResolveMaxTicks(0));
        Assert.Equal(int.MaxValue, AiTickBudget.ResolveMaxTicks(-1));
    }

    [Fact]
    public void PositiveMaxIsKept()
    {
        Assert.Equal(400, AiTickBudget.ResolveMaxTicks(400));
    }

    [Fact]
    public void VanillaSightCapIsTwoHundredPairsPerFrame()
    {
        Assert.Equal(200, AiTickBudget.VanillaMaxInteractionUpdatesPerFrame);
        Assert.Equal(42f, AiTickBudget.VanillaInteractionDivisor);
        Assert.Equal(0.65f, AiTickBudget.VanillaFovDot);
        Assert.Equal(2, AiTickBudget.VanillaCanSeeRaycastSamples);
    }

    [Fact]
    public void InteractionDivisorFallsBackWhenInvalid()
    {
        Assert.Equal(42f, AiTickBudget.ResolveInteractionDivisor(0f));
        Assert.Equal(12f, AiTickBudget.ResolveInteractionDivisor(12f));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(0, 1)]
    [InlineData(8, 4)]
    public void SightSamplesStayInASaneRange(int input, int expected)
    {
        Assert.Equal(expected, AiTickBudget.ResolveSightSamples(input));
    }

    [Theory]
    [InlineData(0.25f, 0.25f)]
    [InlineData(2f, 0.65f)]
    [InlineData(-2f, 0.65f)]
    public void FovDotMustBeAValidCosine(float input, float expected)
    {
        Assert.Equal(expected, AiTickBudget.ResolveFovDot(input));
    }

    [Theory]
    [InlineData(5f, 0.2f)]
    [InlineData(10f, 0.1f)]
    [InlineData(20f, 0.05f)]
    public void PeriodIsTheInverseOfTargetHertz(float hz, float period)
    {
        Assert.Equal(period, AiTickBudget.ResolvePeriodSeconds(hz), precision: 5);
    }

    [Fact]
    public void InvalidHertzKeepsVanillaPeriod()
    {
        Assert.Equal(AiTickBudget.VanillaPeriodSeconds, AiTickBudget.ResolvePeriodSeconds(0f));
        Assert.Equal(AiTickBudget.VanillaPeriodSeconds, AiTickBudget.ResolvePeriodSeconds(-3f));
    }
}
