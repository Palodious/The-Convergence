using UnityEngine;

[CreateAssetMenu(fileName = "New Gun Stats", menuName = "Guns/Gun Stats")]
public class gunStats : ScriptableObject
{
    [Header("~=~= Gun Model =~=~")]
    public GameObject gunModel;
    [SerializeField] public GunType gunType;

    [Header("~=~= Combat Stats =~=~")]
    [Range(10, 50)] public int shootDamage;
    [Range(15, 60)] public int shootDist;
    [Range(0.1f, 2)] public float shootRate;

    [Header("~=~= Ammo =~=~")]
    public int ammoCur;
    [Range(5, 50)] public int ammoMax;

    [Header("~=~= VFX & Audio =~=~")]
    public ParticleSystem hitEffect;
    public AudioClip[] shootSound;
    [Range(0, 1)] public float shootSoundVol;

    public AudioClip[] reloadSound;
    [Range(0, 1)] public float reloadSoundVol = 1f;

    public void ApplyUpgrade(UpgradeStat upgradeType, float amount)
    {
        switch (upgradeType)
        {
            case UpgradeStat.Damage:
                shootDamage += Mathf.RoundToInt(amount);
                shootDamage = Mathf.Clamp(shootDamage, 10, 60);
                break;

            case UpgradeStat.Distance:
                shootDist += Mathf.RoundToInt(amount);
                shootDist = Mathf.Clamp(shootDist, 15, 80);
                break;

            case UpgradeStat.Rate:
                shootRate -= amount; // Lower = faster fire rate
                shootRate = Mathf.Clamp(shootRate, 0.1f, 2f);
                break;

            case UpgradeStat.Ammo:
                ammoMax += Mathf.RoundToInt(amount);
                ammoMax = Mathf.Clamp(ammoMax, 5, 60);
                break;

            default:
              Debug.LogWarning($"gunStats.ApplyUpgrade: Unsupported upgrade type {upgradeType}");
                break;
    }
}

    public void ApplyUpgrade(string upgradeType, float amount) //overloaded method takes string for backwards compatibility/external systems
    {
        if (System.Enum.TryParse(upgradeType, true, out UpgradeStat parsed))
        {
            ApplyUpgrade(parsed, amount); // Call the enum version
        }
        else
        {
            Debug.LogWarning($"gunStats.ApplyUpgrade: Unknown upgrade string '{upgradeType}'");
        }
    }
}