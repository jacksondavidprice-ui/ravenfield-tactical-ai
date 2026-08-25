namespace Ravenfield.AiTick;

/// <summary>
/// When a dead ragdoll is slow enough to freeze. Vanilla waits 5s then
/// Sleep()s only if hip speed is under 0.01, so bodies keep simulating.
/// </summary>
public static class RagdollSettlePolicy
{
    public const float VanillaForceSleepSeconds = 5f;
    public const float VanillaSleepSpeed = 0.01f;
    public const float DefaultFreezeSpeed = 0.35f;
    public const float DefaultMinSecondsDead = 1.25f;

    public static float ResolveFreezeSpeed(float configured)
    {
        return configured <= 0f ? DefaultFreezeSpeed : configured;
    }

    public static bool ShouldFreeze(
        bool dead,
        bool alreadyFrozen,
        bool ragdollActive,
        float secondsDead,
        float minSecondsDead,
        float hipSpeed,
        float freezeSpeed)
    {
        if (!dead || alreadyFrozen || !ragdollActive)
        {
            return false;
        }

        if (secondsDead < minSecondsDead)
        {
            return false;
        }

        return hipSpeed <= freezeSpeed;
    }
}
