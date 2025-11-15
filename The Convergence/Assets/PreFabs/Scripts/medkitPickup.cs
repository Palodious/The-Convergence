using UnityEngine;

public class medkitPickup : MonoBehaviour
{
    [SerializeField] medkitStats medkit;

    private void OnTriggerEnter(Collider other)
    {
        IPickup pik = other.GetComponent<IPickup>();

        if (pik != null)
        {
            pik.GetItem(medkit);
            Destroy(gameObject);
        }
    }
}