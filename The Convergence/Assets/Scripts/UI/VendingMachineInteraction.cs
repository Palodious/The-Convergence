using UnityEngine;
using TMPro;

public class VendingMachineInteraction : MonoBehaviour
{
    [Header("Store UI")]
    public GameObject storeUIPanel;

    [Header("Prompt UI")]
    public TextMeshProUGUI interactionPromptText;

    private bool playerIsNearby = false;

    private void Update()
    {
        if (!playerIsNearby)
            return;

        if (storeUIPanel == null)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleStoreUI();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && storeUIPanel.activeSelf)
        {
            if (gamemanager.instance != null)
                gamemanager.instance.SuppressCancelOnce();

            SetStoreOpen(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerIsNearby = true;

            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
            playerIsNearby = false;

            if (interactionPromptText != null)
                interactionPromptText.gameObject.SetActive(false);

            if (storeUIPanel != null && storeUIPanel.activeSelf)
        {
                SetStoreOpen(false);
            }
        }

    private void ToggleStoreUI()
    {
        bool open = !storeUIPanel.activeSelf;
        SetStoreOpen(open);
    }

    private void SetStoreOpen(bool open)
    {
        if (storeUIPanel == null)
            return;

        if (open)
        {


            // Refresh button displays / state
            if (Store.Instance == null)
            {
              //  Debug.LogError("StoreSystem is missing or inactive. Store UI will not open.");
            return;
            }

            storeUIPanel.SetActive(true);

            Store.Instance.SetStoreOpen();

            // Pause game
            if (gamemanager.instance != null)
                gamemanager.instance.statePause();
        }
        else
        {
            // Close panel
            storeUIPanel.SetActive(false);

            // Unpause game
            if (gamemanager.instance != null)
                gamemanager.instance.stateUnpause();
        }
    }
}