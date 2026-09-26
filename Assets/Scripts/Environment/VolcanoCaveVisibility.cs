using UnityEngine;

/// <summary>Fades only authored cave segments between the player and camera. No physics changes.</summary>
[DefaultExecutionOrder(100)]
public sealed class VolcanoCaveVisibility : MonoBehaviour
{
    public Renderer[] occluders;
    public Transform target;
    public Camera viewCamera;
    [Range(0,1)] public float obscuredVisibility = 0.08f;
    public float fadeSpeed = 7f;
    float[] visibility;
    MaterialPropertyBlock block;
    static readonly int Fade = Shader.PropertyToID("_Fade");

    void OnEnable()
    {
        block = new MaterialPropertyBlock();
        visibility = new float[occluders == null ? 0 : occluders.Length];
        for (int i=0;i<visibility.Length;i++) visibility[i]=1;
    }
    void LateUpdate()
    {
        if (!viewCamera) viewCamera = Camera.main;
        if (!target) { var player=GameObject.FindGameObjectWithTag("Player"); if(player) target=player.transform; }
        if (!target || !viewCamera || occluders == null) return;
        Vector3 focus=target.position+Vector3.up*.9f;
        Vector3 delta=focus-viewCamera.transform.position;
        float distance=delta.magnitude;
        if(distance<.01f) return;
        Ray ray=new Ray(viewCamera.transform.position,delta/distance);
        for(int i=0;i<occluders.Length;i++)
        {
            Renderer r=occluders[i]; if(!r) continue;
            Bounds bounds=r.bounds; bounds.Expand(.35f);
            bool hit=bounds.Contains(ray.origin) || (bounds.IntersectRay(ray,out float entry) && entry<distance);
            visibility[i]=Mathf.MoveTowards(visibility[i],hit ? obscuredVisibility : 1,Time.unscaledDeltaTime*fadeSpeed);
            r.GetPropertyBlock(block); block.SetFloat(Fade,visibility[i]); r.SetPropertyBlock(block);
        }
    }
    void OnDisable()
    {
        if(occluders==null || block==null) return;
        foreach(var r in occluders) if(r) { r.GetPropertyBlock(block); block.SetFloat(Fade,1); r.SetPropertyBlock(block); }
    }
}
