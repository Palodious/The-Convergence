using UnityEngine;
using System.Collections;

public class keyPickup : MonoBehaviour
{
    [Header("~=~= Key Pickup =~=~")]
    [SerializeField] KeyItem key;

    private void OnTriggerEnter(Collider other)
    {
        IPickup pik = other.GetComponent<IPickup>();

        if (pik != null)
        {
            pik.GetItem(key);
            StartCoroutine(DelayedDestroy());
        }
    }

    IEnumerator DelayedDestroy()
    {
        yield return null;
        Destroy(gameObject);
    }
}