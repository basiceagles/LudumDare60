#ifndef DISTANCE_DITHER_FOG_BAYER4X4_INCLUDED
#define DISTANCE_DITHER_FOG_BAYER4X4_INCLUDED

// ============================================================================
// Distance Dither Fog
// Bayer4x4.hlsl
//
// 4x4 Bayer ordered dithering for distance-based visibility.
//
// Designed for:
// - Unity 6
// - Universal Render Pipeline (URP)
// - No texture required
// - No sampler required
// - Stable screen-space pattern
// - Very low GPU cost
//
// The matrix:
//
//      0   8   2  10
//     12   4  14   6
//      3  11   1   9
//     15   7  13   5
//
// Values are normalized to approximately:
//
//      0.0 -> 0.9375
//
// A small offset is added when converting the matrix to a threshold so
// that the result behaves correctly when compared against a 0..1 fog factor.
// ============================================================================


// ============================================================================
// BAYER MATRIX
// ============================================================================
//
// Returns the raw 0..15 Bayer matrix value.
//
// Keeping this as a function allows the compiler to optimize it into a small
// amount of arithmetic / constant logic instead of requiring a texture.
// ============================================================================

float DistanceFog_Bayer4x4Raw(int2 pixel)
{
    // Wrap pixel coordinates to a 4x4 tile.

    int2 p = pixel & 3;

    int index =
        p.x +
        p.y * 4;

    // 4x4 Bayer matrix flattened into a constant array.
//
//     0   8   2  10
//    12   4  14   6
//     3  11   1   9
//    15   7  13   5

    const int bayer[16] =
    {
         0,  8,  2, 10,
        12,  4, 14,  6,
         3, 11,  1,  9,
        15,  7, 13,  5
    };

    return (float)bayer[index];
}


// ============================================================================
// NORMALIZED BAYER VALUE
// ============================================================================
//
// Returns:
//
//     0.0
//     ...
//     0.9375
//
// The +0.5 offset is intentionally NOT applied here.
//
// Keeping the raw normalized value separate allows the caller to choose how
// the threshold should be interpreted.
// ============================================================================

float DistanceFog_Bayer4x4(
    int2 pixel
)
{
    float value =
        DistanceFog_Bayer4x4Raw(pixel);

    return value / 16.0;
}


// ============================================================================
// BAYER THRESHOLD
// ============================================================================
//
// Converts the Bayer matrix into a conventional threshold:
//
//     0.03125
//     ...
//     0.96875
//
// The half-step prevents a threshold of exactly zero and exactly one.
//
// This produces a balanced 16-step ordered dither.
// ============================================================================

float DistanceFog_Bayer4x4Threshold(
    int2 pixel
)
{
    float value =
        DistanceFog_Bayer4x4Raw(pixel);

    return (value + 0.5) / 16.0;
}


// ============================================================================
// FLOAT2 VERSION
// ============================================================================
//
// Some shaders naturally have floating-point screen coordinates.
//
// This helper avoids requiring the caller to manually convert them.
// ============================================================================

float DistanceFog_Bayer4x4ThresholdFloat2(
    float2 pixelPosition
)
{
    int2 pixel =
        int2(floor(pixelPosition));

    return DistanceFog_Bayer4x4Threshold(pixel);
}


// ============================================================================
// SCREEN UV VERSION
// ============================================================================
//
// Converts normalized screen UV coordinates into pixel coordinates.
//
// Parameters:
//
// uv
//     0..1 screen-space UV.
//
// screenSize
//     Actual render target resolution.
//
// Example:
//
//     float threshold = DistanceFog_Bayer4x4Screen(
//         input.positionCS.xy,
//         _ScreenParams.xy
//     );
//
// ============================================================================

float DistanceFog_Bayer4x4Screen(
    float2 uv,
    float2 screenSize
)
{
    float2 pixelPosition =
        uv * screenSize;

    return DistanceFog_Bayer4x4ThresholdFloat2(
        pixelPosition
    );
}


// ============================================================================
// DIRECT CLIP TEST
// ============================================================================
//
// Returns:
//
//     true  = keep fragment
//     false = discard fragment
//
// fogFactor:
//
//     0 = completely visible
//     1 = completely hidden
//
// The comparison:
//
//     threshold >= fogFactor
//
// means:
//
//     fog = 0
//         All pixels survive.
//
//     fog = 0.5
//         Approximately half survive.
//
//     fog = 1
//         No pixels survive.
//
// ============================================================================

