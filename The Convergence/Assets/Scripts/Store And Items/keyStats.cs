using UnityEngine;

[CreateAssetMenu(menuName = "Items/Key")]
public class keyStats : ScriptableObject
{
    public string keyName = "Golden Key";
    public int keyCount = 1; // How many keys this pickup gives
    public GameObject pickupEffect; // optional particle effect
}