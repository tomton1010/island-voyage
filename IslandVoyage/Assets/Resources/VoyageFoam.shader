Shader "IslandVoyage/Foam"
{
    Properties { _BaseColor("Tint", Color)=(1,1,1,1) _Patch("Soft patch", Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+21" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Patch;
            CBUFFER_END
            float _OceanLight,_Bioluminescence;
            struct A { float4 position:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct V { float4 position:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; };
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(c),hash(c+float2(1,0)),f.x),lerp(hash(c+float2(0,1)),hash(c+1),f.x),f.y);
            }
            V vert(A i){V o;o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);o.color=i.color*_BaseColor;o.uv=i.uv;return o;}
            half4 frag(V i):SV_Target
            {
                float n=noise(i.world.xz*4)*.65+noise(i.world.xz*13)*.35;
                float bubbles=smoothstep(.38,.69,n);
                float edge=smoothstep(0,.22,min(i.uv.y,1-i.uv.y));
                float patch=1-smoothstep(.35,1,length((i.uv-.5)*2));
                float envelope=lerp(edge,patch,_Patch);
                return half4(i.color.rgb*lerp(.15,1,_OceanLight)+float3(.02,.6,.95)*_Bioluminescence,i.color.a*envelope*bubbles);
            }
            ENDHLSL
        }
    }
}
