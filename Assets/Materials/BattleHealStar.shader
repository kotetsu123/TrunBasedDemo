Shader "Battle/Heal Star"
{
    Properties
    {
        _TintColor ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            fixed4 _TintColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _TintColor;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = abs(i.uv * 2.0 - 1.0);
                float2 aa = max(fwidth(p), 0.006);
                float widthX = 0.008 + 0.07 * pow(saturate(1.0 - p.x), 2.0);
                float widthY = 0.008 + 0.07 * pow(saturate(1.0 - p.y), 2.0);
                float horizontal = (1.0 - smoothstep(widthX, widthX + aa.y, p.y))
                    * (1.0 - smoothstep(0.55, 1.0, p.x));
                float vertical = (1.0 - smoothstep(widthY, widthY + aa.x, p.x))
                    * (1.0 - smoothstep(0.55, 1.0, p.y));
                float center = pow(saturate(1.0 - length(p) * 4.0), 2.0);
                fixed3 color = lerp(i.color.rgb, fixed3(0.85, 1.0, 0.9), center * 0.7);
                return fixed4(color, max(horizontal, vertical) * i.color.a);
            }
            ENDCG
        }
    }
}
