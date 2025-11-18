using UnityEngine;

public class medkitPickup : MonoBehaviour
{
    [SerializeField] medkitStats medkit;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IPickup pickup = other.GetComponent<IPickup>();
            if (pickup != null)
            {
                pickup.GetItem(medkit);
                Destroy(gameObject);
            }
        }
    }
}