Shader "VolcanoCave/Lava"
{
    Properties { _Color("Lava",Color)=(1,.12,.008,1) _Emission("Heat",Float)=2 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color; float _Emission;
        struct Input { float3 worldPos; };
        float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float noise(float2 p)
        {
            float2 a=floor(p), f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz*2+float2(_Time.y*.045,-_Time.y*.065);
            float n=noise(p)+noise(p*2.1)*.45+noise(p*4.2)*.2;
            float heat=smoothstep(.45,1.35,n);
            float3 c=lerp(float3(.24,.015,.003),float3(1,.30,.008),heat);
            c=lerp(c,float3(1,.68,.08),smoothstep(.84,1,heat));
            o.Albedo=c*.35; o.Emission=c*_Emission; o.Smoothness=.35; o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
