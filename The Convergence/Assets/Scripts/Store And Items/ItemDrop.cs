using UnityEngine;
using UnityEngine.SceneManagement; // Add this namespace

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

    [Header("Scene Restrictions")]
    [SerializeField] private string[] noKeyDropScenes = { "Tutorial", "Game Play Scene Tutorial" }; 
    [SerializeField] private bool checkSceneName = true;

    public static bool keyHasDropped = false;

    public void TryDrop()
    {
        DropAllItems();

        if (!IsInRestrictedScene())
        {
            DropKey();
        }
    }

    private bool IsInRestrictedScene()
    {
        if (!checkSceneName) return false;

        string currentScene = SceneManager.GetActiveScene().name;
        foreach (string sceneName in noKeyDropScenes)
        {
            if (currentScene.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
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

        keyHasDropped = true;
    }

    public static void ResetKeyDrop()
    {
        keyHasDropped = false;
    }
}