using UnityEngine;

/// <summary>
/// Makes shaders animate in UNSCALED time.
///
/// Every frame this pushes Time.unscaledTime into the global shader
/// property _UnscaledTime, so materials (like UI Hologram) keep
/// animating even when the game is paused (timeScale = 0).
///
/// It requires no scene setup: the updater object is created hidden and
/// persistent at runtime, and it is never serialized into a scene.
/// </summary>
public static class UnscaledTimeSetter
{
    private static readonly int UnscaledTimeID =
        Shader.PropertyToID("_UnscaledTime");

    private static bool initialized;


    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad
    )]
    private static void Initialize()
    {
        if (initialized)
            return;

        initialized = true;


        GameObject host =
            new GameObject(
                "[UnscaledTime]"
            );


        host.hideFlags =
            HideFlags.HideAndDontSave;


        Object.DontDestroyOnLoad(
            host
        );


        host.AddComponent<
            UnscaledTimeUpdater
        >();
    }


    /// <summary>
    /// Hidden per-frame updater. Lives only at runtime.
    /// </summary>
    private sealed class UnscaledTimeUpdater : MonoBehaviour
    {
        private void Update()
        {
            Shader.SetGlobalFloat(
                UnscaledTimeID,
                Time.unscaledTime
            );
        }
    }
}
