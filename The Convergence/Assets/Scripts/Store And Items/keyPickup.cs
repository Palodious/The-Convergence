using UnityEngine;

public class keyPickup : MonoBehaviour
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

        // Check if player already has keys and self-destruct
        if (playerController.keyCount > 0)
        {
            // Destroy this key if player already has some
            //Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPickup) return;
        if (!other.CompareTag("Player")) return;

        playerController player = other.GetComponent<playerController>();
        if (player != null)
        {
            // DETACH the light before giving the key
            Light keyLight = GetComponent<Light>();
            if (keyLight != null)
            {
                // Detach from parent BEFORE destroying
                keyLight.transform.SetParent(null);
                Destroy(keyLight.gameObject); // Destroy the light GameObject completely
            }

            player.GetItem(key);
            Destroy(gameObject);
        }
    }

    public void EnablePickup()
    {
        isPickup = true;
        this.enabled = true;
    }
}