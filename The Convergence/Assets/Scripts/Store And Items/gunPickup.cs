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

            if (Store.Instance != null && gunType != GunType.None && gunType != gun.gunType)
            {
               // Debug.LogWarning($"gunPickup '{name}': Inspector gunType ({gunType}) doesn't match gunStats.gunType ({gun.gunType}). Using gunStats.gunType.");
            }

            Store.Instance.UnlockGun(gun != null ? gun.gunType : gunType);


            StartCoroutine(DelayedDestroy());
        }
    }

    IEnumerator DelayedDestroy()
    {
        yield return null;
        Destroy(gameObject);
    }
}