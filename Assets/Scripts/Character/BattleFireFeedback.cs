using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BattleFireFeedback : MonoBehaviour
{
    [SerializeField] private BaseController controller;
    [SerializeField] private SkillData fireSkill;
    [SerializeField] private Material particleMaterial;
    [SerializeField, Min(0.05f)] private float flightTime = 0.5f;
    [SerializeField, Min(0.05f)] private float impactTime = 0.3f;
    [SerializeField, Min(0.05f)] private float projectileSize = 0.65f;

    private GameObject _effect;
    private bool _playing;

    public bool MatchesSkill(SkillData skill) => skill != null && skill == fireSkill;

    public IEnumerator Play(BaseController target, Action onImpact)
    {
        if (_playing) yield break;
        if (controller == null) controller = GetComponent<BaseController>();
        if (!CanHit(target)) yield break;
        if (particleMaterial == null)
        {
            onImpact?.Invoke();
            yield break;
        }

        var effect = new GameObject("FireFeedback");
        effect.layer = gameObject.layer;
        _effect = effect;
        _playing = true;
        try
        {
            Vector3 origin = BodyCenter(controller);
            Vector3 direction = ImpactPoint(target) - origin;
            Vector3 start = origin + direction.normalized * Mathf.Min(0.65f, direction.magnitude * 0.2f)
                + Vector3.up * 0.15f;
            effect.transform.position = start;
            float duration = Mathf.Max(0.05f, flightTime);
            float tail = Mathf.Max(0.05f, impactTime);
            float size = Mathf.Max(0.05f, projectileSize);
            var glow = CreateParticles(effect, "Flame", duration + 1f, size, new Color(1f, 0.22f, 0.025f), false);
            var core = CreateParticles(effect, "Core", duration + 1f, size * 0.5f, new Color(1f, 0.85f, 0.35f), false);
            var trail = CreateParticles(effect, "Trail", tail, size * 0.42f, new Color(1f, 0.32f, 0.035f), true);
            var emission = trail.emission;
            emission.enabled = true;
            emission.rateOverTime = 75f;
            trail.Play();
            glow.Play(); glow.Emit(1);
            core.Play(); core.Emit(1);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (effect == null || !CanHit(target)) yield break;
                float progress = Mathf.Clamp01(elapsed / duration);
                effect.transform.position = Vector3.Lerp(start, ImpactPoint(target), progress)
                    + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.2f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (effect == null || !CanHit(target)) yield break;
            effect.transform.position = ImpactPoint(target);
            glow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            core.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            var sparks = CreateParticles(effect, "Impact", tail, size * 0.32f, new Color(1f, 0.55f, 0.08f), true);
            var main = sparks.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            var shape = sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.22f;
            sparks.Play(); sparks.Emit(18);
            onImpact?.Invoke();

            // Keep the impact tail independent of a target that dies at the hit.
            elapsed = 0f;
            while (elapsed < tail)
            {
                if (effect == null || this == null || !isActiveAndEnabled) yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            if (_effect == effect) ClearEffect();
        }
    }

    private ParticleSystem CreateParticles(GameObject root, string name, float lifetime, float size, Color tint, bool fading)
    {
        var child = new GameObject(name);
        child.layer = root.layer;
        child.transform.SetParent(root.transform, false);
        var particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = Mathf.Max(1f, flightTime + impactTime + 0.5f);
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = tint;
        main.maxParticles = 64;
        main.simulationSpace = fading ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        if (fading)
        {
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var scale = particles.sizeOverLifetime;
            scale.enabled = true;
            scale.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        }
        var renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    private bool CanHit(BaseController target)
    {
        return this != null && isActiveAndEnabled && IsAlive(controller) && IsAlive(target)
            && controller.data.Team != target.data.Team;
    }

    private static bool IsAlive(BaseController actor)
    {
        return actor != null && actor.gameObject.activeInHierarchy && actor.data != null
            && !actor.isDead && !actor.data.isDead && actor.data.Hp > 0;
    }

    private static Vector3 BodyCenter(BaseController actor)
    {
        var collider = actor.GetComponent<Collider>();
        return collider != null && collider.enabled ? collider.bounds.center : actor.transform.position;
    }

    private static Vector3 ImpactPoint(BaseController target)
    {
        var collider = target.GetComponent<Collider>();
        // Upper-body contact keeps the burst visible instead of buried inside a cube.
        return collider != null && collider.enabled
            ? collider.bounds.center + Vector3.up * (collider.bounds.extents.y * 0.85f)
            : target.transform.position + Vector3.up * 0.4f;
    }

    private void OnDisable() => ClearEffect();
    private void OnDestroy() => ClearEffect();

    private void ClearEffect()
    {
        if (_effect != null)
        {
            _effect.SetActive(false);
            Destroy(_effect);
        }
        _effect = null;
        _playing = false;
    }
}
