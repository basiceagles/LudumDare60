using UnityEngine;

/// <summary>
/// Reusable settings asset for Distance Dither Fog.
///
/// Create these assets from:
///
///     Assets -> Create -> Distance Dither Fog -> Settings
///
/// The settings can then be assigned to a DistanceFogController.
///
/// This keeps fog configuration separate from the scene and makes it easy
/// to create multiple presets.
///
/// Example presets:
///
///     OutdoorFog
///     IndoorFog
///     DenseHorrorFog
///     NightFog
///     CaveFog
/// </summary>
[CreateAssetMenu(
    fileName = "DistanceFogSettings",
    menuName = "Distance Dither Fog/Settings",
    order = 100
)]
public sealed class DistanceFogSettings : ScriptableObject
{
    // ========================================================================
    // DISTANCE
    // ========================================================================

    [Header("Distance")]

    [Tooltip(
        "Distance from the camera where dithering begins."
    )]
    [Min(0f)]
    public float fogStart = 20f;


    [Tooltip(
        "Distance from the camera where objects are completely hidden."
    )]
    [Min(0.01f)]
    public float fogEnd = 50f;


    [Tooltip(
        "Overall strength of the fog. " +
        "0 = no fog. " +
        "1 = full effect."
    )]
    [Range(0f, 1f)]
    public float fogStrength = 1f;


    // ========================================================================
    // HEIGHT
    // ========================================================================

    [Header("Height Fog")]

    [Tooltip(
        "Enables additional fog based on world-space height."
    )]
    public bool heightFog = false;


    [Tooltip(
        "World-space height where height fog begins."
    )]
    public float fogHeight = 0f;


    [Tooltip(
        "Vertical distance over which height fog reaches full strength."
    )]
    [Min(0.01f)]
    public float fogHeightFalloff = 10f;


    // ========================================================================
    // VALIDATION
    // ========================================================================

    private void OnValidate()
    {
        fogStart =
            Mathf.Max(
                0f,
                fogStart
            );


        fogEnd =
            Mathf.Max(
                fogStart + 0.01f,
                fogEnd
            );


        fogStrength =
            Mathf.Clamp01(
                fogStrength
            );


        fogHeightFalloff =
            Mathf.Max(
                0.01f,
                fogHeightFalloff
            );
    }


    // ========================================================================
    // COPY
    // ========================================================================
    //
    // Returns a runtime copy of this settings object.
    //
    // Useful if you want to modify fog values during gameplay without
    // modifying the original asset.
    // ========================================================================

    public DistanceFogSettings CreateRuntimeCopy()
    {
        DistanceFogSettings copy =
            Instantiate(this);

        copy.name =
            name + " (Runtime)";

        return copy;
    }


    // ========================================================================
    // APPLY
    // ========================================================================
    //
    // Applies these settings to a DistanceFogController.
    // ========================================================================

    public void ApplyTo(
        DistanceFogController controller
    )
    {
        if (controller == null)
            return;


        controller.SetVisionDistance(
            fogStart,
            fogEnd
        );


        controller.SetFogStrength(
            fogStrength
        );


        controller.SetHeightFog(
            heightFog
        );


        controller.SetHeightFogSettings(
            fogHeight,
            fogHeightFalloff
        );
    }


    // ========================================================================
    // FACTORY PRESETS
    // ========================================================================
    //
    // These are convenience methods used by the editor/context menu.
    // ========================================================================

    [ContextMenu("Preset - Dense Horror")]
    private void PresetDenseHorror()
    {
        fogStart = 8f;

        fogEnd = 25f;

        fogStrength = 1f;

        heightFog = false;

        fogHeight = 0f;

        fogHeightFalloff = 10f;
    }


    [ContextMenu("Preset - Outdoor")]
    private void PresetOutdoor()
    {
        fogStart = 30f;

        fogEnd = 80f;

        fogStrength = 1f;

        heightFog = false;

        fogHeight = 0f;

        fogHeightFalloff = 10f;
    }


    [ContextMenu("Preset - Indoor")]
    private void PresetIndoor()
    {
        fogStart = 12f;

        fogEnd = 35f;

        fogStrength = 1f;

        heightFog = false;

        fogHeight = 0f;

        fogHeightFalloff = 10f;
    }


    [ContextMenu("Preset - Night")]
    private void PresetNight()
    {
        fogStart = 15f;

        fogEnd = 40f;

        fogStrength = 1f;

        heightFog = false;

        fogHeight = 0f;

        fogHeightFalloff = 10f;
    }


    [ContextMenu("Preset - Ground Mist")]
    private void PresetGroundMist()
    {
        fogStart = 20f;

        fogEnd = 60f;

        fogStrength = 1f;

        heightFog = true;

        fogHeight = 2f;

        fogHeightFalloff = 8f;
    }


    [ContextMenu("Preset - Very Short Vision")]
    private void PresetVeryShortVision()
    {
        fogStart = 5f;

        fogEnd = 15f;

        fogStrength = 1f;

        heightFog = false;

        fogHeight = 0f;

        fogHeightFalloff = 10f;
    }


    [ContextMenu("Preset - Long Vision")]
    private void PresetLongVision()
    {
        fogStart = 60f;

        fogEnd = 150f;

        fogStrength = 1f;

        heightFog = false;

        fogHeight = 0f;

        fogHeightFalloff = 10f;
    }


    [ContextMenu("Reset")]
    private void ResetSettings()
    {
        fogStart = 20f;

        fogEnd = 50f;

        fogStrength = 1f;

        heightFog = false;

        fogHeight = 0f;

        fogHeightFalloff = 10f;
    }
}