using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ravenfield.AiTick;

/// <summary>
/// Leaves a frozen visual copy of corpses and wrecks, then lets vanilla
/// respawn/destroy the live object. Physics and scripts on the copy are off.
/// </summary>
internal static class PersistentRemains
{
    internal static bool KeepCorpses = true;
    internal static bool KeepWrecks = true;
    internal static int MaxCorpses = RemainsBudget.DefaultMaxCorpses;
    internal static int MaxWrecks = RemainsBudget.DefaultMaxWrecks;
    internal static float CorpseCullHeight = BakeLodPolicy.DefaultCorpseCullHeight;
    internal static float WreckCullHeight = BakeLodPolicy.DefaultWreckCullHeight;
    internal static bool DisableShadows = true;

    private static BakeCap corpseCap = new BakeCap(RemainsBudget.DefaultMaxCorpses);
    private static BakeCap wreckCap = new BakeCap(RemainsBudget.DefaultMaxWrecks);
    private static readonly Dictionary<int, GameObject> corpses = new Dictionary<int, GameObject>();
    private static readonly Dictionary<int, GameObject> wrecks = new Dictionary<int, GameObject>();
    private static bool loggedFirstCorpse;
    private static bool loggedFirstWreck;

    private static FieldInfo? actorDead;
    private static FieldInfo? actorRagdoll;
    private static FieldInfo? ragdollObject;
    private static BepInEx.Logging.ManualLogSource? log;

    internal static void Initialize(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
    {
        log = logger;
        corpseCap = new BakeCap(MaxCorpses);
        wreckCap = new BakeCap(MaxWrecks);

        var actor = AccessTools.TypeByName("Actor");
        var vehicle = AccessTools.TypeByName("Vehicle");
        var raggy = AccessTools.TypeByName("ActiveRaggy");
        if (actor is null || vehicle is null || raggy is null)
        {
            logger.LogError("Actor/Vehicle/ActiveRaggy not found. Persistent remains disabled.");
            return;
        }

        actorDead = AccessTools.Field(actor, "dead");
        actorRagdoll = AccessTools.Field(actor, "ragdoll");
        ragdollObject = AccessTools.Field(raggy, "ragdollObject");

        var spawnAt = AccessTools.Method(actor, "SpawnAt");
        var cleanup = AccessTools.Method(vehicle, "Cleanup");
        if (spawnAt is null || cleanup is null || actorDead is null || actorRagdoll is null || ragdollObject is null)
        {
            logger.LogError("SpawnAt/Cleanup or ragdoll fields missing. Persistent remains disabled.");
            return;
        }

        harmony.Patch(spawnAt, prefix: new HarmonyMethod(typeof(PersistentRemains), nameof(SpawnAtPrefix)));
        harmony.Patch(cleanup, prefix: new HarmonyMethod(typeof(PersistentRemains), nameof(CleanupPrefix)));
        logger.LogInfo(
            $"Persistent remains: corpses={KeepCorpses} cap={MaxCorpses} cull={CorpseCullHeight:0.###}, " +
            $"wrecks={KeepWrecks} cap={MaxWrecks} cull={WreckCullHeight:0.###}, shadows={!DisableShadows}");
    }

    internal static void ApplyLiveCaps()
    {
        Evict(corpseCap.SetMax(MaxCorpses), corpses);
        Evict(wreckCap.SetMax(MaxWrecks), wrecks);
    }

    private static void Evict(List<int> ids, Dictionary<int, GameObject> map)
    {
        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            if (map.TryGetValue(id, out var old))
            {
                map.Remove(id);
                DestroyBaked(old);
            }
        }
    }

    private static void SpawnAtPrefix(object __instance)
    {
        if (!KeepCorpses || actorDead is null)
        {
            return;
        }

        try
        {
            if (!(bool)actorDead.GetValue(__instance))
            {
                return;
            }

            var ragdoll = actorRagdoll!.GetValue(__instance);
            if (ragdoll is null)
            {
                return;
            }

            var source = ragdollObject!.GetValue(ragdoll) as GameObject;
            if (source is null)
            {
                return;
            }

            Bake(source, corpseCap, corpses, BakeLodPolicy.ResolveCullHeight(CorpseCullHeight, BakeLodPolicy.DefaultCorpseCullHeight));
            if (!loggedFirstCorpse)
            {
                loggedFirstCorpse = true;
                log?.LogInfo($"First corpse baked at world root. Later bakes are silent. held={corpses.Count}");
            }
        }
        catch (System.Exception ex)
        {
            log?.LogWarning("Failed to bake a corpse: " + ex.Message);
        }
    }

