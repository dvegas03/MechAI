Shader "UI/FrostedGlass"
{
    Properties
    {
        [Header(Appearance)]
        _TintColor ("Tint Color", Color) = (1, 1, 1, 0.5)
        _BlurStrength ("Blur Strength", Range(0, 10)) = 2.0

        [Header(Shape)]
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.1
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.1)) = 0.01

        [Header(Lighting)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 0.2)
        _RimPower ("Rim Power", Range(0.1, 10)) = 2.0
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 0.1)
        _SpecularGloss ("Specular Gloss", Range(1, 256)) = 32
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 screenPos    : TEXCOORD0;
                float2 uv           : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float4 color        : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TintColor;
                float _BlurStrength;
                float _CornerRadius;
                float _EdgeSoftness;
                float4 _RimColor;
                float _RimPower;
                float4 _SpecularColor;
                float _SpecularGloss;
            CBUFFER_END

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }

            float4 KawaseBlur(float2 uv, float strength)
            {
                float2 pixelSize = _ScreenParams.zw - 1.0;
                float2 offset = pixelSize * strength;

                float4 col = 0;
                col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(offset.x, offset.y));
                col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(-offset.x, offset.y));
                col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(offset.x, -offset.y));
                col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(-offset.x, -offset.y));

                offset *= 2.0;
                col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(offset.x, 0));
                col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(-offset.x, 0));
                col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(0, offset.y));
                col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(0, -offset.y));

                return col / 8.0;
            }

            float RoundedRectSDF(float2 uv, float2 size, float radius)
            {
                float2 q = abs(uv) - size + radius;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // SDF Mask
                float2 centeredUV = input.uv - 0.5;
                float sdf = RoundedRectSDF(centeredUV, float2(0.5, 0.5), _CornerRadius);
                float mask = 1.0 - smoothstep(-_EdgeSoftness, 0.0, sdf);

                // Screen UVs
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // Background Blur
                float4 blurredBg = KawaseBlur(screenUV, _BlurStrength);

                // Base Color
                float4 baseColor = blurredBg * _TintColor;

                // Lighting
                float3 viewDir = normalize(_WorldSpaceCameraPos - mul(UNITY_MATRIX_M, float4(input.uv, 0, 1)).xyz);
                float3 normalDir = normalize(input.normalWS);

                // Rim Light
                float rim = 1.0 - saturate(dot(viewDir, normalDir));
                float4 rimLight = _RimColor * pow(rim, _RimPower);

                // Specular Light
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float3 halfDir = normalize(viewDir + lightDir);
                float spec = pow(saturate(dot(normalDir, halfDir)), _SpecularGloss);
                float4 specular = _SpecularColor * spec;

                // Final Composite
                float4 finalColor = baseColor + rimLight + specular;
                finalColor.a = _TintColor.a * mask;

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}