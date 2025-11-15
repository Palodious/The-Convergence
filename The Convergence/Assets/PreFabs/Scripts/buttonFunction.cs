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
    public void respawnPlayer()
    {
        gamemanager.instance.playerScript.respawnPlayer();
        gamemanager.instance.stateUnpause();
    }
    public void loadLevel(int lvl)
    {
        SceneManager.LoadScene(lvl);
        gamemanager.instance.stateUnpause();
    }


}
