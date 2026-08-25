namespace Ravenfield.AiTick;

/// <summary>
/// Controls whether Ravenfield can use its existing temporary-cover behavior
/// outside cells that the game classifies as close-quarters combat areas.
/// </summary>
public static class DefensiveCoverPolicy
{
    public static bool UseExtendedCoverGate(bool enabled, bool vanillaIsInCqcZone)
    {
        return UseExtendedCoverGate(true, enabled, vanillaIsInCqcZone);
    }

    public static bool UseExtendedCoverGate(
        bool patchAvailable,
        bool enabled,
        bool vanillaIsInCqcZone)
    {
        return vanillaIsInCqcZone || (patchAvailable && enabled);
    }
}
