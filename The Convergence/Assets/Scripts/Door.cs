using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("~=~= Door Settings =~=~")]
    [SerializeField] Animator anim;
    [SerializeField] bool requiresKey = false;
    [SerializeField] bool requiresEnemiesDefeated = true;
    [SerializeField] int keysRequired = 1; // NEW: How many keys needed

    private bool isOpen = false;

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
                // Use required number of keys
                for (int i = 0; i < keysRequired; i++)
                {
                    player.UseKey();
                }
            }
            OpenDoor();
        }
        else
        {
            Debug.Log("Cannot open door. Requirements not met.");
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        if (anim != null)
            anim.SetTrigger("Open");

        Debug.Log("Door opened!");
    }
}