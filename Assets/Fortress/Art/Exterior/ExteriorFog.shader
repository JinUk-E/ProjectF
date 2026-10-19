Shader "Fortress/ExteriorFog"
{
    Properties
    {
        _BaseMap("Surface",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _PlayableRect("Playable XZ rectangle",Vector)=(-64,-64,64,64)
        _ExteriorFogColor("Exterior haze",Color)=(.48,.53,.56,1)
        _ExteriorFogRange("Outside distance start/end",Vector)=(2,35,0,0)
        _Triplanar("World triplanar mapping",Float)=0
        _TextureScale("World texture scale",Float)=.2
        _Desaturate("Rock desaturation",Range(0,1))=0
        _TopMap("Gentle slope ground",2D)="white"{}
        _SlopeGrass("Blend ground on gentle slopes",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_TopMap); SAMPLER(sampler_TopMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor, _PlayableRect, _ExteriorFogColor, _ExteriorFogRange;
                float _Triplanar, _TextureScale, _Desaturate, _SlopeGrass;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; half fog:TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings o; VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(input.normalOS); o.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                o.fog=ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half3 normal=normalize(input.normalWS);Light sun=GetMainLight();
                half3 lighting=SampleSH(normal)+sun.color*saturate(dot(normal,sun.direction));
                half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb;
                if(_Triplanar>.5)
                {
                    float3 weights=pow(abs(normal),4);weights/=max(dot(weights,float3(1,1,1)),.0001);
                    float3 p=input.positionWS*_TextureScale;
                    albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*weights.x+
                        SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*weights.y+
                        SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*weights.z;
                }
                albedo=lerp(albedo,dot(albedo,half3(.2126,.7152,.0722)),_Desaturate);
                if(_SlopeGrass>.5)
                    albedo=lerp(albedo,SAMPLE_TEXTURE2D(_TopMap,sampler_TopMap,input.positionWS.xz/12).rgb,
                        smoothstep(.58,.92,normal.y)*.85);
                half3 color=albedo*_BaseColor.rgb*lighting;
                float2 outside=max(max(_PlayableRect.xy-input.positionWS.xz,input.positionWS.xz-_PlayableRect.zw),0);
                float fade=smoothstep(_ExteriorFogRange.x,max(_ExteriorFogRange.x+.01,_ExteriorFogRange.y),length(outside));
                color=lerp(color,_ExteriorFogColor.rgb,fade);
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
