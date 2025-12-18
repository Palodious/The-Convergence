using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class directionalPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Button exitButton;

    private bool dialogueActive = false;
    private bool hasShown = false;

    public static bool PopupIsOpen { get; private set; }

    void Start()
    {
        dialoguePanel.SetActive(false);
        exitButton.onClick.AddListener(CloseDialogue);
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (hasShown)
            return;

        if (!dialogueActive && other.CompareTag("Player"))
        {
            OpenDialogue();
            hasShown = true;
        }
    }

    void OpenDialogue()
    {
        dialogueActive = true;
        dialoguePanel.SetActive(true);

        PopupIsOpen = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseDialogue()
    {
        dialogueActive = false;
        dialoguePanel.SetActive(false);

        PopupIsOpen = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(ResetPopupFlag());

    }

    private IEnumerator ResetPopupFlag()
    {
        yield return new WaitForEndOfFrame();
        PopupIsOpen = false;
    }
}