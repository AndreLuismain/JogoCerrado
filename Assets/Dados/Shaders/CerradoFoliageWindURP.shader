Shader "Universal Render Pipeline/Cerrado/FoliageTranslucencyWind"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo / Folha (RGBA)", 2D) = "white" {}
        [MainColor] _BaseColor ("Cor Base", Color) = (1, 1, 1, 1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Intensidade Normal", Range(0, 2)) = 0.85
        _Cutoff ("Alpha Cutoff", Range(0.05, 0.9)) = 0.40
        _Smoothness ("Smoothness (Cera Natural)", Range(0, 1)) = 0.18
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _NormalUpBlend ("Suavizacao Esferica da Copa", Range(0, 0.75)) = 0.32

        [Header(Translucidez Subsurface do Sol)]
        _TranslucencyColor ("Tonalidade Translucidez", Color) = (0.90, 0.96, 0.44, 1)
        _TranslucencyStrength ("Intensidade Translucidez", Range(0, 2)) = 0.80
        _TranslucencyDistortion ("Distorcao Normal Subsurface", Range(0, 1)) = 0.35
        _TranslucencyPower ("Foco Contra-Luz", Range(1, 10)) = 3.4

        [Header(Vento Organico World Space)]
        _WindAmplitude ("Amplitude do Vento", Range(0, 0.2)) = 0.040
        _WindFrequency ("Frequencia do Vento", Float) = 1.45
        _WindFlutter ("Tremular Fino nas Pontas", Range(0, 0.08)) = 0.014
        _WindIsGroundGrass ("Modo Grama Rasteira (1=Base Y Fixa, 0=Copa)", Float) = 0.0

        [HideInInspector] _Cull ("__cull", Float) = 0.0
        [HideInInspector] _AlphaClip ("__clip", Float) = 1.0
        [HideInInspector] _AlphaToMask ("__alphaToMask", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _BumpScale;
            half   _Cutoff;
            half   _Smoothness;
            half   _Metallic;
            half   _NormalUpBlend;
            half4  _TranslucencyColor;
            half   _TranslucencyStrength;
            half   _TranslucencyDistortion;
            half   _TranslucencyPower;
            float  _WindAmplitude;
            float  _WindFrequency;
            float  _WindFlutter;
            float  _WindIsGroundGrass;
        CBUFFER_END

        // Aplica vento sutil e orgânico em espaço de mundo mantendo a raiz/tronco 100% fixos
        float3 ApplyFoliageWindWS(float3 posOS, float3 posWS)
        {
            float grassMask = saturate(posOS.y * 1.65);
            float radialDist = length(posOS.xz);
            float canopyMask = saturate((radialDist - 0.05) * 0.75) * saturate((posOS.y + 0.15) * 0.65);
            float flexMask = lerp(canopyMask, grassMask, step(0.5, _WindIsGroundGrass));
            flexMask = flexMask * flexMask; // Curva quadrática: base imóvel, pontas macias

            float t = _Time.y * _WindFrequency;
            float spatialPhase = dot(posWS.xz, float2(0.39, 0.27)) + posWS.y * 0.21;

            float gustX = sin(t + spatialPhase) + 0.45 * sin(t * 1.83 + spatialPhase * 1.7 + 1.3);
            float gustZ = cos(t * 0.87 + spatialPhase * 1.15) * 0.75;
            float flutter = sin(t * 4.3 + dot(posWS.xyz, float3(3.1, 4.7, 2.9))) * _WindFlutter;

            float3 offsetWS = float3(
                gustX * _WindAmplitude + flutter,
                -abs(gustX) * (_WindAmplitude * 0.14),
                gustZ * _WindAmplitude + flutter * 0.7
            ) * flexMask;

            return posWS + offsetWS;
        }
        ENDHLSL

        // =========================================================================
        // 1. FORWARD LIT (PBR + Translucidez Solar + Vento + Dupla Face)
        // =========================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            AlphaToMask Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 baseWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 windyWS = ApplyFoliageWindWS(IN.positionOS.xyz, baseWS);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionWS = windyWS;
                OUT.positionCS = TransformWorldToHClip(windyWS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS   = normInputs.normalWS;
                OUT.tangentWS  = float4(normInputs.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 texSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                clip(texSample.a - _Cutoff);

                float flipSign = (facing >= 0.0h) ? 1.0 : -1.0;
                float3 N = normalize(IN.normalWS) * flipSign;

                #if defined(_NORMALMAP)
                    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                    float3 T = normalize(IN.tangentWS.xyz);
                    float3 B = cross(N, T) * IN.tangentWS.w;
                    N = normalize(TransformTangentToWorld(normalTS, half3x3(T, B, N)));
                #endif

                // Suaviza normais da copa em direção ao zênite para eliminar sombreamento duro entre planos de folhas
                float3 softNormalWS = normalize(lerp(N, float3(0.0, 1.0, 0.0), _NormalUpBlend));

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = softNormalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), IN.fogFactor);
                inputData.bakedGI = SampleSH(softNormalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = texSample.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = 1.0h;

                half4 col = UniversalFragmentPBR(inputData, surfaceData);

                // Translucidez Subsurface: luz quente do entardecer atravessando as bordas das folhas e capim
                Light mainLight = GetMainLight(inputData.shadowCoord);
                float3 transLightDir = normalize(mainLight.direction + softNormalWS * _TranslucencyDistortion);
                float backViewDot = pow(saturate(dot(inputData.viewDirectionWS, -transLightDir)), _TranslucencyPower);
                float wrapBackLight = saturate((dot(-softNormalWS, mainLight.direction) + 0.40) / 1.40);
                half shadowTerm = lerp(0.32h, 1.0h, mainLight.shadowAttenuation);
                half3 subsurface = mainLight.color * (mainLight.distanceAttenuation * shadowTerm)
                                 * texSample.rgb * _TranslucencyColor.rgb
                                 * (half)((backViewDot * 0.65 + wrapBackLight * 0.35) * _TranslucencyStrength);

                col.rgb += subsurface;
                col.rgb = MixFog(col.rgb, inputData.fogCoord);
                return half4(col.rgb, 1.0h);
            }
            ENDHLSL
        }

        // =========================================================================
        // 2. SHADOW CASTER (Sombras recortadas acompanhando o vento)
        // =========================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 baseWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 windyWS = ApplyFoliageWindWS(IN.positionOS.xyz, baseWS);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(windyWS, normalWS, _LightDirection));
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // =========================================================================
        // 3. DEPTH ONLY (Recorte Alpha exato + Vento para Prepass e DoF)
        // =========================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 baseWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 windyWS = ApplyFoliageWindWS(IN.positionOS.xyz, baseWS);

                OUT.positionCS = TransformWorldToHClip(windyWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 DepthFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // =========================================================================
        // 4. DEPTH NORMALS (SSAO de Alta Qualidade nas Copas e Grama)
        // =========================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormalsVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 baseWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 windyWS = ApplyFoliageWindWS(IN.positionOS.xyz, baseWS);

                OUT.positionCS = TransformWorldToHClip(windyWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 DepthNormalsFrag(Varyings IN, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);

                float flipSign = (facing >= 0.0h) ? 1.0 : -1.0;
                float3 normalWS = normalize(lerp(normalize(IN.normalWS) * flipSign, float3(0.0, 1.0, 0.0), _NormalUpBlend));

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

    FallBack "Universal Render Pipeline/Simple Lit"
}
