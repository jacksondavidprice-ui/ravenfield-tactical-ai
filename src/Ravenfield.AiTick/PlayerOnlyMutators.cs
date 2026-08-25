using System;
using System.Collections.Generic;

namespace Ravenfield.AiTick;

/// <summary>
/// Mutators that only exist for the local player's camera or controls.
/// They should keep running every frame and are not applied to bots by design.
/// </summary>
public static class PlayerOnlyMutators
{
    public static readonly string[] DefaultKeywords =
    {
        "war thunder",
        "guntuck",
        "gun tuck",
        "fly-by",
        "flyby",
        "fly by",
        "first-person ragdoll",
        "first person ragdoll",
        "fps ragdoll",
    };

    public static bool Matches(string? name, IReadOnlyList<string> keywords)
    {
        if (string.IsNullOrEmpty(name) || keywords.Count == 0)
        {
            return false;
        }

        var haystack = name!;

        for (var i = 0; i < keywords.Count; i++)
        {
            var keyword = keywords[i];
            if (string.IsNullOrEmpty(keyword))
            {
                continue;
            }

            if (haystack.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
