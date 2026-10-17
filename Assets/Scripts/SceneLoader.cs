using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene To Load")]
    [SerializeField] private string sceneToLoad;

    [Header("Loading Screen")]
    [SerializeField] private bool useLoadingScreen = true;

    // Call this from a UI Button
    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogError("SceneLoader: No scene has been assigned.");
            return;
        }

        if (useLoadingScreen)
        {
            //LoadingSceneData.SceneToLoad = sceneToLoad;

            SceneManager.LoadScene("LoadingScreen");
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}