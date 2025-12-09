using UnityEngine;

public class keyPickup : MonoBehaviour
{
    [SerializeField] keyStats key;
    [SerializeField] private bool isPickup = true;

    private void Start()
    {
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
            // DETACH the light before giving the key
            Light keyLight = GetComponent<Light>();
            if (keyLight != null)
            {
                keyLight.transform.SetParent(null);
                Destroy(keyLight.gameObject);
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