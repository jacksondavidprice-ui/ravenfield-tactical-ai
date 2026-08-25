using Ravenfield.AiTick;

namespace Ravenfield.AiTick.Tests;

public sealed class AmmoConservePolicyTests
{
    [Fact]
    public void CqbIsUnrestrictedFullAuto()
    {
        Assert.Equal(0, AmmoConservePolicy.BurstSize(10f, 30f, 80f, 3, 1));
        Assert.Equal(0, AmmoConservePolicy.BurstSize(30f, 30f, 80f, 3, 1));
    }

    [Fact]
    public void MidRangeUsesShortBursts()
    {
        Assert.Equal(3, AmmoConservePolicy.BurstSize(50f, 30f, 80f, 3, 1));
        Assert.Equal(3, AmmoConservePolicy.BurstSize(80f, 30f, 80f, 3, 1));
    }

    [Fact]
    public void LongRangeIsSemiAuto()
    {
        Assert.Equal(1, AmmoConservePolicy.BurstSize(120f, 30f, 80f, 3, 1));
    }

    [Fact]
    public void PauseBlocksUntilItExpires()
    {
        Assert.False(AmmoConservePolicy.CanFireNow(1f, 1.4f));
        Assert.True(AmmoConservePolicy.CanFireNow(1.4f, 1.4f));
    }

    [Fact]
    public void ThreeShotBurstThenPauses()
    {
        var shots = 0;
        var last = 0f;
        var pauseUntil = 0f;
        AmmoConservePolicy.OnShot(ref shots, ref last, ref pauseUntil, 1f, 3, 0.4f, 1.5f);
        AmmoConservePolicy.OnShot(ref shots, ref last, ref pauseUntil, 1.1f, 3, 0.4f, 1.5f);
        Assert.Equal(0f, pauseUntil);
        AmmoConservePolicy.OnShot(ref shots, ref last, ref pauseUntil, 1.2f, 3, 0.4f, 1.5f);
        Assert.Equal(1.6f, pauseUntil, precision: 4);
        Assert.Equal(0, shots);
    }

    [Fact]
    public void NewEngagementResetsTheBurst()
    {
        var shots = 2;
        var last = 1f;
        var pauseUntil = 0f;
        AmmoConservePolicy.OnShot(ref shots, ref last, ref pauseUntil, 3f, 3, 0.4f, 1.5f);
        Assert.Equal(1, shots);
        Assert.Equal(0f, pauseUntil);
    }
}
