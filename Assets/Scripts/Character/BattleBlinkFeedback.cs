using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BattleBlinkFeedback : MonoBehaviour
{
    [SerializeField] private Material effectMaterial;
    [SerializeField, Min(0.05f)] private float duration = 0.2f;
    [SerializeField, Range(0f, 1.5f)] private float horizontalStretch = 0.65f;
    [SerializeField, Range(4, 32)] private int streakCount = 18;

    private readonly List<Burst> _bursts = new List<Burst>(4);
    private MaterialPropertyBlock _properties;
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");
    private static readonly int SourceColor = Shader.PropertyToID("_SourceColor");
    private static readonly int Progress = Shader.PropertyToID("_Progress");
    private static readonly int Arriving = Shader.PropertyToID("_Arriving");
    private static readonly int Axis = Shader.PropertyToID("_Axis");
    private static readonly int Height = Shader.PropertyToID("_OriginHeight");
    private static readonly int Stretch = Shader.PropertyToID("_Stretch");
    private static readonly int CopyOffset = Shader.PropertyToID("_CopyOffset");
    private static readonly int CopyWeight = Shader.PropertyToID("_CopyWeight");
    private static readonly int IsStreak = Shader.PropertyToID("_IsStreak");

    private sealed class Part
    {
        public Mesh mesh;
        public bool owned;
        public Matrix4x4 matrix;
        public Material[] materials;
    }

    private sealed class Burst
    {
        public readonly List<Part> parts = new List<Part>();
        public Mesh streaks;
        public Vector3 right;
        public Bounds bounds;
        public float elapsed;
        public float lifetime;
        public bool arriving;
    }

    public bool IsPlaying => _bursts.Count > 0;

    public void Emit(Transform display, bool arriving)
    {
        if (!isActiveAndEnabled || display == null || effectMaterial == null
            || effectMaterial.shader == null || !effectMaterial.shader.isSupported) return;

        var camera = Camera.main;
        var burst = new Burst
        {
            arriving = arriving,
            right = camera != null ? camera.transform.right : Vector3.right,
            lifetime = Mathf.Max(0.05f, duration)
        };
        bool hasBounds = false;
        foreach (var renderer in display.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled) continue;
            Mesh mesh = null;
            bool owned = false;
            Matrix4x4 matrix = renderer.transform.localToWorldMatrix;
            if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh != null)
            {
                mesh = new Mesh { name = "Blink Pose", hideFlags = HideFlags.DontSave };
                skin.BakeMesh(mesh, true);
                owned = true;
                // Request scale-aware local vertices, then use the same world matrix as the source.
                var localBounds = mesh.bounds;
                float scale = Mathf.Max(0.001f, Mathf.Min(Mathf.Abs(skin.transform.lossyScale.x),
                    Mathf.Abs(skin.transform.lossyScale.y), Mathf.Abs(skin.transform.lossyScale.z)));
                localBounds.Expand(renderer.bounds.size.y * 2f / scale);
                mesh.bounds = localBounds;
            }
            else if (renderer is MeshRenderer)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null) mesh = filter.sharedMesh;
            }
            if (mesh == null) continue;
            burst.parts.Add(new Part
            {
                mesh = mesh, owned = owned, matrix = matrix,
                materials = renderer.sharedMaterials
            });
            if (hasBounds) burst.bounds.Encapsulate(renderer.bounds);
            else { burst.bounds = renderer.bounds; hasBounds = true; }
        }
        if (!hasBounds) return;
        burst.streaks = CreateStreaks(burst.bounds, burst.right, camera != null ? camera.transform.up : Vector3.up);
        if (_bursts.Count == 4) { Release(_bursts[0]); _bursts.RemoveAt(0); }
        _bursts.Add(burst);
    }

    private void LateUpdate()
    {
        for (int i = _bursts.Count - 1; i >= 0; i--)
        {
            var burst = _bursts[i];
            burst.elapsed += Time.deltaTime;
            if (burst.elapsed >= burst.lifetime || effectMaterial == null)
            {
                Release(burst); _bursts.RemoveAt(i); continue;
            }
            Draw(burst);
        }
    }

    private void Draw(Burst burst)
    {
        if (_properties == null) _properties = new MaterialPropertyBlock();
        _properties.Clear();
        _properties.SetFloat(Progress, burst.elapsed / burst.lifetime);
        _properties.SetFloat(Arriving, burst.arriving ? 1f : 0f);
        _properties.SetVector(Axis, burst.right);
        _properties.SetVector(Height, new Vector4(burst.bounds.min.y, Mathf.Max(0.1f, burst.bounds.size.y), 0f, 0f));
        _properties.SetFloat(Stretch, burst.bounds.size.y * horizontalStretch);
        _properties.SetFloat(IsStreak, 0f);
        foreach (var part in burst.parts)
        {
            if (part.mesh == null) continue;
            for (int submesh = 0; submesh < part.mesh.subMeshCount; submesh++)
            {
                var source = submesh < part.materials.Length ? part.materials[submesh] : null;
                bool textured = source != null && source.HasProperty(MainTex);
                _properties.SetTexture(MainTex, textured && source.GetTexture(MainTex) != null
                    ? source.GetTexture(MainTex) : Texture2D.whiteTexture);
                Vector2 scale = textured ? source.GetTextureScale("_MainTex") : Vector2.one;
                Vector2 offset = textured ? source.GetTextureOffset("_MainTex") : Vector2.zero;
                _properties.SetVector(MainTexST, new Vector4(scale.x, scale.y, offset.x, offset.y));
                _properties.SetColor(SourceColor, source != null && source.HasProperty("_Color")
                    ? source.GetColor("_Color") : Color.white);
                // Reuse one baked pose for a sharp centre and two softer horizontal echoes.
                for (int copy = 0; copy < 3; copy++)
                {
                    _properties.SetFloat(CopyOffset, copy == 0 ? 0f : copy == 1 ? -0.045f : 0.045f);
                    _properties.SetFloat(CopyWeight, copy == 0 ? 0.7f : 0.22f);
                    DrawMesh(part.mesh, part.matrix, submesh);
                }
            }
        }
        _properties.SetTexture(MainTex, Texture2D.whiteTexture);
        _properties.SetFloat(IsStreak, 1f);
        _properties.SetFloat(CopyOffset, 0f);
        _properties.SetFloat(CopyWeight, 1f);
        DrawMesh(burst.streaks, Matrix4x4.identity, 0);
    }

    private void DrawMesh(Mesh mesh, Matrix4x4 matrix, int submesh)
    {
        Graphics.DrawMesh(mesh, matrix, effectMaterial, gameObject.layer, null, submesh,
            _properties, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
    }

    private Mesh CreateStreaks(Bounds bounds, Vector3 right, Vector3 up)
    {
        int count = Mathf.Clamp(streakCount, 4, 32);
        var vertices = new Vector3[count * 4];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[count * 6];
        float height = Mathf.Max(0.1f, bounds.size.y);
        for (int i = 0; i < count; i++)
        {
            float seed = Mathf.Repeat(Mathf.Sin(i * 17.17f + 3.1f) * 4375.8f, 1f);
            Vector3 center = bounds.center + Vector3.up * (((i + 0.5f) / count - 0.5f) * height * 0.95f)
                + right * ((seed - 0.5f) * height * 0.2f);
            Vector3 x = right * (height * Mathf.Lerp(0.2f, 0.48f, seed));
            Vector3 y = up * (height * Mathf.Lerp(0.002f, 0.005f, seed));
            int v = i * 4, t = i * 6;
            vertices[v] = center - x - y; vertices[v + 1] = center + x - y;
            vertices[v + 2] = center + x + y; vertices[v + 3] = center - x + y;
            uv[v] = Vector2.zero; uv[v + 1] = Vector2.right;
            uv[v + 2] = Vector2.one; uv[v + 3] = Vector2.up;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
            triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        var mesh = new Mesh { name = "Blink Horizontal Streaks", hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
        mesh.RecalculateBounds();
        var expanded = mesh.bounds; expanded.Expand(height * 2f); mesh.bounds = expanded;
        return mesh;
    }

    public void Clear()
    {
        foreach (var burst in _bursts) Release(burst);
        _bursts.Clear();
    }

    private void Release(Burst burst)
    {
        foreach (var part in burst.parts)
            if (part.owned && part.mesh != null) Destroy(part.mesh);
        if (burst.streaks != null) Destroy(burst.streaks);
    }

    private void OnDisable() => Clear();
    private void OnDestroy() => Clear();
}
