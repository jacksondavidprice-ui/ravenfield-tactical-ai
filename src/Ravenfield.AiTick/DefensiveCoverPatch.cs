using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Ravenfield.AiTick;

/// <summary>
/// Lets the vanilla combat-movement coroutine consider temporary cover outside
/// CQC cells. All vanilla order, vehicle, squad, and cooldown checks remain.
/// </summary>
internal static class DefensiveCoverPatch
{
    internal static bool Enabled = true;
    internal static bool Available;

    private static BepInEx.Logging.ManualLogSource? log;
    private static FieldInfo? cqcField;

    internal static void Initialize(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
    {
        Available = false;
        log = logger;
        var ai = AccessTools.TypeByName("AiActorController");
        var aiOrders = ai is null ? null : AccessTools.Method(ai, "AiOrders");
        var moveNext = aiOrders is null ? null : AccessTools.EnumeratorMoveNext(aiOrders);
        cqcField = ai is null ? null : AccessTools.Field(ai, "isInCqcZone");
        if (moveNext is null || cqcField is null)
        {
            logger.LogError("AiOrders/isInCqcZone not found. Extended defensive cover disabled.");
            return;
        }

        try
        {
            harmony.Patch(
                moveNext,
                transpiler: new HarmonyMethod(typeof(DefensiveCoverPatch), nameof(TranspileCoverGate)));
        }
        catch (System.Exception ex)
        {
            Available = false;
            logger.LogError("Could not patch extended defensive cover. Vanilla behavior is unchanged. " + ex.Message);
            return;
        }

        if (!Available)
        {
            return;
        }

        logger.LogInfo(Enabled
            ? "Extended defensive cover is on. Bots may seek temporary cover outside CQC."
            : "Extended defensive cover is off. Ravenfield's CQC-only cover behavior is unchanged.");
    }

    public static bool UseExtendedCoverGate(bool vanillaIsInCqcZone)
    {
        return DefensiveCoverPolicy.UseExtendedCoverGate(Available, Enabled, vanillaIsInCqcZone);
    }

    private static IEnumerable<CodeInstruction> TranspileCoverGate(
        IEnumerable<CodeInstruction> instructions,
        MethodBase method)
    {
        var source = new List<CodeInstruction>(instructions);
        var matches = 0;
        for (var i = 0; i < source.Count; i++)
        {
            if (source[i].opcode == OpCodes.Ldfld && Equals(source[i].operand, cqcField))
            {
                matches++;
            }
        }

        if (matches != 1)
        {
            Available = false;
            log?.LogError(
                $"Expected one CQC cover gate in {method.Name}, found {matches}. " +
                "Extended defensive cover disabled; vanilla behavior is unchanged.");
            return source;
        }

        Available = true;
        var output = new List<CodeInstruction>(source.Count + 1);
        var gate = AccessTools.Method(typeof(DefensiveCoverPatch), nameof(UseExtendedCoverGate));
        foreach (var instruction in source)
        {
            output.Add(instruction);
            if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, cqcField))
            {
                output.Add(new CodeInstruction(OpCodes.Call, gate));
            }
        }

        return output;
    }
}
