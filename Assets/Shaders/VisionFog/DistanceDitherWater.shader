Shader "Distance Dither Fog/Water"
{
    Properties
    {
        // ================================================================
        // WATER COLOR
        // ================================================================

        _WaterColor(
            "Water Color",
            Color
        ) = (0.05, 0.25, 0.35, 1)

        _DeepColor(
            "Deep Water Color",
            Color
        ) = (0.01, 0.08, 0.12, 1)

        _WaterOpacity(
            "Water Opacity",
            Range(0,1)
        ) = 0.65

        _DepthColorStrength(
            "Depth Color Strength",
            Range(0,1)
        ) = 0.75


        // ================================================================
        // WATER TEXTURE
        // ================================================================

        _WaterTexture(
            "Water Texture",
            2D
        ) = "white" {}

        _WaterTextureStrength(
            "Water Texture Strength",
            Range(0,2)
        ) = 0.35

        _TextureTiling(
            "Texture Tiling",
            Float
        ) = 1

        _WaterUVScale(
            "Water UV Scale (World)",
            Range(0.1,20)
        ) = 1


        // ================================================================
        // NORMAL MAP 1
        // ================================================================

        _NormalMap1(
            "Normal Map 1",
            2D
        ) = "bump" {}

        _NormalStrength1(
            "Normal Strength 1",
            Range(0,2)
        ) = 0.6

        _NormalSpeed1(
            "Normal Speed 1",
            Vector
        ) = (0.035, 0.02, 0, 0)


        // ================================================================
        // NORMAL MAP 2
        // ================================================================

        _NormalMap2(
            "Normal Map 2",
            2D
        ) = "bump" {}

        _NormalStrength2(
            "Normal Strength 2",
            Range(0,2)
        ) = 0.4

        _NormalSpeed2(
            "Normal Speed 2",
            Vector
        ) = (-0.025, 0.015, 0, 0)


        // ================================================================
        // RIPPLE / WAVE DISPLACEMENT
        // ================================================================

        _WaveAmplitude(
            "Wave Amplitude",
            Range(0,1)
        ) = 0.08

        _WaveFrequency(
            "Wave Frequency",
            Range(0.1,10)
        ) = 2.0

        _WaveSpeed(
            "Wave Speed",
            Range(0,5)
        ) = 0.8

        _WaveDirection(
            "Wave Direction",
            Vector
        ) = (1, 0.25, 0, 0)


        _Wave2Amplitude(
            "Wave 2 Amplitude",
            Range(0,1)
        ) = 0.045

        _Wave2Frequency(
            "Wave 2 Frequency",
            Range(0.1,10)
        ) = 3.5

        _Wave2Speed(
            "Wave 2 Speed",
            Range(0,5)
        ) = 1.15

        _Wave2Direction(
            "Wave 2 Direction",
            Vector
        ) = (-0.45, 1, 0, 0)


        _Wave3Amplitude(
            "Wave 3 Amplitude",
            Range(0,1)
        ) = 0.025

        _Wave3Frequency(
            "Wave 3 Frequency",
            Range(0.1,15)
        ) = 6.0

        _Wave3Speed(
            "Wave 3 Speed",
            Range(0,5)
        ) = 1.7

        _Wave3Direction(
            "Wave 3 Direction",
            Vector
        ) = (0.8, 0.65, 0, 0)


        _WaveParallax(
            "Wave Parallax (Pixel Waves)",
            Range(0,1)
        ) = 0.1


        // ================================================================
        // WATER RIPPLE BREAKUP
        // ================================================================

        _RippleBreakup(
            "Ripple Breakup",
            Range(0,1)
        ) = 0.45

        _RippleScale(
            "Ripple Scale",
            Range(0.1,20)
        ) = 4


        // ================================================================
        // FRESNEL
        // ================================================================

        _FresnelPower(
            "Fresnel Power",
            Range(0.1,10)
        ) = 4

        _FresnelStrength(
            "Fresnel Strength",
            Range(0,2)
        ) = 0.65

        _FresnelColor(
            "Fresnel Color",
            Color
        ) = (0.15, 0.5, 0.65, 1)


        // ================================================================
        // DEPTH
        // ================================================================

        _DepthFadeDistance(
            "Depth Fade Distance",
            Range(0.01,100)
        ) = 5

        _ShallowColor(
            "Shallow Color",
            Color
        ) = (0.08, 0.45, 0.45, 1)


        // ================================================================
        // DISTANCE DITHER FOG
        // ================================================================

        [Toggle]
        _DistanceFogEnabled(
            "Distance Fog Enabled",
            Float
        ) = 1

        _DistanceFogStart(
            "Distance Fog Start",
            Float
        ) = 20

        _DistanceFogEnd(
            "Distance Fog End",
            Float
        ) = 50

        _DistanceFogDitherScale(
            "Dither Scale",
            Range(0.25,4)
        ) = 1

        _DistanceFogDitherBias(
            "Dither Bias",
            Range(-1,1)
        ) = 0


        // ================================================================
        // RENDERING
        // ================================================================

        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull(
            "Cull",
            Float
        ) = 2
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "UniversalMaterialType" = "Lit"
        }

        LOD 300


        // ================================================================
        // FORWARD WATER
        // ================================================================

        Pass
        {
            Name "WaterForward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite Off

            ZTest LEqual

            Cull [_Cull]


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex WaterVertex

            #pragma fragment WaterFragment


            // ============================================================
            // UNITY KEYWORDS
            // ============================================================

            #pragma multi_compile_fog

            #pragma multi_compile_instancing

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP


            // ============================================================
            // URP
            // ============================================================

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


            // ============================================================
            // DITHER
            // ============================================================

            #include "Bayer4x4.hlsl"


            // ============================================================
            // TEXTURES
            // ============================================================

            TEXTURE2D(_WaterTexture);
            SAMPLER(sampler_WaterTexture);

            TEXTURE2D(_NormalMap1);
            SAMPLER(sampler_NormalMap1);

            TEXTURE2D(_NormalMap2);
            SAMPLER(sampler_NormalMap2);


            // ============================================================
            // MATERIAL DATA
            // ============================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _WaterColor;
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FresnelColor;

                float4 _WaterTexture_ST;

                float4 _NormalMap1_ST;
                float4 _NormalMap2_ST;

                float _WaterOpacity;

                float _DepthColorStrength;

                float _WaterTextureStrength;

                float _TextureTiling;

                float _WaterUVScale;


                float _NormalStrength1;

                float4 _NormalSpeed1;


                float _NormalStrength2;

                float4 _NormalSpeed2;


                float _WaveAmplitude;

                float _WaveFrequency;

                float _WaveSpeed;

                float4 _WaveDirection;


                float _Wave2Amplitude;

                float _Wave2Frequency;

                float _Wave2Speed;

                float4 _Wave2Direction;


                float _Wave3Amplitude;

                float _Wave3Frequency;

                float _Wave3Speed;

                float4 _Wave3Direction;

                float _WaveParallax;


                float _RippleBreakup;

                float _RippleScale;


                float _FresnelPower;

                float _FresnelStrength;


                float _DepthFadeDistance;


                float _DistanceFogEnabled;

                float _DistanceFogStart;

                float _DistanceFogEnd;

                float _DistanceFogDitherScale;

                float _DistanceFogDitherBias;


            CBUFFER_END


            // ============================================================
            // STRUCTURES
            // ============================================================

            struct Attributes
            {
                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                float4 tangentOS : TANGENT;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float3 normalWS : TEXCOORD1;

                float2 uv : TEXCOORD2;

                float4 screenPosition : TEXCOORD3;

                float3 viewDirectionWS : TEXCOORD4;

                UNITY_VERTEX_INPUT_INSTANCE_ID

                UNITY_VERTEX_OUTPUT_STEREO
            };


            // ============================================================
            // HASH
            // Cheap deterministic hash for water breakup.
            // ============================================================

            float Hash21(float2 p)
            {
                p =
                    frac(
                        p * float2(
                            123.34,
                            456.21
                        )
                    );

                p +=
                    dot(
                        p,
                        p + 45.32
                    );

                return frac(
                    p.x * p.y
                );
            }


            // ============================================================
            // VALUE NOISE
            // ============================================================

            float ValueNoise(float2 p)
            {
                float2 i =
                    floor(p);

                float2 f =
                    frac(p);

                f =
                    f * f *
                    (
                        3.0
                        -
                        2.0 * f
                    );


                float a =
                    Hash21(
                        i
                    );

                float b =
                    Hash21(
                        i + float2(1, 0)
                    );

                float c =
                    Hash21(
                        i + float2(0, 1)
                    );

                float d =
                    Hash21(
                        i + float2(1, 1)
                    );


                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }


            // ============================================================
            // WATER BREAKUP
            //
            // This prevents the entire plane from moving as one giant
            // sine wave. Instead the waves are modulated by several
            // different noise scales.
            // ============================================================

            float CalculateRippleBreakup(
                float2 positionXZ
            )
            {
                float n1 =
                    ValueNoise(
                        positionXZ
                        * _RippleScale
                    );


                float n2 =
                    ValueNoise(
                        positionXZ
                        * _RippleScale
                        * 2.17
                        + 37.4
                    );


                float n3 =
                    ValueNoise(
                        positionXZ
                        * _RippleScale
                        * 4.31
                        - 18.7
                    );


                float noise =
                    n1 * 0.55
                    +
                    n2 * 0.30
                    +
                    n3 * 0.15;


                return lerp(
                    1.0,
                    noise,
                    _RippleBreakup
                );
            }


            // ============================================================
            // WAVE
            // ============================================================

            float CalculateWave(
                float2 positionXZ,
                float2 direction,
                float frequency,
                float speed,
                float time
            )
            {
                direction =
                    normalize(
                        direction
                    );


                float phase =
                    dot(
                        positionXZ,
                        direction
                    )
                    *
                    frequency
                    +
                    time
                    *
                    speed;


                return sin(
                    phase
                );
            }


            // ============================================================
            // TOTAL WATER DISPLACEMENT
            // ============================================================

            float CalculateWaterHeight(
                float2 positionXZ
            )
            {
                float time =
                    _Time.y;


                float wave1 =
                    CalculateWave(
                        positionXZ,
                        _WaveDirection.xy,
                        _WaveFrequency,
                        _WaveSpeed,
                        time
                    );


                float wave2 =
                    CalculateWave(
                        positionXZ,
                        _Wave2Direction.xy,
                        _Wave2Frequency,
                        _Wave2Speed,
                        time
                    );


                float wave3 =
                    CalculateWave(
                        positionXZ,
                        _Wave3Direction.xy,
                        _Wave3Frequency,
                        _Wave3Speed,
                        time
                    );


                // Break the waves into irregular patches.
                float breakup =
                    CalculateRippleBreakup(
                        positionXZ
                    );


                float height =
                    wave1
                    *
                    _WaveAmplitude
                    *
                    breakup;


                height +=
                    wave2
                    *
                    _Wave2Amplitude
                    *
                    breakup;


                height +=
                    wave3
                    *
                    _Wave3Amplitude;


                return height;
            }


            // ============================================================
            // VERTEX NORMAL FROM WAVE HEIGHT
            // ============================================================

            float3 CalculateWaveNormal(
                float2 positionXZ
            )
            {
                const float sampleDistance = 0.08;


                float center =
                    CalculateWaterHeight(
                        positionXZ
                    );


                float sampleX =
                    CalculateWaterHeight(
                        positionXZ
                        +
                        float2(
                            sampleDistance,
                            0
                        )
                    );


                float sampleZ =
                    CalculateWaterHeight(
                        positionXZ
                        +
                        float2(
                            0,
                            sampleDistance
                        )
                    );


                float slopeX =
                    (
                        sampleX
                        -
                        center
                    )
                    /
                    sampleDistance;


                float slopeZ =
                    (
                        sampleZ
                        -
                        center
                    )
                    /
                    sampleDistance;


                return normalize(
                    float3(
                        -slopeX,
                        1.0,
                        -slopeZ
                    )
                );
            }


            // ============================================================
            // VERTEX
            // ============================================================

            Varyings WaterVertex(
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


                float3 positionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz
                    );


                // --------------------------------------------------------
                // The mesh is left geometrically flat.
                //
                // Waves are computed per-pixel in the fragment shader
                // (CalculateWaveHeight / CalculateWaveNormal), so they
                // appear everywhere on the surface even on a mesh with
                // only a handful of polygons.
                // --------------------------------------------------------

                output.positionWS =
                    positionWS;


                output.positionCS =
                    TransformWorldToHClip(
                        positionWS
                    );


                output.normalWS =
                    float3(0, 1, 0);


                output.uv =
                    input.uv;


                output.screenPosition =
                    ComputeScreenPos(
                        output.positionCS
                    );


                output.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(
                        positionWS
                    );


                return output;
            }


            // ============================================================
            // DISTANCE DITHER
            // ============================================================

            void ApplyWaterDistanceDither(
                float3 positionWS,
                float4 positionCS
            )
            {
                if (_DistanceFogEnabled < 0.5)
                    return;


                float distanceToCamera =
                    distance(
                        positionWS,
                        GetCameraPositionWS()
                    );


                float visibility =
                    1.0 -
                    smoothstep(
                        _DistanceFogStart,
                        _DistanceFogEnd,
                        distanceToCamera
                    );


                float2 pixelPosition =
                    positionCS.xy;


                float threshold =
                    DistanceFog_Bayer4x4Scaled(
                        pixelPosition,
                        _DistanceFogDitherScale
                    );


                threshold +=
                    _DistanceFogDitherBias;


                clip(
                    visibility
                    -
                    threshold
                );
            }


            // ============================================================
            // DEPTH FADE
            // ============================================================

            float CalculateDepthFade(
                float4 screenPosition
            )
            {
                float2 screenUV =
                    screenPosition.xy
                    /
                    screenPosition.w;


                float sceneDepth =
                    SampleSceneDepth(
                        screenUV
                    );


                float sceneEyeDepth =
                    LinearEyeDepth(
                        sceneDepth,
                        _ZBufferParams
                    );


                float waterEyeDepth =
                    LinearEyeDepth(
                        screenPosition.z
                        /
                        screenPosition.w,
                        _ZBufferParams
                    );


                float depthDifference =
                    max(
                        sceneEyeDepth
                        -
                        waterEyeDepth,
                        0.0
                    );


                return saturate(
                    depthDifference
                    /
                    max(
                        _DepthFadeDistance,
                        0.001
                    )
                );
            }


            // ============================================================
            // FRAGMENT
            // ============================================================

            half4 WaterFragment(
                Varyings input
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(
                    input
                );


                // --------------------------------------------------------
                // Distance visibility
                // --------------------------------------------------------

                ApplyWaterDistanceDither(
                    input.positionWS,
                    input.positionCS
                );


                // --------------------------------------------------------
                // Per-pixel waves.
                //
                // The wave height field is evaluated in the fragment
                // shader, so it animates everywhere on the surface even
                // when the mesh has only a handful of polygons.
                // --------------------------------------------------------

                float3 waveNormal =
                    CalculateWaveNormal(
                        input.positionWS.xz
                    );


                float waveHeight =
                    CalculateWaterHeight(
                        input.positionWS.xz
                    );


                // --------------------------------------------------------
                // Fake displacement (parallax).
                //
                // Shifts the sampling position along the view direction
                // by the local wave height. The surface itself stays
                // flat, but the shading/refraction slides with the
                // waves, which reads as real undulation.
                // --------------------------------------------------------

                float3 viewDir =
                    normalize(
                        input.viewDirectionWS
                    );


                float2 waveOffset =
                    waveHeight
                    *
                    _WaveParallax
                    *
                    viewDir.xz;


                float2 samplePos =
                    input.positionWS.xz
                    +
                    waveOffset;


                // --------------------------------------------------------
                // UV (world space so waves match across the mesh)
                // --------------------------------------------------------

                float2 worldUV =
                    samplePos
                    *
                    _WaterUVScale;


                float2 waterUV =
                    worldUV
                    *
                    _TextureTiling;


                float2 normalUV1 =
                    worldUV
                    +
                    _NormalMap1_ST.zw;


                float2 normalUV2 =
                    worldUV
                    +
                    _NormalMap2_ST.zw;


                float2 scrollingUV1 =
                    normalUV1
                    +
                    _Time.y
                    *
                    _NormalSpeed1.xy;


                float2 scrollingUV2 =
                    normalUV2
                    +
                    _Time.y
                    *
                    _NormalSpeed2.xy;


                // --------------------------------------------------------
                // Water texture
                // --------------------------------------------------------

                half3 waterTexture =
                    SAMPLE_TEXTURE2D(
                        _WaterTexture,
                        sampler_WaterTexture,
                        waterUV
                    ).rgb;


                // --------------------------------------------------------
                // Normal maps
                // --------------------------------------------------------

                half3 normalA =
                    UnpackNormalScale(
                        SAMPLE_TEXTURE2D(
                            _NormalMap1,
                            sampler_NormalMap1,
                            scrollingUV1
                        ),
                        _NormalStrength1
                    );


                half3 normalB =
                    UnpackNormalScale(
                        SAMPLE_TEXTURE2D(
                            _NormalMap2,
                            sampler_NormalMap2,
                            scrollingUV2
                        ),
                        _NormalStrength2
                    );


                half3 normalTS =
                    normalize(
                        normalA
                        +
                        normalB
                    );


                // --------------------------------------------------------
                // World normal
                // --------------------------------------------------------

                float3 worldNormal =
                    normalize(
                        waveNormal
                        +
                        float3(
                            normalTS.x,
                            normalTS.z,
                            normalTS.y
                        )
                        *
                        0.5
                    );


                // --------------------------------------------------------
                // Fresnel
                // --------------------------------------------------------

                float fresnel =
                    pow(
                        1.0 -
                        saturate(
                            dot(
                                worldNormal,
                                viewDir
                            )
                        ),
                        _FresnelPower
                    );


                fresnel *=
                    _FresnelStrength;


                // --------------------------------------------------------
                // Depth
                // --------------------------------------------------------

                float depthFade =
                    CalculateDepthFade(
                        input.screenPosition
                    );


                // --------------------------------------------------------
                // Water color
                // --------------------------------------------------------

                half3 depthColor =
                    lerp(
                        _DeepColor.rgb,
                        _ShallowColor.rgb,
                        depthFade
                    );


                half3 baseWater =
                    lerp(
                        depthColor,
                        _WaterColor.rgb,
                        _DepthColorStrength
                    );


                // --------------------------------------------------------
                // Texture
                // --------------------------------------------------------

                baseWater *=
                    lerp(
                        1.0h,
                        waterTexture,
                        _WaterTextureStrength
                    );


                // --------------------------------------------------------
                // Fresnel
                // --------------------------------------------------------

                baseWater =
                    lerp(
                        baseWater,
                        _FresnelColor.rgb,
                        saturate(
                            fresnel
                        )
                    );


                // --------------------------------------------------------
                // Lighting
                // --------------------------------------------------------

                Light mainLight =
                    GetMainLight();


                float NdotL =
                    saturate(
                        dot(
                            worldNormal,
                            mainLight.direction
                        )
                    );


                half3 diffuse =
                    baseWater
                    *
                    mainLight.color
                    *
                    NdotL;


                half3 ambient =
                    baseWater
                    *
                    0.35h;


                half3 finalColor =
                    ambient
                    +
                    diffuse;


                finalColor +=
                    _FresnelColor.rgb
                    *
                    fresnel;


                // --------------------------------------------------------
                // Alpha
                // --------------------------------------------------------

                float alpha =
                    _WaterOpacity;


                alpha *=
                    lerp(
                        0.55,
                        1.0,
                        depthFade
                    );


                return half4(
                    finalColor,
                    saturate(alpha)
                );
            }

            ENDHLSL
        }
    }


    FallBack Off
}