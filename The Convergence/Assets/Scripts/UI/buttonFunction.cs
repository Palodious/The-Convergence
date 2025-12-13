using UnityEngine;
using UnityEngine.SceneManagement;

public class buttonFunction : MonoBehaviour
{

    private void CleanupBeforeSceneChange()
    {
        if (Store.Instance != null)
        {
            Store.Instance.ExitStore();
        }

        // Ensure game is unpaused through gamemanager
        if (gamemanager.instance != null)
        {
            gamemanager.instance.stateUnpause();
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        SaveManager.PendingLoad = false;
    }

    public void resume()
    {
        if (gamemanager.instance != null)
            gamemanager.instance.stateUnpause();
        else
            Time.timeScale = 1f;
    }
    public void restart()
    {
        CleanupBeforeSceneChange();

        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }
    public void quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
   Application.Quit();
#endif
    }
    public void respawn()
    {
        CleanupBeforeSceneChange();

        if (gamemanager.instance != null && gamemanager.instance.playerScript != null)
            gamemanager.instance.playerScript.respawn();
    }
    public void loadLevel(int lvl)
    {
        CleanupBeforeSceneChange();

        string sceneName = SceneManager.GetSceneByBuildIndex(lvl).name;
        SceneLoader.LoadSceneWithLoadingScreen(sceneName);
    }
    public void mainMenu()
    {
        CleanupBeforeSceneChange();
        SceneLoader.LoadSceneWithLoadingScreen("Main Menu");
    }
}
