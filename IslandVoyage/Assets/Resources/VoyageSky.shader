Shader "IslandVoyage/GradientSky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            float _Night,_CloudCover,_StormDarkness,_OceanTime,_Lightning;
            float4 _WeatherWind,_SeaHorizon;
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(c),hash(c+float2(1,0)),f.x),lerp(hash(c+float2(0,1)),hash(c+1),f.x),f.y);
            }
            struct A {float4 position:POSITION;};
            struct V {float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
            V vert(A i)
            {
                V o;o.position=TransformObjectToHClip(i.position.xyz);
                o.direction=mul((float3x3)unity_ObjectToWorld,i.position.xyz);return o;
            }
            half4 frag(V i):SV_Target
            {
                float3 direction=normalize(i.direction);float height=pow(saturate(direction.y),.55);
                float3 horizon=_SeaHorizon.rgb;
                float3 zenith=lerp(float3(.18,.46,.68),float3(.007,.018,.049),_Night);
                zenith=lerp(zenith,float3(.16,.21,.26)*(1-_Night*.85),_StormDarkness);
                half3 sky=SRGBToLinear(lerp(horizon,zenith,height));
                float2 p=direction.xz/(max(.12,direction.y)+.22)*2-_WeatherWind.xy*_OceanTime*.003;
                float n=noise(p)*.53+noise(p*2.1)*.29+noise(p*4.3)*.18;
                float cloud=smoothstep(.88-_CloudCover*.74,.99-_CloudCover*.62,n)*smoothstep(-.03,.16,direction.y);
                float3 cloudColor=lerp(float3(.90,.94,.95),float3(.23,.27,.32),_StormDarkness);
                sky=lerp(sky,SRGBToLinear(cloudColor)*(1-_Night*.91),cloud*.96);
                Light sun=GetMainLight();float alignment=saturate(dot(direction,sun.direction));
                sky+=sun.color*(pow(alignment,1400)*.65+pow(alignment,80)*.06)*(1-_Night)*(1-cloud);
                sky+=float3(.35,.42,.52)*_Lightning;
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
