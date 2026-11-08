Shader "Battle/Magma"
{
    Properties
    {
        _Mode ("Column", Float) = 0
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Phase ("Animation Time", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            float _Mode, _Opacity, _Phase;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                if (_Mode < 0.5)
                    return fixed4(i.color.rgb, _Opacity * (0.75 + 0.25 * sin(_Phase * 18.0)));

                float angle = i.uv.x * 6.283185;
                float y = i.uv.y;
                float wave = sin(angle * 7.0 + y * 8.0 - _Phase * 10.0)
                    + 0.5 * sin(angle * 13.0 - y * 13.0 + _Phase * 14.0);
                float tongues = 0.72 + 0.17 * sin(angle * 5.0 + _Phase * 5.0)
                    + 0.09 * sin(angle * 11.0 - _Phase * 8.0);
                float top = 1.0 - smoothstep(tongues - 0.22, tongues, y);
                float streak = pow(saturate(wave * 0.4 + 0.45), 2.0);
                float bands = 0.75 + 0.25 * sin(y * 32.0 - _Phase * 22.0 + wave);
                float base = 1.0 - smoothstep(0.0, 0.28, y);
                float alpha = (0.12 + streak * 0.63 + base * 0.2) * top * bands * _Opacity;
                fixed3 color = lerp(fixed3(1.0, 0.12, 0.008), fixed3(1.0, 0.8, 0.12),
                    saturate(streak * 0.8 + base * 0.25));
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
