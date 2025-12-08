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

    public void ApplyUpgrade(string upgradeType, float amount)
    {
        switch (upgradeType.ToLower())
        {
            case "damage":
                shootDamage += Mathf.RoundToInt(amount);
                shootDamage = Mathf.Clamp(shootDamage, 10, 50);
                break;
            case "distance":
                shootDist += Mathf.RoundToInt(amount);
                shootDist = Mathf.Clamp(shootDist, 15, 60);
                break;
            case "rate":
                shootRate -= amount;
                shootRate = Mathf.Clamp(shootRate, 0.1f, 2f);
                break;
            case "ammo":
                ammoMax += Mathf.RoundToInt(amount);
                ammoMax = Mathf.Clamp(ammoMax, 5, 50);
                break;

            default:
                break;
        }
    }
}

