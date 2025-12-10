using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneLoadTrigger : MonoBehaviour
{
    [Header("Scene To Load")]
    [SerializeField] private string sceneName = "Game Play Scene 3";

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger for the player
        if (other.CompareTag("Player"))
            return;

            SaveManager.PendingLoad = false;

        SceneLoader.LoadSceneWithLoadingScreen(sceneName);
    }
}