    private static void CleanupPrefix(object __instance)
    {
        if (!KeepWrecks)
        {
            return;
        }

        try
        {
            var source = ((Component)__instance).gameObject;
            Bake(source, wreckCap, wrecks, BakeLodPolicy.ResolveCullHeight(WreckCullHeight, BakeLodPolicy.DefaultWreckCullHeight));
            if (!loggedFirstWreck)
            {
                loggedFirstWreck = true;
                log?.LogInfo($"First wreck baked at world root. Later bakes are silent. held={wrecks.Count}");
            }
        }
        catch (System.Exception ex)
        {
            log?.LogWarning("Failed to bake a wreck: " + ex.Message);
        }
    }

    private static Transform? holder;

    private static Transform Holder()
    {
        if (holder == null)
        {
            holder = new GameObject("BakedRemains").transform;
        }

        return holder;
    }

    private static void Bake(GameObject source, BakeCap cap, Dictionary<int, GameObject> map, float cullHeight)
    {
        var worldPos = source.transform.position;
        var worldRot = source.transform.rotation;
        var worldScale = source.transform.lossyScale;
        var clone = Object.Instantiate(source, worldPos, worldRot, Holder());
        clone.transform.SetParent(Holder(), true);
        clone.transform.position = worldPos;
        clone.transform.rotation = worldRot;
        clone.transform.localScale = worldScale;
        FreezeClone(clone);
        clone.SetActive(true);
        ApplyLod(clone, cullHeight);
        var id = clone.GetInstanceID();
        map[id] = clone;
        var evict = cap.Register(id);
        if (evict is int oldId && map.TryGetValue(oldId, out var old))
        {
            map.Remove(oldId);
            DestroyBaked(old);
        }
    }

    private static void DestroyBaked(GameObject old)
    {
        var filters = old.GetComponentsInChildren<MeshFilter>(true);
        for (var i = 0; i < filters.Length; i++)
        {
            var mesh = filters[i].sharedMesh;
            if (mesh != null && mesh.name == "BakedRemainMesh")
            {
                Object.Destroy(mesh);
            }
        }

        Object.Destroy(old);
    }

    private static void FreezeClone(GameObject clone)
    {
        clone.name = "BakedRemain";
        var behaviours = clone.GetComponentsInChildren<MonoBehaviour>(true);
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
            {
                Object.DestroyImmediate(behaviours[i]);
            }
        }

        var animators = clone.GetComponentsInChildren<Animator>(true);
        for (var i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                Object.DestroyImmediate(animators[i]);
            }
        }

        foreach (var body in clone.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(body);
        }

        foreach (var collider in clone.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        foreach (var particles in clone.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.gameObject.SetActive(false);
        }

        foreach (var audio in clone.GetComponentsInChildren<AudioSource>(true))
        {
            audio.enabled = false;
        }

        ReplaceSkinnedWithStatic(clone);
        CheapenRenderers(clone);
        StripNonRenderingTransforms(clone);
    }

    private static void ReplaceSkinnedWithStatic(GameObject clone)
    {
        var skinned = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (var i = 0; i < skinned.Length; i++)
        {
            var src = skinned[i];
            if (src == null || src.sharedMesh == null)
            {
                continue;
            }

            var mesh = new Mesh { name = "BakedRemainMesh" };
            src.BakeMesh(mesh);
            var go = src.gameObject;
            var filter = go.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = go.AddComponent<MeshFilter>();
            }

            filter.sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = go.AddComponent<MeshRenderer>();
            }

            renderer.sharedMaterials = src.sharedMaterials;
            src.enabled = false;
            Object.DestroyImmediate(src);
        }
    }

    private static void CheapenRenderers(GameObject clone)
    {
        var renderers = clone.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = true;
            if (DisableShadows)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }

    private static void StripNonRenderingTransforms(GameObject clone)
    {
        var keep = new HashSet<Transform>();
        var renderers = clone.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            var t = renderer.transform;
            while (t != null)
            {
                keep.Add(t);
                if (t.gameObject == clone)
                {
                    break;
                }

                t = t.parent;
            }
        }

        var transforms = clone.GetComponentsInChildren<Transform>(true);
        for (var i = transforms.Length - 1; i >= 0; i--)
        {
            var t = transforms[i];
            if (t == null || t.gameObject == clone || keep.Contains(t))
            {
                continue;
            }

            Object.DestroyImmediate(t.gameObject);
        }
    }

    private static void ApplyLod(GameObject clone, float cullHeight)
    {
        var found = clone.GetComponentsInChildren<Renderer>(true);
        var visible = new List<Renderer>(found.Length);
        for (var i = 0; i < found.Length; i++)
        {
            var renderer = found[i];
            if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            visible.Add(renderer);
        }

        if (visible.Count == 0)
        {
            return;
        }

        var lod = clone.AddComponent<LODGroup>();
        lod.fadeMode = LODFadeMode.None;
        lod.SetLODs(new[] { new LOD(cullHeight, visible.ToArray()) });
        lod.RecalculateBounds();
    }
}
