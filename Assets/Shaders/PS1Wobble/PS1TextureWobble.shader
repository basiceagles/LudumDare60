
Shader "Custom/PS1TextureWobble"
{
    Properties
    {
        _WarpAmount ("Wobble Strength", Range(0, 0.02)) = 0.0015
        _WarpFrequency ("Wobble Frequency", Range(1, 100)) = 25
        _WarpSpeed ("Wobble Speed", Range(0, 10)) = 1
        _PixelSize ("Pixelation Size", Range(1, 8)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "PS1TextureWobble"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _WarpAmount;
                float _WarpFrequency;
                float _WarpSpeed;
                float _PixelSize;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // Animated, subtle horizontal texture wobble.
                float wave1 = sin(
                    uv.y * _WarpFrequency +
                    _Time.y * _WarpSpeed
                );

                float wave2 = sin(
                    uv.y * (_WarpFrequency * 0.47) -
                    _Time.y * (_WarpSpeed * 0.7)
                );

                float wave3 = sin(
                    uv.x * (_WarpFrequency * 0.35) +
                    _Time.y * (_WarpSpeed * 0.5)
                );

                uv.x += (wave1 + wave2 * 0.5)
                        * _WarpAmount;

                uv.y += wave3 * _WarpAmount * 0.25;

                // Optional screen-space pixelation.
                float2 texelSize = _BlitTexture_TexelSize.xy;
                float pixelSize = max(_PixelSize, 1.0);

                if (pixelSize > 1.0)
                {
                    float2 pixelStep = texelSize * pixelSize;
                    uv = (floor(uv / pixelStep) + 0.5)
                         * pixelStep;
                }

                // Keep samples inside the screen boundaries.
                uv = saturate(uv);

                half4 color = SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv,
                    0
                );

                return color;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
