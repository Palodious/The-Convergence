using UnityEngine;
using System.Collections;

public class gunPickup : MonoBehaviour
{
    [SerializeField] gunStats gun;
    [Range(0, 50)][SerializeField] int bonusAmmo;
    [SerializeField] private GunType gunType = GunType.None;

    private void OnTriggerEnter(Collider other)
    {
        IPickup pik = other.GetComponent<IPickup>();

        if (pik != null)
        {
            pik.GetItem(gun);

            if (Store.Instance != null)
                Store.Instance.UnlockGun(gunType);

            StartCoroutine(DelayedDestroy());
        }
    }

    IEnumerator DelayedDestroy()
    {
        yield return null;
        Destroy(gameObject);
    }
}