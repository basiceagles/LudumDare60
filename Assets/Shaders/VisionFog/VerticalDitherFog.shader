Shader "Distance Dither Fog/Vertical Hole Fog"
{
    Properties
    {
        // ============================================================
        // FOG COLOR
        // ============================================================

        _FogColor(
            "Fog Color (Multiply)",
            Color
        ) = (0, 0, 0, 1)


        // ============================================================
        // VERTICAL FOG
        //
        // Purely vertical, based on the object's own proportions.
        // The world position of the object is ignored.
        //
        // The values are FRACTIONS of the object's height (0-1):
        //
        // _FogTop    = 1.0 (top)    -> 0% fog (fully clear)
        // _FogBottom = 0.0 (bottom) -> 100% fog (fully dark)
        //
        // Like water: at the top you see through clearly, and the
        // fog colour grows stronger toward the bottom.
        //
        // Because the values are fractions, scaling the object up
        // automatically stretches the fog zone with it.
        // ============================================================

        _FogTop(
            "Clear At (Height Fraction)",
            Range(0,1)
        ) = 1

        _FogBottom(
            "Full Fog At (Height Fraction)",
            Range(0,1)
        ) = 0

        _FogPower(
            "Fog Power",
            Range(0.25,4)
        ) = 1


        // ============================================================
        // DEPTH FADE
        //
        // Smoothly fades the fog out where its surface touches or
        // intersects other meshes. Like water, the edge stays soft
        // instead of showing a hard seam where the planes cross.
        // ============================================================

        _DepthFadeDistance(
            "Depth Fade Distance",
            Range(0.01,100)
        ) = 2


        // ============================================================
        // DISTANCE DITHER FOG
        //
        // Same shared dithering as the other Distance Dither Fog
        // materials. The fog progressively disappears with distance.
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
            Name "VerticalHoleFog"

            Tags
            {
                "LightMode" = "UniversalForward"
            }


            // Multiply blend: result = src * dst.
            // White keeps the background unchanged (transparent),
            // dark colors darken the background (fog).
            Blend DstColor Zero

            ZWrite Off

            ZTest LEqual

            Cull Off

            // Depth offset to prevent z-fighting when this fog surface
            // is coplanar with (or attached to) another object.
            Offset -1, -1


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

                float4 _FogColor;

                float _FogTop;

                float _FogBottom;

                float _FogPower;

                float _DepthFadeDistance;

                float _DistanceFogEnabled;

                float _DistanceFogDitherScale;

                float _DistanceFogDitherBias;

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

                float localY : TEXCOORD1;

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


                output.localY =
                    input.positionOS.y;


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
            // VERTICAL FOG
            //
            // THIS IS THE IMPORTANT PART.
            //
            // We measure ONLY the vertical position of the object,
            // expressed as a FRACTION of the object's height (0-1).
            //
            // height fraction 0.0 (bottom) -> 100% fog (fully dark)
            // height fraction 1.0 (top)    -> 0% fog (fully clear)
            //
            // The world position of the object is ignored.
            //
            // Because the fraction is in object space, scaling the
            // object up stretches the fog gradient with it.
            //
            // Fully static: no camera distance, no depth, no light.
            // ============================================================

            float CalculateVerticalFog(
                float localY
            )
            {
                // ----------------------------------------------------
                // Local Y to height fraction.
                //
                // Assumes a mesh 1 unit tall, centered at the origin
                // (the default Unity cube).
                //
                // 0.0 = bottom of the object
                // 0.5 = middle of the object
                // 1.0 = top of the object
                // ----------------------------------------------------

                float heightFraction =
                    saturate(
                        localY + 0.5
                    );


                // ----------------------------------------------------
                // Full gradient.
                //
                // _FogTop    = 1.0 (top)    -> 0% fog (fully clear)
                // _FogBottom = 0.0 (bottom) -> 100% fog (fully dark)
                //
                // Like water: you see through clearly at the top,
                // and the fog colour grows stronger toward the bottom.
                // ----------------------------------------------------

                float fogAmount =
                    1.0
                    -
                    smoothstep(
                        _FogBottom,
                        _FogTop,
                        heightFraction
                    );


                // ----------------------------------------------------
                // Custom curve.
                // ----------------------------------------------------

                fogAmount =
                    pow(
                        saturate(fogAmount),
                        _FogPower
                    );


                return fogAmount;
            }


            // ============================================================
            // DEPTH FADE
            //
            // Softens the edge where the fog surface touches or
            // intersects other meshes. When the fog is right against
            // another object (depth difference ~ 0), it fades out
            // smoothly instead of showing a hard seam. In open air
            // the fog stays at full strength.
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


                float fogEyeDepth =
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
                        fogEyeDepth,
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
                // Vertical fog amount.
                // ----------------------------------------------------

                float fogAmount =
                    CalculateVerticalFog(
                        input.localY
                    );


                // ----------------------------------------------------
                // Depth fade.
                //
                // Fades the fog out where its surface is near another
                // mesh, so intersections get a smooth transition
                // instead of a hard seam.
                // ----------------------------------------------------

                float depthFade =
                    CalculateDepthFade(
                        input.screenPosition
                    );


                fogAmount *=
                    depthFade;


                // ----------------------------------------------------
                // Distance dither.
                //
                // Same shared dithering as the other Distance Dither
                // Fog materials: the fog progressively disappears
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
                // Multiply blend:
                // white = fully transparent (no darkening),
                // fog color = fully dark.
                // ----------------------------------------------------

                half3 multiplyColor =
                    lerp(
                        half3(1, 1, 1),
                        _FogColor.rgb,
                        fogAmount
                    );


                return half4(
                    multiplyColor,
                    1.0
                );
            }

            ENDHLSL
        }
    }


    FallBack Off
}
