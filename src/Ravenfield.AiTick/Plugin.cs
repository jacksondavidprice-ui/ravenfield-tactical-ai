using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;

namespace Ravenfield.AiTick;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.ravenfield.tacticalai";
    public const string PluginName = "AI Tick Budget";
    public const string PluginVersion = "2.0.0";

    internal static int MaxTicks = AiTickBudget.VanillaMaxTicksPerFrame;
    internal static int MaxInteractions = AiTickBudget.VanillaMaxInteractionUpdatesPerFrame;
    internal static float Period = AiTickBudget.VanillaPeriodSeconds;
    internal static float InteractionDivisor = AiTickBudget.VanillaInteractionDivisor;
    internal static float FovDot = AiTickBudget.VanillaFovDot;
    internal static int SightSamples = AiTickBudget.VanillaCanSeeRaycastSamples;

    private void Awake()
    {
        PluginConfig.Bind(Config);
        PluginConfig.Apply();

        Logger.LogInfo(
            $"AI tick budget: maxTicks={MaxTicks}, period={Period:0.###}s, maxSightPairs={MaxInteractions}, " +
            $"sightPassFrames={InteractionDivisor:0.#}, fovDot={FovDot:0.###}, sightRays={SightSamples}");
        Logger.LogInfo("Press " + PluginConfig.OverlayKey.Value + " in-game for the AI settings panel.");

        var actorManager = AccessTools.TypeByName("ActorManager");
        if (actorManager is null)
        {
            Logger.LogError("ActorManager not found. Plugin will do nothing.");
            return;
        }

        var updateAi = AccessTools.Method(actorManager, "UpdateAI");
        var throttled = AccessTools.Method(actorManager, "AITickIsThrottled");
        var updateInteractions = AccessTools.Method(actorManager, "UpdateInteractionTime");
        var aiController = AccessTools.TypeByName("AiActorController");
        var seeFov = aiController is null ? null : AccessTools.Method(aiController, "CanSeeActorFOV");
        var pointFov = aiController is null ? null : AccessTools.Method(aiController, "CanSeePointFOV");
        if (updateAi is null || throttled is null || updateInteractions is null)
        {
            Logger.LogError("UpdateAI, AITickIsThrottled, or UpdateInteractionTime not found. Plugin will do nothing.");
            return;
        }

        var harmony = new Harmony(PluginGuid);
        var transpiler = new HarmonyMethod(typeof(Plugin), nameof(TranspileConstants));
        harmony.Patch(updateAi, transpiler: transpiler);
        harmony.Patch(throttled, transpiler: transpiler);
        harmony.Patch(
            updateInteractions,
            transpiler: new HarmonyMethod(typeof(Plugin), nameof(TranspileInteractionCap)));
        if (seeFov is null || pointFov is null)
        {
            Logger.LogWarning("CanSeeActorFOV/CanSeePointFOV not found. Sight cone stays vanilla.");
        }
        else
        {
            var fovTranspiler = new HarmonyMethod(typeof(Plugin), nameof(TranspileFovDot));
            harmony.Patch(seeFov, transpiler: fovTranspiler);
            harmony.Patch(pointFov, transpiler: fovTranspiler);
        }

        var seeCheck = AccessTools.Method(actorManager, "DoCanSeeCheck");
        if (seeCheck is null)
        {
            Logger.LogWarning("DoCanSeeCheck not found. Sight ray sample count stays vanilla.");
        }
        else
        {
            harmony.Patch(seeCheck, transpiler: new HarmonyMethod(typeof(Plugin), nameof(TranspileSightSamples)));
        }

        Logger.LogInfo("Patched ActorManager AI ticks, sight-check cap, FOV cone, and sight ray samples.");

        if (MutatorUpdatePatch.SkipEmpty || MutatorUpdatePatch.Stagger)
        {
            MutatorUpdatePatch.Initialize(harmony, Logger);
        }
        else
        {
            Logger.LogInfo("Mutator Update gate is off.");
        }

        PersistentRemains.Initialize(harmony, Logger);
        RagdollSettlePatch.Initialize(harmony, Logger);
        BloodDropPatch.Initialize(harmony, Logger);
        AmmoConservePatch.Initialize(harmony, Logger);
        DefensiveCoverPatch.Initialize(harmony, Logger);
        InfantryLethalityPatch.Initialize(harmony, Logger);
        BoundingOverwatchPatch.Initialize(harmony, Logger);
        SettingsOverlay.Initialize(harmony);
    }

    private void Update()
    {
        SettingsOverlay.Tick();
    }

    private void OnGUI()
    {
        SettingsOverlay.Draw();
    }

    public static int GetMaxTicks() => MaxTicks;

    public static int GetMaxInteractions() => MaxInteractions;

    public static float GetPeriod() => Period;

    public static float GetInteractionDivisor() => InteractionDivisor;

    public static float GetFovDot() => FovDot;

    public static int GetSightSamples() => SightSamples;

    private static IEnumerable<CodeInstruction> TranspileConstants(
        IEnumerable<CodeInstruction> instructions,
        MethodBase method)
    {
        var getMax = AccessTools.Method(typeof(Plugin), nameof(GetMaxTicks));
        var getPeriod = AccessTools.Method(typeof(Plugin), nameof(GetPeriod));
        var replaced = 0;
        foreach (var ins in instructions)
        {
            if (IsLoadInt(ins, AiTickBudget.VanillaMaxTicksPerFrame))
            {
                replaced++;
                yield return new CodeInstruction(OpCodes.Call, getMax);
                continue;
            }

            if (ins.opcode == OpCodes.Ldc_R4
                && ins.operand is float value
                && value == AiTickBudget.VanillaPeriodSeconds)
            {
                replaced++;
                yield return new CodeInstruction(OpCodes.Call, getPeriod);
                continue;
            }

            yield return ins;
        }

        if (replaced == 0)
        {
            throw new InvalidOperationException(
                "AI Tick Budget found no vanilla constants to replace in " + method.Name + ".");
        }
    }

    private static IEnumerable<CodeInstruction> TranspileInteractionCap(
        IEnumerable<CodeInstruction> instructions,
        MethodBase method)
    {
        var getMax = AccessTools.Method(typeof(Plugin), nameof(GetMaxInteractions));
        var getDivisor = AccessTools.Method(typeof(Plugin), nameof(GetInteractionDivisor));
        var replacedCap = 0;
        var replacedDivisor = 0;
        foreach (var ins in instructions)
        {
            if (IsLoadInt(ins, AiTickBudget.VanillaMaxInteractionUpdatesPerFrame))
            {
                replacedCap++;
                yield return new CodeInstruction(OpCodes.Call, getMax);
                continue;
            }

            if (ins.opcode == OpCodes.Ldc_R4
                && ins.operand is float divisor
                && divisor == AiTickBudget.VanillaInteractionDivisor)
            {
                replacedDivisor++;
                yield return new CodeInstruction(OpCodes.Call, getDivisor);
                continue;
            }

            yield return ins;
        }

        if (replacedCap == 0 || replacedDivisor == 0)
        {
            throw new InvalidOperationException(
                "AI Tick Budget found no sight-check cap or pass-frame divisor to replace in " + method.Name + ".");
        }
    }

    private static IEnumerable<CodeInstruction> TranspileFovDot(
        IEnumerable<CodeInstruction> instructions,
        MethodBase method)
    {
        var getFov = AccessTools.Method(typeof(Plugin), nameof(GetFovDot));
        var replaced = 0;
        foreach (var ins in instructions)
        {
            if (ins.opcode == OpCodes.Ldc_R4
                && ins.operand is float value
                && value == AiTickBudget.VanillaFovDot)
            {
                replaced++;
                yield return new CodeInstruction(OpCodes.Call, getFov);
                continue;
            }

            yield return ins;
        }

        if (replaced == 0)
        {
            throw new InvalidOperationException(
                "AI Tick Budget found no FOV dot constant to replace in " + method.Name + ".");
        }
    }

    private static IEnumerable<CodeInstruction> TranspileSightSamples(
        IEnumerable<CodeInstruction> instructions,
        MethodBase method)
    {
        var getSamples = AccessTools.Method(typeof(Plugin), nameof(GetSightSamples));
        var replaced = 0;
        foreach (var ins in instructions)
        {
            if (IsLoadInt(ins, AiTickBudget.VanillaCanSeeRaycastSamples))
            {
                replaced++;
                yield return new CodeInstruction(OpCodes.Call, getSamples);
                continue;
            }

            yield return ins;
        }

        if (replaced == 0)
        {
            throw new InvalidOperationException(
                "AI Tick Budget found no sight ray sample count to replace in " + method.Name + ".");
        }
    }

    private static bool IsLoadInt(CodeInstruction instruction, int value)
    {
        if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int boxed && boxed == value)
        {
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_S)
        {
            try
            {
                return Convert.ToInt32(instruction.operand) == value;
            }
            catch (Exception)
            {
                return false;
            }
        }

        switch (value)
        {
            case 0: return instruction.opcode == OpCodes.Ldc_I4_0;
            case 1: return instruction.opcode == OpCodes.Ldc_I4_1;
            case 2: return instruction.opcode == OpCodes.Ldc_I4_2;
            case 3: return instruction.opcode == OpCodes.Ldc_I4_3;
            case 4: return instruction.opcode == OpCodes.Ldc_I4_4;
            case 5: return instruction.opcode == OpCodes.Ldc_I4_5;
            case 6: return instruction.opcode == OpCodes.Ldc_I4_6;
            case 7: return instruction.opcode == OpCodes.Ldc_I4_7;
            case 8: return instruction.opcode == OpCodes.Ldc_I4_8;
            default: return false;
        }
    }
}
