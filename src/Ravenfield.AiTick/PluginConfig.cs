using System;
using BepInEx.Configuration;
using UnityEngine;

namespace Ravenfield.AiTick;

/// <summary>
/// BepInEx config entries. Apply() copies them into the live statics the
/// patches read, so the F8 panel can change values during a match.
/// </summary>
internal static class PluginConfig
{
    internal static ConfigEntry<int> MaxTicks = null!;
    internal static ConfigEntry<float> TickRateHz = null!;
    internal static ConfigEntry<int> MaxInteractions = null!;
    internal static ConfigEntry<float> PassFrames = null!;
    internal static ConfigEntry<float> FovDot = null!;
    internal static ConfigEntry<int> SightSamples = null!;
    internal static ConfigEntry<bool> ExtendedDefensiveCover = null!;
    internal static ConfigEntry<bool> BoundingOverwatch = null!;
    internal static ConfigEntry<int> MinimumTacticalSquadSize = null!;
    internal static ConfigEntry<float> BoundDistance = null!;
    internal static ConfigEntry<float> BoundTimeout = null!;

    internal static ConfigEntry<bool> OneHitInfantry = null!;
    internal static ConfigEntry<bool> OneHitAffectsPlayer = null!;
    internal static ConfigEntry<float> HandgunNonFatalBeyondMeters = null!;

    internal static ConfigEntry<bool> ConserveAmmo = null!;
    internal static ConfigEntry<float> CqbRange = null!;
    internal static ConfigEntry<float> MidRange = null!;
    internal static ConfigEntry<int> MidBurst = null!;
    internal static ConfigEntry<int> LongBurst = null!;
    internal static ConfigEntry<float> MidPause = null!;
    internal static ConfigEntry<float> LongPause = null!;

    internal static ConfigEntry<bool> KeepCorpses = null!;
    internal static ConfigEntry<int> MaxCorpses = null!;
    internal static ConfigEntry<bool> KeepWrecks = null!;
    internal static ConfigEntry<int> MaxWrecks = null!;
    internal static ConfigEntry<bool> FreezeRagdolls = null!;
    internal static ConfigEntry<float> RagdollFreezeSpeed = null!;
    internal static ConfigEntry<int> MaxBloodDrops = null!;

    internal static ConfigEntry<bool> SkipEmptyMutators = null!;
    internal static ConfigEntry<bool> StaggerMutators = null!;
    internal static ConfigEntry<int> MutatorMaxPerFrame = null!;
    internal static ConfigEntry<string> PlayerOnlyKeywords = null!;

    internal static ConfigEntry<float> CorpseCullHeight = null!;
    internal static ConfigEntry<float> WreckCullHeight = null!;
    internal static ConfigEntry<bool> DisableShadows = null!;

    internal static ConfigEntry<KeyCode> OverlayKey = null!;

