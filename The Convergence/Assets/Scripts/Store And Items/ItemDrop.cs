using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [Header("Item Drops")]
    public bool enableDrops = true;
    [Range(0f, 1f)] public float dropChance = 1f;
    public GameObject pickupPrefab;
    public ScriptableObject[] dropItems;
    [Range(0f, 1f)] public float dropSpread = 0.5f;

    [Header("Key Drop")]
    public bool dropsKey = false;
    public keyPickup keyPrefab;
    [Range(0f, 1f)] public float keyDropChance = 1f;

    public static bool keyHasDropped = false;

    public void TryDrop()
    {
        // Drop items
        if (enableDrops && dropItems != null && dropItems.Length > 0 && pickupPrefab != null)
        {
            if (Random.value <= dropChance)
            {
                foreach (var item in dropItems)
                {
                    Vector3 spawnPos = transform.position + Vector3.up * 0.5f + Random.insideUnitSphere * dropSpread;
                    GameObject drop = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);

                    var comps = drop.GetComponents<MonoBehaviour>();
                    foreach (var comp in comps)
                    {
                        if (comp == null) continue;
                        var method = comp.GetType().GetMethod("AssignItem", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (method != null)
                        {
                            try
                            {
                                method.Invoke(comp, new object[] { item });
                                break;
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        // Drop key once globally
        if (!keyHasDropped && dropsKey && keyPrefab != null && Random.value <= keyDropChance)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            keyPickup keyDrop = Instantiate(keyPrefab, spawnPos, Quaternion.identity);
            if (keyDrop != null)
            {
                keyDrop.EnablePickup();
                keyHasDropped = true;
            }
        }
    }

}
