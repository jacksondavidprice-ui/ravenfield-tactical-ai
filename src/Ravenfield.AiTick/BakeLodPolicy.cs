namespace Ravenfield.AiTick;

/// <summary>
/// Screen-relative cull heights for baked remains. Unity LODGroups hide the
/// object when its bounding box is smaller than this fraction of the screen.
/// </summary>
public static class BakeLodPolicy
{
    public const float DefaultCorpseCullHeight = 0.04f;
    public const float DefaultWreckCullHeight = 0.03f;

    public static float ResolveCullHeight(float configured, float fallback)
    {
        return configured <= 0f ? fallback : configured;
    }
}