    internal static void Bind(ConfigFile config)
    {
        MaxTicks = config.Bind(
            "AI",
            "MaxTicksPerFrame",
            500,
            "Ceiling on bot AI coroutine ticks per frame. Vanilla is 50. Set 0 for no cap.");
        TickRateHz = config.Bind(
            "AI",
            "TargetTickRateHz",
            15f,
            "How often each bot should think, in Hertz. Vanilla is 5.");
        MaxInteractions = config.Bind(
            "AI",
            "MaxInteractionUpdatesPerFrame",
            4000,
            "Enemy-pair sight checks per frame. Vanilla is 200. 0 means no cap.");
        PassFrames = config.Bind(
            "AI",
            "InteractionPassFrames",
            12f,
            "Frames to finish every enemy pair. Vanilla is 42. Smaller is faster notice.");
        FovDot = config.Bind(
            "AI",
            "SightFovDot",
            0.2f,
            "Minimum facing dot to count as in FOV. Vanilla is 0.65. Valid range is -1 to 1.");
        SightSamples = config.Bind(
            "AI",
            "SightRaySamples",
            1,
            "Raycasts per enemy pair. Vanilla is 2.");
        ExtendedDefensiveCover = config.Bind(
            "Tactics",
            "ExtendedDefensiveCover",
            true,
            "Let suppressed or engaged infantry seek Ravenfield's temporary cover outside CQC areas. Turn off for vanilla movement.");
        BoundingOverwatch = config.Bind(
            "Tactics",
            "BoundingOverwatch",
            true,
            "Split AI-led attack squads into alternating support and maneuver elements.");
        MinimumTacticalSquadSize = config.Bind(
            "Tactics",
            "MinimumSquadSize",
            BoundingOverwatchPolicy.HardMinimumSquadSize,
            "Smallest live infantry squad that may use bounding overwatch. The hard minimum is four.");
        BoundDistance = config.Bind(
            "Tactics",
            "BoundDistanceMeters",
            BoundingOverwatchPolicy.DefaultBoundDistance,
            "Forward distance for one maneuver element's short bound.");
        BoundTimeout = config.Bind(
            "Tactics",
            "BoundTimeoutSeconds",
            BoundingOverwatchPolicy.DefaultPhaseTimeout,
            "Maximum time before support and maneuver elements swap roles.");

        OneHitInfantry = config.Bind(
            "Damage",
            "OneHitInfantry",
            true,
            "Make ordinary direct rifle, SMG, semi-auto rifle, sniper, shotgun, and close handgun hits lethal.");
        OneHitAffectsPlayer = config.Bind(
            "Damage",
            "OneHitAffectsPlayer",
            true,
            "Apply infantry one-hit lethality to the player as well as bots.");
        HandgunNonFatalBeyondMeters = config.Bind(
            "Damage",
            "HandgunNonFatalBeyondMeters",
            LethalityPolicy.DefaultHandgunNonFatalBeyondMeters,
            "Do not force handgun hits to be lethal at or beyond this distance.");

        ConserveAmmo = config.Bind(
            "Fire",
            "ConserveAmmo",
            true,
            "Bots stop spraying automatic weapons at range. Full auto only inside CQB.");
        CqbRange = config.Bind(
            "Fire",
            "CqbRange",
            AmmoConservePolicy.VanillaCqbRange,
            "Inside this distance (meters) bots may full-auto.");
        MidRange = config.Bind(
            "Fire",
            "MidRange",
            AmmoConservePolicy.DefaultMidRange,
            "Between CQB and this distance bots fire short bursts.");
        MidBurst = config.Bind(
            "Fire",
            "MidBurst",
            AmmoConservePolicy.DefaultMidBurst,
            "Shots per burst at mid range.");
        LongBurst = config.Bind(
            "Fire",
            "LongBurst",
            AmmoConservePolicy.DefaultLongBurst,
            "Shots per burst beyond mid range. 1 is semi-auto.");
        MidPause = config.Bind(
            "Fire",
            "MidPause",
            AmmoConservePolicy.DefaultMidPause,
            "Seconds to wait between mid-range bursts.");
        LongPause = config.Bind(
            "Fire",
            "LongPause",
            AmmoConservePolicy.DefaultLongPause,
            "Seconds to wait between long-range shots.");

        KeepCorpses = config.Bind(
            "Remains",
            "KeepCorpses",
            true,
            "Leave a frozen visual copy of a body when that actor respawns.");
        MaxCorpses = config.Bind(
            "Remains",
            "MaxCorpses",
            RemainsBudget.DefaultMaxCorpses,
            "Oldest baked corpses are removed past this count.");
        if (MaxCorpses.Value == RemainsBudget.OldDefaultMaxCorpses)
        {
            MaxCorpses.Value = RemainsBudget.DefaultMaxCorpses;
        }

        KeepWrecks = config.Bind(
            "Remains",
            "KeepWrecks",
            true,
            "Leave a frozen visual copy of a vehicle when vanilla would destroy it.");
        MaxWrecks = config.Bind(
            "Remains",
            "MaxWrecks",
            RemainsBudget.DefaultMaxWrecks,
            "Oldest baked wrecks are removed past this count.");
        if (MaxWrecks.Value == RemainsBudget.OldDefaultMaxWrecks)
        {
            MaxWrecks.Value = RemainsBudget.DefaultMaxWrecks;
        }

        CorpseCullHeight = config.Bind(
            "Remains",
            "CorpseCullScreenHeight",
            BakeLodPolicy.DefaultCorpseCullHeight,
            "Hide a baked body when its on-screen height drops below this fraction.");
        WreckCullHeight = config.Bind(
            "Remains",
            "WreckCullScreenHeight",
            BakeLodPolicy.DefaultWreckCullHeight,
            "Hide a baked wreck when its on-screen height drops below this fraction.");
        DisableShadows = config.Bind(
            "Remains",
            "DisableShadows",
            true,
            "Baked remains do not cast or receive shadows.");
        FreezeRagdolls = config.Bind(
            "Remains",
            "FreezeSettledRagdolls",
            true,
            "When a dead ragdoll stops moving, freeze its physics until that actor respawns.");
        RagdollFreezeSpeed = config.Bind(
            "Remains",
            "RagdollFreezeSpeed",
            RagdollSettlePolicy.DefaultFreezeSpeed,
            "Hip speed at or below this freezes a dead ragdoll.");
        MaxBloodDrops = config.Bind(
            "Perf",
            "MaxBloodDropsPerFrame",
            BloodDropBudget.DefaultMaxDropsPerFrame,
            "New blood-drop objects allowed per frame. 0 is unlimited.");

        SkipEmptyMutators = config.Bind(
            "Mutators",
            "SkipEmptyUpdates",
            false,
            "Skip Unity Update on mutator scripts that have no Lua update().");
        StaggerMutators = config.Bind(
            "Mutators",
            "StaggerUpdates",
            false,
            "Round-robin mutator Lua update() calls. Leave off for visual mutators.");
        MutatorMaxPerFrame = config.Bind(
            "Mutators",
            "MaxUpdatesPerFrame",
            10,
            "How many mutator Lua update() scripts may run on one frame when staggering is on.");
        PlayerOnlyKeywords = config.Bind(
            "Mutators",
            "PlayerOnlyNameKeywords",
            string.Join(", ", PlayerOnlyMutators.DefaultKeywords),
            "Mutator names containing these substrings keep running every frame.");

        OverlayKey = config.Bind(
            "Overlay",
            "ToggleKey",
            KeyCode.F8,
            "Opens the in-game AI settings panel. Changes apply immediately and are saved.");

        Watch(MaxTicks);
        Watch(TickRateHz);
        Watch(MaxInteractions);
        Watch(PassFrames);
        Watch(FovDot);
        Watch(SightSamples);
        Watch(ExtendedDefensiveCover);
        Watch(BoundingOverwatch);
        Watch(MinimumTacticalSquadSize);
        Watch(BoundDistance);
        Watch(BoundTimeout);
        Watch(OneHitInfantry);
        Watch(OneHitAffectsPlayer);
        Watch(HandgunNonFatalBeyondMeters);
        Watch(ConserveAmmo);
        Watch(CqbRange);
        Watch(MidRange);
        Watch(MidBurst);
        Watch(LongBurst);
        Watch(MidPause);
        Watch(LongPause);
        Watch(KeepCorpses);
        Watch(MaxCorpses);
        Watch(KeepWrecks);
        Watch(MaxWrecks);
        Watch(CorpseCullHeight);
        Watch(WreckCullHeight);
        Watch(DisableShadows);
        Watch(FreezeRagdolls);
        Watch(RagdollFreezeSpeed);
        Watch(MaxBloodDrops);
        Watch(SkipEmptyMutators);
        Watch(StaggerMutators);
        Watch(MutatorMaxPerFrame);
        Watch(PlayerOnlyKeywords);
    }

