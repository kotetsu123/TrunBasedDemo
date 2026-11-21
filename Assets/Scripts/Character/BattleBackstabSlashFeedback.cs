using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BattleBackstabSlashFeedback : MonoBehaviour
{
    [SerializeField] private Material slashMaterial;
    [SerializeField, Min(.05f)] private float duration = .16f;

    private static readonly int Opacity = Shader.PropertyToID("_Opacity");
    private static readonly Quaternion Tilt = Quaternion.AngleAxis(-35f, Vector3.forward);
    private Mesh _mesh;
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _properties;
    private Bounds _targetBounds;
    private float _elapsed;
    private bool _playing;

    public void Emit(Bounds targetBounds)
    {
        if (!isActiveAndEnabled || slashMaterial == null || slashMaterial.shader == null || !slashMaterial.shader.isSupported) return;
        if (_renderer == null)
        {
            var visual = new GameObject("Backstab Slash", typeof(MeshFilter), typeof(MeshRenderer));
            visual.layer = gameObject.layer;
            visual.transform.SetParent(transform, false);
            if (_mesh == null) _mesh = BuildSlash();
            visual.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = visual.GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = slashMaterial;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _properties = new MaterialPropertyBlock();
        }
        _targetBounds = targetBounds;
        _elapsed = 0f;
        _playing = true;
        PoseSlash(0f);
    }

    private void LateUpdate()
    {
        if (!_playing) return;
        float lifetime = Mathf.Max(.05f, duration);
        if (_elapsed >= lifetime) { StopSlash(); return; }
        PoseSlash(_elapsed / lifetime);
        _elapsed += Time.deltaTime;
    }

    private void PoseSlash(float progress)
    {
        if (_renderer == null) return;
        var camera = Camera.main;
        _renderer.enabled = camera != null;
        if (camera == null) return;
        // Draw across the visible side of the target so its body cannot hide the slash.
        Vector3 forward = camera.transform.forward;
        Vector3 depthAxis = new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z));
        float depth = Vector3.Dot(_targetBounds.extents, depthAxis) + .03f;
        Vector3 center = _targetBounds.center - forward * depth;
        float size = Mathf.Clamp(_targetBounds.size.magnitude, .8f, 2.4f) * Mathf.Lerp(.94f, 1.13f, progress);
        _renderer.transform.SetPositionAndRotation(center, camera.transform.rotation * Tilt);
        _renderer.transform.localScale = Vector3.one * size;
        _properties.SetFloat(Opacity, (1f - progress) * (1f - progress));
        _renderer.SetPropertyBlock(_properties);
    }

    private static Mesh BuildSlash()
    {
        const int segments = 8;
        var vertices = new Vector3[(segments + 1) * 3];
        var colors = new Color[vertices.Length];
        var indices = new int[segments * 12];
        Color edge = new Color(.38f, .29f, .92f, .03f);
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float taper = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI));
            int v = i * 3;
            var center = new Vector3(t - .5f, .09f * taper, 0f);
            vertices[v] = center - Vector3.up * .067f * taper;
            vertices[v + 1] = center;
            vertices[v + 2] = center + Vector3.up * .078f * taper;
            colors[v] = colors[v + 2] = edge;
            colors[v + 1] = new Color(.86f, .91f, 1f, Mathf.Sqrt(taper));
            if (i == segments) continue;
            for (int row = 0; row < 2; row++)
            {
                int a = v + row, index = i * 12 + row * 6;
                indices[index] = a;     indices[index + 1] = a + 1; indices[index + 2] = a + 3;
                indices[index + 3] = a + 1; indices[index + 4] = a + 4; indices[index + 5] = a + 3;
            }
        }
        var mesh = new Mesh { name = "Backstab Slash", hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = indices;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void StopSlash()
    {
        _playing = false;
        if (_renderer != null) _renderer.enabled = false;
    }

    private void OnDisable() => StopSlash();
    private void OnDestroy()
    {
        if (_renderer != null) Destroy(_renderer.gameObject);
        if (_mesh != null) Destroy(_mesh);
    }
}
