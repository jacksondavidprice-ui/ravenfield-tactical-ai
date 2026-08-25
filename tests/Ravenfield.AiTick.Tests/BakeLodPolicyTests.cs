using Ravenfield.AiTick;

namespace Ravenfield.AiTick.Tests;

public sealed class BakeLodPolicyTests
{
    [Fact]
    public void UsesFallbackWhenConfiguredHeightIsNotPositive()
    {
        Assert.Equal(0.04f, BakeLodPolicy.ResolveCullHeight(0f, 0.04f));
        Assert.Equal(0.04f, BakeLodPolicy.ResolveCullHeight(-1f, 0.04f));
    }

    [Fact]
    public void KeepsAPositiveConfiguredHeight()
    {
        Assert.Equal(0.01f, BakeLodPolicy.ResolveCullHeight(0.01f, 0.04f));
    }
}
