Shader "IslandVoyage/Water"
{
    Properties
    {
        _Surface("Surface layer", Float) = 1
        _ZWrite("Write depth", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite]
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Surface;
                int _IslandCount;
                float4 _Islands[8],_PhasesA,_PhasesB;
            CBUFFER_END
            float _OceanTime,_OceanLight,_Night,_WaveScale,_SwellStrength,_BoatWaterMask,_Bioluminescence,_StormDarkness;
            float4 _Swell,_SeaFog,_SeaHorizon;
            float4x4 _BoatWorldToLocal;
            struct A { float4 vertex:POSITION; };
            struct V {float4 vertex:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            float shore(float2 p)
            {
                float d=10000;
                [loop] for(int j=0;j<8;j++)
                {
                    if(j>=_IslandCount)break;
                    float4 island=_Islands[j];
                    if(length(p-island.xy)-max(island.z,island.w)*1.08>d)continue;
                    float2 q=(p-island.xy)/island.zw;float a=atan2(q.y,q.x);float phase=j<4?_PhasesA[j]:_PhasesB[j-4];
                    d=min(d,(length(q)-(1+.045*sin(a*5+phase)+.035*cos(a*3-phase)))*min(island.z,island.w));
                }
                return d;
            }
            void wave(float2 p,float2 direction,float wavelength,float amplitude,inout float height,inout float2 slope)
            {
                float k=6.2831853/wavelength;float phase=k*dot(p,direction)-sqrt(9.81*k)*_OceanTime;
                height+=sin(phase)*amplitude;slope+=direction*cos(phase)*amplitude*k;
            }
            V vert(A i)
            {
                V o;o.world=TransformObjectToWorld(i.vertex.xyz);float height=0;float2 slope=0;
                wave(o.world.xz,float2(.94,.341174),18,.16,height,slope);
                wave(o.world.xz,float2(.52,-.854166),7.5,.07,height,slope);
                wave(o.world.xz,float2(-.31,-.950736),3.1,.025,height,slope);
                height*=_WaveScale;slope*=_WaveScale;
                float along=dot(o.world.xz-_Swell.xy,_Swell.zw);
                float pulse=_SwellStrength*exp(-along*along/128);
                height+=pulse;slope+=_Swell.zw*(-along/64)*pulse;
                float attenuation=.18+.82*smoothstep(0,9,shore(o.world.xz));
                o.world.y+=height*attenuation*_Surface;o.normal=normalize(float3(-slope.x*attenuation,1,-slope.y*attenuation));
                o.vertex=TransformWorldToHClip(o.world);return o;
            }
            half4 frag(V i):SV_Target
            {
                // The animated water surface stops at the watertight hull, including roll and pitch.
                if(_Surface>.5 && _BoatWaterMask>.5)
                {
                    float3 local=mul(_BoatWorldToLocal,float4(i.world,1)).xyz;
                    float u=(local.z+2.2)/4.7;
                    if(u>.008 && u<.992 && local.y>-.55 && local.y<.78)
                    {
                        float halfBeam=pow(max(.001,sin(u*3.14159265)),.52)*(.88+.13*u);
                        float sheer=.57+.12*abs(u-.5)*2;
                        float sectionCos=saturate((sheer-local.y)/.93);
                        float width=halfBeam*sqrt(1-sectionCos*sectionCos)-.008;
                        clip(abs(local.x)-width);
                    }
                }
                float2 p=i.world.xz;
                float d=shore(p);
                float variation=sin(p.x*.24+p.y*.16)*sin(p.y*.3-p.x*.11);
                float depth=saturate((d+variation*1.2)/24);
                float3 shallow=float3(.33,.73,.66), deep=float3(.022,.25,.37);
                float3 color=lerp(shallow,deep,smoothstep(0,1,pow(depth,.8)));
                float2 ripple=float2(cos(p.x*2.3+p.y*1.1-_OceanTime*1.4),sin(p.y*2.5-p.x*.9-_OceanTime*1.1))*.017;
                float3 normal=normalize(i.normal+float3(ripple.x,0,ripple.y));float3 view=GetWorldSpaceNormalizeViewDir(i.world);
                Light sun=GetMainLight();float reflection=pow(saturate(dot(normal,normalize(sun.direction+view))),180)*.32*_Surface;
                float fresnel=.025+.32*pow(1-saturate(dot(normal,view)),5);
                color=lerp(color,lerp(float3(.48,.66,.74),float3(.25,.31,.36),_StormDarkness),fresnel*_Surface);
                float caustic=pow(saturate(1-abs(sin(p.x*1.4+p.y*.8+sin(p.y*1.7+_OceanTime*.25))*.5+sin(p.y*1.3-p.x*.7-_OceanTime*.3)*.5)),22);
                color+=caustic*.026*(1-depth);
                float surfPhase=d*.95+_OceanTime*1.8+sin(p.x*.34+p.y*.27)*.35;
                float crest=smoothstep(.72,1,sin(surfPhase));
                float surfZone=smoothstep(-.4,.3,d)*(1-smoothstep(2.0,8.0,d));
                float runup=(1-smoothstep(.05,1.2,d))*saturate(.4+.35*sin(surfPhase));
                float foam=saturate(crest*surfZone*.75+runup*.8)*_Surface;
                float whitecap=smoothstep(.18,.38,length(i.normal.xz))*.35*saturate(_WaveScale-1)*_Surface;
                foam=saturate(foam+whitecap);
                float3 litColor=SRGBToLinear(lerp(color,float3(.95,.99,.93),foam))*_OceanLight+reflection*sun.color;
                // Sparse plankton colonies, rather than a glowing repetition of seabed caustics.
                float colony=smoothstep(.60,.82,noise(p*.16-float2(_OceanTime*.016,0))+.12*noise(p*.45));
                float2 cell=p*4.5;float seed=hash(floor(cell));
                float speck=pow(seed,18)*(1-smoothstep(.05,.25,length(frac(cell)-.5)))*(.65+.35*sin(_OceanTime*1.3+seed*6.28));
                float glow=(foam*.85+speck*colony*.5+colony*surfZone*.018)*_Bioluminescence*_Surface;
                litColor+=float3(.015,.68,.95)*glow;
                // Evaluate depth per pixel: the seabed is a single large quad.
                // Use the same clock-driven haze in the main camera and offscreen previews.
                float cameraDistance=distance(i.world,GetCameraPositionWS());
                float haze=smoothstep(_SeaFog.x,_SeaFog.y,cameraDistance);
                float3 horizon=SRGBToLinear(_SeaHorizon.rgb);
                return half4(lerp(litColor,horizon,haze),_Surface>.5?saturate(lerp(.25,.76,depth)+foam*.5):1);
            }
            ENDHLSL
        }
    }
}
