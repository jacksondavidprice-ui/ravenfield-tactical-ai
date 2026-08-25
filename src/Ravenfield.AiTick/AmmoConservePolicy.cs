namespace Ravenfield.AiTick;

/// <summary>
/// Range bands for bot auto-fire. 0 burst size means unrestricted full auto.
/// Matches vanilla's 30 m close-quarters threshold, then semi/burst beyond that.
/// </summary>
public static class AmmoConservePolicy
{
    public const float VanillaCqbRange = 30f;
    public const float DefaultMidRange = 80f;
    public const int DefaultMidBurst = 3;
    public const int DefaultLongBurst = 1;
    public const float DefaultMidPause = 0.4f;
    public const float DefaultLongPause = 0.55f;
    public const float DefaultEngagementReset = 1.5f;

    public static float ResolveRange(float configured, float fallback)
    {
        return configured <= 0f ? fallback : configured;
    }

    public static int ResolveBurst(int configured, int fallback)
    {
        return configured < 1 ? fallback : configured;
    }

    public static float ResolvePause(float configured, float fallback)
    {
        return configured < 0f ? fallback : configured;
    }

    public static int BurstSize(float distance, float cqbRange, float midRange, int midBurst, int longBurst)
    {
        if (distance <= cqbRange)
        {
            return 0;
        }

        if (distance <= midRange)
        {
            return midBurst;
        }

        return longBurst;
    }

    public static bool CanFireNow(float now, float pauseUntil)
    {
        return now >= pauseUntil;
    }

    public static void OnShot(
        ref int shotsThisBurst,
        ref float lastShotTime,
        ref float pauseUntil,
        float now,
        int burstSize,
        float pause,
        float engagementReset)
    {
        if (burstSize <= 0)
        {
            shotsThisBurst = 0;
            pauseUntil = 0f;
            lastShotTime = now;
            return;
        }

        if (now - lastShotTime > engagementReset)
        {
            shotsThisBurst = 0;
        }

        lastShotTime = now;
        shotsThisBurst++;
        if (shotsThisBurst < burstSize)
        {
            return;
        }

        shotsThisBurst = 0;
        pauseUntil = now + pause;
    }
}
