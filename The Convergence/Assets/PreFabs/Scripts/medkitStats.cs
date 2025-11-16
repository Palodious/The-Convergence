using UnityEngine;

[CreateAssetMenu]
public class medkitStats : ScriptableObject
{
    [Range(10, 200)] public int healAmount;

    //If true, medkit is stored in inventory instead of instant use
    public bool storeInInventory = false;

    // Only used if storeInInventory = true
    public float cooldown = 5f;
}
