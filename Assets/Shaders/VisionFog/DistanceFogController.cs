using UnityEngine;

/// <summary>
/// Distance Dither Fog controller for Unity 6 URP.
///
/// This component controls the global parameters used by:
///     DistanceDitherLit.shader
///
/// The effect is NOT a traditional color fog.
/// It is a distance-based visibility limiter that uses ordered dithering
/// to progressively remove object fragments as they become distant.
///
/// Main characteristics:
/// - No fog color blending.
/// - Does not brighten dark objects.
/// - Does not create bright silhouettes in darkness.
/// - Skybox is unaffected.
/// - Uses global shader parameters.
/// - Works with all materials using Distance Dither Fog/Lit.
/// - Can be changed at runtime.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class DistanceFogController : MonoBehaviour
{
    // ========================================================================
    // SHADER PROPERTY IDs
    // ========================================================================

    private static readonly int FogStartID =
        Shader.PropertyToID("_DistanceFogStart");

    private static readonly int FogEndID =
        Shader.PropertyToID("_DistanceFogEnd");

    private static readonly int FogStrengthID =
        Shader.PropertyToID("_DistanceFogStrength");

    private static readonly int FogHeightEnabledID =
        Shader.PropertyToID("_DistanceFogHeightEnabled");

    private static readonly int FogHeightID =
        Shader.PropertyToID("_DistanceFogHeight");

    private static readonly int FogHeightFalloffID =
        Shader.PropertyToID("_DistanceFogHeightFalloff");


    // ========================================================================
    // DISTANCE SETTINGS
    // ========================================================================

    [Header("Distance Fog")]

    [Tooltip(
        "Distance from the camera where the dither transition begins."
    )]
    [Min(0f)]
    [SerializeField]
    private float fogStart = 20f;


    [Tooltip(
        "Distance from the camera where objects are completely hidden."
    )]
    [Min(0.01f)]
    [SerializeField]
    private float fogEnd = 50f;


    [Tooltip(
        "Overall strength of the distance fog. " +
        "0 disables the effect globally. " +
        "1 uses the full configured distance."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float fogStrength = 1f;


    // ========================================================================
    // HEIGHT FOG
    // ========================================================================

    [Header("Height Fog")]

    [Tooltip(
        "Adds additional visibility reduction based on world height. " +
        "Useful for ground mist or low-lying fog."
    )]
    [SerializeField]
    private bool heightFog = false;


    [Tooltip(
        "World-space height where height fog begins."
    )]
    [SerializeField]
    private float fogHeight = 0f;


    [Tooltip(
        "Vertical distance over which height fog reaches full strength."
    )]
    [Min(0.01f)]
    [SerializeField]
    private float fogHeightFalloff = 10f;


    // ========================================================================
    // CAMERA SETTINGS
    // ========================================================================

    [Header("Camera")]

    [Tooltip(
        "Camera used by the fog calculations. " +
        "When disabled, Camera.main is used."
    )]
    [SerializeField]
    private Camera targetCamera;


    // ========================================================================
    // AUTOMATIC UPDATE
    // ========================================================================

    [Header("Update")]

    [Tooltip(
        "Automatically update shader parameters every frame. " +
        "Keep this enabled when the fog values or camera can change at runtime."
    )]
    [SerializeField]
    private bool updateEveryFrame = true;


    [Tooltip(
        "When enabled, the component reapplies the shader values whenever " +
        "the component is enabled."
    )]
    [SerializeField]
    private bool applyOnEnable = true;


    // ========================================================================
    // DEBUG
    // ========================================================================

    [Header("Debug")]

    [Tooltip(
        "Prints the current fog configuration to the Unity Console when " +
        "values are applied."
    )]
    [SerializeField]
    private bool debugLogging = false;


    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    private void OnEnable()
    {
        ValidateSettings();

        if (applyOnEnable)
        {
            ApplySettings();
        }
    }


    private void OnDisable()
    {
        // Do not reset the global shader values here.
        //
        // This is intentional.
        //
        // Another camera/controller may still be using the same global
        // shader parameters.
    }


    private void Update()
    {
        if (!updateEveryFrame)
            return;

        ApplySettings();
    }


    private void OnValidate()
    {
        ValidateSettings();

        ApplySettings();
    }


    // ========================================================================
    // VALIDATION
    // ========================================================================

    private void ValidateSettings()
    {
        fogStart = Mathf.Max(0f, fogStart);

        fogEnd = Mathf.Max(
            fogStart + 0.01f,
            fogEnd
        );

        fogStrength = Mathf.Clamp01(
            fogStrength
        );

        fogHeightFalloff = Mathf.Max(
            0.01f,
            fogHeightFalloff
        );
    }


    // ========================================================================
    // CAMERA
    // ========================================================================

    private Camera GetTargetCamera()
    {
        if (targetCamera != null)
            return targetCamera;

        return Camera.main;
    }


    // ========================================================================
    // APPLY SETTINGS
    // ========================================================================
    //
    // This is the central function that sends values to the shader.
    //
    // Shader.SetGlobalFloat is used because all Distance Dither Fog materials
    // should share the same vision distance.
    //
    // This means you can have hundreds or thousands of objects using the
    // shader without putting individual fog values into every material.
    // ========================================================================

    public void ApplySettings()
    {
        ValidateSettings();


        // --------------------------------------------------------------------
        // Distance
        // --------------------------------------------------------------------

        Shader.SetGlobalFloat(
            FogStartID,
            fogStart
        );


        Shader.SetGlobalFloat(
            FogEndID,
            fogEnd
        );


        Shader.SetGlobalFloat(
            FogStrengthID,
            fogStrength
        );


        // --------------------------------------------------------------------
        // Height
        // --------------------------------------------------------------------

        Shader.SetGlobalFloat(
            FogHeightEnabledID,
            heightFog ? 1f : 0f
        );


        Shader.SetGlobalFloat(
            FogHeightID,
            fogHeight
        );


        Shader.SetGlobalFloat(
            FogHeightFalloffID,
            fogHeightFalloff
        );


        // --------------------------------------------------------------------
        // Optional debug
        // --------------------------------------------------------------------

        if (debugLogging)
        {
            Debug.Log(
                $"[Distance Dither Fog] " +
                $"Start: {fogStart:F2}, " +
                $"End: {fogEnd:F2}, " +
                $"Strength: {fogStrength:F2}, " +
                $"Height Fog: {heightFog}",
                this
            );
        }
    }


    // ========================================================================
    // PUBLIC API
    // ========================================================================
    //
    // These methods allow gameplay scripts to change the vision distance.
    //
    // Example:
    //
    //     fog.SetVisionDistance(10f, 30f);
    //
    // ========================================================================

    /// <summary>
    /// Sets the distance at which fog begins and the distance at which
    /// objects become completely hidden.
    /// </summary>
    public void SetVisionDistance(
        float start,
        float end
    )
    {
        fogStart = Mathf.Max(
            0f,
            start
        );

        fogEnd = Mathf.Max(
            fogStart + 0.01f,
            end
        );

        ApplySettings();
    }


    /// <summary>
    /// Sets the global fog strength.
    /// </summary>
    public void SetFogStrength(
        float strength
    )
    {
        fogStrength =
            Mathf.Clamp01(
                strength
            );

        ApplySettings();
    }


    /// <summary>
    /// Enables or disables height fog.
    /// </summary>
    public void SetHeightFog(
        bool enabled
    )
    {
        heightFog = enabled;

        ApplySettings();
    }


    /// <summary>
    /// Sets the world-space height and falloff of the height fog.
    /// </summary>
    public void SetHeightFogSettings(
        float height,
        float falloff
    )
    {
        fogHeight = height;

        fogHeightFalloff =
            Mathf.Max(
                0.01f,
                falloff
            );

        ApplySettings();
    }


    // ========================================================================
    // GETTERS
    // ========================================================================

    public float FogStart
    {
        get => fogStart;
        set
        {
            fogStart = Mathf.Max(
                0f,
                value
            );

            if (fogEnd <= fogStart)
            {
                fogEnd =
                    fogStart + 0.01f;
            }

            ApplySettings();
        }
    }


    public float FogEnd
    {
        get => fogEnd;
        set
        {
            fogEnd =
                Mathf.Max(
                    fogStart + 0.01f,
                    value
                );

            ApplySettings();
        }
    }


    public float FogStrength
    {
        get => fogStrength;
        set
        {
            fogStrength =
                Mathf.Clamp01(
                    value
                );

            ApplySettings();
        }
    }


    public bool HeightFogEnabled
    {
        get => heightFog;
        set
        {
            heightFog = value;

            ApplySettings();
        }
    }


    public float FogHeight
    {
        get => fogHeight;
        set
        {
            fogHeight = value;

            ApplySettings();
        }
    }


    public float FogHeightFalloff
    {
        get => fogHeightFalloff;
        set
        {
            fogHeightFalloff =
                Mathf.Max(
                    0.01f,
                    value
                );

            ApplySettings();
        }
    }


    // ========================================================================
    // RESET / PRESETS
    // ========================================================================

    /// <summary>
    /// Resets the fog to a sensible default visibility range.
    /// </summary>
    [ContextMenu("Reset To Default")]
    public void ResetToDefault()
    {
        fogStart = 20f;

        fogEnd = 50f;

        fogStrength = 1f;

        heightFog = false;

        fogHeight = 0f;

        fogHeightFalloff = 10f;

        ApplySettings();
    }


    /// <summary>
    /// Example preset for a very dense horror-game visibility range.
    /// </summary>
    [ContextMenu("Preset - Dense Horror")]
    public void PresetDenseHorror()
    {
        fogStart = 8f;

        fogEnd = 25f;

        fogStrength = 1f;

        heightFog = false;

        ApplySettings();
    }


    /// <summary>
    /// Example preset for a medium outdoor visibility range.
    /// </summary>
    [ContextMenu("Preset - Outdoor")]
    public void PresetOutdoor()
    {
        fogStart = 30f;

        fogEnd = 80f;

        fogStrength = 1f;

        heightFog = false;

        ApplySettings();
    }


    /// <summary>
    /// Example preset for ground-level mist.
    /// </summary>
    [ContextMenu("Preset - Ground Mist")]
    public void PresetGroundMist()
    {
        fogStart = 20f;

        fogEnd = 60f;

        fogStrength = 1f;

        heightFog = true;

        fogHeight = 2f;

        fogHeightFalloff = 8f;

        ApplySettings();
    }


    // ========================================================================
    // GIZMOS
    // ========================================================================
    //
    // These are editor-only visualizations.
    //
    // The first sphere shows fogStart.
    // The second sphere shows fogEnd.
    //
    // They are intentionally only drawn when the component is selected.
    // ========================================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Camera camera = GetTargetCamera();

        if (camera == null)
            return;


        Vector3 position =
            camera.transform.position;


        // -------------------------------------------------------------
        // Start distance
        // -------------------------------------------------------------

        UnityEditor.Handles.DrawWireDisc(
            position,
            Vector3.up,
            fogStart
        );


        // -------------------------------------------------------------
        // End distance
        // -------------------------------------------------------------

        UnityEditor.Handles.DrawWireDisc(
            position,
            Vector3.up,
            fogEnd
        );


        // -------------------------------------------------------------
        // Height
        // -------------------------------------------------------------

        if (heightFog)
        {
            Vector3 heightCenter =
                new Vector3(
                    position.x,
                    fogHeight,
                    position.z
                );


            UnityEditor.Handles.DrawWireDisc(
                heightCenter,
                Vector3.up,
                2f
            );
        }
    }

#endif
}
