using System;
using System.Collections.Generic;
using UnityEngine;

public class GunUpgradeManager : MonoBehaviour, ISaveable
{
    public static GunUpgradeManager Instance;

    [Header("Gun Stats References")]
    public gunStats smgStats;
    public gunStats rifleStats;
    public gunStats arStats;

    private Dictionary<GunType, gunStats> gunMap;

    [Serializable]
    private struct GunBaseSnapshot
    {
        public int shootDamage;
        public int shootDist;
        public float shootRate;
        public int ammoMax;
    }

    private Dictionary<GunType, GunBaseSnapshot> baseSnapshots = new();

    [Serializable]
    private struct GunUpgradeSaveData
    {
        public GunStatSave smg;
        public GunStatSave rifle;
        public GunStatSave ar;
    }

    [Serializable]
    private struct GunStatSave
    {
        public bool hasData;
        public int shootDamage;
        public int shootDist;
        public float shootRate;
        public int ammoMax;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGunMap();
            CacheBaseSnapshots();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGunMap()
    {
        gunMap = new Dictionary<GunType, gunStats>();

        if (smgStats != null) gunMap[GunType.SMG] = smgStats;
        else// Debug.LogWarning("GunUpgradeManager: smgStats is not assigned.");

        if (rifleStats != null) gunMap[GunType.Rifle] = rifleStats;
        else// Debug.LogWarning("GunUpgradeManager: rifleStats is not assigned.");

        if (arStats != null) gunMap[GunType.AR] = arStats;
        ;
    }

    public gunStats GetGunStats(GunType type)
    {
        if (type == GunType.None)
        {
           // Debug.LogWarning("GunUpgradeManager.GetGunStats called with GunType.None");
            return null;
        }

        if (gunMap == null || gunMap.Count == 0)
        {
            InitializeGunMap();
        }

        if (gunMap.TryGetValue(type, out var stats) && stats != null)
        {
            return stats;
        }

       // Debug.LogError($"GunUpgradeManager: gunStats not found or missing reference for GunType: {type}");
        return null;
    }

    private void CacheBaseSnapshots()
    {
        baseSnapshots.Clear();

        CacheBase(GunType.SMG, smgStats);
        CacheBase(GunType.Rifle, rifleStats);
        CacheBase(GunType.AR, arStats);
    }

    private void CacheBase(GunType type, gunStats stats)
    {
        if (stats == null) return;

        baseSnapshots[type] = new GunBaseSnapshot
        {
            shootDamage = stats.shootDamage,
            shootDist = stats.shootDist,
            shootRate = stats.shootRate,
            ammoMax = stats.ammoMax
        };
    }
    public void ResetToBase()
    {
        ApplySnapshot(GunType.SMG, smgStats, GetBase(GunType.SMG));
        ApplySnapshot(GunType.Rifle, rifleStats, GetBase(GunType.Rifle));
        ApplySnapshot(GunType.AR, arStats, GetBase(GunType.AR));
    }

    private GunBaseSnapshot GetBase(GunType type)
    {
        if (baseSnapshots.TryGetValue(type, out var s))
            return s;

        return default;
    }
    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state);

    public object CaptureState()
    {
        return new GunUpgradeSaveData
        {
            smg = CaptureOne(smgStats),
            rifle = CaptureOne(rifleStats),
            ar = CaptureOne(arStats)
        };
    }

    private GunStatSave CaptureOne(gunStats stats)
    {
        if (stats == null)
            return new GunStatSave { hasData = false };

        return new GunStatSave
        {
            hasData = true,
            shootDamage = stats.shootDamage,
            shootDist = stats.shootDist,
            shootRate = stats.shootRate,
            ammoMax = stats.ammoMax
        };
    }
    public void RestoreState(object state)
    {
        if (state is not GunUpgradeSaveData s)
        {
          //  Debug.LogError($"GunUpgradeManager.RestoreState: expected GunUpgradeSaveData, got {state?.GetType()} on {name}");
            return;
        }

        RestoreOne(GunType.SMG, smgStats, s.smg);
        RestoreOne(GunType.Rifle, rifleStats, s.rifle);
        RestoreOne(GunType.AR, arStats, s.ar);
    }

    private void RestoreOne(GunType type, gunStats stats, GunStatSave save)
    {
        if (stats == null)
        {
           // Debug.LogWarning($"GunUpgradeManager.RestoreOne: {type} stats is null (skipping restore).");
            return;
        }

        if (!save.hasData)
        {
          //  Debug.LogWarning($"GunUpgradeManager.RestoreOne: No saved data for {type} (leaving current values).");
            return;
        }

        stats.shootDamage = Mathf.Clamp(save.shootDamage, 10, 60);
        stats.shootDist = Mathf.Clamp(save.shootDist, 15, 80);
        stats.shootRate = Mathf.Clamp(save.shootRate, 0.1f, 2f);
        stats.ammoMax = Mathf.Clamp(save.ammoMax, 5, 60);
        stats.ammoCur = Mathf.Clamp(stats.ammoCur, 0, stats.ammoMax);
    }
    private void ApplySnapshot(GunType type, gunStats stats, GunBaseSnapshot snap)
    {
        if (stats == null) return;

        // If snapshot is default, do nothing
        if (snap.shootDamage == 0 && snap.shootDist == 0 && snap.shootRate == 0f && snap.ammoMax == 0)
            return;

        stats.shootDamage = Mathf.Clamp(snap.shootDamage, 10, 50);
        stats.shootDist = Mathf.Clamp(snap.shootDist, 15, 60);
        stats.shootRate = Mathf.Clamp(snap.shootRate, 0.1f, 2f);
        stats.ammoMax = Mathf.Clamp(snap.ammoMax, 5, 50);
        stats.ammoCur = Mathf.Clamp(stats.ammoCur, 0, stats.ammoMax);
    }
}

