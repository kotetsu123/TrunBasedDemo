using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BattleBurnFeedback : MonoBehaviour
{
    [SerializeField] private BaseController controller;
    [SerializeField] private SkillData burnSkill;
    [SerializeField] private Material particleMaterial;
    [SerializeField, Min(0.1f)] private float duration = 0.45f;
    [SerializeField, Range(1, 48)] private int sparkCount = 18;

    private GameObject _effect;
    private Coroutine _routine;
    private bool _bound;

    private void OnEnable()
    {
        if (controller == null) controller = GetComponent<BaseController>();
        if (controller == null) return;
        controller.OnDebuffTicked += HandleTick;
        _bound = true;
    }

    private void HandleTick(SkillData skill, int damage)
    {
        if (skill == null || skill != burnSkill || damage <= 0 || particleMaterial == null || !CanDisplay()) return;
        StopAndClear();
        _routine = StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        var root = new GameObject("BurnSparks");
        root.layer = gameObject.layer;
        _effect = root;
        try
        {
            var collider = controller.GetComponent<Collider>();
            var bounds = collider != null && collider.enabled
                ? collider.bounds : new Bounds(controller.transform.position, Vector3.one);
            root.transform.position = bounds.center;
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            float total = Mathf.Max(0.1f, duration);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(total * 0.65f, total);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.3f, 0.025f), new Color(1f, 0.8f, 0.15f));
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.35f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2f;
            renderer.velocityScale = 0.05f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var camera = Camera.main;
            Vector3 right = camera != null ? camera.transform.right : Vector3.right;
            Vector3 towardCamera = camera != null ? (camera.transform.position - bounds.center).normalized : Vector3.back;
            Vector3 front = bounds.center + towardCamera * (bounds.extents.magnitude + 0.03f);
            particles.Play();
            for (int i = 0; i < Mathf.Clamp(sparkCount, 1, 48); i++)
            {
                Vector3 position = front + right * Random.Range(-bounds.extents.x * 0.9f, bounds.extents.x * 0.9f)
                    + Vector3.up * Random.Range(-bounds.extents.y * 0.7f, bounds.extents.y * 0.9f);
                particles.Emit(new ParticleSystem.EmitParams { position = position }, 1);
            }

            float elapsed = 0f;
            while (elapsed < total)
            {
                // A lethal tick may still show sparks during the existing death hold/fade.
                if (root == null || !CanDisplay()) yield break;
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

    private bool CanDisplay() => this != null && isActiveAndEnabled && controller != null
        && controller.gameObject.activeInHierarchy;

    private void OnDisable()
    {
        if (_bound && controller != null) controller.OnDebuffTicked -= HandleTick;
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
