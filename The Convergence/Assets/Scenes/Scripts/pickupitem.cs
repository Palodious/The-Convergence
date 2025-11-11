using UnityEngine;

public class pickupitem : MonoBehaviour
{
    public enum PickupType { Health, Flow }

    [SerializeField] PickupType type;
    [SerializeField] int amount = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerController player = other.GetComponent<playerController>();
            if (player != null)
            {
                if (type == PickupType.Health)
                {
                    player.CurrentHP += amount;  // Use public property instead of inaccessible HP field
                }
                else if (type == PickupType.Flow)
                {
                    player.addFlow(amount);
                }
            }
            Destroy(gameObject);
        }
    }
}
