using HarmonyLib;
using UnityEngine;

namespace Ravenfield.AiTick;

/// <summary>
/// In-game settings panel. Ravenfield's own Options UI cannot take extra tabs
/// (Game / Input / Video are hardcoded), so this overlay is the live control.
/// </summary>
internal static class SettingsOverlay
{
    internal static bool Open;

    private static int tab;
    private static Rect window = new Rect(40f, 40f, 520f, 640f);
    private static Vector2 scroll;
    private static bool patchedCursor;

    internal static void Initialize(Harmony harmony)
    {
        var fps = AccessTools.TypeByName("FpsActorController");
        var cursorFree = fps is null ? null : AccessTools.Method(fps, "IsCursorFree");
        if (cursorFree is null)
        {
            return;
        }

        harmony.Patch(cursorFree, postfix: new HarmonyMethod(typeof(SettingsOverlay), nameof(CursorFreePostfix)));
        patchedCursor = true;
    }

    internal static void Tick()
    {
        if (PluginConfig.OverlayKey == null)
        {
            return;
        }

        if (Input.GetKeyDown(PluginConfig.OverlayKey.Value))
        {
            Open = !Open;
        }
    }

    internal static void Draw()
    {
        if (!Open)
        {
            return;
        }

        var scale = Mathf.Max(1f, Screen.height / 1080f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        window = GUILayout.Window(0x74696B, window, DrawWindow, "AI Tick settings");
        GUI.matrix = Matrix4x4.identity;
    }

    private static void CursorFreePostfix(ref bool __result)
    {
        if (Open)
        {
            __result = true;
        }
    }

    private static void DrawWindow(int id)
    {
        GUILayout.Label(patchedCursor
            ? "Changes apply immediately. Mouse look is off while this is open."
            : "Changes apply immediately.");
        GUILayout.BeginHorizontal();
        TabButton(0, "AI");
        TabButton(1, "Fire");
        TabButton(2, "Remains");
        TabButton(3, "Tactics");
        GUILayout.EndHorizontal();

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(520f));
        if (tab == 0)
        {
            DrawAi();
        }
        else if (tab == 1)
        {
            DrawFire();
        }
        else if (tab == 2)
        {
            DrawRemains();
        }
        else
        {
            DrawTactics();
        }

        GUILayout.EndScrollView();
        if (GUILayout.Button("Close (" + PluginConfig.OverlayKey.Value + ")"))
        {
            Open = false;
        }

        GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
    }

    private static void TabButton(int index, string label)
    {
        var pressed = tab == index
            ? GUILayout.Button("[ " + label + " ]")
            : GUILayout.Button(label);
        if (pressed)
        {
            tab = index;
        }
    }

    private static void DrawAi()
    {
        IntSlider(PluginConfig.MaxTicks, "AI ticks per frame", 50, 1000);
        FloatSlider(PluginConfig.TickRateHz, "Think rate (Hz)", 5f, 30f);
        IntSlider(PluginConfig.MaxInteractions, "Sight pairs per frame", 200, 8000);
        FloatSlider(PluginConfig.PassFrames, "Sight pass frames", 4f, 42f);
        FloatSlider(PluginConfig.FovDot, "Sight FOV dot", -0.2f, 0.65f);
        IntSlider(PluginConfig.SightSamples, "Sight rays per pair", 1, 2);
    }

    private static void DrawFire()
    {
        Toggle(PluginConfig.ConserveAmmo, "Conserve ammo (no full auto past CQB)");
        FloatSlider(PluginConfig.CqbRange, "CQB range (m)", 10f, 80f);
        FloatSlider(PluginConfig.MidRange, "Mid range (m)", 30f, 160f);
        IntSlider(PluginConfig.MidBurst, "Mid burst shots", 1, 8);
        IntSlider(PluginConfig.LongBurst, "Long burst shots", 1, 4);
        FloatSlider(PluginConfig.MidPause, "Mid pause (s)", 0.1f, 1.5f);
        FloatSlider(PluginConfig.LongPause, "Long pause (s)", 0.1f, 2f);
    }

    private static void DrawTactics()
    {
        Toggle(PluginConfig.BoundingOverwatch, "Bounding overwatch (AI attack squads)");
        IntSlider(PluginConfig.MinimumTacticalSquadSize, "Minimum tactical squad size", 4, 12);
        FloatSlider(PluginConfig.BoundDistance, "Short bound distance (m)", 6f, 20f);
        FloatSlider(PluginConfig.BoundTimeout, "Bound failure timeout (s)", 3f, 12f);
        if (!BoundingOverwatchPatch.Available)
        {
            GUILayout.Label("Bounding overwatch is unavailable for this Ravenfield version.");
        }

        GUILayout.Space(8f);
        Toggle(PluginConfig.ExtendedDefensiveCover, "Extended defensive cover");
        if (!DefensiveCoverPatch.Available)
        {
            GUILayout.Label("Unavailable for this Ravenfield version. Vanilla movement is active.");
        }
        else
        {
            GUILayout.Label("On: engaged or suppressed infantry can seek nearby cover anywhere.");
        }

        GUILayout.Space(8f);
        Toggle(PluginConfig.OneHitInfantry, "One-hit infantry firearm damage");
        Toggle(PluginConfig.OneHitAffectsPlayer, "One-hit damage affects player");
        FloatSlider(
            PluginConfig.HandgunNonFatalBeyondMeters,
            "Handgun vanilla-damage threshold (m)",
            10f,
            100f);
        if (!InfantryLethalityPatch.Available)
        {
            GUILayout.Label("Infantry lethality patch is unavailable for this Ravenfield version.");
        }

        GUILayout.Space(8f);
        GUILayout.Label("Support holds and uses Ravenfield's normal target and friendly-fire checks.");
        GUILayout.Label("Maneuver advances in short bounds and prefers local cover when available.");
    }

    private static void DrawRemains()
    {
        Toggle(PluginConfig.KeepCorpses, "Keep corpses");
        IntSlider(PluginConfig.MaxCorpses, "Max corpses", 20, 800);
        Toggle(PluginConfig.KeepWrecks, "Keep wrecks");
        IntSlider(PluginConfig.MaxWrecks, "Max wrecks", 5, 120);
        Toggle(PluginConfig.FreezeRagdolls, "Freeze settled ragdolls");
        FloatSlider(PluginConfig.RagdollFreezeSpeed, "Ragdoll freeze speed", 0.05f, 1.5f);
        IntSlider(PluginConfig.MaxBloodDrops, "Blood drops per frame", 0, 80);
    }

    private static void Toggle(BepInEx.Configuration.ConfigEntry<bool> entry, string label)
    {
        var next = GUILayout.Toggle(entry.Value, label);
        if (next != entry.Value)
        {
            entry.Value = next;
        }
    }

    private static void FloatSlider(BepInEx.Configuration.ConfigEntry<float> entry, string label, float min, float max)
    {
        GUILayout.Label(label + ": " + entry.Value.ToString("0.##"));
        var next = GUILayout.HorizontalSlider(entry.Value, min, max);
        next = Mathf.Round(next * 100f) / 100f;
        if (!Mathf.Approximately(next, entry.Value))
        {
            entry.Value = next;
        }
    }

    private static void IntSlider(BepInEx.Configuration.ConfigEntry<int> entry, string label, int min, int max)
    {
        GUILayout.Label(label + ": " + entry.Value);
        var next = Mathf.RoundToInt(GUILayout.HorizontalSlider(entry.Value, min, max));
        if (next != entry.Value)
        {
            entry.Value = next;
        }
    }
}
