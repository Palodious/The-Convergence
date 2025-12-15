using UnityEngine;
using System.Collections;

public class gunPickup : MonoBehaviour
{
    [SerializeField] gunStats gun;
    [Range(0,50)][SerializeField] int bonusAmmo;

    private void OnTriggerEnter(Collider other)
    {
        IPickup pik = other.GetComponent<IPickup>();
       

        if (pik != null)
        {
            gun.ammoCur = gun.ammoMax;
            pik.GetItem(gun);

         

            StartCoroutine(DelayedDestroy());
        }
    }

    IEnumerator DelayedDestroy()
    {
        yield return null;
        Destroy(gameObject);
    }
}