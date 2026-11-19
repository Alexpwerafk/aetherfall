Shader "Aetherfall/CelToonURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.21, 0.24, 0.32, 1)
        _ShadowTint ("Shadow Tint", Color) = (0.06, 0.07, 0.12, 1)
        _RimColor ("Rim Color", Color) = (0.17, 0.91, 0.96, 1)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.5
        _RimStrength ("Rim Strength", Range(0.0, 1.5)) = 0.25
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _PulseSpeed ("Emission Pulse Speed", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowTint;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half4 _EmissionColor;
                half _PulseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normInputs.normalWS);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half QuantizeToon(half val)
            {
                half band1 = smoothstep(0.06h, 0.11h, val);
                half band2 = smoothstep(0.48h, 0.54h, val);
                return band1 * 0.56h + band2 * 0.44h;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 N = normalize(input.normalWS);
                half3 V = normalize(GetWorldSpaceViewDir(input.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half ndl = saturate(dot(N, mainLight.direction));
                half shadow = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half mainBand = QuantizeToon(ndl * shadow);

                half3 litColor = _BaseColor.rgb * mainLight.color;
                half3 shadowColor = _BaseColor.rgb * _ShadowTint.rgb * 1.45h + _ShadowTint.rgb * 0.25h;
                half3 color = lerp(shadowColor, litColor, mainBand);

                // Ambient gradient bounce
                half3 ambient = SampleSH(N) * _BaseColor.rgb * 0.45h;
                color += ambient;

                // Additional point/spot lights (braziers, spell lights)
                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightCount; ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    half addNdl = saturate(dot(N, addLight.direction));
                    half addAtten = addLight.distanceAttenuation * addLight.shadowAttenuation;
                    half addBand = smoothstep(0.05h, 0.14h, addNdl) * smoothstep(0.02h, 0.20h, addAtten);
                    color += _BaseColor.rgb * addLight.color * addBand * saturate(addAtten * 1.4h);
                }
                #endif

                // Stylized silhouette rim
                half fresnel = pow(saturate(1.0h - saturate(dot(N, V))), _RimPower);
                half rimMask = smoothstep(0.35h, 0.65h, fresnel);
                color += _RimColor.rgb * rimMask * _RimStrength;

                // HDR Emission with optional pulse
                half pulse = (_PulseSpeed > 0.01h) ? (0.85h + 0.35h * sin(_Time.y * _PulseSpeed)) : 1.0h;
                color += _EmissionColor.rgb * pulse;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirWS));
                output.positionCS = ApplyShadowClamping(positionCS);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings DepthVert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 DepthFrag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
