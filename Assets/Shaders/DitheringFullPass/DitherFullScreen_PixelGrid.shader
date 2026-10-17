Shader "Hidden/Custom/DitherFullScreen"
{
    Properties
    {
        _BitDepth ("Bit Depth", Range(1,8)) = 4
        _DitherStrength ("Dither Strength", Range(0,1)) = 0.25
        // At a 640x360 camera target, each output texel represents a 2x2 block
        // in the 1280x720 reference image. Use 1 here: the target is already logical resolution.
        _DitherScale ("Dither Scale", Range(1,8)) = 1
        _NoiseAmount ("Noise Amount", Range(0,1)) = 0
        _ReferenceResolution ("Reference Resolution", Vector) = (640,360,0,0)
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    TEXTURE2D_X(_BlitTexture);
    SAMPLER(sampler_PointClamp);

    float _BitDepth;
    float _DitherStrength;
    float _DitherScale;
    float _NoiseAmount;
    float4 _ReferenceResolution;

    struct Attributes { uint vertexID : SV_VertexID; };
    struct Varyings { float4 positionCS : SV_POSITION; float2 texcoord : TEXCOORD0; };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    static const float Bayer4[16] =
    {
         0,  8,  2, 10,
        12,  4, 14,  6,
         3, 11,  1,  9,
        15,  7, 13,  5
    };

    half4 Frag(Varyings input) : SV_Target
    {
        float2 res = max(_ReferenceResolution.xy, float2(1.0, 1.0));
        // One fragment/texel in the low-resolution render target = one logical pixel.
        float2 pixel = min(floor(input.texcoord * res), res - 1.0);
        float scale = max(round(_DitherScale), 1.0);
        float2 ditherCell = floor(pixel / scale);
        int x = (int)ditherCell.x & 3;
        int y = (int)ditherCell.y & 3;
        float dither = Bayer4[y * 4 + x] / 16.0 - 0.5;

        // Read the same logical texel; the render target is point-filtered.
        float2 sampleUV = (pixel + 0.5) / res;
        half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, sampleUV);

        float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
        float levels = exp2(_BitDepth);
        float newLuminance = floor(luminance * levels + dither * _DitherStrength) / levels;
        float factor = luminance > 0.001 ? newLuminance / luminance : 0.0;
        col.rgb *= factor;
        return col;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
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
