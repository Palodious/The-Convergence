using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // If you're using TextMeshPro

public class Door : MonoBehaviour
{
    [Header("~=~= Door Settings =~=~")]
    [SerializeField] bool requiresKey = false;
    [SerializeField] bool requiresEnemiesDefeated = true;
    [Range(1, 10)][SerializeField] int keysRequired = 1;

    [Header("~=~= Interaction Settings =~=~")]
    [SerializeField] KeyCode interactionKey = KeyCode.E;
    [SerializeField] float interactionRange = 3f;

    [Header("~=~= UI Settings =~=~")]
    [SerializeField] GameObject doorPopupUI; // Assign your popup UI GameObject here
    [SerializeField] TextMeshProUGUI doorText; // If using TextMeshPro
                                               // [SerializeField] Text doorText; // If using regular Unity UI Text

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

    private void Awake()
    {
        closedPos = transform.position;

        // Initialize UI - hide it at start
        if (doorPopupUI != null)
        {
            doorPopupUI.SetActive(false);
        }
    }

    private void Update()
    {
        // Check for E key press when player is in range
        if (Input.GetKeyDown(interactionKey) && isPlayerInRange && !isOpen)
        {
            TryOpenDoor();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpen) return;
        if (!other.CompareTag("Player")) return;

        // Set player reference and range flag
        playerTransform = other.transform;
        isPlayerInRange = true;

        // Show UI prompt
        ShowDoorPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = false;
        playerTransform = null;

        // Hide UI prompt
        HideDoorPrompt();
    }

    private void ShowDoorPrompt()
    {
        if (doorPopupUI != null)
        {
            doorPopupUI.SetActive(true);

            // Update text based on requirements
            if (doorText != null)
            {
                if (requiresKey)
                {
                    // Get current key count from player
                    int playerKeyCount = 0;
                    if (playerTransform != null)
                    {
                        playerController player = playerTransform.GetComponent<playerController>();
                        if (player != null)
                        {
                            playerKeyCount = playerController.keyCount;
                        }
                    }

                    doorText.text = $"Press E to open door\nKeys: {playerKeyCount}/{keysRequired}";
                }
                else
                {
                    doorText.text = "Press E to open door";
                }
            }
        }
        else
        {
            Debug.Log("Press E to open door");
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
        // Additional distance check (optional but good practice)
        if (playerTransform != null &&
            Vector3.Distance(transform.position, playerTransform.position) > interactionRange)
        {
            return;
        }

        playerController player = playerTransform.GetComponent<playerController>();
        if (player == null) return;

        bool canOpen = true;

        // Check key requirement if needed
        if (requiresKey)
        {
            canOpen = canOpen && (playerController.keyCount >= keysRequired);
        }

        // Check enemies requirement if needed
        if (requiresEnemiesDefeated)
        {
            canOpen = canOpen && (gamemanager.instance.GetGameGoalCount() <= 0);
        }

        if (canOpen)
        {
            if (requiresKey && playerController.keyCount >= keysRequired)
            {
                // Spend the required number of keys
                for (int i = 0; i < keysRequired; i++)
                    player.UseKey();
            }

            OpenDoor(); // Door is allowed to open
        }
        else
        {
            Debug.Log("Cannot open door. Requirements not met.");
            // Optional: Show a different message when requirements aren't met
            if (doorText != null)
            {
                string requirementMessage = "";
                if (requiresKey && playerController.keyCount < keysRequired)
                {
                    requirementMessage = $"Need {keysRequired - playerController.keyCount} more key(s)";
                }
                else if (requiresEnemiesDefeated && gamemanager.instance.GetGameGoalCount() > 0)
                {
                    requirementMessage = $"Defeat {gamemanager.instance.GetGameGoalCount()} more enemy(ies)";
                }

                // Temporarily show requirement message
                if (!string.IsNullOrEmpty(requirementMessage))
                {
                    string originalText = doorText.text;
                    doorText.text = requirementMessage;

                    // Optionally revert back after a delay
                    Invoke(nameof(UpdateDoorText), 1.5f);
                }
            }
        }
    }

    private void UpdateDoorText()
    {
        if (doorText != null && isPlayerInRange && !isOpen)
        {
            if (requiresKey)
            {
                int playerKeyCount = 0;
                if (playerTransform != null)
                {
                    playerController player = playerTransform.GetComponent<playerController>();
                    if (player != null)
                    {
                        playerKeyCount = playerController.keyCount;
                    }
                }
                doorText.text = $"Press E to open door\nKeys: {playerKeyCount}/{keysRequired}";
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
        isPlayerInRange = false; // Prevent further interactions

        // Hide UI prompt
        HideDoorPrompt();

        // Start sliding the door upward
        StartCoroutine(SlideUp());

        Debug.Log("Door opened!");
    }

    private System.Collections.IEnumerator SlideUp()
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
                Debug.Log("Player data saved for scene transition");
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
                Debug.LogWarning("No next scene available in build settings!");
            }
        }
    }

    // Optional: Visualize interaction range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}