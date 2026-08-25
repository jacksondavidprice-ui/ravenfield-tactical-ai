using System.Collections.Generic;

namespace Ravenfield.AiTick;

/// <summary>
/// FIFO cap for baked corpse/wreck clones. Returns the oldest instance id
/// when a new bake would exceed the limit.
/// </summary>
public sealed class BakeCap
{
    private int max;
    private readonly Queue<int> ids = new Queue<int>();

    public BakeCap(int max)
    {
        this.max = max < 1 ? 1 : max;
    }

    public int Count => ids.Count;

    public int Max => max;

    public int? Register(int instanceId)
    {
        ids.Enqueue(instanceId);
        if (ids.Count <= max)
        {
            return null;
        }

        return ids.Dequeue();
    }

    public List<int> SetMax(int newMax)
    {
        max = newMax < 1 ? 1 : newMax;
        var evicted = new List<int>();
        while (ids.Count > max)
        {
            evicted.Add(ids.Dequeue());
        }

        return evicted;
    }
}
