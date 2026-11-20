Shader "Battle/Holy Sword"
{
    Properties
    {
        _TintColor ("Tint", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Intensity ("Intensity", Range(0, 3)) = 0.85
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _TintColor;
            float _Opacity, _Intensity;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _TintColor;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                return float4(i.color.rgb * _Intensity, i.color.a * _Opacity);
            }
            ENDCG
        }
    }
    Fallback Off
}
