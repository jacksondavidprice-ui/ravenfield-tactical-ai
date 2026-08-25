using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Ravenfield.AiTick;

/// <summary>
/// Limits per-frame Lua Update() on mutator scripts only. Weapon and map
/// ScriptedBehaviours are left alone.
/// </summary>
internal static class MutatorUpdatePatch
{
    internal static bool SkipEmpty = true;
    internal static bool Stagger = true;
    internal static int MaxPerFrame = 10;
    internal static IReadOnlyList<string> PlayerOnlyKeywords = PlayerOnlyMutators.DefaultKeywords;

    private static FieldInfo? sourceMutator;
    private static FieldInfo? mutatorName;
    private static FieldInfo? update;
    private static FieldInfo? monitors;
    private static MethodInfo? isNil;
    private static MethodInfo? updateMonitors;
    private static readonly List<int> order = new List<int>();
    private static readonly Dictionary<int, int> indexById = new Dictionary<int, int>();
    private static BepInEx.Logging.ManualLogSource? log;
    private static int lastLoggedCount = -1;

    internal static void Initialize(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
    {
        log = logger;
        var type = AccessTools.TypeByName("Lua.ScriptedBehaviour");
        if (type is null)
        {
            logger.LogError("Lua.ScriptedBehaviour not found. Mutator gating will do nothing.");
            return;
        }

        sourceMutator = AccessTools.Field(type, "sourceMutator");
        mutatorName = AccessTools.Field(AccessTools.TypeByName("MutatorEntryData"), "name");
        update = AccessTools.Field(type, "update");
        monitors = AccessTools.Field(type, "monitors");
        updateMonitors = AccessTools.Method(type, "UpdateMonitors");
        var methodType = AccessTools.TypeByName("Lua.LuaClass+Method")
            ?? AccessTools.Inner(AccessTools.TypeByName("Lua.LuaClass"), "Method");
        isNil = methodType is null ? null : AccessTools.Method(methodType, "IsNil");

        if (sourceMutator is null || update is null || monitors is null || isNil is null || updateMonitors is null)
        {
            logger.LogError("Lua.ScriptedBehaviour fields were not what we expected. Mutator gating will do nothing.");
            return;
        }

        var prefix = new HarmonyMethod(typeof(MutatorUpdatePatch), nameof(Prefix));
        var updateMethod = AccessTools.Method(type, "Update");
        if (updateMethod is null)
        {
            logger.LogError("Lua.ScriptedBehaviour.Update not found.");
            return;
        }

        harmony.Patch(updateMethod, prefix: prefix);
        logger.LogInfo(
            $"Mutator Update gate: skipEmpty={SkipEmpty}, stagger={Stagger}, maxPerFrame={MaxPerFrame}");
    }

    private static bool Prefix(object __instance)
    {
        var mutator = sourceMutator!.GetValue(__instance);
        if (mutator is null)
        {
            return true;
        }

        if (PlayerOnlyMutators.Matches(mutatorName?.GetValue(mutator) as string, PlayerOnlyKeywords))
        {
            return true;
        }

        var updateMethod = update!.GetValue(__instance);
        var hasLua = updateMethod != null && !IsNil(updateMethod);
        var monitorList = monitors!.GetValue(__instance) as ICollection;
        var hasMonitors = monitorList != null && monitorList.Count > 0;

        if (SkipEmpty && !hasLua && !hasMonitors)
        {
            return false;
        }

        if (!hasLua || !Stagger)
        {
            return true;
        }

        var behaviour = (UnityEngine.Object)__instance;
        var index = IndexOf(behaviour);
        var count = order.Count;
        if (count != lastLoggedCount)
        {
            lastLoggedCount = count;
            log?.LogInfo($"Mutator scripts with Update(): {count}, max {MaxPerFrame} per frame");
        }

        if (MutatorUpdateGate.ShouldRunLuaUpdate(Time.frameCount, index, count, MaxPerFrame, true))
        {
            return true;
        }

        updateMonitors!.Invoke(__instance, null);
        return false;
    }

    private static bool IsNil(object method)
    {
        return (bool)isNil!.Invoke(method, Array.Empty<object>());
    }

    private static int IndexOf(UnityEngine.Object behaviour)
    {
        var id = behaviour.GetInstanceID();
        if (indexById.TryGetValue(id, out var existing))
        {
            return existing;
        }

        var index = order.Count;
        order.Add(id);
        indexById[id] = index;
        return index;
    }
}
