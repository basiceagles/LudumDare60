#ifndef COMMON_FOG_INCLUDED
#define COMMON_FOG_INCLUDED


// ============================================================================
// Distance Dither Fog
// ============================================================================
//
// This is visibility fog rather than color fog.
//
// The object is gradually removed using a Bayer dithering pattern.
//
// 1.0 = fully visible
// 0.0 = completely hidden
//
// No fog color is applied.
// No MixFog() is used.
//
// ============================================================================


// ============================================================================
// Forward declaration
// ============================================================================
//
// Bayer4x4.hlsl is included after this file by the shader.
//
// This declaration allows the shared function below to call the Bayer
// function before its implementation is encountered.
//
// ============================================================================

float DistanceFog_Bayer4x4Scaled(
    float2 pixelPosition,
    float scale
);


// ============================================================================
// Distance Fog Parameters
// ============================================================================
//
// These values are set globally by DistanceFogController.cs:
//
//     Shader.SetGlobalFloat("_DistanceFogStart", ...);
//     Shader.SetGlobalFloat("_DistanceFogEnd", ...);
//
// ============================================================================

float _DistanceFogStart;

float _DistanceFogEnd;


// ============================================================================
// DistanceFog_GetFactor
// ============================================================================
//
// Returns a visibility value.
//
// Before Start:
//     1.0
//
// At Start:
//     1.0
//
// Between Start and End:
//     smoothly decreases
//
// At End:
//     0.0
//
// Beyond End:
//     0.0
//
// ============================================================================

float DistanceFog_GetFactor(
    float3 positionWS,
    float3 cameraPositionWS
)
{
    float distanceToCamera =
        distance(
            positionWS,
            cameraPositionWS
        );


    float distanceRange =
        max(
            _DistanceFogEnd - _DistanceFogStart,
            0.0001
        );


    float factor =
        1.0 -
        (
            distanceToCamera - _DistanceFogStart
        )
        / distanceRange;


    return saturate(factor);
}


// ============================================================================
// ApplyDistanceFogDither
// ============================================================================
//
// Shared by:
//
//     ForwardLit
//     ShadowCaster
//     DepthOnly
//     DepthNormals
//
// IMPORTANT:
//
// The dither scale and bias are passed as parameters rather than referenced
// directly here. This allows the function to work correctly in every
// individual HLSL pass.
//
// ============================================================================

void ApplyDistanceFogDither(
    float3 positionWS,
    float4 positionCS,
    float fogEnabled,
    float ditherScale,
    float ditherBias
)
{
    if (fogEnabled < 0.5)
        return;


    float3 cameraPositionWS =
        GetCameraPositionWS();


    float fogFactor =
        DistanceFog_GetFactor(
            positionWS,
            cameraPositionWS
        );


    fogFactor =
        saturate(
            fogFactor + ditherBias
        );


    float2 pixelPosition =
        positionCS.xy;


    float threshold =
        DistanceFog_Bayer4x4Scaled(
            pixelPosition,
            ditherScale
        );


    // Keep the pixel if its visibility factor is greater than
    // the Bayer threshold.
    //
    // As distance increases, fewer pixels survive.
    clip(
        fogFactor - threshold
    );
}


#endif
