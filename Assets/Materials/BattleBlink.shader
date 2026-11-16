Shader "Battle/Blink Afterimage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint", Color) = (0, 0, 0, 0.6)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST, _TintColor, _SourceColor, _Axis, _OriginHeight;
            float _Progress, _Arriving, _Stretch, _CopyOffset, _CopyWeight, _IsStreak;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float height : TEXCOORD1; };
            float bandNoise(float band) { return frac(sin(band * 12.9898 + 3.7) * 43758.5453); }

            v2f vert(appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.height = (world.y - _OriginHeight.x) / max(0.1, _OriginHeight.y);
                float scatter = lerp(_Progress, 1.0 - _Progress, _Arriving);
                float band = floor(o.height * 32.0);
                float offset = (bandNoise(band) - 0.5) * scatter * scatter * _Stretch;
                offset += _CopyOffset * _OriginHeight.y * (0.25 + scatter);
                if (_IsStreak > 0.5) offset = (v.uv.x - 0.5) * scatter * _Stretch;
                world += _Axis.xyz * offset;
                o.vertex = mul(UNITY_MATRIX_VP, float4(world, 1));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float p = saturate(_Progress);
                float scatter = lerp(p, 1.0 - p, _Arriving);
                float envelope = pow(1.0 - p, 1.3);
                if (_IsStreak > 0.5)
                {
                    float2 edge = abs(i.uv * 2.0 - 1.0);
                    float alpha = pow(saturate(1.0 - edge.x), 1.6)
                        * (1.0 - smoothstep(0.1, 1.0, edge.y));
                    return fixed4(_TintColor.rgb, alpha * envelope * _TintColor.a * 0.85);
                }
                float band = floor(i.height * 32.0);
                float noise = bandNoise(band);
                float dissolve = 1.0 - smoothstep(noise - 0.12, noise + 0.12, scatter);
                float stripe = 0.45 + 0.55 * smoothstep(0.05, 0.3, frac(i.height * 32.0));
                fixed4 source = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw) * _SourceColor;
                fixed3 color = lerp(source.rgb, fixed3(1, 1, 1), 0.65) * _TintColor.rgb;
                return fixed4(color, source.a * _TintColor.a * _CopyWeight * envelope * dissolve * stripe);
            }
            ENDCG
        }
    }
    Fallback Off
}
