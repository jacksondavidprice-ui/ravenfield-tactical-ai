using Ravenfield.AiTick;

namespace Ravenfield.AiTick.Tests;

public sealed class RemainsBudgetTests
{
    [Fact]
    public void MigratesTheOldCorpseDefault()
    {
        Assert.Equal(600, RemainsBudget.MigrateOldDefault(120, 120, 600));
        Assert.Equal(200, RemainsBudget.MigrateOldDefault(200, 120, 600));
    }

    [Fact]
    public void CapFloorIsOne()
    {
        Assert.Equal(1, RemainsBudget.ResolveCap(0));
        Assert.Equal(600, RemainsBudget.ResolveCap(600));
    }
}

public sealed class BloodDropBudgetTests
{
    [Fact]
    public void UnlimitedWhenMaxIsZero()
    {
        var last = -1;
        var spawned = 0;
        Assert.True(BloodDropBudget.CanSpawn(1, ref last, ref spawned, 0));
        Assert.True(BloodDropBudget.CanSpawn(1, ref last, ref spawned, 0));
    }

    [Fact]
    public void StopsAfterTheFrameBudget()
    {
        var last = -1;
        var spawned = 0;
        Assert.True(BloodDropBudget.CanSpawn(10, ref last, ref spawned, 2));
        Assert.True(BloodDropBudget.CanSpawn(10, ref last, ref spawned, 2));
        Assert.False(BloodDropBudget.CanSpawn(10, ref last, ref spawned, 2));
        Assert.True(BloodDropBudget.CanSpawn(11, ref last, ref spawned, 2));
    }
}

public sealed class RagdollSettlePolicyTests
{
    [Fact]
    public void WaitsUntilTheBodyHasFlopped()
    {
        Assert.False(RagdollSettlePolicy.ShouldFreeze(true, false, true, 0.5f, 1.25f, 0f, 0.35f));
        Assert.True(RagdollSettlePolicy.ShouldFreeze(true, false, true, 1.3f, 1.25f, 0.2f, 0.35f));
    }

    [Fact]
    public void LeavesAStillMovingBodyAlone()
    {
        Assert.False(RagdollSettlePolicy.ShouldFreeze(true, false, true, 2f, 1.25f, 1.5f, 0.35f));
    }

    [Fact]
    public void IgnoresLivingActors()
    {
        Assert.False(RagdollSettlePolicy.ShouldFreeze(false, false, true, 3f, 1.25f, 0f, 0.35f));
    }
}
