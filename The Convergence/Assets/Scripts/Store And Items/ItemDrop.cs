using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [Header("Item Drops")]
    public bool enableDrops = true;
    [Range(0f, 1f)] public float dropChance = 1f;
    public GameObject[] dropItemPrefabs;
    [Range(0f, 1f)] public float dropSpread = 0.5f;

    [Header("Key Drop")]
    public bool dropsKey = false;
    public GameObject keyPrefab;
    [Range(0f, 1f)] public float keyDropChance = 1f;

    public static bool keyHasDropped = false;

    public void TryDrop()
    {
        DropAllItems();

        DropKey();
    }

    private void DropAllItems()
    {
        if (!enableDrops || dropItemPrefabs == null || dropItemPrefabs.Length == 0)
            return;

        if (Random.value > dropChance)
            return;

        foreach (var itemPrefab in dropItemPrefabs)
        {
            if (itemPrefab != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 0.5f + Random.insideUnitSphere * dropSpread;
                Instantiate(itemPrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    private void DropKey()
    {
        if (!dropsKey || keyPrefab == null || keyHasDropped)
            return;

        if (Random.value > keyDropChance)
            return;

        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject keyObject = Instantiate(keyPrefab, spawnPos, Quaternion.identity);

        keyPickup keyScript = keyObject.GetComponent<keyPickup>();
        if (keyScript != null)
        {
            keyScript.EnablePickup();
        }
        else
        {
          //  Debug.LogWarning("Key prefab doesn't have a keyPickup component!");
        }

        keyHasDropped = true;
       // Debug.Log($"Key dropped! Global flag set. Key dropped by: {gameObject.name}");
    }

    public static void ResetKeyDrop()
    {
        keyHasDropped = false;
    }
}