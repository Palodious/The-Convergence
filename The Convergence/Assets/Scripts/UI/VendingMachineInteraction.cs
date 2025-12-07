using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class VendingMachineInteraction : MonoBehaviour
{
    public GameObject storeUIPanel;

    private bool playerIsNearby = false;

    public TextMeshProUGUI interactionPromptText;

    private void Update()
    {
        if (playerIsNearby && Input.GetKeyDown(KeyCode.E))
        {
            ToggleStoreUI();
        }
        if (playerIsNearby && Input.GetKeyDown(KeyCode.Escape))
        {
            storeUIPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerIsNearby=true;

            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerIsNearby = false;

            
            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
            }

            if (storeUIPanel.activeSelf)
            {
                storeUIPanel.SetActive(false);
            }
        }
    }


    private void ToggleStoreUI()
    {
        bool isUIVisible = storeUIPanel.activeSelf;
        storeUIPanel.SetActive(!isUIVisible);

        if (!isUIVisible)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;

        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;

        }
    }
    
    
}
