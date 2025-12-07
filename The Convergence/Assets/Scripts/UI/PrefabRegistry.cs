using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrefabRegistry : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public string key;
        public GameObject prefab;
    }

    [Header("Prefabs I can spawn by key")]
    [SerializeField] private Entry[] entries;

    private Dictionary<string, GameObject> lookup;

    void Awake()
    {
        // Build the lookup one time.
        lookup = entries
            .Where(e => !string.IsNullOrEmpty(e.key) && e.prefab != null)
            .ToDictionary(e => e.key, e => e.prefab);
    }

    /// Spawns a prefab by its string key. Returns the spawned GameObject or null if the key is unknown.
    public GameObject SpawnByKey(string key)
    {
        if (lookup == null || !lookup.TryGetValue(key, out var prefab) || prefab == null)
        {
            Debug.LogWarning($"PrefabRegistry: No prefab found for key '{key}'");
            return null;
        }

        return Instantiate(prefab);
    }
}
