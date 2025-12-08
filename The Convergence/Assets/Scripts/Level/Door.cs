using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    [Header("~=~= Door Settings =~=~")]
    [SerializeField] bool requiresKey = false;
    [SerializeField] bool requiresEnemiesDefeated = true;
    [Range(1, 10)][SerializeField] int keysRequired = 1;

    [Header("~=~= Interaction Settings =~=~")]
    [SerializeField] KeyCode interactionKey = KeyCode.E;
    [SerializeField] float interactionRange = 3f;

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

        // Optional: Show UI prompt here
        Debug.Log("Press E to open door");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = false;
        playerTransform = null;

        // Optional: Hide UI prompt here
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
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        isPlayerInRange = false; // Prevent further interactions

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