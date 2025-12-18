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
        if (!playerIsNearby || storeUIPanel == null) return;

        if (Input.GetKeyDown(KeyCode.E))
            ToggleStoreUI();

        if (Input.GetKeyDown(KeyCode.Escape) && storeUIPanel.activeSelf)
            SetStoreOpen(false);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        playerIsNearby = true;
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerIsNearby = false;

        if (interactionPromptText != null)
            interactionPromptText.gameObject.SetActive(false);

        if (storeUIPanel != null && storeUIPanel.activeSelf) SetStoreOpen(false);
    }


    private void ToggleStoreUI()
    {
        if (storeUIPanel == null) return;
        bool open = !storeUIPanel.activeSelf;
        SetStoreOpen(open);
    }



    private void SetStoreOpen(bool open)
    {
        if (storeUIPanel == null)
            return;

        if (open)
        {
            if (Store.Instance == null) return;

            storeUIPanel.SetActive(true);
            Store.Instance.SetStoreOpen();

            if (interactionPromptText != null) interactionPromptText.gameObject.SetActive(false);
            if (gamemanager.instance != null) gamemanager.instance.statePause();
        }

        else
        {
            storeUIPanel.SetActive(false);

            if (interactionPromptText != null && playerIsNearby) interactionPromptText.gameObject.SetActive(true);
            if (gamemanager.instance != null) gamemanager.instance.stateUnpause();
        }

    }
}