using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("~=~= Door Settings =~=~")]
    // Regular door requirement settings
    [SerializeField] bool requiresKey = false;
    [SerializeField] bool requiresEnemiesDefeated = true;
    [Range(1, 10)] [SerializeField] int keysRequired = 1; // How many keys needed

    [Header("~=~= Slide Settings =~=~")]
    [Range(0.5f, 10f)] [SerializeField] float slideDistance = 3f; // How far upward the door moves when opened

    [Range(0.1f, 10f)] [SerializeField] float slideSpeed = 2f; // How fast the door slides up

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
    }
}
