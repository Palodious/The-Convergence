using UnityEngine;

[CreateAssetMenu(menuName = "Items/Key")]
public class keyStats : ScriptableObject
{
    [Header("Key Settings")]
    public string keyName = "Golden Key";
    public int keyCount = 1;

    [Header("Pickup Effects")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;
    [Range(0, 1)] public float pickupSoundVol = 1f;

    [Header("Visual Properties")]
    public Color lightColor = Color.yellow;
    public float lightRange = 3f;
    public float lightIntensity = 1.5f;
}