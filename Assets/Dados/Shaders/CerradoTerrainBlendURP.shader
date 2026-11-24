Shader "Universal Render Pipeline/Cerrado/TerrainOrganicBlend"
{
    Properties
    {
        _GrassMap ("Textura Grama (BaseColor)", 2D) = "white" {}
        _GrassColor ("Cor Grama Cerrado", Color) = (0.93, 0.86, 0.60, 1)
        _DirtMap ("Textura Terra Seca (BaseColor)", 2D) = "white" {}
        _DirtColor ("Cor Terra Seca", Color) = (0.85, 0.66, 0.47, 1)
        _BumpMap ("Normal Map Solo", 2D) = "bump" {}
        _BumpScale ("Forca Normal Map", Range(0, 2)) = 1.25
        _WorldTiling ("Tiling World-Space (por metro)", Float) = 0.52
        _Smoothness ("Smoothness", Range(0, 1)) = 0.07
        _NoiseScaleMacro ("Escala Ruido Macro", Float) = 0.14
        _NoiseScaleMicro ("Escala Ruido Micro", Float) = 0.52
        _NoiseStrength ("Forca Distorcao Borda", Range(0, 0.6)) = 0.34
        _EdgeSoftness ("Suavidade da Transicao", Range(0.05, 0.5)) = 0.28
        _PatchVariation ("Manchas Naturais no Campo", Range(0, 0.4)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_GrassMap); SAMPLER(sampler_GrassMap);
            TEXTURE2D(_DirtMap);  SAMPLER(sampler_DirtMap);
            TEXTURE2D(_BumpMap);  SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _GrassColor;
                float4 _DirtColor;
                float  _BumpScale;
                float  _WorldTiling;
                float  _Smoothness;
                float  _NoiseScaleMacro;
                float  _NoiseScaleMicro;
                float  _NoiseStrength;
                float  _EdgeSoftness;
                float  _PatchVariation;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i + float2(0.0, 0.0));
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbmNoise(float2 p)
            {
                float v = 0.0;
                float amp = 0.52;
                float2x2 rot = float2x2(0.80, -0.60, 0.60, 0.80);
                for (int i = 0; i < 4; i++)
                {
                    v += amp * valueNoise(p);
                    p = mul(rot, p) * 2.03 + float2(17.1, 31.7);
                    amp *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = normInputs.normalWS;
                OUT.tangentWS  = float4(normInputs.tangentWS, IN.tangentOS.w);
                OUT.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // Coordenadas contínuas em espaço de mundo XZ eliminam qualquer quebra de UV entre faces
                float2 worldUV = IN.positionWS.xz * _WorldTiling;
                float2 worldUVMacro = IN.positionWS.xz * (_WorldTiling * 0.27);

                half3 grassCol = SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, worldUV).rgb * _GrassColor.rgb;
                half3 grassMacro = SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, worldUVMacro).rgb * _GrassColor.rgb;
                grassCol = lerp(grassCol, grassMacro, 0.22h);

                half3 dirtCol = SAMPLE_TEXTURE2D(_DirtMap, sampler_DirtMap, worldUV).rgb * _DirtColor.rgb;
                half3 dirtMacro = SAMPLE_TEXTURE2D(_DirtMap, sampler_DirtMap, worldUVMacro).rgb * _DirtColor.rgb;
                dirtCol = lerp(dirtCol, dirtMacro, 0.25h);

                // Ruído FBM multi-escala em World Position XZ para dissolver totalmente a linha reta terra/grama
                float macroNoise = fbmNoise(IN.positionWS.xz * _NoiseScaleMacro);
                float microNoise = fbmNoise(IN.positionWS.xz * _NoiseScaleMicro + float2(43.2, 19.8));
                float combinedNoise = (macroNoise * 0.68 + microNoise * 0.32) - 0.5;

                // IN.color.r = 0 (grama) .. 0.5 (fronteira exata) .. 1 (terra), suavizado na malha
                float baseMask = IN.color.r;
                float perturbedMask = baseMask + combinedNoise * (_NoiseStrength * 2.0);
                float blend = smoothstep(0.5 - _EdgeSoftness, 0.5 + _EdgeSoftness, perturbedMask);

                // Manchas sutis orgânicas no interior da grama e bordas secas para naturalidade do Cerrado
                float dryPatch = smoothstep(0.56, 0.84, macroNoise) * _PatchVariation;
                float grassWisp = smoothstep(0.60, 0.86, microNoise) * (_PatchVariation * 0.65);
                half3 naturalGrass = lerp(grassCol, dirtCol * 1.05h, (half)dryPatch);
                half3 naturalDirt = lerp(dirtCol, grassCol * 0.92h, (half)(grassWisp * (1.0 - saturate((baseMask - 0.5) * 2.0))));

                // Aproximação tonal suave exatamente na zona de transição (evita contraste artificial)
                float edgeBand = 1.0 - abs(blend * 2.0 - 1.0);
                half3 albedo = lerp(naturalGrass, naturalDirt, (half)blend);
                albedo = lerp(albedo, (naturalGrass + naturalDirt) * 0.5h, (half)(edgeBand * 0.16));

                // Normal Map em espaço tangente (blend suave de escala entre grama e terra)
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, worldUV), _BumpScale);
                float3 N = normalize(IN.normalWS);
                float3 T = normalize(IN.tangentWS.xyz);
                float3 B = cross(N, T) * IN.tangentWS.w;
                float3 normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(T, B, N)));

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), IN.fogFactor);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0.0h;
                surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
                surfaceData.smoothness = (half)_Smoothness;
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = 1.0h;

                half4 col = UniversalFragmentPBR(inputData, surfaceData);
                col.rgb = MixFog(col.rgb, inputData.fogCoord);
                return col;
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
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
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
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TerrainDepthNormalsVert
            #pragma fragment TerrainDepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings TerrainDepthNormalsVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 TerrainDepthNormalsFrag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    return half4(PackFloat2To888(remappedOctNormalWS), 0.0h);
                #else
                    return half4(NormalizeNormalPerPixel(normalWS), 0.0h);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
