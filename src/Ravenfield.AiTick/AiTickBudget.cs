namespace Ravenfield.AiTick;

/// <summary>
/// Replacement values for Ravenfield's AI tick scheduler.
/// Vanilla uses a 50-tick-per-frame cap and a 5 Hz target (one think every 0.2s).
/// </summary>
public static class AiTickBudget
{
    public const int VanillaMaxTicksPerFrame = 50;
    public const int VanillaMaxInteractionUpdatesPerFrame = 200;
    public const float VanillaPeriodSeconds = 0.2f;
    public const float VanillaInteractionDivisor = 42f;
    public const float VanillaFovDot = 0.65f;
    public const int VanillaCanSeeRaycastSamples = 2;

    public static int ResolveMaxTicks(int configuredMax)
    {
        return configuredMax <= 0 ? int.MaxValue : configuredMax;
    }

    public static float ResolvePeriodSeconds(float targetHz)
    {
        return targetHz <= 0f ? VanillaPeriodSeconds : 1f / targetHz;
    }

    public static float ResolveInteractionDivisor(float frames)
    {
        return frames <= 0f ? VanillaInteractionDivisor : frames;
    }

    public static float ResolveFovDot(float dot)
    {
        if (dot < -1f || dot > 1f)
        {
            return VanillaFovDot;
        }

        return dot;
    }

    public static int ResolveSightSamples(int samples)
    {
        if (samples <= 0)
        {
            return 1;
        }

        return samples > 4 ? 4 : samples;
    }
}
