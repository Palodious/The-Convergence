using UnityEngine;
using UnityEngine.SceneManagement;

public class buttonFunction : MonoBehaviour
{
    public void resume()
    {
        gamemanager.instance.stateUnpause();
    }
    public void restart()
    {
        Time.timeScale = 1f;
        SaveManager.PendingLoad = false;

        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        gamemanager.instance.stateUnpause();
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
        gamemanager.instance.playerScript.respawn();
        gamemanager.instance.stateUnpause();
    }
    public void loadLevel(int lvl)
    {
        Time.timeScale = 1f;
        SaveManager.PendingLoad = false;

        string sceneName = SceneManager.GetSceneByBuildIndex(lvl).name;
        SceneLoader.LoadSceneWithLoadingScreen(sceneName);

        gamemanager.instance.stateUnpause();
    }
    public void mainMenu()
    {
        Time.timeScale = 1f;
        SaveManager.PendingLoad = false;
        SceneLoader.LoadSceneWithLoadingScreen("Main Menu");
    }

}
