using HarmonyLib;
using UnityEngine;

namespace Ravenfield.AiTick;

internal static class BloodDropPatch
{
    internal static int MaxPerFrame = BloodDropBudget.DefaultMaxDropsPerFrame;

    private static int lastFrame = -1;
    private static int spawnedThisFrame;

    internal static void Initialize(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
    {
        var decal = AccessTools.TypeByName("DecalManager");
        var create = decal is null ? null : AccessTools.Method(decal, "CreateBloodDrop");
        if (create is null)
        {
            logger.LogWarning("DecalManager.CreateBloodDrop not found. Blood-drop cap disabled.");
            return;
        }

        harmony.Patch(create, prefix: new HarmonyMethod(typeof(BloodDropPatch), nameof(Prefix)));
        logger.LogInfo($"Blood-drop cap: maxPerFrame={MaxPerFrame} (0 is unlimited)");
    }

    private static bool Prefix()
    {
        return BloodDropBudget.CanSpawn(Time.frameCount, ref lastFrame, ref spawnedThisFrame, MaxPerFrame);
    }
}
