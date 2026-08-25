namespace Ravenfield.AiTick;

/// <summary>
/// Per-frame budget for blood-drop spawns. Vanilla can instantiate 16
/// raycasting drops per hit, which at hundreds of bots is a main-thread spike.
/// </summary>
public static class BloodDropBudget
{
    public const int VanillaMaxDropsPerDamage = 16;
    public const int DefaultMaxDropsPerFrame = 32;

    public static int ResolveMaxPerFrame(int configured)
    {
        return configured < 0 ? 0 : configured;
    }

    public static bool CanSpawn(int frame, ref int lastFrame, ref int spawnedThisFrame, int maxPerFrame)
    {
        if (maxPerFrame <= 0)
        {
            return true;
        }

        if (frame != lastFrame)
        {
            lastFrame = frame;
            spawnedThisFrame = 0;
        }

        if (spawnedThisFrame >= maxPerFrame)
        {
            return false;
        }

        spawnedThisFrame++;
        return true;
    }
}
