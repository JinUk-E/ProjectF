Shader "Fortress/ExteriorFog"
{
    Properties
    {
        _BaseMap("Surface",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _PlayableRect("Playable XZ rectangle",Vector)=(-64,-64,64,64)
        _ExteriorFogColor("Exterior haze",Color)=(.48,.53,.56,1)
        _ExteriorFogRange("Outside distance start/end",Vector)=(2,35,0,0)
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
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor, _PlayableRect, _ExteriorFogColor, _ExteriorFogRange;
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
                half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*_BaseColor.rgb*lighting;
                float2 outside=max(max(_PlayableRect.xy-input.positionWS.xz,input.positionWS.xz-_PlayableRect.zw),0);
                float fade=smoothstep(_ExteriorFogRange.x,max(_ExteriorFogRange.x+.01,_ExteriorFogRange.y),length(outside));
                color=lerp(color,_ExteriorFogColor.rgb,fade);
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
