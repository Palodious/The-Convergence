using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [SerializeField] keyStats key;
    [SerializeField] private bool isPickup = true; // Toggle for enemies dropping keys

    private void Start()
    {
        // If this is attached to an enemy, the enemy should control whether it drops this
        if (!isPickup)
        {
            this.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPickup) return;
        if (!other.CompareTag("Player")) return;

        playerController player = other.GetComponent<playerController>();
        if (player != null)
        {
            player.GetItem(key);
            Destroy(gameObject);
        }
    }

    // Public method for enemies to enable dropping
    public void EnablePickup()
    {
        isPickup = true;
        this.enabled = true;
    }
}