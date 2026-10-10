Shader "IslandVoyage/Rain"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+25" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 position:POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            struct V {float4 position:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
            V vert(A i){V o;o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);o.color=i.color;o.uv=i.uv;return o;}
            half4 frag(V i):SV_Target
            {
                clip(i.world.y+.4);
                float fade=saturate(1-abs(i.uv.x-.5)*2)*saturate(1-abs(i.uv.y-.5)*2);
                return half4(i.color.rgb,i.color.a*fade);
            }
            ENDHLSL
        }
    }
}
