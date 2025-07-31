// Shader: Custom/URP/BlackCoreSimpleXR
Shader "Custom/URP/BlackCoreSimpleXR"
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
            
            // XR 지원을 위한 multi_compile 추가
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
            CBUFFER_END
            
            struct Attributes 
            { 
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID  // XR 지원
            };
            
            struct Varyings 
            { 
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO      // XR 지원
            };
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // XR 초기화
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.positionHCS = TransformObjectToHClip(float4(IN.positionOS,1));
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // XR 스테레오 설정
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                return _CoreColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}