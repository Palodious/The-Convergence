using UnityEngine;

[CreateAssetMenu(menuName = "Items/Ammo")]
public class AmmoStats : ScriptableObject
{
    [Header("Ammo Settings")]
    public int ammoAmount = 30; // Amount of ammo this pickup gives
    public gunStats gunType; // Which gun this ammo is for (null = current gun)

    [Header("Pickup Effects")]
    public GameObject pickupEffect; // Optional particle effect
    public AudioClip pickupSound; // Optional pickup sound
    [Range(0, 1)] public float pickupSoundVol = 1f;

    [Header("Visual Properties")]
    public Color lightColor = Color.yellow;
    public float lightRange = 3f;
    public float lightIntensity = 1.5f;
}