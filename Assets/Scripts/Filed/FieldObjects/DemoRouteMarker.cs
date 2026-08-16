using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum DemoRouteMarkerType
{
    Start,
    Tutorial,
    Chest,
    SlimeEncounter,
    Recruit,
    GroupEncounter,
    BossGate,
    Boss,
    Ending,
    Custom
}

public class DemoRouteMarker : MonoBehaviour
{
    [Header("Marker")]
    [SerializeField] private DemoRouteMarkerType markerType = DemoRouteMarkerType.Custom;
    [SerializeField] private string customLabel;

    [Header("Gizmo")]
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private float labelHeight = 1.2f;
    [SerializeField] private bool drawLineToGround = true;

    private void OnDrawGizmos()
    {
        Color markerColor = GetMarkerColor();
        Vector3 position = transform.position;

        // 灰盒阶段用球体标记关键路线点，方便在 Scene 视图里快速辨认。
        Gizmos.color = markerColor;
        Gizmos.DrawSphere(position, radius);
        Gizmos.DrawWireSphere(position, radius * 1.4f);

        if (drawLineToGround)
        {
            Gizmos.DrawLine(position, position + Vector3.down * labelHeight);
        }

#if UNITY_EDITOR
        DrawEditorLabel(position, markerColor);
#endif
    }

    private string GetLabel()
    {
        if (!string.IsNullOrWhiteSpace(customLabel))
            return customLabel;

        return markerType.ToString();
    }

    private Color GetMarkerColor()
    {
        switch (markerType)
        {
            case DemoRouteMarkerType.Start:
                return new Color(0.2f, 0.8f, 1f);
            case DemoRouteMarkerType.Tutorial:
                return new Color(0.5f, 1f, 0.3f);
            case DemoRouteMarkerType.Chest:
                return new Color(1f, 0.85f, 0.2f);
            case DemoRouteMarkerType.SlimeEncounter:
                return new Color(0.35f, 1f, 0.45f);
            case DemoRouteMarkerType.Recruit:
                return new Color(0.9f, 0.45f, 1f);
            case DemoRouteMarkerType.GroupEncounter:
                return new Color(1f, 0.45f, 0.15f);
            case DemoRouteMarkerType.BossGate:
                return new Color(1f, 0.2f, 0.2f);
            case DemoRouteMarkerType.Boss:
                return new Color(0.8f, 0f, 0f);
            case DemoRouteMarkerType.Ending:
                return new Color(1f, 1f, 1f);
            default:
                return new Color(0.7f, 0.7f, 0.7f);
        }
    }

#if UNITY_EDITOR
    private void DrawEditorLabel(Vector3 position, Color markerColor)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = markerColor
            }
        };

        // Handles 只在 Unity Editor 内编译，不会进入正式运行时构建。
        Handles.Label(position + Vector3.up * labelHeight, GetLabel(), style);
    }
#endif
}
