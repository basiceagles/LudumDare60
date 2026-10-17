Shader "Distance Dither Fog/Animated"
{
    Properties
    {
        // ================================================================
        // Surface
        // ================================================================

        _BaseMap("Albedo", 2D) = "white" {}

        _BaseColor("Color", Color) = (1,1,1,1)

        [Toggle(_ALPHATEST_ON)]
        _AlphaClip("Alpha Clipping", Float) = 0

        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        _Metallic("Metallic", Range(0,1)) = 0.0

        _Smoothness("Smoothness", Range(0,1)) = 0.5

        _BumpMap("Normal Map", 2D) = "bump" {}

        _BumpScale("Normal Scale", Range(0,2)) = 1.0

        _OcclusionMap("Occlusion", 2D) = "white" {}

        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0

        _EmissionMap("Emission", 2D) = "black" {}

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)


        // ================================================================
        // Texture Swap
        //
        // A second base + emission texture set. The material alternates
        // between set 1 (_BaseMap + _EmissionMap) and set 2 (_BaseMap2 +
        // _EmissionMap2), keeping each set visible for _SwapInterval
        // seconds.
        //
        // The switching happens INSIDE the shader using _Time, so there
        // is no script, no per-frame update and no material churn. Only
        // the active set is sampled, so the cost is a single branch on
        // a coherent value.
        //
        // Set _SwapInterval to 0 to disable swapping (set 1 only).
        //
        // NOTE: both sets share the same UV transform (tiling/offset),
        // so the two textures are expected to use the same layout.
        // ================================================================

        _SwapInterval(
            "Swap Interval (seconds)",
            Float
        ) = 2

        _BaseMap2("Albedo 2", 2D) = "white" {}

        _EmissionMap2("Emission 2", 2D) = "black" {}


        // ================================================================
        // Distance Dither Fog
        // ================================================================

        [Toggle]
        _DistanceFogEnabled("Distance Fog Enabled", Float) = 1

        _DistanceFogDitherScale(
            "Dither Scale",
            Range(1,4)
        ) = 1

        _DistanceFogDitherBias(
            "Dither Bias",
            Float
        ) = 0


        // ================================================================
        // Rendering
        // ================================================================

        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull("Cull", Float) = 2
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
        }

        LOD 300


        // ================================================================
        // FORWARD / FORWARD+
        // ================================================================

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            Blend One Zero

            ZWrite On

            ZTest LEqual

            Cull [_Cull]

            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex DistanceFogLitVertex
            #pragma fragment DistanceFogLitFragment

            // ------------------------------------------------------------
            // URP keywords
            // ------------------------------------------------------------

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            // Unity 6 URP Forward+ / clustered lighting.
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #pragma multi_compile_fog

            #pragma multi_compile_instancing


            // ------------------------------------------------------------
            // Material keywords
            // ------------------------------------------------------------

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF


            // ------------------------------------------------------------
            // URP libraries
            // ------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"


            // ------------------------------------------------------------
            // Distance fog libraries
            // ------------------------------------------------------------

            #include "CommonFog.hlsl"

            #include "Bayer4x4.hlsl"


            // ============================================================
            // MATERIAL DATA
            // ============================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;

                float4 _BaseMap_ST;

                float4 _BumpMap_ST;

                float4 _OcclusionMap_ST;

                float4 _EmissionMap_ST;

                float _Cutoff;

                float _Metallic;

                float _Smoothness;

                float _BumpScale;

                float _OcclusionStrength;

                float4 _EmissionColor;

                float _SwapInterval;

                float _DistanceFogEnabled;

                float _DistanceFogDitherScale;

                float _DistanceFogDitherBias;

            CBUFFER_END


            // ============================================================
            // SECOND TEXTURE SET
            //
            // Set 1 (_BaseMap, _EmissionMap) is declared by
            // SurfaceInput.hlsl above. Set 2 is declared here.
            // ============================================================

            TEXTURE2D(_BaseMap2);

            SAMPLER(sampler_BaseMap2);

            TEXTURE2D(_EmissionMap2);

            SAMPLER(sampler_EmissionMap2);


            // ============================================================
            // SWAP PHASE
            //
            // Returns true when the second texture set is active.
            //
            // Each set stays visible for _SwapInterval seconds. The
            // branch is based on a uniform, so the GPU only executes
            // (and samples) one side.
            // ============================================================

            bool IsSecondSetActive()
            {
                if (_SwapInterval <= 0.0)
                    return false;

                float swapPeriod =
                    _SwapInterval * 2.0;

                float phase =
                    frac(
                        _Time.y / swapPeriod
                    );

                return phase >= 0.5;
            }


            // ============================================================
            // STRUCTURES
            // ============================================================

            struct Attributes
            {
                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                float4 tangentOS : TANGENT;

                float2 texcoord : TEXCOORD0;

                float2 lightmapUV : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float2 uv : TEXCOORD0;

                float3 positionWS : TEXCOORD1;

                float3 normalWS : TEXCOORD2;

                float4 tangentWS : TEXCOORD3;

                float3 viewDirWS : TEXCOORD4;

                DECLARE_LIGHTMAP_OR_SH(
                    lightmapUV,
                    vertexSH,
                    TEXCOORD5
                );

                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID

                UNITY_VERTEX_OUTPUT_STEREO
            };


            // ============================================================
            // VERTEX
            // ============================================================

            Varyings DistanceFogLitVertex(
                Attributes input
            )
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_TRANSFER_INSTANCE_ID(
                    input,
                    output
                );

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(
                    output
                );


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS,
                        input.tangentOS
                    );


                output.positionCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;

                output.normalWS =
                    normalInputs.normalWS;


                output.tangentWS =
                    float4(
                        normalInputs.tangentWS,
                        input.tangentOS.w
                        * GetOddNegativeScale()
                    );


                output.uv =
                    TRANSFORM_TEX(
                        input.texcoord,
                        _BaseMap
                    );


                output.viewDirWS =
                    GetWorldSpaceNormalizeViewDir(
                        positionInputs.positionWS
                    );


                OUTPUT_LIGHTMAP_UV(
                    input.lightmapUV,
                    unity_LightmapST,
                    output.lightmapUV
                );


                OUTPUT_SH(
                    output.normalWS,
                    output.vertexSH
                );


                return output;
            }


            // ============================================================
            // SURFACE
            // ============================================================

            void InitializeDistanceFogSurface(
                float2 uv,
                out SurfaceData surfaceData
            )
            {
                surfaceData =
                    (SurfaceData)0;


                bool useSecondSet =
                    IsSecondSetActive();


                half4 albedoAlpha =
                    useSecondSet
                    ? SAMPLE_TEXTURE2D(
                        _BaseMap2,
                        sampler_BaseMap2,
                        uv
                    )
                    : SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        uv
                    );


                surfaceData.albedo =
                    albedoAlpha.rgb
                    * _BaseColor.rgb;


                surfaceData.alpha =
                    Alpha(
                        albedoAlpha.a,
                        _BaseColor,
                        _Cutoff
                    );


                surfaceData.metallic =
                    _Metallic;


                surfaceData.smoothness =
                    _Smoothness;


                #ifdef _NORMALMAP

                    surfaceData.normalTS =
                        SampleNormal(
                            uv,
                            TEXTURE2D_ARGS(
                                _BumpMap,
                                sampler_BumpMap
                            ),
                            _BumpScale
                        );

                #else

                    surfaceData.normalTS =
                        half3(
                            0.0h,
                            0.0h,
                            1.0h
                        );

                #endif


                #ifdef _OCCLUSIONMAP

                    half occlusion =
                        SAMPLE_TEXTURE2D(
                            _OcclusionMap,
                            sampler_OcclusionMap,
                            uv
                        ).g;


                    surfaceData.occlusion =
                        LerpWhiteTo(
                            occlusion,
                            _OcclusionStrength
                        );

                #else

                    surfaceData.occlusion =
                        1.0h;

                #endif


                // Emission is always sampled.
                //
                // Do NOT use URP's SampleEmission() helper here: it returns
                // 0 unless the _EMISSION keyword is enabled, and that keyword
                // is only auto-set by the built-in URP Lit custom editor.
                // This shader has no custom editor, so _EMISSION would never
                // turn on and emission would silently do nothing.
                //
                // Sampling the map directly keeps emission working for every
                // material: black emission color (the default) stays dark,
                // and any material with a map/color emits.

                surfaceData.emission =
                    (
                        useSecondSet
                        ? SAMPLE_TEXTURE2D(
                            _EmissionMap2,
                            sampler_EmissionMap2,
                            uv
                        )
                        : SAMPLE_TEXTURE2D(
                            _EmissionMap,
                            sampler_EmissionMap,
                            uv
                        )
                    ).rgb
                    * _EmissionColor.rgb;


                surfaceData.specular =
                    0.0h;

                surfaceData.clearCoatMask =
                    0.0h;

                surfaceData.clearCoatSmoothness =
                    1.0h;
            }


            // ============================================================
            // INPUT DATA
            // ============================================================

            void InitializeDistanceFogInput(
                Varyings input,
                half3 normalTS,
                out InputData inputData
            )
            {
                inputData =
                    (InputData)0;


                inputData.positionWS =
                    input.positionWS;


                inputData.viewDirectionWS =
                    SafeNormalize(
                        input.viewDirWS
                    );


                half3 normalWS =
                    NormalizeNormalPerPixel(
                        input.normalWS
                    );


                #ifdef _NORMALMAP

                    float3 bitangent =
                        input.tangentWS.w
                        * cross(
                            input.normalWS,
                            input.tangentWS.xyz
                        );


                    float3x3 tangentToWorld =
                        float3x3(
                            input.tangentWS.xyz,
                            bitangent,
                            input.normalWS
                        );


                    normalWS =
                        TransformTangentToWorld(
                            normalTS,
                            tangentToWorld
                        );


                    normalWS =
                        NormalizeNormalPerPixel(
                            normalWS
                        );

                #endif


                inputData.normalWS =
                    normalWS;


                inputData.shadowCoord =
                    TransformWorldToShadowCoord(
                        input.positionWS
                    );


                inputData.bakedGI =
                    SAMPLE_GI(
                        input.lightmapUV,
                        input.vertexSH,
                        normalWS
                    );


                inputData.fogCoord =
                    0.0h;


                inputData.vertexLighting =
                    0.0h;


                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(
                        input.positionCS
                    );


                inputData.shadowMask =
                    half4(
                        1.0h,
                        1.0h,
                        1.0h,
                        1.0h
                    );
            }


            // ============================================================
            // FRAGMENT
            // ============================================================

            half4 DistanceFogLitFragment(
                Varyings input
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(
                    input
                );


                ApplyDistanceFogDither(
                    input.positionWS,
                    input.positionCS,
                    _DistanceFogEnabled,
                    _DistanceFogDitherScale,
                    _DistanceFogDitherBias
                );


                SurfaceData surfaceData;


                InitializeDistanceFogSurface(
                    input.uv,
                    surfaceData
                );


                #ifdef _ALPHATEST_ON

                    clip(
                        surfaceData.alpha - _Cutoff
                    );

                #endif


                InputData inputData;


                InitializeDistanceFogInput(
                    input,
                    surfaceData.normalTS,
                    inputData
                );


                half4 color =
                    UniversalFragmentPBR(
                        inputData,
                        surfaceData
                    );


                return color;
            }

            ENDHLSL
        }


        // ================================================================
        // SHADOW CASTER
        // ================================================================

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On

            ZTest LEqual

            ColorMask 0

            Cull [_Cull]

            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex DistanceFogShadowVertex

            #pragma fragment DistanceFogShadowFragment

            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment _ALPHATEST_ON


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "CommonFog.hlsl"

            #include "Bayer4x4.hlsl"


            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;

                // Required by TRANSFORM_TEX().
                float4 _BaseMap_ST;

                float _Cutoff;

                float _SwapInterval;

                float _DistanceFogEnabled;

                float _DistanceFogDitherScale;

                float _DistanceFogDitherBias;

            CBUFFER_END


            TEXTURE2D(_BaseMap);

            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_BaseMap2);

            SAMPLER(sampler_BaseMap2);


            bool ShadowIsSecondSetActive()
            {
                if (_SwapInterval <= 0.0)
                    return false;

                float swapPeriod =
                    _SwapInterval * 2.0;

                return frac(
                    _Time.y / swapPeriod
                ) >= 0.5;
            }


            struct ShadowAttributes
            {
                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float2 uv : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            ShadowVaryings DistanceFogShadowVertex(
                ShadowAttributes input
            )
            {
                ShadowVaryings output;

                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_TRANSFER_INSTANCE_ID(
                    input,
                    output
                );


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                output.positionCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;


                output.uv =
                    TRANSFORM_TEX(
                        input.texcoord,
                        _BaseMap
                    );


                return output;
            }


            half4 DistanceFogShadowFragment(
                ShadowVaryings input
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


                #ifdef _ALPHATEST_ON

                    half alpha =
                        (
                            ShadowIsSecondSetActive()
                            ? SAMPLE_TEXTURE2D(
                                _BaseMap2,
                                sampler_BaseMap2,
                                input.uv
                            )
                            : SAMPLE_TEXTURE2D(
                                _BaseMap,
                                sampler_BaseMap,
                                input.uv
                            )
                        ).a
                        * _BaseColor.a;


                    clip(
                        alpha - _Cutoff
                    );

                #endif


                ApplyDistanceFogDither(
                    input.positionWS,
                    input.positionCS,
                    _DistanceFogEnabled,
                    _DistanceFogDitherScale,
                    _DistanceFogDitherBias
                );


                return 0;
            }

            ENDHLSL
        }


        // ================================================================
        // DEPTH ONLY
        // ================================================================

        Pass
        {
            Name "DepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On

            ColorMask 0

            Cull [_Cull]

            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex DistanceFogDepthVertex

            #pragma fragment DistanceFogDepthFragment

            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment _ALPHATEST_ON


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "CommonFog.hlsl"

            #include "Bayer4x4.hlsl"


            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;

                // Required by TRANSFORM_TEX().
                float4 _BaseMap_ST;

                float _Cutoff;

                float _SwapInterval;

                float _DistanceFogEnabled;

                float _DistanceFogDitherScale;

                float _DistanceFogDitherBias;

            CBUFFER_END


            TEXTURE2D(_BaseMap);

            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_BaseMap2);

            SAMPLER(sampler_BaseMap2);


            bool DepthIsSecondSetActive()
            {
                if (_SwapInterval <= 0.0)
                    return false;

                float swapPeriod =
                    _SwapInterval * 2.0;

                return frac(
                    _Time.y / swapPeriod
                ) >= 0.5;
            }


            struct DepthAttributes
            {
                float4 positionOS : POSITION;

                float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float2 uv : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            DepthVaryings DistanceFogDepthVertex(
                DepthAttributes input
            )
            {
                DepthVaryings output;

                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_TRANSFER_INSTANCE_ID(
                    input,
                    output
                );


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                output.positionCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;


                output.uv =
                    TRANSFORM_TEX(
                        input.texcoord,
                        _BaseMap
                    );


                return output;
            }


            half4 DistanceFogDepthFragment(
                DepthVaryings input
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


                #ifdef _ALPHATEST_ON

                    half alpha =
                        (
                            DepthIsSecondSetActive()
                            ? SAMPLE_TEXTURE2D(
                                _BaseMap2,
                                sampler_BaseMap2,
                                input.uv
                            )
                            : SAMPLE_TEXTURE2D(
                                _BaseMap,
                                sampler_BaseMap,
                                input.uv
                            )
                        ).a
                        * _BaseColor.a;


                    clip(
                        alpha - _Cutoff
                    );

                #endif


                ApplyDistanceFogDither(
                    input.positionWS,
                    input.positionCS,
                    _DistanceFogEnabled,
                    _DistanceFogDitherScale,
                    _DistanceFogDitherBias
                );


                return 0;
            }

            ENDHLSL
        }


        // ================================================================
        // DEPTH NORMALS
        // ================================================================

        Pass
        {
            Name "DepthNormals"

            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }

            ZWrite On

            Cull [_Cull]

            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex DistanceFogDepthNormalsVertex

            #pragma fragment DistanceFogDepthNormalsFragment

            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma shader_feature_local_fragment _NORMALMAP


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            #include "CommonFog.hlsl"

            #include "Bayer4x4.hlsl"


            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;

                // Required by TRANSFORM_TEX().
                float4 _BaseMap_ST;

                float _Cutoff;

                float _BumpScale;

                float _DistanceFogEnabled;

                float _DistanceFogDitherScale;

                float _DistanceFogDitherBias;

            CBUFFER_END


            // IMPORTANT:
            //
            // _BaseMap and sampler_BaseMap are NOT redeclared here.
            //
            // SurfaceInput.hlsl already provides them.


            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                float4 tangentOS : TANGENT;

                float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float3 normalWS : TEXCOORD1;

                float4 tangentWS : TEXCOORD2;

                float2 uv : TEXCOORD3;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            DepthNormalsVaryings DistanceFogDepthNormalsVertex(
                DepthNormalsAttributes input
            )
            {
                DepthNormalsVaryings output;

                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_TRANSFER_INSTANCE_ID(
                    input,
                    output
                );


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS,
                        input.tangentOS
                    );


                output.positionCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;

                output.normalWS =
                    normalInputs.normalWS;


                output.tangentWS =
                    float4(
                        normalInputs.tangentWS,
                        input.tangentOS.w
                        * GetOddNegativeScale()
                    );


                output.uv =
                    TRANSFORM_TEX(
                        input.texcoord,
                        _BaseMap
                    );


                return output;
            }


            half4 DistanceFogDepthNormalsFragment(
                DepthNormalsVaryings input
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


                #ifdef _ALPHATEST_ON

                    half alpha =
                        SAMPLE_TEXTURE2D(
                            _BaseMap,
                            sampler_BaseMap,
                            input.uv
                        ).a
                        * _BaseColor.a;


                    clip(
                        alpha - _Cutoff
                    );

                #endif


                ApplyDistanceFogDither(
                    input.positionWS,
                    input.positionCS,
                    _DistanceFogEnabled,
                    _DistanceFogDitherScale,
                    _DistanceFogDitherBias
                );


                half3 normalWS =
                    NormalizeNormalPerPixel(
                        input.normalWS
                    );


                #ifdef _NORMALMAP

                    half3 normalTS =
                        SampleNormal(
                            input.uv,
                            TEXTURE2D_ARGS(
                                _BumpMap,
                                sampler_BumpMap
                            ),
                            _BumpScale
                        );


                    float3 bitangent =
                        input.tangentWS.w
                        * cross(
                            input.normalWS,
                            input.tangentWS.xyz
                        );


                    float3x3 tangentToWorld =
                        float3x3(
                            input.tangentWS.xyz,
                            bitangent,
                            input.normalWS
                        );


                    normalWS =
                        TransformTangentToWorld(
                            normalTS,
                            tangentToWorld
                        );


                    normalWS =
                        NormalizeNormalPerPixel(
                            normalWS
                        );

                #endif


                return half4(
                    normalWS * 0.5h + 0.5h,
                    0.0h
                );
            }

            ENDHLSL
        }


        // ================================================================
        // META
        // ================================================================

        Pass
        {
            Name "Meta"

            Tags
            {
                "LightMode" = "Meta"
            }

            Cull Off

            HLSLPROGRAM

            #pragma vertex DistanceFogMetaVertex

            #pragma fragment DistanceFogMetaFragment


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"


            struct MetaAttributes
            {
                float4 positionOS : POSITION;

                float2 texcoord0 : TEXCOORD0;

                float2 texcoord1 : TEXCOORD1;

                float2 texcoord2 : TEXCOORD2;
            };


            struct MetaVaryings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;
            };


            MetaVaryings DistanceFogMetaVertex(
                MetaAttributes input
            )
            {
                MetaVaryings output;


                // Pass only the xyz position to avoid the implicit
                // float4 -> float3 truncation warning.

                output.positionCS =
                    UnityMetaVertexPosition(
                        input.positionOS.xyz,
                        input.texcoord1,
                        input.texcoord2
                    );


                output.uv =
                    input.texcoord0;


                return output;
            }


            half4 DistanceFogMetaFragment(
                MetaVaryings input
            ) : SV_Target
            {
                MetaInput metaInput;


                metaInput.Albedo =
                    0.5h;


                metaInput.Emission =
                    0.0h;


                return MetaFragment(
                    metaInput
                );
            }

            ENDHLSL
        }
    }


    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}