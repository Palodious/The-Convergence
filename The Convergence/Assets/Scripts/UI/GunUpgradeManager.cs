using System.Collections.Generic;
using UnityEngine;

public class GunUpgradeManager : MonoBehaviour
{
    public static GunUpgradeManager Instance;

    [Header("Gun Stats References")]
    public gunStats smgStats;
    public gunStats rifleStats;
    public gunStats arStats;

    private Dictionary<GunType, gunStats> gunMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeGunMap();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGunMap()
    {
        gunMap = new Dictionary<GunType, gunStats>
        {
            { GunType.SMG, smgStats },
            { GunType.Rifle, rifleStats },
            { GunType.AR, arStats }
        };
    }

    public gunStats GetGunStats(GunType type)
    {
        if (gunMap.ContainsKey(type))
        {
            return gunMap[type];
        }
        Debug.LogError($"GunStats not found for GunType: {type}");
        return null;
    }
}

