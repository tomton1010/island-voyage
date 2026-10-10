Shader "IslandVoyage/Stylized"
{
    Properties { _BaseColor("Tint", Color) = (1,1,1,1) _Swim("Body flex",Vector)=(0,0,0,0) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _Swim;
            CBUFFER_END
            half3 _VoyageAmbient;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;half4 color:COLOR;float3 world:TEXCOORD1;half fog:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
            Varyings vert(Attributes a)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(a); UNITY_TRANSFER_INSTANCE_ID(a,o);
                float bend=sin(_Swim.x+a.positionOS.z*.8)*_Swim.y*smoothstep(.3,2.3,-a.positionOS.z)*_Swim.w;
                a.positionOS.x+=bend*(1-_Swim.z);a.positionOS.y+=bend*_Swim.z;
                VertexPositionInputs p = GetVertexPositionInputs(a.positionOS.xyz);
                o.positionCS = p.positionCS; o.normalWS = TransformObjectToWorldNormal(a.normalOS);
                o.color=a.color*_BaseColor;o.world=p.positionWS;o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                half direct=saturate(dot(normalize(i.normalWS),sun.direction));
                half3 color=SRGBToLinear(i.color.rgb)*(_VoyageAmbient+sun.color*direct*sun.shadowAttenuation*.56);
                color = MixFog(color,i.fog);
                return half4(color,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Off
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _Swim;
            CBUFFER_END
            float3 _LightDirection;
            struct A {float4 position:POSITION;float3 normal:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 shadowVert(A i):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 p=TransformObjectToWorld(i.position.xyz),n=TransformObjectToWorldNormal(i.normal);
                float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,_LightDirection));
                #if UNITY_REVERSED_Z
                clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #else
                clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #endif
                return clip;
            }
            half4 shadowFrag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
