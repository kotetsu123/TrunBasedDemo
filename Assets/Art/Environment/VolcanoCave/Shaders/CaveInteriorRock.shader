Shader "VolcanoCave/Interior Rock"
{
    Properties
    {
        _Color ("Stone", Color) = (0.42,0.40,0.36,1)
        _DarkColor ("Mineral seams", Color) = (0.19,0.20,0.19,1)
        _Scale ("Rock scale", Float) = 1
        _Roughness ("Roughness", Range(0,1)) = 0.94
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        fixed4 _Color, _DarkColor;
        float _Scale, _Roughness;
        struct Input { float3 worldPos; float3 worldNormal; float4 color : COLOR; INTERNAL_DATA };

        float hash(float3 p) { return frac(sin(dot(p, float3(127.1,311.7,74.7))) * 43758.5453); }
        float noise(float3 p)
        {
            float3 i = floor(p), f = frac(p);
            f = f*f*(3-2*f);
            return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),
                             lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),
                        lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),
                             lerp(hash(i+float3(0,1,1)),hash(i+float3(1,1,1)),f.x),f.y),f.z);
        }
        float stone(float3 p)
        {
            return noise(p*.9)*.5 + noise(p*2.13)*.27 + noise(p*5.17)*.15 + noise(p*13.1)*.08;
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos * _Scale;
            float h = stone(p);
            float mineral = saturate((h-.27)*2.5);
            float layers = noise(float3(p.x*.7,p.y*4.8+noise(p*.8)*1.7,p.z*.7));
            float seams = smoothstep(.012,.09,abs(layers-.44));
            float floorBlend = saturate(IN.worldNormal.y*1.8-.6);
            o.Albedo = lerp(_DarkColor.rgb,_Color.rgb,mineral) * lerp(.82,1,seams) * IN.color.rgb;
            o.Albedo *= lerp(float3(1,1,1),float3(.71,.73,.70),floorBlend);
            float3 tangent = normalize(WorldNormalVector(IN,float3(1,0,0)));
            float3 bitangent = normalize(WorldNormalVector(IN,float3(0,1,0)));
            float tx = (stone(p+tangent*.04)-h)/.04;
            float ty = (stone(p+bitangent*.04)-h)/.04;
            o.Normal = normalize(float3(-tx*.23,-ty*.23,1));
            o.Occlusion = IN.color.a;
            o.Smoothness = 1-_Roughness;
            o.Metallic = .025;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
