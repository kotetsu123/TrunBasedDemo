using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BattleDeathFeedback : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private BattleHitFeedback hitFeedback;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Material fadeMaterial;
    [SerializeField, Min(0f)] private float hitHoldTime = 0.25f;
    [SerializeField, Min(0.01f)] private float vanishTime = 0.35f;
    [SerializeField, Min(0.01f)] private float particleLifetime = 0.7f;
    [SerializeField, Range(1, 128)] private int particleCount = 48;
    [SerializeField] private Color particleColor = new Color(0.8f, 0.85f, 0.9f, 1f);

    private ParticleSystem _particles;
    private bool _playing;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private readonly List<FadeTarget> _fadeTargets = new List<FadeTarget>();

    private sealed class FadeTarget
    {
        public Renderer renderer;
        public Material[] originals;
        public Material[] instances;
        public Color[] colors;
        public ShadowCastingMode shadows;
    }

    public IEnumerator Play()
    {
        if (_playing || !isActiveAndEnabled || visualRoot == null
            || visualRoot == transform || !visualRoot.IsChildOf(transform)
            || !visualRoot.gameObject.activeInHierarchy)
            yield break;

        _playing = true;
        try
        {
            float elapsed = 0f;
            while (elapsed < hitHoldTime)
            {
                if (this == null || !isActiveAndEnabled || visualRoot == null) yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (hitFeedback != null) hitFeedback.enabled = false;
            EmitParticles();
            BeginFade();

            float duration = Mathf.Max(0.01f, vanishTime);
            float tail = Mathf.Max(duration, particleLifetime);
            elapsed = 0f;
            while (elapsed < tail)
            {
                if (this == null || !isActiveAndEnabled || visualRoot == null) yield break;
                float progress = Mathf.Clamp01(elapsed / duration);
                SetOpacity(1f - progress);
                if (progress >= 1f) visualRoot.gameObject.SetActive(false);
                elapsed += Time.deltaTime;
                yield return null;
            }

            visualRoot.gameObject.SetActive(false);
        }
        finally
        {
            ReleaseFadeMaterials();
            ClearParticles();
            _playing = false;
        }
    }

    private void BeginFade()
    {
        foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            var originals = renderer.sharedMaterials;
            var target = new FadeTarget
            {
                renderer = renderer,
                originals = originals,
                instances = new Material[originals.Length],
                colors = new Color[originals.Length],
                shadows = renderer.shadowCastingMode
            };
            var assigned = (Material[])originals.Clone();
            for (int i = 0; i < originals.Length; i++)
            {
                var source = originals[i];
                if (source == null) continue;
                if (source.shader.name != "Standard" && source.shader.name != "Standard (Specular setup)")
                {
                    Debug.LogWarning("[BattleDeathFeedback] Alpha fade requires a built-in Standard material: " + source.name, this);
                    continue;
                }

                var template = fadeMaterial != null && fadeMaterial.shader == source.shader ? fadeMaterial : source;
                var material = new Material(template) { name = source.name + " (Death Fade)" };
                material.CopyPropertiesFromMaterial(source);
                // Standard's Fade mode must blend alpha, not just change the color value.
                material.SetFloat("_Mode", 2f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.SetOverrideTag("RenderType", "Transparent");
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
                target.instances[i] = material;
                target.colors[i] = source.GetColor(ColorId);
                assigned[i] = material;
            }
            _fadeTargets.Add(target);
            renderer.sharedMaterials = assigned;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private void SetOpacity(float opacity)
    {
        foreach (var target in _fadeTargets)
        {
            for (int i = 0; i < target.instances.Length; i++)
            {
                if (target.instances[i] == null) continue;
                Color color = target.colors[i];
                color.a *= opacity;
                target.instances[i].SetColor(ColorId, color);
            }
        }
    }

    private void ReleaseFadeMaterials()
    {
        foreach (var target in _fadeTargets)
        {
            if (target.renderer != null)
            {
                target.renderer.sharedMaterials = target.originals;
                target.renderer.shadowCastingMode = target.shadows;
            }
            foreach (var material in target.instances)
                if (material != null) Destroy(material);
        }
        _fadeTargets.Clear();
    }

    private void EmitParticles()
    {
        if (particleMaterial == null) return;

        var bounds = new Bounds(visualRoot.position, Vector3.one * 0.5f);
        var renderers = visualRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        }

        var effect = new GameObject("DeathParticles");
        // Simulate in world space so particles drift away from the fading model.
        effect.transform.SetParent(transform, true);
        effect.transform.position = bounds.center;
        _particles = effect.AddComponent<ParticleSystem>();
        _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = Mathf.Max(0.01f, particleLifetime);
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.65f, particleLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = particleColor;
        main.maxParticles = 128;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = _particles.emission;
        emission.enabled = false;
        var shape = _particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = Vector3.Max(bounds.size, Vector3.one * 0.1f);
        var velocity = _particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var color = _particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var size = _particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        var particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = particleMaterial;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
        _particles.Play();
        _particles.Emit(Mathf.Clamp(particleCount, 1, 128));
    }

    private void OnDisable()
    {
        ReleaseFadeMaterials();
        ClearParticles();
    }

    private void ClearParticles()
    {
        if (_particles != null) Destroy(_particles.gameObject);
        _particles = null;
    }
}
