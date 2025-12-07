using UnityEngine;
using System.Collections;

public class gunPickup : MonoBehaviour
{
    [SerializeField] private gunStats baseGunStats;

    private void OnTriggerEnter(Collider other)
    {
        IPickup pik = other.GetComponent<IPickup>();

        if (pik != null)
        {
            gunStats runtimeGunStats = Instantiate(baseGunStats);
            runtimeGunStats.ammoCur = runtimeGunStats.ammoMax;
            pik.GetItem(runtimeGunStats);
            StartCoroutine(DelayedDestroy());
        }
    }

    IEnumerator DelayedDestroy()
    {
        yield return null;
        Destroy(gameObject);
    }
}