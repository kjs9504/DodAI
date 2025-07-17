Shader "Custom/URP/FresnelHalo"
{
    Properties
    {
        _FresnelPower ("Fresnel Power", Float) = 5
        _FresnelScale ("Fresnel Scale", Float) = 1
        _FresnelBias  ("Fresnel Bias", Float) = -0.05
        _EdgeWidth    ("Edge Width", Range(0,1)) = 0.1
        _UseRampTex   ("Use Ramp Tex", Float) = 1
        _RampTex      ("Ramp 1D", 2D) = "white" {}
        [HDR]_EdgeColorB ("Edge Color", Color) = (1,0.82,0.55,1)

        _EdgeNoiseTex ("Edge Noise", 2D) = "white" {}
        _EdgeNoiseTiling ("EdgeNoise Tiling", Vector) = (6,1,0,0)
        _EdgeNoiseOffset ("EdgeNoise Offset", Vector) = (0,0,0,0)
        _AlphaCutBias  ("Noise Bias", Range(-1,1)) = -0.25
        _AlphaCutScale ("Noise Scale", Float) = 2
        _Stretch       ("Radial Stretch", Float) = 1.8
        _OuterFade     ("Outer Fade", Float) = 0.6

        _EmissionStrength ("Emission Strength", Float) = 15
        _AlphaMul      ("Alpha Mul", Range(0,1)) = 1
        _MaskSharp     ("Upper Mask Sharpness", Float) = 2
    }
    SubShader
    {
        Tags{ "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _FresnelPower;
                float _FresnelScale;
                float _FresnelBias;
                float _EdgeWidth;
                float _UseRampTex;
                float4 _EdgeColorB;
                float4 _EdgeNoiseTiling;
                float4 _EdgeNoiseOffset;
                float _AlphaCutBias;
                float _AlphaCutScale;
                float _Stretch;
                float _OuterFade;
                float _EmissionStrength;
                float _AlphaMul;
                float _MaskSharp;
            CBUFFER_END

            TEXTURE2D(_RampTex);      SAMPLER(sampler_RampTex);
            TEXTURE2D(_EdgeNoiseTex); SAMPLER(sampler_EdgeNoiseTex);

            struct Attributes { float3 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 posOS:TEXCOORD2; };

            Varyings Vert(Attributes IN){
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS);
                OUT.positionHCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.posOS = IN.positionOS;
                return OUT;
            }

            float FresnelBase(float3 nWS, float3 vWS){
                float ndv = saturate(dot(normalize(nWS), normalize(vWS)));
                float f = 1.0 - ndv;
                f = (f * _FresnelScale) + _FresnelBias;
                f = saturate(f);
                return pow(f, _FresnelPower);
            }

            float EdgeFromFresnel(float f){
                return smoothstep(1.0 - _EdgeWidth, 1.0, f);
            }

            float UpperMask(float3 nWS){
                float up = saturate(nWS.y * 0.5 + 0.5);
                return pow(up, _MaskSharp);
            }

            float SampleEdgeNoiseStretch(float3 posOS){
                // radial from object origin in XZ
                float2 p = posOS.xz;
                float rN = length(p) * _Stretch; // >1 -> 바깥으로
                float2 uv;
                uv.x = posOS.x * _EdgeNoiseTiling.x + _EdgeNoiseOffset.x; // 단순
                uv.y = rN        * _EdgeNoiseTiling.y + _EdgeNoiseOffset.y;
                uv = frac(uv);
                float n = SAMPLE_TEXTURE2D(_EdgeNoiseTex, sampler_EdgeNoiseTex, uv).r;
                n = pow(saturate(n), _AlphaCutScale);
                n = saturate(n + _AlphaCutBias);
                return n;
            }

            float4 SampleRamp(float t){
                if(_UseRampTex>0.5){
                    float2 uv = float2(saturate(t),0.5);
                    return SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, uv);
                }
                return _EdgeColorB;
            }

            half4 Frag(Varyings IN):SV_Target{
                float3 V = GetWorldSpaceViewDir(IN.positionWS);
                float f = FresnelBase(IN.normalWS, V);
                float edge = EdgeFromFresnel(f);
                float noise = SampleEdgeNoiseStretch(IN.posOS);
                float up = UpperMask(IN.normalWS);

                // outer fade
                float rN = length(IN.posOS.xz) * _Stretch;
                float outer = saturate(1.0 - max(0,rN-1.0)/max(_OuterFade,1e-5));

                float mask = edge * noise * up * outer;

                float4 ramp = SampleRamp(f);
                float3 emis = ramp.rgb * _EmissionStrength * mask;
                float alpha = ramp.a * _AlphaMul * mask;
                return half4(emis, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
