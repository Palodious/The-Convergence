using UnityEngine;

[CreateAssetMenu(menuName = "Items/Ammo")]
public class AmmoStats : ScriptableObject
{
    [Header("Ammo Settings")]
    public int ammoAmount = 30;
    public gunStats[] gunType;

    [Header("Pickup Effects")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;
    [Range(0, 1)] public float pickupSoundVol = 1f;

    [Header("Visual Properties")]
    public Color lightColor = Color.yellow;
    public float lightRange = 3f;
    public float lightIntensity = 1.5f;
}