bool DistanceFog_Bayer4x4KeepPixel(
    float fogFactor,
    int2 pixel
)
{
    float threshold =
        DistanceFog_Bayer4x4Threshold(pixel);

    return threshold >= saturate(fogFactor);
}


// ============================================================================
// FLOAT2 CLIP TEST
// ============================================================================

bool DistanceFog_Bayer4x4KeepPixelFloat2(
    float fogFactor,
    float2 pixelPosition
)
{
    float threshold =
        DistanceFog_Bayer4x4ThresholdFloat2(
            pixelPosition
        );

    return threshold >= saturate(fogFactor);
}


// ============================================================================
// SCREEN UV CLIP TEST
// ============================================================================
//
// This is the function the final shader can use directly.
//
// Example:
//
//     float fog = DistanceFog_GetFactor(...);
//
//     if (!DistanceFog_Bayer4x4KeepScreenPixel(
//             fog,
//             input.positionCS.xy,
//             _ScreenParams.xy))
//     {
//         clip(-1);
//     }
//
// ============================================================================

bool DistanceFog_Bayer4x4KeepScreenPixel(
    float fogFactor,
    float2 screenPosition,
    float2 screenSize
)
{
    float threshold =
        DistanceFog_Bayer4x4Screen(
            screenPosition / screenSize,
            screenSize
        );

    return threshold >= saturate(fogFactor);
}


// ============================================================================
// DITHER VALUE WITH OPTIONAL SCALE
// ============================================================================
//
// A scale of 1 means the standard 4x4 Bayer pattern.
//
// Larger values repeat the same pattern over larger screen regions.
//
// For example:
//
//     scale = 1
//         4x4 pixel pattern.
//
//     scale = 2
//         Each Bayer cell covers approximately 2x2 pixels.
//
// Usually keep this at 1 for the cleanest result.
// ============================================================================

float DistanceFog_Bayer4x4Scaled(
    float2 pixelPosition,
    float scale
)
{
    scale = max(scale, 1.0);

    float2 scaledPosition =
        floor(pixelPosition / scale);

    return DistanceFog_Bayer4x4ThresholdFloat2(
        scaledPosition
    );
}


// ============================================================================
// SCALED CLIP TEST
// ============================================================================

bool DistanceFog_Bayer4x4KeepPixelScaled(
    float fogFactor,
    float2 pixelPosition,
    float scale
)
{
    float threshold =
        DistanceFog_Bayer4x4Scaled(
            pixelPosition,
            scale
        );

    return threshold >= saturate(fogFactor);
}


// ============================================================================
// TEMPORAL OFFSET
// ============================================================================
//
// Ordered dithering is intentionally stable.
//
// However, if temporal AA is enabled, a very small optional offset can be
// introduced by the final shader/controller.
//
// This function provides a deterministic way of offsetting the Bayer tile.
//
// IMPORTANT:
// Do NOT animate this every frame unless you specifically want temporal
// dithering. A moving Bayer pattern can create visible crawling/shimmering.
//
// offset should normally be:
//
//     int2(0, 0)
//
// ============================================================================

float DistanceFog_Bayer4x4ThresholdOffset(
    int2 pixel,
    int2 offset
)
{
    return DistanceFog_Bayer4x4Threshold(
        pixel + offset
    );
}


// ============================================================================
// OPTIONAL INVERTED DITHER
// ============================================================================
//
// Normally:
//
//     higher fog = fewer pixels.
//
// This function reverses the pattern.
//
// It is not used by the default fog implementation but is provided for
// completeness if another effect needs the opposite behavior.
// ============================================================================

float DistanceFog_Bayer4x4InvertedThreshold(
    int2 pixel
)
{
    return 1.0 -
        DistanceFog_Bayer4x4Threshold(pixel);
}


// ============================================================================
// DEBUG VALUE
// ============================================================================
//
// Returns the Bayer threshold as grayscale.
//
// Useful temporarily in a fragment shader:
//
//     return float4(
//         DistanceFog_Bayer4x4Threshold(pixel).xxx,
//         1
//     );
//
// ============================================================================

float DistanceFog_Bayer4x4Debug(
    int2 pixel
)
{
    return DistanceFog_Bayer4x4Threshold(pixel);
}


// ===========================================================================
// END
// ===========================================================================

#endif