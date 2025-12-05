using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scene to load when pressing Start")]
    [SerializeField] string firstLevelSceneName = "Level1";

    [Header("Panels")]
    [SerializeField] GameObject optionsPanel;

    [SerializeField] private Button continueButton;
    void Start()
    {
        RefreshContinueButtonState();
    }

    void Awake()
    {
        // Just in case you came here from a paused game scene
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void RefreshContinueButtonState()
    {
        // 1) If we forgot to wire the button, fail safely.
        if (continueButton == null)
        {
            Debug.LogWarning("MainMenu: continueButton is not assigned in the Inspector.");
            return;
        }

        // 2) If there is no SaveManager in this scene, disable the button.
        if (SaveManager.Instance == null)
        {
            continueButton.interactable = false;
            return;
        }

        // 3) Check if a valid save exists.
        SaveData tmp;
        bool hasValidSave = SaveManager.Instance.TryLoad(out tmp);

        // Gray-out unless there is actually a loadable save.
        continueButton.interactable = hasValidSave;
    }

    // Called by Start Game button
    public void StartGame()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Click");

        SceneManager.LoadScene(firstLevelSceneName);
    }

    // Called by Continue button – only works if a save exists
    public void ContinueGame()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("Continue pressed but no SaveManager in the Main Menu scene.");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_Error");

            return;
        }

        SaveData data;
        if (!SaveManager.Instance.TryLoad(out data))
        {
            Debug.LogWarning("Continue pressed but no save file found.");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_Error");

            return;
        }

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Click");

        // Tell the next scene's gamemanager to auto-load on Awake.
        SaveManager.PendingLoad = true;

        // Load whatever scene was saved.
        SceneManager.LoadScene(data.scene);
    }

    // Called by Options button
    public void OpenOptions()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Open");

        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    // Hook this up to a Close button on the options panel
    public void CloseOptions()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Close");

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