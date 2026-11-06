using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BattleHealFeedback : MonoBehaviour
{
    [SerializeField] private BaseController controller;
    [SerializeField] private Material glowMaterial;
    [SerializeField] private Material starMaterial;
    [SerializeField, Min(0.3f)] private float duration = 0.95f;
    [SerializeField, Range(1, 24)] private int starCount = 8;
    [SerializeField, Min(0.05f)] private float starSize = 0.24f;

    private GameObject _effect;
    private Coroutine _routine;
    private bool _bound;

    private void OnEnable()
    {
        if (controller == null) controller = GetComponent<BaseController>();
        if (controller == null) return;
        controller.OnHealed += HandleHealed;
        _bound = true;
    }

    private void HandleHealed(int amount)
    {
        if (amount <= 0 || !CanPlay() || glowMaterial == null && starMaterial == null) return;
        StopAndClear();
        _routine = StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        var root = new GameObject("HealFeedback");
        root.layer = gameObject.layer;
        _effect = root;
        try
        {
            var collider = controller.GetComponent<Collider>();
            var bounds = collider != null && collider.enabled
                ? collider.bounds : new Bounds(controller.transform.position, Vector3.one);
            root.transform.position = bounds.center;
            float total = Mathf.Max(0.3f, duration);
            float spawnTime = Mathf.Min(0.25f, total * 0.3f);
            if (glowMaterial != null)
            {
                var glow = CreateParticles(root, "Glow", glowMaterial, total * 0.85f,
                    Mathf.Max(bounds.size.x, bounds.size.y) * 1.8f, new Color(0.12f, 1f, 0.35f, 0.3f));
                glow.Play();
                glow.Emit(new ParticleSystem.EmitParams { position = bounds.center }, 1);
            }
            var stars = starMaterial != null
                ? CreateParticles(root, "Stars", starMaterial, total - spawnTime,
                    Mathf.Max(0.05f, starSize), new Color(0.3f, 1f, 0.58f, 1f)) : null;
            if (stars != null)
            {
                var velocity = stars.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                velocity.y = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                stars.Play();
            }

            var camera = Camera.main;
            Vector3 right = camera != null ? camera.transform.right : Vector3.right;
            Vector3 forward = camera != null ? (camera.transform.position - bounds.center).normalized : Vector3.back;
            Vector3 front = bounds.center + forward * (bounds.extents.magnitude + 0.05f);
            int count = Mathf.Clamp(starCount, 1, 24);
            int emitted = 0;
            float elapsed = 0f;
            while (elapsed < total)
            {
                if (root == null || !CanPlay()) yield break;
                // Stagger camera-facing stars outside the model instead of hiding them inside it.
                while (stars != null && emitted < count && elapsed >= spawnTime * emitted / Mathf.Max(1, count - 1))
                {
                    float progress = (float)emitted / Mathf.Max(1, count - 1);
                    float side = emitted % 2 == 0 ? -1f : 1f;
                    float spread = Mathf.Lerp(0.3f, 0.72f, Mathf.Repeat(emitted * 0.618f, 1f));
                    Vector3 position = front + right * (side * spread * Mathf.Max(0.6f, bounds.size.x))
                        + Vector3.up * Mathf.Lerp(-bounds.size.y * 0.4f, bounds.size.y * 0.7f, progress);
                    stars.Emit(new ParticleSystem.EmitParams { position = position }, 1);
                    emitted++;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            if (_effect == root)
            {
                ClearEffect();
                _routine = null;
            }
        }
    }

    private ParticleSystem CreateParticles(GameObject root, string name, Material material, float lifetime, float size, Color tint)
    {
        var child = new GameObject(name);
        child.layer = root.layer;
        child.transform.SetParent(root.transform, false);
        var particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = Mathf.Max(0.05f, lifetime);
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = tint;
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0.7f, 0.55f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var scale = particles.sizeOverLifetime;
        scale.enabled = true;
        scale.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.2f, 1f), new Keyframe(0.65f, 0.8f), new Keyframe(1f, 0f)));
        var renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    private bool CanPlay()
    {
        return this != null && isActiveAndEnabled && controller != null && controller.gameObject.activeInHierarchy
            && controller.data != null && !controller.isDead && !controller.data.isDead && controller.data.Hp > 0;
    }

    private void OnDisable()
    {
        if (_bound && controller != null) controller.OnHealed -= HandleHealed;
        _bound = false;
        StopAndClear();
    }

    private void OnDestroy() => StopAndClear();

    private void StopAndClear()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        ClearEffect();
    }

    private void ClearEffect()
    {
        if (_effect != null)
        {
            _effect.SetActive(false);
            Destroy(_effect);
        }
        _effect = null;
    }
}
