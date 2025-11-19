using UnityEngine;

[CreateAssetMenu]

public class gunStats : ScriptableObject
{
    public GameObject gunModel;

    [Range(10, 50)] public int shootDamage;
    [Range(15, 60)] public int shootDist;
    [Range(0.1f, 2)] public float shootRate;
    public int ammoCur;
    [Range(5, 50)] public int ammoMax;

    public ParticleSystem hitEffect;
    public AudioClip[] shootSound;
    [Range(0, 1)] public float shootSoundVol;
}