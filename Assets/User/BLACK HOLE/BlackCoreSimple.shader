// Shader: Custom/URP/BlackCoreSimple
Shader "Custom/URP/BlackCoreSimple"
{
    Properties
    {
        _CoreColor ("Core Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags{ "RenderPipeline"="UniversalRenderPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ForwardLitLikeButBlack"
            Tags{ "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
            CBUFFER_END

            struct Attributes { float3 positionOS:POSITION; };
            struct Varyings { float4 positionHCS:SV_POSITION; };

            Varyings vert(Attributes IN){
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(float4(IN.positionOS,1));
                return OUT;
            }
            half4 frag(Varyings IN):SV_Target{
                return _CoreColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
