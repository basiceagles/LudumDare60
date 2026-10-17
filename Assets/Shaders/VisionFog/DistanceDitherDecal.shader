Shader "Distance Dither Fog/Decal Projection"
{
    Properties
    {
        // ============================================================
        // DECAL SPRITE
        // ============================================================

        _DecalTexture(
            "Sprite Atlas",
            2D
        ) = "white" {}

        _TintColor(
            "Color",
            Color
        ) = (1, 1, 1, 1)


        [HideInInspector]
        _AtlasRect(
            "Atlas Rect (x, y, w, h)",
            Vector
        ) = (0, 0, 1, 1)


        // Scales the sprite INSIDE the mesh footprint without changing
        // the mesh. Larger values shrink the sprite, smaller values
        // enlarge (and crop) it. Set per decal via MaterialPropertyBlock.
        [HideInInspector]
        _DecalUVScale(
            "Decal UV Scale (x, y, 0, 0)",
            Vector
        ) = (1, 1, 0, 0)


        // ============================================================
        // PROJECTION
        //
        // The decal is a CUBE volume placed over (or overlapping) the
        // mesh that should receive the sprite.
        //
        // The sprite is projected straight down the cube's local -Y
        // axis onto whatever geometry is inside the volume.
        //
        // The cube's XZ footprint defines the projection volume. The
        // sprite is scaled INSIDE that footprint by _DecalUVScale (set
        // per decal from the sprite's size), so the mesh size is what
        // controls the volume and _DecalUVScale controls the sprite.
        //
        // _ProjectionFade fades the sprite out the deeper the projected
        // point is below the top of the volume, so the decal does not
        // bleed through thin geometry and appear on the far side.
        //
        // The fade is applied as a hard cutout, not as alpha transparency
        // or dithering, so the decal never looks semi-transparent.
        //
        // ATLAS: the Sprite Atlas texture can hold many sprites. Each
        // decal shares the SAME material and only changes _AtlasRect
        // (set per renderer via MaterialPropertyBlock), so all decals
        // batch together in a single draw call.
        //
        // _AtlasRect is the sprite's normalized region inside the atlas,
        // using the same convention as sprite.textureRect scaled by the
        // texture size:
        //
        //     (x, y) = bottom-left corner of the sprite in UV space
        //     (z, w) = positive width / height
        //
        //     (0, 0, 1, 1) samples the full texture.
        //     A 4x4 grid cell in row r, column c is:
        //         (c/4, r/4, 1/4, 1/4)   with r=0 at the BOTTOM
        // ============================================================

        _ProjectionFade(
            "Projection Fade",
            Range(0,1)
        ) = 0.25


        // ============================================================
        // DISTANCE DITHER FOG
        //
        // Same shared dithering as the other Distance Dither Fog
        // materials. The decal progressively disappears with distance.
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
            Name "DecalProjection"

            Tags
            {
                "LightMode" = "UniversalForward"
            }


            // Standard alpha blend: the sprite composites over the
            // existing surface color.
            Blend SrcAlpha OneMinusSrcAlpha

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

            TEXTURE2D(_DecalTexture);

            SAMPLER(sampler_DecalTexture);


            CBUFFER_START(UnityPerMaterial)

                float4 _TintColor;

                float4 _AtlasRect;

                float4 _DecalUVScale;

                float _ProjectionFade;

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

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float4 screenPosition : TEXCOORD1;

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
                // Reconstruct the world position of the scene geometry
                // behind this pixel from the depth buffer.
                // ----------------------------------------------------

                float2 screenUV =
                    input.screenPosition.xy
                    /
                    input.screenPosition.w;


                float sceneDepth =
                    SampleSceneDepth(
                        screenUV
                    );


                float3 sceneWS =
                    ComputeWorldSpacePosition(
                        screenUV,
                        sceneDepth,
                        UNITY_MATRIX_I_VP
                    );


                // ----------------------------------------------------
                // Convert the scene point into the decal volume's
                // local space.
                //
                // Unit cube (1x1x1, centered at the origin):
                //
                //     xz in -0.5..0.5 -> sprite UV 0..1
                //     y  is the projection depth
                // ----------------------------------------------------

                float3 decalOS =
                    TransformWorldToObject(
                        sceneWS
                    );


                float2 decalXY =
                    decalOS.xz + 0.5;


                // Discard pixels outside the cube's XZ footprint.
                // Keeps the projected decal edge crisp.

                if (
                    any(decalXY < 0.0)
                    || any(decalXY > 1.0)
                )
                {
                    discard;
                }


                // ----------------------------------------------------
                // Projection fade.
                //
                // projectedDepth 0 at the top of the volume (the face
                // nearest the surface) and 1 at the bottom.
                //
                // The sprite is strongest at the top and fades out the
                // deeper it projects, so it does not bleed through to
                // the far side of thin geometry.
                // ----------------------------------------------------

                float projectedDepth =
                    1.0
                    -
                    (decalOS.y + 0.5);


                float projectionFade =
                    1.0
                    -
                    smoothstep(
                        0.0,
                        max(
                            _ProjectionFade,
                            0.001
                        ),
                        projectedDepth
                    );


                // ----------------------------------------------------
                // Sample the sprite inside the atlas.
                //
                // decalXY (0..1 across the cube footprint) is first scaled
                // around the centre by _DecalUVScale, so the sprite can be
                // resized inside the mesh without changing the mesh, then
                // remapped into the tile defined by _AtlasRect.
                // ----------------------------------------------------

                float2 spriteUV =
                    (decalXY - 0.5)
                    * _DecalUVScale.xy
                    + 0.5;


                if (
                    any(spriteUV < 0.0)
                    || any(spriteUV > 1.0)
                )
                {
                    discard;
                }


                float2 decalUV =
                    spriteUV * _AtlasRect.zw
                    + _AtlasRect.xy;


                half4 sprite =
                    SAMPLE_TEXTURE2D(
                        _DecalTexture,
                        sampler_DecalTexture,
                        decalUV
                    );


                // ----------------------------------------------------
                // Distance dither.
                //
                // Same shared dithering as the other Distance Dither
                // Fog materials: the decal progressively disappears
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
                // Composite the sprite over the surface.
                //
                // The projection fade is applied as a hard cutout, so
                // every visible pixel stays fully opaque and there is no
                // dither pattern or semi-transparent look.
                // ----------------------------------------------------

                clip(
                    projectionFade
                    - 0.5
                );


                half3 decalColor =
                    sprite.rgb
                    * _TintColor.rgb;


                half decalAlpha =
                    sprite.a
                    * _TintColor.a;


                return half4(
                    decalColor,
                    decalAlpha
                );
            }

            ENDHLSL
        }
    }


    FallBack Off
}
