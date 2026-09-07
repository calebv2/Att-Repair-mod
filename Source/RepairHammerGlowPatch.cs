using System;
using System.Reflection;
using Alta;
using HarmonyLib;
using UnityEngine;

namespace RepairHammer;

[HarmonyPatch(typeof(PhysicalMaterialPart), nameof(PhysicalMaterialPart.SetMaterial))]
internal static class RepairHammerGlowPatch
{
    private const string GlowObjectName = "Repair Hammer Glow";
    private const float EmissionIntensity = 2f;

    private static Texture2D? haloTexture;

    private static readonly FieldInfo RenderersField = typeof(PhysicalMaterialPart)
        .GetField("renderers", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(PhysicalMaterialPart).FullName, "renderers");

    private static void Postfix(PhysicalMaterialPart __instance, PhysicalMaterial physicalMaterial)
    {
        var repairAlloy = RepairAlloyMaterialRegistration.RepairAlloy;
        var isRepairAlloy = repairAlloy != null
            && physicalMaterial != null
            && physicalMaterial.Hash == repairAlloy.Hash;
        if (!Application.isBatchMode)
        {
            Core.Logger.Msg("Repair Alloy glow check: receivedMaterial="
                + (physicalMaterial == null ? "<null>" : physicalMaterial.Hash.ToString())
                + ", registeredMaterial=" + (repairAlloy == null ? "<null>" : repairAlloy.Hash.ToString())
                + ", repairAlloy=" + isRepairAlloy + ".");
        }
        if (!RepairGlowPolicy.ShouldApply(!Application.isBatchMode, isRepairAlloy))
        {
            return;
        }

        ApplyWhiteEmission(__instance);
        EnsureGlowAura(__instance);
    }

    private static void ApplyWhiteEmission(PhysicalMaterialPart materialPart)
    {
        if (RenderersField.GetValue(materialPart) is not Renderer[] renderers)
        {
            Core.Logger.Warning("Repair Alloy glow was not applied: the hammer head has no renderer array.");
            return;
        }

        var emissiveRendererCount = 0;
        foreach (var renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            var material = renderer.material;
            if (!material.HasProperty("_EmissionColor"))
            {
                continue;
            }

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white * EmissionIntensity);
            emissiveRendererCount++;
        }

        Core.Logger.Msg("Repair Alloy glow emission applied: renderers=" + renderers.Length
            + ", emissiveRenderers=" + emissiveRendererCount + ".");
    }

    private static void EnsureGlowAura(PhysicalMaterialPart materialPart)
    {
        var existing = materialPart.transform.Find(GlowObjectName);
        var glowObject = existing == null ? new GameObject(GlowObjectName) : existing.gameObject;
        if (existing == null)
        {
            glowObject.transform.SetParent(materialPart.transform, false);
        }

        var glowLight = glowObject.GetComponent<Light>() ?? glowObject.AddComponent<Light>();
        glowLight.enabled = false;

        var particles = glowObject.GetComponent<ParticleSystem>() ?? glowObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.startLifetime = RepairGlowPolicy.HaloParticleLifetimeSeconds;
        main.startSpeed = 0.01f;
        main.startSize = RepairGlowPolicy.HaloParticleSize;
        main.startColor = new Color(1f, 1f, 1f, 0.9f);
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = particles.emission;
        emission.rateOverTime = 12f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.045f;

        var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        var haloShader = Shader.Find(RepairGlowPolicy.HaloShaderName);
        if (particleRenderer == null || haloShader == null)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Core.Logger.Warning("Repair Alloy halo was not applied: the game's additive particle shader is unavailable.");
            return;
        }

        var haloMaterial = new Material(haloShader)
        {
            color = Color.white,
            mainTexture = GetHaloTexture()
        };
        if (haloMaterial.HasProperty("_TintColor"))
        {
            haloMaterial.SetColor("_TintColor", Color.white);
        }
        particleRenderer.material = haloMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, RepairGlowPolicy.HaloParticlePulseMinimumScale),
                new Keyframe(0.5f, RepairGlowPolicy.HaloParticlePulseMaximumScale),
                new Keyframe(1f, RepairGlowPolicy.HaloParticlePulseMinimumScale)));

        if (!particles.isPlaying)
        {
            particles.Play();
        }

        Core.Logger.Msg("Repair Alloy white particle aura applied to hammer head.");
    }

    private static Texture2D GetHaloTexture()
    {
        if (haloTexture != null)
        {
            return haloTexture;
        }

        var resolution = RepairGlowPolicy.HaloTextureResolution;
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            name = "Repair Hammer Soft Halo",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color[resolution * resolution];
        for (var y = 0; y < resolution; y++)
        {
            for (var x = 0; x < resolution; x++)
            {
                var horizontal = ((x + 0.5f) / resolution * 2f) - 1f;
                var vertical = ((y + 0.5f) / resolution * 2f) - 1f;
                var distance = Mathf.Sqrt((horizontal * horizontal) + (vertical * vertical));
                var alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                pixels[(y * resolution) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        haloTexture = texture;
        return haloTexture;
    }
}
