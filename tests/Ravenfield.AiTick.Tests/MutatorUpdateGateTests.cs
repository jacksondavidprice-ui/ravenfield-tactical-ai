using Ravenfield.AiTick;

namespace Ravenfield.AiTick.Tests;

public sealed class MutatorUpdateGateTests
{
    [Fact]
    public void RunsEveryoneWhenStaggerIsOff()
    {
        Assert.True(MutatorUpdateGate.ShouldRunLuaUpdate(frame: 7, index: 99, mutatorCount: 40, maxPerFrame: 8, stagger: false));
    }

    [Fact]
    public void RunsEveryoneWhenCountFitsInTheBudget()
    {
        Assert.True(MutatorUpdateGate.ShouldRunLuaUpdate(frame: 3, index: 9, mutatorCount: 8, maxPerFrame: 8, stagger: true));
        Assert.True(MutatorUpdateGate.ShouldRunLuaUpdate(frame: 3, index: 9, mutatorCount: 5, maxPerFrame: 8, stagger: true));
    }

    [Fact]
    public void SpreadsFortyMutatorsAcrossFiveFramesAtEightPerFrame()
    {
        const int count = 40;
        const int max = 8;
        var ran = new int[count];
        for (var frame = 0; frame < 5; frame++)
        {
            var thisFrame = 0;
            for (var index = 0; index < count; index++)
            {
                if (!MutatorUpdateGate.ShouldRunLuaUpdate(frame, index, count, max, stagger: true))
                {
                    continue;
                }

                ran[index]++;
                thisFrame++;
            }

            Assert.Equal(max, thisFrame);
        }

        Assert.All(ran, n => Assert.Equal(1, n));
    }

    [Fact]
    public void InvalidBudgetRunsEveryone()
    {
        Assert.True(MutatorUpdateGate.ShouldRunLuaUpdate(frame: 1, index: 0, mutatorCount: 20, maxPerFrame: 0, stagger: true));
    }
}
