Shader "VolcanoCave/Rock"
{
    Properties
    {
        _Color ("Rock tint", Color) = (0.2,0.22,0.25,1)
        _Fade ("Visibility", Range(0,1)) = 1
        _Roughness ("Roughness", Range(0,1)) = 0.85
        _VertexPalette ("Use vertex palette", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        fixed4 _Color;
        float _Fade, _Roughness, _VertexPalette;
        struct Input { float3 worldPos; float4 screenPos; float4 color : COLOR; };
        float hash(float3 p) { return frac(sin(dot(p,float3(12.9898,78.233,37.719)))*43758.5453); }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 px = floor(IN.screenPos.xy / IN.screenPos.w * _ScreenParams.xy);
            float threshold = frac(52.9829189*frac(dot(px,float2(0.06711056,0.00583715))));
            clip(_Fade - threshold);
            float grain=hash(floor(IN.worldPos*32));
            float band=sin(IN.worldPos.y*17+sin(IN.worldPos.x*3+IN.worldPos.z*5)*.6);
            o.Albedo=_Color.rgb*lerp(float3(1,1,1),IN.color.rgb,_VertexPalette)*(.92+grain*.15+band*.035);
            o.Smoothness=1-_Roughness;
            o.Metallic=.06;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
