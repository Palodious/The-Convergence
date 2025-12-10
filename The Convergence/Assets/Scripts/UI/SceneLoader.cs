using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    // Name of the scene we actually want to end up in
    public static string targetSceneName;

    // Remember where we came from
    public static string previousSceneName;

    private const string LoadingSceneName = "Loading Scene";

    public static void LoadSceneWithLoadingScreen(string sceneToLoad)
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("SceneLoader: sceneToLoad is null or empty.");
            return;
        }

        targetSceneName = sceneToLoad;
        previousSceneName = SceneManager.GetActiveScene().name;

        // Jump into the loading scene
        SceneManager.LoadScene(LoadingSceneName);
    }
}
