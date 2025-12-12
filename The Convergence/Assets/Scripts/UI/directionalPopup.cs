using UnityEngine;
using UnityEngine.UI;

public class directionalPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Button exitButton;

    private bool dialogueActive = false;

    void Start()
    {
        
        dialoguePanel.SetActive(false);

        
        exitButton.onClick.AddListener(CloseDialogue);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!dialogueActive && other.CompareTag("Player"))
        {
            OpenDialogue();
        }
    }

    void OpenDialogue()
    {
        dialogueActive = true;
        dialoguePanel.SetActive(true);

        // Pause game
        Time.timeScale = 0f;

        // Unlock cursor so they can click
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseDialogue()
    {
        dialogueActive = false;
        dialoguePanel.SetActive(false);

        // Unpause game
        Time.timeScale = 1f;

        // Lock cursor again for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
