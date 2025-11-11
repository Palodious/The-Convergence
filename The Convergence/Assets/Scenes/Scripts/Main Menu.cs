using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene to load when pressing Start")]
    [SerializeField] string firstLevelSceneName = "Level1";

    [Header("Panels")]
    [SerializeField] GameObject optionsPanel;

    void Awake()
    {
        // Just in case you came here from a paused game scene
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Called by Start Game button
    public void StartGame()
    {
        if (!string.IsNullOrEmpty(firstLevelSceneName))
            SceneManager.LoadScene(firstLevelSceneName);
        else
            Debug.LogError("First level scene name not set on MainMenu.");
    }

    // Called by Options button
    public void OpenOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    // Hook this up to a Close button on the options panel
    public void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    // Called by Quit button
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}