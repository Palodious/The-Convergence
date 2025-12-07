using UnityEngine;

[CreateAssetMenu]
public class UpgradeDefinition : MonoBehaviour
{
    public enum UpgradeType { Damage, Distance, FireRate, AmmoMax}

    public UpgradeType type;

    public string upgareName;
    public string description;


    public float statIncreaseAmount;

    public gunStats targetGunStats;
}
