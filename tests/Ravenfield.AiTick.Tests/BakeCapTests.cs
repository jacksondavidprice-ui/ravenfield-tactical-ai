using Ravenfield.AiTick;

namespace Ravenfield.AiTick.Tests;

public sealed class BakeCapTests
{
    [Fact]
    public void DoesNotEvictUntilFull()
    {
        var cap = new BakeCap(2);
        Assert.Null(cap.Register(1));
        Assert.Null(cap.Register(2));
        Assert.Equal(2, cap.Count);
    }

    [Fact]
    public void EvictsOldestWhenOverCap()
    {
        var cap = new BakeCap(2);
        cap.Register(1);
        cap.Register(2);
        Assert.Equal(1, cap.Register(3));
        Assert.Equal(2, cap.Register(4));
        Assert.Equal(2, cap.Count);
    }

    [Fact]
    public void TreatsNonPositiveMaxAsOne()
    {
        var cap = new BakeCap(0);
        Assert.Null(cap.Register(1));
        Assert.Equal(1, cap.Register(2));
    }

    [Fact]
    public void SetMaxEvictsOldestWhenShrinking()
    {
        var cap = new BakeCap(3);
        cap.Register(1);
        cap.Register(2);
        cap.Register(3);
        var evicted = cap.SetMax(1);
        Assert.Equal(new[] { 1, 2 }, evicted);
        Assert.Equal(1, cap.Count);
        Assert.Equal(3, cap.Register(4));
    }
}

public sealed class PlayerOnlyMutatorsTests
{
    [Theory]
    [InlineData("War Thunder Flight Model", true)]
    [InlineData("Gun Tuck", true)]
    [InlineData("GunTuck Realistic", true)]
    [InlineData("Fly-By Replacer", true)]
    [InlineData("First-Person Ragdoll", true)]
    [InlineData("Bot Accuracy Tweaks", false)]
    [InlineData(null, false)]
    public void MatchesKnownPlayerFacingNames(string? name, bool expected)
    {
        Assert.Equal(expected, PlayerOnlyMutators.Matches(name, PlayerOnlyMutators.DefaultKeywords));
    }
}
