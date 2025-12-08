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
        if (!playerIsNearby)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleStoreUI();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && storeUIPanel.activeSelf)
        {
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
        if (other.gameObject.CompareTag("Player"))
        {
            playerIsNearby = false;

            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
            }

            if (storeUIPanel.activeSelf)
            {
                SetStoreOpen(false);
            }
        }
    }

    private void ToggleStoreUI()
    {
        bool open = !storeUIPanel.activeSelf;
        SetStoreOpen(open);
    }

    private void SetStoreOpen(bool open)
    {
        storeUIPanel.SetActive(open);

        if (open)
        {
            if (Store.Instance != null)
            {
                Store.Instance.SetStoreOpen();

                gamemanager.instance.statePause();
            }
            else
            {
                storeUIPanel.SetActive(false);

                gamemanager.instance.stateUnpause();
            }
        }
    }

 }