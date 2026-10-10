Shader "VolcanoCave/Boss Lava Flow"
{
    Properties
    {
        _Color ("Lava Color", Color) = (0.874, 0.255, 0, 1)
        _CoolColor ("Cool Crust Color", Color) = (0.10, 0.012, 0.004, 1)
        [HDR] _EmissionColor ("Lava Emission", Color) = (2.79, 0.476, 0, 1)
        [HDR] _HotColor ("Hot Core Emission", Color) = (3.2, 1.35, 0.12, 1)
        _FlowSpeed ("Downhill Flow Speed", Range(0, 0.5)) = 0.10
        _PatternScale ("Pattern Scale", Range(0.25, 8)) = 1.5
        _Contrast ("Heat Contrast", Range(0.25, 3)) = 1.6
        _EdgeDarkening ("Edge Darkening", Range(0, 1)) = 0.65
        _Glossiness ("Smoothness", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        half4 _Color, _CoolColor, _EmissionColor, _HotColor;
        float _FlowSpeed, _PatternScale, _Contrast, _EdgeDarkening;
        half _Glossiness;

        struct Input
        {
            float2 lavaUV;
        };

        void vert(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            output.lavaUV = vertex.texcoord.xy;
        }

        float Hash(float2 samplePosition)
        {
            return frac(sin(dot(samplePosition, float2(127.1, 311.7))) * 43758.5453);
        }

        float ValueNoise(float2 samplePosition)
        {
            float2 cell = floor(samplePosition);
            float2 offset = frac(samplePosition);
            offset = offset * offset * (3.0 - 2.0 * offset);

            return lerp(
                lerp(Hash(cell), Hash(cell + float2(1, 0)), offset.x),
                lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), offset.x),
                offset.y);
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            // V increases downhill on the ribbon; subtract time so features move toward +V.
            float2 flowUV = input.lavaUV - float2(0, _Time.y * max(0, _FlowSpeed));
            float2 samplePosition = flowUV * float2(4.0, 1.25) * _PatternScale;
            float heat = ValueNoise(samplePosition) * 0.7
                + ValueNoise(samplePosition * 2.03 + float2(17.13, 9.2)) * 0.3;
            heat = saturate((heat - 0.5) * _Contrast + 0.5);

            // Keep the cool banks fixed while the heat pattern flows inside them.
            float banks = smoothstep(0, 0.16, input.lavaUV.x)
                * smoothstep(0, 0.16, 1.0 - input.lavaUV.x);
            heat *= lerp(1.0, 0.5 + banks * 0.5, _EdgeDarkening);

            float warm = smoothstep(0.15, 0.75, heat);
            float hot = smoothstep(0.60, 0.90, heat);
            output.Albedo = lerp(_CoolColor.rgb, _Color.rgb, warm) * 0.25;
            output.Emission = lerp(
                _EmissionColor.rgb * lerp(0.06, 1.0, warm), _HotColor.rgb, hot);
            output.Metallic = 0;
            output.Smoothness = _Glossiness;
            output.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
