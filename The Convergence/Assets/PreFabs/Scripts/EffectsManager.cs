using UnityEngine;
using System.Collections.Generic;

public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance;

    [System.Serializable]
    public class EffectEntry
    {
        public string key;     
        public GameObject prefab;
        public int poolSize = 5;
    }

    [SerializeField] private List<EffectEntry> effects = new List<EffectEntry>();

    private Dictionary<string, ObjectPool> effectPools = new Dictionary<string, ObjectPool>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializePools()
    {
        foreach (EffectEntry entry in effects)
        {
            if (string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning("[EffectsManager] Missing key for effect: {entry.prefab.name}");
                continue;
            }

            if (entry.prefab == null)
            {
                Debug.LogError("[EffectsManager] Missing prefab for effect: {entry.key}");
                continue;
            }

            // Create a pool holder
            GameObject poolObj = new GameObject("Pool_{entry.key}");
            poolObj.transform.SetParent(transform);
            ObjectPool pool = poolObj.AddComponent<ObjectPool>();
            pool.prefab = entry.prefab;
            pool.poolSize = entry.poolSize;
            pool.Initialize();

            effectPools[entry.key] = pool;
        }
    }


    /// Creates an effect at position. Returns GameObject tov modify it 
    public GameObject Create(string effectKey, Vector3 position, Quaternion? rotation = null)
    {
        if (!effectPools.ContainsKey(effectKey))
        {
            Debug.LogWarning("[EffectsManager] Effect not found: {effectKey}");
            return null;
        }

        GameObject effect = effectPools[effectKey].GetFromPool();
        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = rotation ?? Quaternion.identity;
            effect.SetActive(true);
        }

        return effect;
    }

    public GameObject Create(string effectKey, Vector3 position, Quaternion rotation)
    {
        return Create(effectKey, position, (Quaternion)rotation);
    }

    //return to pool
  
    public void Return(GameObject effect)
    {
        if (effect == null) return;

        foreach (ObjectPool pool in effectPools.Values)
        {
            if (pool.BelongsToPool(effect))
            {
                pool.ReturnToPool(effect);
                return;
            }
        }

        Debug.LogWarning("[EffectsManager] Tried to return object not in any pool: {effect.name}");
    }
}