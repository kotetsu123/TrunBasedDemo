using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BattleMagmaFeedback : MonoBehaviour
{
    [SerializeField] private BaseController controller;
    [SerializeField] private SkillData magmaSkill;
    [SerializeField] private Material crackMaterial;
    [SerializeField] private Material columnMaterial;
    [SerializeField] private Material particleMaterial;
    [SerializeField, Min(0.05f)] private float crackTime = 0.35f;
    [SerializeField, Min(0.2f)] private float eruptionTime = 0.85f;

    private sealed class Plume
    {
        public BaseController Target;
        public GameObject Root;
        public Transform Crack;
        public Transform Column;
        public MeshRenderer CrackRenderer;
        public MeshRenderer ColumnRenderer;
        public ParticleSystem Sparks;
        public float Radius;
        public float Height;
        public bool Invalid;
    }

    private GameObject _effect;
    private readonly List<Plume> _plumes = new List<Plume>();
    private readonly List<Mesh> _meshes = new List<Mesh>();
    private MaterialPropertyBlock _properties;
    private static readonly int Opacity = Shader.PropertyToID("_Opacity");
    private static readonly int Phase = Shader.PropertyToID("_Phase");

    public bool MatchesSkill(SkillData skill) => skill != null && skill == magmaSkill;

    public Bounds GetPresentationBounds(IReadOnlyList<BaseController> targets)
    {
        Bounds result = default;
        bool found = false;
        foreach (var target in targets)
        {
            if (!IsAlive(target)) continue;
            var body = BodyBounds(target);
            float radius = Mathf.Max(0.8f, Mathf.Max(body.extents.x, body.extents.z) + 0.35f) * 2.1f;
            float height = Mathf.Max(2.8f, body.size.y * 2.5f);
            var bounds = new Bounds(new Vector3(body.center.x, body.min.y + height * 0.5f, body.center.z),
                new Vector3(radius * 2f, height, radius * 2f));
            if (!found) result = bounds; else result.Encapsulate(bounds);
            found = true;
        }
        return result;
    }

    public IEnumerator Play(IReadOnlyList<BaseController> targets, Action<IReadOnlyList<BaseController>> onImpact)
    {
        if (_effect != null) yield break;
        if (controller == null) controller = GetComponent<BaseController>();
        if (!CanDisplay() || targets == null) yield break;
        var unique = new List<BaseController>();
        foreach (var target in targets)
            if (CanTarget(target) && !unique.Contains(target)) unique.Add(target);
        if (unique.Count == 0) yield break;
        if (crackMaterial == null || columnMaterial == null
            || !crackMaterial.shader.isSupported || !columnMaterial.shader.isSupported)
        {
            onImpact?.Invoke(unique);
            yield break;
        }
        if (_properties == null) _properties = new MaterialPropertyBlock();

        var root = new GameObject("MagmaFeedback");
        root.layer = gameObject.layer;
        _effect = root;
        try
        {
            var cracks = BuildCracks();
            var column = BuildColumn();
            foreach (var target in unique) CreatePlume(root, target, cracks, column);
            float lead = Mathf.Max(0.05f, crackTime);
            float tail = Mathf.Max(0.2f, eruptionTime);
            float elapsed = 0f;
            while (elapsed < lead)
            {
                if (root == null || !CanDisplay() || !UpdateTargets()) yield break;
                float progress = Mathf.Clamp01(elapsed / lead);
                Animate(progress, 0f, elapsed, 1f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (root == null || !CanDisplay() || !UpdateTargets()) yield break;
            Animate(1f, 0.08f, lead, 1f);
            foreach (var plume in _plumes)
                if (!plume.Invalid && plume.Sparks != null)
                {
                    plume.Sparks.Play();
                    plume.Sparks.Emit(28);
                }
            var impacted = new List<BaseController>();
            foreach (var plume in _plumes) if (!plume.Invalid) impacted.Add(plume.Target);
            onImpact?.Invoke(impacted);

            elapsed = 0f;
            while (elapsed < tail)
            {
                // Impact is already resolved; lethal targets keep their eruption tail.
                if (root == null || !CanDisplay()) yield break;
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(tail * 0.55f, tail, elapsed));
                Animate(1f, Mathf.SmoothStep(0f, 1f, elapsed / 0.18f), lead + elapsed, fade);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            if (_effect == root) ClearEffect();
        }
    }

    private bool CanDisplay() => this != null && isActiveAndEnabled && IsAlive(controller);
    private bool CanTarget(BaseController target) => IsAlive(target) && target.data.Team != controller.data.Team;
    private static bool IsAlive(BaseController actor) => actor != null && actor.gameObject.activeInHierarchy
        && actor.data != null && !actor.data.isDead && actor.data.Hp > 0;

    private bool UpdateTargets()
    {
        bool any = false;
        foreach (var plume in _plumes)
        {
            if (!plume.Invalid && !CanTarget(plume.Target))
            {
                plume.Invalid = true;
                plume.Root.SetActive(false);
            }
            any |= !plume.Invalid;
        }
        return any;
    }

    private void CreatePlume(GameObject root, BaseController target, Mesh cracks, Mesh column)
    {
        var bounds = BodyBounds(target);
        var go = new GameObject("MagmaTarget");
        go.layer = root.layer;
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.025f, bounds.center.z);
        var plume = new Plume { Target = target, Root = go,
            Radius = Mathf.Max(0.8f, Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.35f),
            Height = Mathf.Max(2.8f, bounds.size.y * 2.5f) };
        _plumes.Add(plume);
        plume.CrackRenderer = CreateMesh(go, "Cracks", cracks, crackMaterial);
        plume.Crack = plume.CrackRenderer.transform;
        plume.Crack.localRotation = Quaternion.Euler(0f, _plumes.Count * 47f, 0f);
        plume.ColumnRenderer = CreateMesh(go, "FlameColumn", column, columnMaterial);
        plume.Column = plume.ColumnRenderer.transform;
        plume.Column.gameObject.SetActive(false);
        if (particleMaterial != null) plume.Sparks = CreateSparks(go, plume.Radius);
    }

    private static MeshRenderer CreateMesh(GameObject parent, string name, Mesh mesh, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.layer = parent.layer;
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private static Bounds BodyBounds(BaseController target)
    {
        var collider = target.GetComponent<Collider>();
        return collider != null && collider.enabled ? collider.bounds : new Bounds(target.transform.position, Vector3.one);
    }

    private void Animate(float crackProgress, float rise, float phase, float opacity)
    {
        foreach (var plume in _plumes)
        {
            if (plume.Invalid) continue;
            float radius = plume.Radius;
            plume.Crack.localScale = Vector3.one * radius * 2.1f
                * Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0f, 1f, crackProgress));
            plume.Column.gameObject.SetActive(rise > 0f);
            plume.Column.localScale = new Vector3(radius, plume.Height * Mathf.Max(0.025f, rise), radius);
            _properties.Clear();
            _properties.SetFloat(Phase, phase);
            _properties.SetFloat(Opacity, opacity * Mathf.Lerp(0.15f, 1f, crackProgress));
            plume.CrackRenderer.SetPropertyBlock(_properties);
            _properties.SetFloat(Opacity, opacity);
            plume.ColumnRenderer.SetPropertyBlock(_properties);
        }
    }

    private ParticleSystem CreateSparks(GameObject parent, float radius)
    {
        var go = new GameObject("MagmaSparks");
        go.layer = parent.layer;
        go.transform.SetParent(parent.transform, false);
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, Mathf.Max(0.4f, eruptionTime));
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.2f, 0.01f), new Color(1f, 0.75f, 0.1f));
        main.gravityModifier = 0.12f;
        main.maxParticles = 48;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = radius;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.15f, 0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2f;
        renderer.velocityScale = 0.04f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    private Mesh BuildCracks()
    {
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        for (int ray = 0; ray < 7; ray++)
        {
            float angle = ray * Mathf.PI * 2f / 7f;
            var forward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var side = Vector3.Cross(Vector3.up, forward);
            var points = new Vector3[6];
            for (int i = 0; i < points.Length; i++)
                points[i] = forward * (i * 0.2f) + side * (i == 0 ? 0f : Mathf.Sin(ray * 7.3f + i * 4.7f) * 0.11f);
            AddRibbon(points, 0.065f, new Color(1f, 0.18f, 0.01f), vertices, uv, colors, triangles);
            AddRibbon(points, 0.022f, new Color(1f, 0.75f, 0.12f), vertices, uv, colors, triangles);
            var branch = new[] { points[3], points[3] + forward * 0.12f + side * 0.23f,
                points[3] + forward * 0.3f + side * 0.38f };
            AddRibbon(branch, 0.035f, new Color(1f, 0.3f, 0.02f), vertices, uv, colors, triangles);
        }
        var mesh = new Mesh { name = "MagmaCracksRuntime" };
        _meshes.Add(mesh);
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddRibbon(Vector3[] points, float width, Color color, List<Vector3> vertices,
        List<Vector2> uv, List<Color> colors, List<int> triangles)
    {
        int start = vertices.Count;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 tangent = points[Mathf.Min(i + 1, points.Length - 1)] - points[Mathf.Max(0, i - 1)];
            Vector3 normal = Vector3.Cross(Vector3.up, tangent).normalized;
            float size = width * Mathf.Lerp(1f, 0.2f, i / (float)(points.Length - 1));
            vertices.Add(points[i] - normal * size); vertices.Add(points[i] + normal * size);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f));
            colors.Add(color); colors.Add(color);
            if (i == 0) continue;
            int n = start + i * 2;
            triangles.AddRange(new[] { n - 2, n, n - 1, n - 1, n, n + 1 });
        }
    }

    private Mesh BuildColumn()
    {
        const int sides = 40, rows = 10;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int row = 0; row <= rows; row++)
            for (int side = 0; side <= sides; side++)
            {
                float u = side / (float)sides, v = row / (float)rows;
                float angle = u * Mathf.PI * 2f;
                float radius = 1f - v * 0.18f;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, v, Mathf.Sin(angle) * radius));
                uv.Add(new Vector2(u, v));
                if (row == 0 || side == 0) continue;
                int n = row * (sides + 1) + side;
                triangles.AddRange(new[] { n - sides - 2, n - 1, n, n - sides - 2, n, n - sides - 1 });
            }
        var mesh = new Mesh { name = "MagmaColumnRuntime" };
        _meshes.Add(mesh);
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDisable() => ClearEffect();
    private void OnDestroy() => ClearEffect();
    private void ClearEffect()
    {
        if (_effect != null) { _effect.SetActive(false); Destroy(_effect); }
        _effect = null;
        _plumes.Clear();
        foreach (var mesh in _meshes) if (mesh != null) Destroy(mesh);
        _meshes.Clear();
    }
}
