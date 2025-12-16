using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // If you're using TextMeshPro
using System.Collections; // Add this for IEnumerator

public class Door : MonoBehaviour
{
    [Header("~=~= Door Settings =~=~")]
    [SerializeField] bool requiresKey = false;
    [SerializeField] bool requiresEnemiesDefeated = true;
    [SerializeField] keyStats requiredKey; // NEW: Specific key required (if requiresKey is true)
    [SerializeField] int keysRequired = 1; // Number of keys required

    [Header("~=~= Interaction Settings =~=~")]
    [SerializeField] KeyCode interactionKey = KeyCode.E;
    [SerializeField] float interactionRange = 3f;

    [Header("~=~= UI Settings =~=~")]
    [SerializeField] GameObject doorPopupUI; // Assign your popup UI GameObject here
    [SerializeField] Text doorText;

    [Header("~=~= Slide Settings =~=~")]
    [Range(0.5f, 10f)][SerializeField] float slideDistance = 3f;
    [Range(0.1f, 10f)][SerializeField] float slideSpeed = 2f;

    [Header("~=~= Scene Settings =~=~")]
    [SerializeField] bool loadNextScene = false;
    [SerializeField] string sceneName = "";
    [SerializeField] int sceneBuildIndex = -1;
    [SerializeField] float sceneLoadDelay = 0.5f;

    private bool isOpen = false;
    private Vector3 closedPos;
    private Transform playerTransform;
    private bool isPlayerInRange = false;
    private playerController playerControllerRef; // Cache player controller reference

    private void Awake()
    {
        closedPos = transform.position;

        if (doorPopupUI != null)
        {
            doorPopupUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(interactionKey) && isPlayerInRange && !isOpen)
        {
            TryOpenDoor();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpen) return;
        if (!other.CompareTag("Player")) return;

        playerTransform = other.transform;
        isPlayerInRange = true;

        playerControllerRef = playerTransform.GetComponent<playerController>();

        ShowDoorPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = false;
        playerTransform = null;
        playerControllerRef = null;

        HideDoorPrompt();
    }

    private void ShowDoorPrompt()
    {
        if (doorPopupUI != null)
        {
            doorPopupUI.SetActive(true);

            if (doorText != null)
            {
                if (requiresKey)
                {
                    int playerKeyCount = 0;
                    bool hasRequiredKey = false;

                    if (playerControllerRef != null)
                    {
                        if (requiredKey != null)
                        {
                            hasRequiredKey = playerControllerRef.HasKey(requiredKey);
                            playerKeyCount = playerControllerRef.GetKeyCount(requiredKey);
                        }
                        else
                        {
                            playerKeyCount = playerControllerRef.GetTotalKeyCount();
                            hasRequiredKey = playerControllerRef.HasAnyKey();
                        }
                    }

                    if (requiredKey != null)
                    {
                        doorText.text = $"Press E to open door\n{requiredKey.keyName}: {playerKeyCount}/{keysRequired}";
                    }
                    else
                    {
                        doorText.text = $"Press E to open door\nKeys: {playerKeyCount}/{keysRequired}";
                    }
                }
                else
                {
                    doorText.text = "Press E to open door";
                }
            }
        }
        else
        {
         
        }
    }

    private void HideDoorPrompt()
    {
        if (doorPopupUI != null)
        {
            doorPopupUI.SetActive(false);
        }
    }

    void TryOpenDoor()
    {
        if (playerTransform != null &&
            Vector3.Distance(transform.position, playerTransform.position) > interactionRange)
        {
            return;
        }

        if (playerControllerRef == null) return;

        bool canOpen = true;

        if (requiresKey)
        {
            if (requiredKey != null)
            {
                int keyCount = playerControllerRef.GetKeyCount(requiredKey);
                canOpen = canOpen && (keyCount >= keysRequired);
            }
            else
            {
                canOpen = canOpen && (playerControllerRef.GetTotalKeyCount() >= keysRequired);
            }
        }

        // Check enemies requirement if needed
        if (requiresEnemiesDefeated)
        {
            canOpen = canOpen && (gamemanager.instance.GetGameGoalCount() <= 0);
        }

        if (canOpen)
        {
            bool keysUsed = false;

            if (requiresKey)
            {
                if (requiredKey != null)
                {
                    for (int i = 0; i < keysRequired; i++)
                    {
                        keysUsed = playerControllerRef.UseKey(requiredKey);
                        if (!keysUsed) break;
                    }
                }
                else
                {
                    for (int i = 0; i < keysRequired; i++)
                    {
                        keysUsed = playerControllerRef.UseAnyKey();
                        if (!keysUsed) break;
                    }
                }

                if (!keysUsed)
                {
                  
                    return;
                }
            }

            OpenDoor();
        }
        else
        {
           
            // Show a different message when requirements aren't met
            if (doorText != null)
            {
                string requirementMessage = "";
                if (requiresKey)
                {
                    if (requiredKey != null)
                    {
                        int keyCount = playerControllerRef.GetKeyCount(requiredKey);
                        if (keyCount < keysRequired)
                        {
                            requirementMessage = $"Need {keysRequired - keyCount} more {requiredKey.keyName}(s)";
                        }
                    }
                    else
                    {
                        int totalKeys = playerControllerRef.GetTotalKeyCount();
                        if (totalKeys < keysRequired)
                        {
                            requirementMessage = $"Need {keysRequired - totalKeys} more key(s)";
                        }
                    }
                }
                else if (requiresEnemiesDefeated && gamemanager.instance.GetGameGoalCount() > 0)
                {
                    requirementMessage = $"Defeat {gamemanager.instance.GetGameGoalCount()} more enemy(ies)";
                }

                if (!string.IsNullOrEmpty(requirementMessage))
                {
                    string originalText = doorText.text;
                    doorText.text = requirementMessage;

                    Invoke(nameof(UpdateDoorText), 1.5f);
                }
            }
        }
    }

    private void UpdateDoorText()
    {
        if (doorText != null && isPlayerInRange && !isOpen && playerControllerRef != null)
        {
            if (requiresKey)
            {
                int playerKeyCount = 0;

                if (requiredKey != null)
                {
                    playerKeyCount = playerControllerRef.GetKeyCount(requiredKey);
                    doorText.text = $"Press E to open door\n{requiredKey.keyName}: {playerKeyCount}/{keysRequired}";
                }
                else
                {
                    playerKeyCount = playerControllerRef.GetTotalKeyCount();
                    doorText.text = $"Press E to open door\nKeys: {playerKeyCount}/{keysRequired}";
                }
            }
            else
            {
                doorText.text = "Press E to open door";
            }
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        isPlayerInRange = false;

        HideDoorPrompt();

        StartCoroutine(SlideUp());

        
    }

    private IEnumerator SlideUp()
    {
        Vector3 targetPos = closedPos + new Vector3(0, slideDistance, 0);

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                slideSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPos;

        // Load scene after door has opened
        if (loadNextScene)
        {
            yield return new WaitForSeconds(sceneLoadDelay);
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        // Find and save player data before loading next scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController player = playerObj.GetComponent<playerController>();
            if (player != null)
            {
                // Call the method to save persistent data
                player.PrepareForSceneTransition();
                
            }
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else if (sceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(sceneBuildIndex);
        }
        else
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = currentSceneIndex + 1;

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                
            }
        }
    }
}