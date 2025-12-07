using UnityEngine;
using UnityEngine.SceneManagement; // Added for scene management

public class Door : MonoBehaviour
{
    [Header("~=~= Door Settings =~=~")]
    // Regular door requirement settings
    [SerializeField] bool requiresKey = false;
    [SerializeField] bool requiresEnemiesDefeated = true;
    [Range(1, 10)][SerializeField] int keysRequired = 1; // How many keys needed

    [Header("~=~= Slide Settings =~=~")]
    [Range(0.5f, 10f)][SerializeField] float slideDistance = 3f; // How far upward the door moves when opened
    [Range(0.1f, 10f)][SerializeField] float slideSpeed = 2f; // How fast the door slides up

    [Header("~=~= Scene Settings =~=~")]
    [SerializeField] bool loadNextScene = false; // Toggle for scene loading
    [SerializeField] string sceneName = ""; // Name of scene to load (if empty, uses build index)
    [SerializeField] int sceneBuildIndex = -1; // Build index of scene to load (-1 for next scene)
    [SerializeField] float sceneLoadDelay = 0.5f; // Delay before loading scene after door opens

    private bool isOpen = false;
    private Vector3 closedPos; // Stores the starting position so we know where to slide from

    private void Awake()
    {
        // Save the door's initial position
        closedPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpen) return;
        if (!other.CompareTag("Player")) return;

        playerController player = other.GetComponent<playerController>();
        if (player == null) return;

        bool canOpen = true;

        // Check key requirement if needed
        if (requiresKey)
        {
            // Access the static keyCount using the class name
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

        // Start sliding the door upward
        StartCoroutine(SlideUp());

        Debug.Log("Door opened!");
    }

    private System.Collections.IEnumerator SlideUp()
    {
        // Determine where the door should end up
        Vector3 targetPos = closedPos + new Vector3(0, slideDistance, 0);

        // Move the door smoothly toward that point
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                slideSpeed * Time.deltaTime
            );

            yield return null; // Wait until next frame
        }

        // Make sure the door finishes exactly in the correct position
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
        if (!string.IsNullOrEmpty(sceneName))
        {
            // Load scene by name
            SceneManager.LoadScene(sceneName);
        }
        else if (sceneBuildIndex >= 0)
        {
            // Load scene by build index
            SceneManager.LoadScene(sceneBuildIndex);
        }
        else
        {
            // Load next scene in build settings
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = currentSceneIndex + 1;

            // Check if next scene exists
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
}