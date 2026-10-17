Shader "Hidden/Custom/DitherFullScreen"
{
    Properties
    {
        _BitDepth ("Bit Depth", Range(1,8)) = 4
        _DitherStrength ("Dither Strength", Range(0,1)) = 0.25
        _DitherScale ("Dither Scale", Range(1,8)) = 2
        _NoiseAmount ("Noise Amount", Range(0,1)) = 0.05
    }


    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


    TEXTURE2D_X(_BlitTexture);


    float _BitDepth;
    float _DitherStrength;
    float _DitherScale;
    float _NoiseAmount;



    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };


    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
    };



    Varyings Vert(Attributes input)
    {
        Varyings output;

        output.positionCS =
            GetFullScreenTriangleVertexPosition(input.vertexID);

        output.texcoord =
            GetFullScreenTriangleTexCoord(input.vertexID);

        return output;
    }



    static const float Bayer4[16] =
    {
         0,  8,  2, 10,
        12,  4, 14,  6,
         3, 11,  1,  9,
        15,  7, 13,  5
    };



    float BayerDither(float2 uv)
    {
        float2 pixel =
            floor(
                uv * _ScreenParams.xy /
                max(_DitherScale,1)
            );


        int x = (int)pixel.x & 3;
        int y = (int)pixel.y & 3;


        return Bayer4[y * 4 + x] / 16.0;
    }



    float RandomNoise(float2 uv)
    {
        return frac(
            sin(dot(uv,float2(12.9898,78.233)))
            * 43758.5453
        );
    }



    half4 Frag(Varyings input) : SV_Target
    {
        float2 uv = input.texcoord;


        half4 col =
            SAMPLE_TEXTURE2D_X(
                _BlitTexture,
                sampler_LinearClamp,
                uv
            );


        float dither =
            BayerDither(uv) - 0.5;


        // Calculate brightness
        float luminance =
            dot(col.rgb, float3(0.299, 0.587, 0.114));


        float levels =
            pow(2.0, _BitDepth);


        // Quantize brightness with dithering
        float newLuminance =
            floor(
                luminance * levels +
                dither * _DitherStrength
            )
            / levels;


        // Preserve original color ratios
        float factor =
            luminance > 0.001
            ? newLuminance / luminance
            : 0;


        col.rgb *= factor;


        return col;
    }


    ENDHLSL



    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
        }


        Pass
        {
            Name "Dither"


            ZWrite Off
            ZTest Always
            Cull Off


            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
}