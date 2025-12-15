using UnityEngine;

[CreateAssetMenu(menuName = "Items/Medkit")]
public class medkitStats : ScriptableObject
{
    [Header("Healing Settings")]
    [Range(10, 200)] public int healAmount = 50;

    [Header("Pickup Effects")]
    public GameObject useEffect;
    public GameObject pickupEffect;
    public AudioClip pickupSound;
    [Range(0, 1)] public float pickupSoundVol = 1f;

    [Header("Visual Properties")]
    public LightColor lightColorType = LightColor.Green;
    public float lightRange = 3f;
    public float lightIntensity = 1.5f;
}

public enum LightColor
{
    Green,
    Red
}