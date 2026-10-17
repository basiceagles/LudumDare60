Shader "Distance Dither Fog/Light Beam"
{
    Properties
    {
        // ============================================================
        // BEAM COLOR
        // ============================================================

        _TintColor(
            "Color (Additive)",
            Color
        ) = (1, 0.8, 0.6, 1)

        _Intensity(
            "Intensity",
            Range(0,10)
        ) = 1


        // ============================================================
        // BEAM SHAPE
        //
        // All values are based on the object's own proportions.
        // The world position of the object is ignored.
        //
        // The mesh is assumed to be 1 unit tall, centered at the
        // origin (the default Unity cylinder/cone), pointing up.
        //
        // _BeamRadius is the beam radius AT THE BASE, in object units.
        // Set it to match your mesh so the soft edge lands on the
        // surface instead of inside or outside it.
        //
        // height fraction 0.0 (base) -> 1.0 (tip)
        // ============================================================

        _BeamRadius(
            "Beam Radius",
            Range(0.01,10)
        ) = 0.5

        _EdgeSoftness(
            "Edge Softness",
            Range(0,4)
        ) = 2

        _FadeTop(
            "Fade Tip From (Height Fraction)",
            Range(0,1)
        ) = 0.7

        _FadeTopPower(
            "Fade Tip Power",
            Range(0.25,4)
        ) = 2

        _FadeBottom(
            "Fade Base To (Height Fraction)",
            Range(0,1)
        ) = 0

        _FadeBottomPower(
            "Fade Base Power",
            Range(0.25,4)
        ) = 2


        // ============================================================
        // DEPTH FADE
        //
        // Softly fades the beam out where it is close to or embedded
        // in other meshes, so it blends cleanly instead of clipping
        // with a hard edge.
        // ============================================================

        _DepthFadeDistance(
            "Depth Fade Distance",
            Range(0.01,100)
        ) = 1


        // ============================================================
        // DISTANCE DITHER FOG
        //
        // Same shared dithering as the other Distance Dither Fog
        // materials. The beam progressively disappears with distance.
        // ============================================================

        [Toggle]
        _DistanceFogEnabled(
            "Distance Fog Enabled",
            Float
        ) = 1

        _DistanceFogDitherScale(
            "Dither Scale",
            Range(1,4)
        ) = 1

        _DistanceFogDitherBias(
            "Dither Bias",
            Float
        ) = 0


        // ============================================================
        // RENDERING
        // ============================================================

        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull(
            "Cull",
            Float
        ) = 0
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "UniversalMaterialType" = "Unlit"
        }


        LOD 250


        // ============================================================
        // FORWARD
        // ============================================================

        Pass
        {
            Name "LightBeam"

            Tags
            {
                "LightMode" = "UniversalForward"
            }


            // Additive blend: the beam adds light on top of the
            // scene. Black adds nothing (invisible).
            Blend One One

            ZWrite Off

            ZTest LEqual

            Cull [_Cull]


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex Vert

            #pragma fragment Frag


            // ============================================================
            // INSTANCING
            // ============================================================

            #pragma multi_compile_instancing


            // ============================================================
            // URP
            // ============================================================

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


            // ============================================================
            // Distance fog libraries
            // ============================================================

            #include "CommonFog.hlsl"

            #include "Bayer4x4.hlsl"


            // ============================================================
            // MATERIAL
            // ============================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _TintColor;

                float _Intensity;

                float _BeamRadius;

                float _EdgeSoftness;

                float _FadeTop;

                float _FadeTopPower;

                float _FadeBottom;

                float _FadeBottomPower;

                float _DepthFadeDistance;

                float _DistanceFogEnabled;

                float _DistanceFogDitherScale;

                float _DistanceFogDitherBias;

                float _Cull;

            CBUFFER_END


            // ============================================================
            // STRUCTURES
            // ============================================================

            struct Attributes
            {
                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float3 localPosition : TEXCOORD1;

                float4 screenPosition : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID

                UNITY_VERTEX_OUTPUT_STEREO
            };


            // ============================================================
            // VERTEX
            // ============================================================

            Varyings Vert(
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


                output.localPosition =
                    input.positionOS.xyz;


                float3 positionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz
                    );


                output.positionWS =
                    positionWS;


                output.positionCS =
                    TransformWorldToHClip(
                        positionWS
                    );


                output.screenPosition =
                    ComputeScreenPos(
                        output.positionCS
                    );


                return output;
            }


            // ============================================================
            // BEAM MASK
            //
            // Builds the beam shape in object space:
            //
            //     0 = fully invisible
            //     1 = full strength
            //
            // THREE parts are multiplied together:
            //
            //     edge fade:  soft falloff from the center axis to the
            //                 surface of the beam.
            //
            //     tip fade:   falls off toward the top (far end), so
            //                 the beam ends softly instead of being cut.
            //
            //     base fade:  optional falloff near the light source.
            //
            // For a cone mesh the surface moves toward the center axis
            // with height, so the edge fade naturally follows the cone
            // silhouette.
            // ============================================================

            float CalculateBeamMask(
                float3 localPosition
            )
            {
                // ----------------------------------------------------
                // Height fraction.
                //
                // Assumes a mesh 1 unit tall, centered at the origin.
                //
                // 0.0 = base of the beam
                // 1.0 = tip of the beam
                // ----------------------------------------------------

                float heightFraction =
                    saturate(
                        localPosition.y + 0.5
                    );


                // ----------------------------------------------------
                // Edge fade.
                //
                // 0.0 at the center axis
                // 1.0 at the beam surface
                // ----------------------------------------------------

                float radial =
                    length(
                        localPosition.xz
                    )
                    / max(
                        _BeamRadius,
                        0.001
                    );


                float edgeFade =
                    pow(
                        saturate(
                            1.0 - radial
                        ),
                        _EdgeSoftness
                    );


                // ----------------------------------------------------
                // Tip fade (far end).
                //
                // Full strength up to _FadeTop, then falls to zero
                // at the very tip.
                // ----------------------------------------------------

                float tipFade =
                    1.0
                    -
                    smoothstep(
                        _FadeTop,
                        1.0,
                        heightFraction
                    );


                tipFade =
                    pow(
                        saturate(tipFade),
                        _FadeTopPower
                    );


                // ----------------------------------------------------
                // Base fade (optional).
                //
                // With _FadeBottom = 0 the beam stays at full strength
                // at the light source. Raising it fades the base.
                // ----------------------------------------------------

                float baseFade =
                    smoothstep(
                        0.0,
                        max(
                            _FadeBottom,
                            0.001
                        ),
                        heightFraction
                    );


                baseFade =
                    pow(
                        saturate(baseFade),
                        _FadeBottomPower
                    );


                return edgeFade * tipFade * baseFade;
            }


            // ============================================================
            // DEPTH FADE
            //
            // Softens the edge where the beam touches or intersects
            // other meshes. In open air the beam stays at full strength.
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


                float beamEyeDepth =
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
                        beamEyeDepth,
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

            half4 Frag(
                Varyings input
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(
                    input
                );


                // ----------------------------------------------------
                // Beam shape.
                // ----------------------------------------------------

                float beamMask =
                    CalculateBeamMask(
                        input.localPosition
                    );


                // ----------------------------------------------------
                // Depth fade.
                // ----------------------------------------------------

                float depthFade =
                    CalculateDepthFade(
                        input.screenPosition
                    );


                // ----------------------------------------------------
                // Distance dither.
                //
                // Same shared dithering as the other Distance Dither
                // Fog materials: the beam progressively disappears
                // with distance from the camera.
                // ----------------------------------------------------

                ApplyDistanceFogDither(
                    input.positionWS,
                    input.positionCS,
                    _DistanceFogEnabled,
                    _DistanceFogDitherScale,
                    _DistanceFogDitherBias
                );


                // ----------------------------------------------------
                // Additive color.
                //
                // Blend One One, so the color is added straight onto
                // the scene. No lights, no textures.
                // ----------------------------------------------------

                half3 beamColor =
                    _TintColor.rgb
                    * _Intensity
                    * beamMask
                    * depthFade;


                return half4(
                    beamColor,
                    1.0
                );
            }

            ENDHLSL
        }
    }


    FallBack Off
}
