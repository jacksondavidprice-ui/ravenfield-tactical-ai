namespace Ravenfield.AiTick;

/// <summary>
/// Caps and one-time migrations for baked corpses and wrecks.
/// </summary>
public static class RemainsBudget
{
    public const int OldDefaultMaxCorpses = 120;
    public const int DefaultMaxCorpses = 600;
    public const int OldDefaultMaxWrecks = 40;
    public const int DefaultMaxWrecks = 80;

    public static int ResolveCap(int configured)
    {
        return configured < 1 ? 1 : configured;
    }

    public static int MigrateOldDefault(int configured, int oldDefault, int newDefault)
    {
        return configured == oldDefault ? newDefault : configured;
    }
}
