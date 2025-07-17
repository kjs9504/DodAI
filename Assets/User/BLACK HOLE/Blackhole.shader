Shader "Custom/URP/Blackhole"
{
    Properties
    {
        // ---- Sphere ----
        _Radius            ("Radius", Float) = 1.0
        _GradientScale     ("Gradient Scale", Float) = 2.0
        _SphereCenterOS    ("Sphere Center (Object)", Vector) = (0,0,0,0)

        // ---- Swirl Controls ----
        _SpinSpeed         ("Spin Speed (rad/s)", Float) = 1.0
        _Twist             ("Twist per Radius", Float) = 2.0
        _AngularTiling     ("Angular Tiling", Float) = 4.0
        _RadialTiling      ("Radial Tiling", Float) = 1.0
        _SwirlOffset       ("Swirl UV Offset (XY)", Vector) = (0,0,0,0)

        // ---- Noise Texture (1장) ----
        _NoiseTex          ("Noise (Gray)", 2D) = "white" {}

        // ---- Ring shaping ----
        _RingLow           ("Ring Low", Range(0,1)) = 0.38
        _RingHigh          ("Ring High", Range(0,1)) = 0.62
        _RingSoftness      ("Ring Softness", Float) = 1.0

        // ---- Emission / Alpha ----
        [HDR]_EmissionColor("Emission Color", Color) = (1,0.82,0.55,1)
        _EmissionStrength  ("Emission Strength", Float) = 10.0
        _AlphaMul          ("Alpha Multiplier", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags{
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        //Blend One One   // <- 애드티브로 바꾸고 싶으면

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

            // ===== Per-Material =====
            CBUFFER_START(UnityPerMaterial)
                float    _Radius;
                float    _GradientScale;
                float4   _SphereCenterOS;

                float    _SpinSpeed;
                float    _Twist;
                float    _AngularTiling;
                float    _RadialTiling;
                float4   _SwirlOffset;

                float    _RingLow;
                float    _RingHigh;
                float    _RingSoftness;

                float4   _EmissionColor;
                float    _EmissionStrength;
                float    _AlphaMul;
            CBUFFER_END

            // Noise tex
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            // ===== Verts =====
            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(float4(IN.positionOS,1));
                OUT.positionHCS = vp.positionCS;
                OUT.positionOS  = IN.positionOS;
                return OUT;
            }

            // ---- Sphere gradient (same as before) ----
            inline float SphereGradient(float3 posOS)
            {
                float3 p = posOS - _SphereCenterOS.xyz;
                float d = length(p);
                float g = 1.0 - saturate(d / max(_Radius, 1e-5));
                g *= _GradientScale;
                return saturate(g);
            }

            // ---- Swirl noise sample ----
            inline float SampleSwirl(float3 posOS)
            {
                // 평면이 XY라고 가정 (Ring이 바닥에 놓였던 스샷 기준)
                float2 p = posOS.xy - _SphereCenterOS.xy;

                // 반경 r, 각도 θ
                float r = length(p);
                float theta = atan2(p.y, p.x); // -PI..PI

                // 시간
                float t = _Time.y; // Unity: _Time.y = time*0.5?  (실제: ShaderVariablesFunctions.hlsl -> _TimeParameters.x=time, y=time*2, z=time*3, w=time*4; 버전에 따라 다름)
                // 최안전: _TimeParameters.x (== time)
                float timeSec = _TimeParameters.x;

                // 기본 회전 + 반경별 트위스트
                theta += timeSec * _SpinSpeed;
                theta += r * _Twist;

                // 각도→0..1 정규화
                const float INVTAU = 0.15915494309189535; // 1/(2PI)
                float ang01 = frac(theta * INVTAU);       // wrap

                // 극좌표 -> 텍스쳐 UV
                float2 uv;
                uv.x = ang01 * _AngularTiling + _SwirlOffset.x;
                uv.y = r      * _RadialTiling  + _SwirlOffset.y;

                uv = frac(uv);

                // 샘플
                return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv).r;
            }

            inline float ShapeRing(float t)
            {
                float e = smoothstep(_RingLow, _RingHigh, t);
                if (_RingSoftness != 1.0)
                    e = pow(abs(e), _RingSoftness);
                return saturate(e);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float g = SphereGradient(IN.positionOS);
                float n = SampleSwirl(IN.positionOS);

                float m = g * n;                // 노이즈 곱 (스월 이미 포함)
                float edge = ShapeRing(m);      // 링 두께

                float3 emis = _EmissionColor.rgb * _EmissionStrength * edge;
                float alpha = edge * _EmissionColor.a * _AlphaMul;
                return half4(emis, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
