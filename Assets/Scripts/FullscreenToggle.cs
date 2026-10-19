
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Press F4 to switch between fullscreen and windowed mode.
/// Locks the game to 60 FPS.
/// </summary>
public class FullscreenToggle : MonoBehaviour
{
    [Header("Fullscreen")]
    [Tooltip("Key that switches between fullscreen and windowed.")]
    public Key toggleKey = Key.F4;

    [Tooltip("Log every switch to the console.")]
    public bool logChanges;

    [Header("Frame Rate")]
    [Tooltip("Target frame rate. Defaults to 60 FPS.")]
    [Min(1)]
    public int targetFrameRate = 60;

    private void Awake()
    {
        // Disable VSync so targetFrameRate controls the frame rate.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            return;
        }

        ToggleFullscreen();
    }

    public void ToggleFullscreen()
    {
        SetFullscreen(!Screen.fullScreen);
    }

    public void SetFullscreen(bool fullscreen)
    {
        // Preserve the current resolution.
        int width = Screen.width;
        int height = Screen.height;

        Screen.SetResolution(
            width,
            height,
            fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed
        );

        if (logChanges)
        {
            Debug.Log(
                $"Fullscreen: {fullscreen} ({width}x{height})",
                this
            );
        }
    }
}
