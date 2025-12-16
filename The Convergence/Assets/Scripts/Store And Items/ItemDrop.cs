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

    [Header("Multiple Item Drops")]
    [SerializeField] private bool allowMultipleItems = true;
    [SerializeField] private int maxItemCount = 5;

    void Start()
    {

    }

    public void TryDrop()
    {
        DropRegularItems();

        DropKey();
    }

    private void DropRegularItems()
    {
        if (!enableDrops || dropItems == null || dropItems.Length == 0 || pickupPrefab == null)
            return;

        if (Random.value > dropChance)
            return;

        int itemsToDrop = allowMultipleItems ? Random.Range(1, Mathf.Min(maxItemCount, dropItems.Length) + 1) : 1;

        for (int i = 0; i < itemsToDrop; i++)
        {
            int itemIndex = Random.Range(0, dropItems.Length);
            ScriptableObject item = dropItems[itemIndex];

            if (item != null)
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

    private void DropKey()
    {
        if (!dropsKey || keyPrefab == null)
            return;

        if (keyHasDropped)
            return;

        if (Random.value > keyDropChance)
            return;

        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        keyPickup keyDrop = Instantiate(keyPrefab, spawnPos, Quaternion.identity);
        if (keyDrop != null)
        {
            keyDrop.EnablePickup();
            keyHasDropped = true;
            Debug.Log($"Key dropped by {gameObject.name}. Global key drop flag set to true.");
        }
    }

    public static void ResetKeyDrop()
    {
        keyHasDropped = false;
    }
}