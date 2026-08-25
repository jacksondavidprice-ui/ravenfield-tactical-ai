namespace Ravenfield.AiTick;

/// <summary>
/// Fair round-robin for mutator Lua Update() calls so N scripts cannot all
/// run on the same frame. Weapon and map scripts are not gated here.
/// </summary>
public static class MutatorUpdateGate
{
    public static bool ShouldRunLuaUpdate(
        int frame,
        int index,
        int mutatorCount,
        int maxPerFrame,
        bool stagger)
    {
        if (!stagger || maxPerFrame <= 0 || mutatorCount <= maxPerFrame)
        {
            return true;
        }

        var interval = (mutatorCount + maxPerFrame - 1) / maxPerFrame;
        var slot = frame % interval;
        return index % interval == slot;
    }
}
