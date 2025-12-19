using UnityEngine;

public class StoreManager : MonoBehaviour
{


    public float damageUpgradeAmount = 5f;
    public float rateUpgradeAmount = 0.1f;

    public float distanceUpgradeAmount = 5f;
    public float ammoUpgradeAmount = 10f;

    private playerController playerRef;

    void Start()
    {
        playerRef = Object.FindFirstObjectByType<playerController>();
    }

    public void BuyDamageUpgrade()
    {
        gunStats target = GetActiveGunInstance();
        if (target != null)
        {
            target.ApplyUpgrade("damage", damageUpgradeAmount);
          //  Debug.Log($"Damage upgraded! New damage: {target.shootDamage}");
        }
    }

    public void BuyFireRateUpgrade()
    {
        gunStats target = GetActiveGunInstance();
        if (target != null)
        {
            target.ApplyUpgrade("rate", rateUpgradeAmount);
           // Debug.Log($"Fire Rate upgraded! New rate: {target.shootRate}");
        }
    }

    public void BuyDistanceUpgrade()
    {
        gunStats target = GetActiveGunInstance();
        if (target != null)
        {
            target.ApplyUpgrade("distance", distanceUpgradeAmount);
          //  Debug.Log($"Distance upgraded! New distance: {target.shootDist}");
        }
    }

    public void BuyAmmoUpgrade()
    {
        gunStats target = GetActiveGunInstance();

        if (target != null)
        {
            target.ApplyUpgrade("ammo", ammoUpgradeAmount);

            target.ammoCur = target.ammoMax;

          //  Debug.Log($"Ammo upgraded! New Max Ammo: {target.ammoMax}");

            playerRef.UpdateAmmoDisplay();
        }
         
    }

    private gunStats GetActiveGunInstance()
    {
        if (playerRef == null)
        {
          //  Debug.LogError("Player Reference is NULL in StoreManager.");
            return null;
        }
        if (playerRef.activeGunStats == null)
        {
          //  Debug.LogWarning("Player has no active gun equipped. Cannot apply upgrade.");
            return null;
        }
        return playerRef.activeGunStats;
    }
}

