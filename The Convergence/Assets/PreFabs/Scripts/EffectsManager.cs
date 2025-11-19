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
        [Range (3, 10)] public int poolSize;
        public AudioClip soundEffect;     
        [Range(0f, 1f)] public float volume;         
    }

    [SerializeField] private List<EffectEntry> effects = new List<EffectEntry>();
    [SerializeField] private AudioSource audioSource; 

    private Dictionary<string, ObjectPool> effectPools = new Dictionary<string, ObjectPool>();
    private Dictionary<string, EffectEntry> effectData = new Dictionary<string, EffectEntry>();

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
        // Create audio source
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        foreach (EffectEntry entry in effects)
        {
            if (string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning($"[EffectsManager] Missing key for effect: {entry.prefab?.name}");
                continue;
            }

            if (entry.prefab == null)
            {
                Debug.LogError($"[EffectsManager] Missing prefab for effect: {entry.key}");
                continue;
            }

            // Create pool
            GameObject poolObj = new GameObject($"Pool_{entry.key}");
            poolObj.transform.SetParent(transform);
            ObjectPool pool = poolObj.AddComponent<ObjectPool>();
            pool.prefab = entry.prefab;
            pool.poolSize = entry.poolSize;
            pool.Initialize();

            effectPools[entry.key] = pool;
            effectData[entry.key] = entry; // Store effect data for audio lookup

            Debug.Log($"[EffectsManager] Initialized pool for: {entry.key}");
        }
    }

    // Creates an effect at position and plays sound
    public GameObject Create(string effectKey, Vector3 position, Quaternion? rotation = null)
    {
        if (!effectPools.ContainsKey(effectKey))
        {
            Debug.LogWarning($"[EffectsManager] Effect not found: {effectKey}");
            return null;
        }

        GameObject effect = effectPools[effectKey].GetFromPool();
        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = rotation ?? Quaternion.identity;
            effect.SetActive(true);

            // Play sound 
            if (effectData[effectKey].soundEffect != null)
            {
                audioSource.PlayOneShot(effectData[effectKey].soundEffect, effectData[effectKey].volume);
            }
        }

        return effect;
    }

    public GameObject Create(string effectKey, Vector3 position, Quaternion rotation)
    {
        return Create(effectKey, position, (Quaternion?)rotation);
    }

    // Creates an effect WITHOUT playing sound (for manual sound control)
    public GameObject CreateSilent(string effectKey, Vector3 position, Quaternion? rotation = null)
    {
        if (!effectPools.ContainsKey(effectKey))
        {
            Debug.LogWarning($"[EffectsManager] Effect not found: {effectKey}");
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
    // Return effect to pool
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

        Debug.LogWarning($"[EffectsManager] Tried to return object not in any pool: {effect.name}");
    }
}