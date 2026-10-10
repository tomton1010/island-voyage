Shader "IslandVoyage/BlenderSurface"
{
    Properties { _BaseColor("Tint", Color)=(1,1,1,1) _Smoothness("Smoothness",Range(0,1))=.3 _Detail("Detail type",Float)=0 _EncounterVisibility("Encounter visibility",Range(0,1))=1 _Aquatic("Aquatic material",Float)=0 _WaterDepth("Depth below water",Float)=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness;
                float _Detail;
                float _EncounterVisibility;
                float _Aquatic,_WaterDepth;
            CBUFFER_END
            half3 _VoyageAmbient;float _OceanLight;
            struct A {float4 position:POSITION;float3 normal:NORMAL;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;half4 color:COLOR;float3 local:TEXCOORD2;half fog:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID};
            V vert(A i)
            {
                V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);
                VertexPositionInputs p=GetVertexPositionInputs(i.position.xyz);
                o.position=p.positionCS;o.world=p.positionWS;o.normal=TransformObjectToWorldNormal(i.normal);o.color=i.color;o.local=i.position.xyz;o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half4 frag(V i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float dither=frac(52.9829189*frac(dot(floor(i.position.xy),float2(.06711056,.00583715))));
                clip(_EncounterVisibility-dither);
                half3 albedo=SRGBToLinear(i.color.rgb)*_BaseColor.rgb;
                if(_Detail>.5 && _Detail<1.5)
                {
                    float grain=sin(i.local.x*142+sin(i.local.z*2.1)*2+sin(i.local.y*18)*.5);
                    albedo*=.98+grain*.035;
                }
                SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.metallic=0;s.specular=half3(.04,.04,.04);s.smoothness=_Smoothness;s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=1;
                InputData data=(InputData)0;data.positionWS=i.world;data.normalWS=normalize(i.normal);data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);data.shadowCoord=TransformWorldToShadowCoord(i.world);data.bakedGI=_VoyageAmbient;data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.position);data.shadowMask=half4(1,1,1,1);
                half4 color=UniversalFragmentPBR(data,s);
                if(_Aquatic>.5 && _WaterDepth>.01)
                {
                    // Absorption removes bright reds first; distance and oblique views hide detail.
                    float distanceToFish=distance(i.world,GetCameraPositionWS());
                    float path=_WaterDepth/max(.28,abs(data.viewDirectionWS.y));
                    float absorption=1-exp(-path*.28);
                    float distant=smoothstep(16,58,distanceToFish)*saturate(_WaterDepth/2.5);
                    half3 tint=SRGBToLinear(half3(.10,.40,.43))*_OceanLight;
                    color.rgb*=exp(-_WaterDepth*half3(.24,.085,.05));
                    color.rgb=lerp(color.rgb,tint,saturate(absorption*.76+distant*.38));
                }
                color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
