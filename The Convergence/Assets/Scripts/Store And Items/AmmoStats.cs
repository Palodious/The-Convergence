using UnityEngine;

[CreateAssetMenu(fileName = "NewAmmoStats", menuName = "Weapons/Ammo Stats")]
public class AmmoStats : ScriptableObject
{
    [Header("Ammo Type")]
    public string ammoName;
    public gunStats compatibleGun;
    public Sprite ammoIcon;

    [Header("Ammo Properties")]
    [Range(1, 100)] public int ammoAmount;
    [Range(1, 999)] public int maxAmmoCapacity;

    [Header("Pickup Visuals")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;
    [Range(0, 1)] public float pickupSoundVol = 1f;

    [Header("UI")]
    public Color ammoUIColor = Color.yellow;
}