    internal static void Apply()
    {
        Plugin.MaxTicks = AiTickBudget.ResolveMaxTicks(MaxTicks.Value);
        Plugin.MaxInteractions = AiTickBudget.ResolveMaxTicks(MaxInteractions.Value);
        Plugin.Period = AiTickBudget.ResolvePeriodSeconds(TickRateHz.Value);
        Plugin.InteractionDivisor = AiTickBudget.ResolveInteractionDivisor(PassFrames.Value);
        Plugin.FovDot = AiTickBudget.ResolveFovDot(FovDot.Value);
        Plugin.SightSamples = AiTickBudget.ResolveSightSamples(SightSamples.Value);
        DefensiveCoverPatch.Enabled = ExtendedDefensiveCover.Value;
        BoundingOverwatchPatch.SetEnabled(BoundingOverwatch.Value);
        BoundingOverwatchPatch.MinimumSquadSize = Math.Max(
            BoundingOverwatchPolicy.HardMinimumSquadSize,
            MinimumTacticalSquadSize.Value);
        BoundingOverwatchPatch.BoundDistance = BoundingOverwatchPolicy.ResolveBoundDistance(BoundDistance.Value);
        BoundingOverwatchPatch.PhaseTimeout = BoundingOverwatchPolicy.ResolvePhaseTimeout(BoundTimeout.Value);
        InfantryLethalityPatch.Enabled = OneHitInfantry.Value;
        InfantryLethalityPatch.AffectPlayer = OneHitAffectsPlayer.Value;
        InfantryLethalityPatch.HandgunNonFatalBeyondMeters = float.IsNaN(HandgunNonFatalBeyondMeters.Value)
            ? LethalityPolicy.DefaultHandgunNonFatalBeyondMeters
            : Math.Max(0f, HandgunNonFatalBeyondMeters.Value);

        AmmoConservePatch.Enabled = ConserveAmmo.Value;
        AmmoConservePatch.CqbRange = AmmoConservePolicy.ResolveRange(CqbRange.Value, AmmoConservePolicy.VanillaCqbRange);
        AmmoConservePatch.MidRange = AmmoConservePolicy.ResolveRange(MidRange.Value, AmmoConservePolicy.DefaultMidRange);
        AmmoConservePatch.MidBurst = AmmoConservePolicy.ResolveBurst(MidBurst.Value, AmmoConservePolicy.DefaultMidBurst);
        AmmoConservePatch.LongBurst = AmmoConservePolicy.ResolveBurst(LongBurst.Value, AmmoConservePolicy.DefaultLongBurst);
        AmmoConservePatch.MidPause = AmmoConservePolicy.ResolvePause(MidPause.Value, AmmoConservePolicy.DefaultMidPause);
        AmmoConservePatch.LongPause = AmmoConservePolicy.ResolvePause(LongPause.Value, AmmoConservePolicy.DefaultLongPause);

        PersistentRemains.KeepCorpses = KeepCorpses.Value;
        PersistentRemains.MaxCorpses = RemainsBudget.ResolveCap(MaxCorpses.Value);
        PersistentRemains.KeepWrecks = KeepWrecks.Value;
        PersistentRemains.MaxWrecks = RemainsBudget.ResolveCap(MaxWrecks.Value);
        PersistentRemains.CorpseCullHeight = CorpseCullHeight.Value;
        PersistentRemains.WreckCullHeight = WreckCullHeight.Value;
        PersistentRemains.DisableShadows = DisableShadows.Value;
        PersistentRemains.ApplyLiveCaps();

        RagdollSettlePatch.Enabled = FreezeRagdolls.Value;
        RagdollSettlePatch.FreezeSpeed = RagdollSettlePolicy.ResolveFreezeSpeed(RagdollFreezeSpeed.Value);
        BloodDropPatch.MaxPerFrame = BloodDropBudget.ResolveMaxPerFrame(MaxBloodDrops.Value);

        MutatorUpdatePatch.SkipEmpty = SkipEmptyMutators.Value;
        MutatorUpdatePatch.Stagger = StaggerMutators.Value;
        MutatorUpdatePatch.MaxPerFrame = MutatorMaxPerFrame.Value;
        MutatorUpdatePatch.PlayerOnlyKeywords = SplitKeywords(PlayerOnlyKeywords.Value);
    }

    private static void Watch<T>(ConfigEntry<T> entry)
    {
        entry.SettingChanged += (_, __) => Apply();
    }

    private static string[] SplitKeywords(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return PlayerOnlyMutators.DefaultKeywords;
        }

        var parts = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var trimmed = new string[parts.Length];
        var n = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            var value = parts[i].Trim();
            if (value.Length > 0)
            {
                trimmed[n++] = value;
            }
        }

        if (n == parts.Length)
        {
            return trimmed;
        }

        var compact = new string[n];
        Array.Copy(trimmed, compact, n);
        return compact;
    }
}
