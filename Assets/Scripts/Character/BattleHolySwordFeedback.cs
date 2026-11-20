using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BattleHolySwordFeedback : MonoBehaviour
{
    [SerializeField] private BaseController controller;
    [SerializeField] private SkillData holySwordSkill;
    [SerializeField] private Material swordMaterial;
    [SerializeField] private Material particleMaterial;
    [SerializeField, Min(0.05f)] private float summonTime = 0.28f;
    [SerializeField, Min(0.05f)] private float flightTime = 0.09f;
    [SerializeField, Min(0.05f)] private float impactTime = 0.3f;
    [SerializeField, Min(0.1f)] private float swordScale = .65f;

    private const float BladeTip = 1.65f;
    private static readonly int Opacity = Shader.PropertyToID("_Opacity");
    private GameObject _effect;
    private Mesh _mesh;
    private MaterialPropertyBlock _properties;

    public bool MatchesSkill(SkillData skill) => skill != null && skill == holySwordSkill;

    public IEnumerator Play(BaseController target, Action onImpact)
    {
        if (_effect != null) yield break;
        if (controller == null) controller = GetComponent<BaseController>();
        if (!CanHit(target)) yield break;
        if (swordMaterial == null || swordMaterial.shader == null || !swordMaterial.shader.isSupported)
        {
            onImpact?.Invoke();
            yield break;
        }

        var root = new GameObject("HolySwordFeedback");
        root.layer = gameObject.layer;
        _effect = root;
        if (_properties == null) _properties = new MaterialPropertyBlock();
        try
        {
            Bounds body = BodyBounds(controller);
            Vector3 start = body.center + Vector3.up * (body.extents.y + .3f);
            Vector3 direction = Direction(ImpactPoint(target) - start);
            start += Vector3.Cross(Vector3.up, direction).normalized * .45f;
            direction = Direction(ImpactPoint(target) - start);
            float scale = Mathf.Min(Mathf.Max(.1f, swordScale),
                Mathf.Max(.1f, Vector3.Distance(start, ImpactPoint(target)) / (BladeTip + .5f)));
            var sword = new GameObject("LightSword");
            sword.layer = root.layer;
            sword.transform.SetParent(root.transform, false);
            sword.transform.SetPositionAndRotation(start, BladeRotation(direction, start));
            sword.transform.localScale = Vector3.one * scale;
            _mesh = BuildLightBlade();
            var blade = CreateBlade(sword.transform, "Blade", Vector3.one);
            var glow = CreateBlade(sword.transform, "BladeGlow", new Vector3(1.18f, 1.18f, 1.005f));
            var tip = new GameObject("SwordTip");
            tip.layer = root.layer;
            tip.transform.SetParent(sword.transform, false);
            tip.transform.localPosition = Vector3.right * BladeTip;
            var trail = tip.AddComponent<TrailRenderer>();
            trail.sharedMaterial = swordMaterial;
            trail.time = .11f; trail.minVertexDistance = .04f;
            trail.startWidth = .11f; trail.endWidth = 0f;
            trail.startColor = new Color(1f, .9f, .5f, .7f);
            trail.endColor = new Color(1f, .7f, .2f, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off; trail.receiveShadows = false;
            trail.emitting = false;

            float elapsed = 0f;
            float charge = Mathf.Max(.05f, summonTime);
            while (elapsed < charge)
            {
                if (root == null || !CanHit(target)) yield break;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / charge);
                Fade(blade, progress); Fade(glow, progress * .08f);
                sword.transform.rotation = BladeRotation(Direction(ImpactPoint(target) - start), start);
                sword.transform.localScale = Vector3.one * (scale * (.85f + .15f * progress));
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (root == null || !CanHit(target)) yield break;
            Fade(blade, 1f); Fade(glow, .08f);
            sword.transform.localScale = Vector3.one * scale;
            Vector3 launchTip = tip.transform.position;
            trail.Clear(); trail.emitting = true;
            trail.AddPosition(launchTip);
            elapsed = 0f;
            float flight = Mathf.Max(.05f, flightTime);
            while (elapsed < flight)
            {
                yield return null;
                if (root == null || !CanHit(target)) yield break;
                elapsed += Time.deltaTime;
                if (elapsed >= flight) break;
                Vector3 contact = ImpactPoint(target);
                direction = Direction(contact - start);
                Vector3 tipPosition = Vector3.Lerp(launchTip, contact, elapsed / flight);
                Quaternion rotation = BladeRotation(direction, tipPosition);
                sword.transform.SetPositionAndRotation(tipPosition - rotation * (Vector3.right * BladeTip * scale), rotation);
            }

            if (root == null || !CanHit(target)) yield break;
            Vector3 impact = ImpactPoint(target);
            direction = Direction(impact - start);
            Quaternion arrivalRotation = BladeRotation(direction, impact);
            Vector3 arrival = impact - arrivalRotation * (Vector3.right * BladeTip * scale);
            sword.transform.SetPositionAndRotation(arrival, arrivalRotation);
            trail.emitting = false;
            trail.AddPosition(impact);
            blade.enabled = glow.enabled = false;
            var flash = CreateRing(root.transform, "HolyImpact", .35f);
            var camera = Camera.main;
            flash.transform.SetPositionAndRotation(impact,
                Rotation(camera != null ? Direction(camera.transform.position - impact) : -direction));
            EmitSparks(root.transform, impact);
            onImpact?.Invoke();

            // A lethal hit still keeps its own short impact tail, independent of the target.
            elapsed = 0f;
            float tail = Mathf.Max(.05f, impactTime);
            while (elapsed < tail)
            {
                if (root == null || this == null || !isActiveAndEnabled) yield break;
                float progress = Mathf.Clamp01(elapsed / tail);
                flash.transform.localScale = Vector3.one * (.6f + progress * 1.1f);
                Fade(flash, (1f - progress) * .85f);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            if (_effect == root) ClearEffect();
        }
    }

    private MeshRenderer CreateBlade(Transform parent, string name, Vector3 scale)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.layer = gameObject.layer; go.transform.SetParent(parent, false); go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = _mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = swordMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        Fade(renderer, 0f);
        return renderer;
    }

    private LineRenderer CreateRing(Transform parent, string name, float radius)
    {
        var go = new GameObject(name, typeof(LineRenderer));
        go.layer = gameObject.layer; go.transform.SetParent(parent, false);
        var line = go.GetComponent<LineRenderer>();
        line.sharedMaterial = swordMaterial; line.useWorldSpace = false; line.loop = true;
        line.positionCount = 40; line.widthMultiplier = .035f;
        line.startColor = line.endColor = new Color(1f, .83f, .3f, 1f);
        line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
        Fade(line, 0f);
        return line;
    }

    private void EmitSparks(Transform parent, Vector3 position)
    {
        if (particleMaterial == null) return;
        var go = new GameObject("HolySparks");
        go.layer = gameObject.layer; go.transform.SetParent(parent, false); go.transform.position = position;
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false; main.loop = false; main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.15f, .3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(.07f, .14f);
        main.startColor = new Color(1f, .92f, .55f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 24;
        var emission = particles.emission; emission.enabled = false;
        var shape = particles.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .12f;
        var color = particles.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, .75f, .25f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = particleMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        particles.Play(); particles.Emit(16);
    }

    private void Fade(Renderer renderer, float alpha)
    {
        _properties.Clear(); _properties.SetFloat(Opacity, alpha); renderer.SetPropertyBlock(_properties);
    }

    private static Mesh BuildLightBlade()
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
        Color light = new Color(1f, .98f, .88f, .85f);
        Color edge = new Color(1f, .82f, .3f, .12f);
        Vector3[] outline = {
            new Vector3(BladeTip, 0, 0), new Vector3(.1f, .075f, 0),
            new Vector3(0, .46f, 0), new Vector3(-.1f, .075f, 0),
            new Vector3(-.7f, 0, 0), new Vector3(-.1f, -.075f, 0),
            new Vector3(0, -.46f, 0), new Vector3(.1f, -.075f, 0)
        };
        void Triangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            int first = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
            colors.Add(ca); colors.Add(cb); colors.Add(cc);
        }
        void Needle(Vector3 front, Vector3 back, Vector3 width, Color color)
        {
            Triangle(front, width, back, color, color, color);
            Triangle(front, back, -width, color, color, color);
        }
        for (int i = 0; i < outline.Length; i++)
            Triangle(Vector3.zero, outline[i], outline[(i + 1) % outline.Length], light, edge, edge);
        Needle(Vector3.right * 1.5f, Vector3.left * .58f, Vector3.up * .025f, light);
        Needle(Vector3.up * .38f, Vector3.down * .38f, Vector3.right * .018f, light);
        var mesh = new Mesh { name = "Holy Cross Blade Mesh", hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetColors(colors);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    private bool CanHit(BaseController target) => this != null && isActiveAndEnabled && IsAlive(controller)
        && IsAlive(target) && controller.data.Team != target.data.Team;
    private static bool IsAlive(BaseController actor) => actor != null && actor.gameObject.activeInHierarchy
        && actor.data != null && !actor.isDead && !actor.data.isDead && actor.data.Hp > 0;
    private static Bounds BodyBounds(BaseController actor)
    {
        var collider = actor.GetComponent<Collider>();
        return collider != null && collider.enabled ? collider.bounds : new Bounds(actor.transform.position, Vector3.one);
    }
    private static Vector3 ImpactPoint(BaseController target)
    { var bounds = BodyBounds(target); return bounds.center + Vector3.up * bounds.extents.y * .15f; }
    private static Vector3 Direction(Vector3 vector) => vector.sqrMagnitude > .0001f ? vector.normalized : Vector3.forward;
    private static Quaternion Rotation(Vector3 forward) => Quaternion.LookRotation(forward,
        Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > .98f ? Vector3.forward : Vector3.up);
    private static Quaternion BladeRotation(Vector3 direction, Vector3 position)
    {
        // Face the camera while pointing the long light ray toward the target on screen.
        var camera = Camera.main;
        Vector3 normal = camera != null ? Direction(camera.transform.position - position) : Vector3.up;
        Vector3 axis = Vector3.ProjectOnPlane(direction, normal);
        if (axis.sqrMagnitude < .0001f)
            axis = Vector3.ProjectOnPlane(camera != null ? camera.transform.right : Vector3.right, normal);
        if (axis.sqrMagnitude < .0001f) axis = Vector3.Cross(normal, Vector3.forward);
        return Quaternion.LookRotation(normal, Vector3.Cross(normal, axis.normalized));
    }
    private void OnDisable() => ClearEffect();
    private void OnDestroy() => ClearEffect();
    private void ClearEffect()
    {
        if (_effect != null) { _effect.SetActive(false); Destroy(_effect); }
        if (_mesh != null) Destroy(_mesh);
        _effect = null; _mesh = null;
    }
}
