using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public void PurchaseAndApplyUpgrade(UpgradeDefinition upgrade)
    {
        if (upgrade.targetGunStats == null)
        {
            return;
        }
        switch (upgrade.type)
        {
            case UpgradeDefinition.UpgradeType.Damage:
                upgrade.targetGunStats.shootDamage += (int)upgrade.statIncreaseAmount;
                break;
            case UpgradeDefinition.UpgradeType.Distance:
                upgrade.targetGunStats.shootDamage += (int)upgrade.statIncreaseAmount;
                break;
            case UpgradeDefinition.UpgradeType.FireRate:
                upgrade.targetGunStats.shootRate += (int)upgrade.statIncreaseAmount;
                break;
            case UpgradeDefinition.UpgradeType.AmmoMax:
                upgrade.targetGunStats.ammoMax += (int)upgrade.statIncreaseAmount;
                break;
        }
    }

    
}
