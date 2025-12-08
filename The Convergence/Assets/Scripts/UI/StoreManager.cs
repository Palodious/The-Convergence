using UnityEngine;

public class StoreManager : MonoBehaviour
{
    [SerializeField] private gunStats targetGunStats;

    public float damageUpgradeAmount = 5f;
    public float rateUpgradeAmount = 0.1f;

    public float distanceUpgradeAmount = 5f;
    public float ammoUpgradeAmount = 10f;

    public void BuyDamageUpgrade()
    {
        if (targetGunStats != null)
        {
            targetGunStats.ApplyUpgrade("damage", damageUpgradeAmount);
            Debug.Log($"Damage upgraded! New damage: {targetGunStats.shootDamage}");
        }
        else
        {
            Debug.LogError("targetGunStats is not assigned! Cannot apply upgrade.");
        }
    }

    public void BuyFireRateUpgrade()
    {
        if (targetGunStats != null)
        {
            targetGunStats.ApplyUpgrade("damage", damageUpgradeAmount);
            Debug.Log($"Damage upgraded! New damage: {targetGunStats.shootDamage}");
        }
        else
        {
            Debug.LogError("targetGunStats is not assigned! Cannot apply upgrade.");
        }
    }

    public void BuyDistanceUpgrade()
    {
        if (targetGunStats != null)
        {
            targetGunStats.ApplyUpgrade("damage", damageUpgradeAmount);
            Debug.Log($"Damage upgraded! New damage: {targetGunStats.shootDamage}");
        }
        else
        {
            Debug.LogError("targetGunStats is not assigned! Cannot apply upgrade.");
        }
    }

    public void BuyAmmoUpgrade()
    {
        if (targetGunStats != null)
        {
            targetGunStats.ApplyUpgrade("damage", damageUpgradeAmount);
            Debug.Log($"Damage upgraded! New damage: {targetGunStats.shootDamage}");
        }
        else
        {
            Debug.LogError("targetGunStats is not assigned! Cannot apply upgrade.");
        }
    }
}
