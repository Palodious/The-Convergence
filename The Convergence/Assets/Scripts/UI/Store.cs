using UnityEngine;
using System.Collections.Generic;

public enum GunType
{
    SMG,
    Rifle,
    AR,
    None,
}

[System.Serializable]
public class StoreItem
{
    public int id;
    public string itemName;
    public int cost;
    public ItemType type; // Upgrade, Consumable
    public GunType gunType = GunType.None;
    public string upgradeStat = "";
    public float upgradeAmount = 0f;
    public string state;

    public int quantity = 1;
}

[System.Serializable]
public class PlayerState
{
    // CONSUMABLES:
    public int potionCount = 0;

    
    public List<int> purchasedIds = new List<int>();
}


public enum ItemType
{
    Upgrade,    // Single, one-time purchase
    Consumable, // Multiple purchases allowed
   
}



public class Store : MonoBehaviour
{
    public static Store Instance;

    [Header("Player State (Runtime)")]
    
    public PlayerState playerState = new PlayerState();

    [Header("Store Data")]
    public List<StoreItem> upgradeItems = new List<StoreItem>();
    public List<StoreItem> consumableItems = new List<StoreItem>();
    private List<StoreButtonUI> registeredButtons = new List<StoreButtonUI>();

    [Header("~=~= UI References =~=~")]
    [SerializeField] private GameObject storeUIPanel;

    public void RegisterButton(StoreButtonUI button)
    {
        registeredButtons.Add(button);
    }
    private void RefreshAllButtonDisplays()
    {
        foreach (StoreButtonUI button in registeredButtons)
        {
            button.UpdateDisplay();
        }
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeStoreData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

    }

    private void InitializeStoreData()
    {

        upgradeItems.Add(new StoreItem { id = 101, itemName = "SMG Upgrade I", cost = 5, type = ItemType.Upgrade, gunType = GunType.SMG, upgradeStat = "ammo", upgradeAmount = 5f });
        upgradeItems.Add(new StoreItem { id = 102, itemName = "SMG Upgrade II", cost = 10, type = ItemType.Upgrade, gunType = GunType.SMG, upgradeStat = "damage", upgradeAmount = 5f });
        upgradeItems.Add(new StoreItem { id = 103, itemName = "SMG Upgrade III", cost = 15, type = ItemType.Upgrade, gunType = GunType.SMG, upgradeStat = "rate", upgradeAmount = 5f });
        upgradeItems.Add(new StoreItem { id = 104, itemName = "SMG Upgrade IIII", cost = 20, type = ItemType.Upgrade, gunType = GunType.SMG, upgradeStat = "distance", upgradeAmount = 5f });

        upgradeItems.Add(new StoreItem { id = 111, itemName = "Rifle Upgrade I", cost = 5, type = ItemType.Upgrade, gunType = GunType.Rifle, upgradeStat = "ammo", upgradeAmount = 5f });
        upgradeItems.Add(new StoreItem { id = 112, itemName = "Rifle Upgrade II", cost = 10, type = ItemType.Upgrade, gunType = GunType.Rifle, upgradeStat = "damage", upgradeAmount = 5f });
        upgradeItems.Add(new StoreItem { id = 113, itemName = "Rifle Upgrade III", cost = 15, type = ItemType.Upgrade, gunType = GunType.Rifle, upgradeStat = "rate", upgradeAmount = 5f });
        upgradeItems.Add(new StoreItem { id = 114, itemName = "Rifle Upgrade IIII", cost = 20, type = ItemType.Upgrade, gunType = GunType.Rifle, upgradeStat = "distance", upgradeAmount = 5f });

        upgradeItems.Add(new StoreItem { id = 121, itemName = "AR Upgrade I", cost = 5, type = ItemType.Upgrade, gunType = GunType.AR, upgradeStat = "ammo", upgradeAmount = 5f });
        upgradeItems.Add(new StoreItem { id = 122, itemName = "AR Upgrade II", cost = 10, type = ItemType.Upgrade, gunType = GunType.AR, upgradeStat = "damage", upgradeAmount = 5f });
        upgradeItems.Add(new StoreItem { id = 123, itemName = "AR Upgrade III", cost = 15, type = ItemType.Upgrade, gunType = GunType.AR, upgradeStat = "rate", upgradeAmount = 5f });
        upgradeItems.Add(new StoreItem { id = 124, itemName = "AR Upgrade IIII", cost = 20, type = ItemType.Upgrade, gunType = GunType.AR, upgradeStat = "distance", upgradeAmount = 5f });

        upgradeItems.Add(new StoreItem { id = 201, itemName = "Health Upgrade I", cost = 10, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 202, itemName = "Health Upgrade II", cost = 20, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 203, itemName = "Health Upgrade III", cost = 30, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 204, itemName = "Health Upgrade IIII", cost = 40, type = ItemType.Upgrade });

        consumableItems.Add(new StoreItem { id = 301, itemName = "Health Potion (10c)", cost = 10, type = ItemType.Consumable, state = "potionCount" });
        consumableItems.Add(new StoreItem { id = 302, itemName = "Health Potion (20c)", cost = 20, type = ItemType.Consumable, state = "potionCount" });
        consumableItems.Add(new StoreItem { id = 303, itemName = "Health Potion (30c)", cost = 30, type = ItemType.Consumable, state = "potionCount" });
        consumableItems.Add(new StoreItem { id = 304, itemName = "Health Potion (40c)", cost = 40, type = ItemType.Consumable, state = "potionCount" });

    }


    public bool CanBuyItem(StoreItem item, out string reason)
    {

        if (RiftShardManager.Instance == null || RiftShardManager.Instance.Amount < item.cost)
        {
            reason = "Not Enough Rift Shards";
            return false;
        }


            if (item.type == ItemType.Upgrade)
        {

            if (playerState.purchasedIds.Contains(item.id))
            {
                reason = "Already Purchased";
                return false;
            }

            if (item.quantity <= 0)
            {
                reason = "Out of Stock";
                return false;
            }

            reason = "Available"; // Quantity is 1 (Available for purchase)
            return true;
        }
        else if (item.type == ItemType.Consumable)
        {
            // Consumables are always available if you have coins
            reason = "Available";
            return true;
        }

        reason = "Unknown Item Type";
        return false;
    }


    public bool BuyItem(int itemId)
    {
        StoreItem item = FindItemById(itemId);
        if (item == null)
        {

            return false;
        }
        if (item.type == ItemType.Upgrade && playerState.purchasedIds.Contains(item.id))
        {
            Debug.LogWarning($"CRITICAL BLOCK: Purchase of already-owned upgrade (ID: {itemId}) blocked inside BuyItem.");
            return false;
        }

        if (CanBuyItem(item, out string reason))
        {
            if (!RiftShardManager.Instance.TrySpend(item.cost))
            {
                return false;
            }

                // Apply Effect based on Type
                if (item.type == ItemType.Upgrade)
            {
                if (item.gunType != GunType.None)
                {
                    gunStats targetGun = GunUpgradeManager.Instance.GetGunStats(item.gunType);

                    if (targetGun != null)
                    {
                        targetGun.ApplyUpgrade(item.upgradeStat, item.upgradeAmount);
                        Debug.Log($"Applied {item.itemName} to {item.gunType}. New {item.upgradeStat}: {targetGun.shootDamage} (example stat)");
                    }
                }

                // Mark as purchased, effectively setting its quantity to 0
                playerState.purchasedIds.Add(item.id);
                item.quantity = 0;

            }
            else if (item.type == ItemType.Consumable)
            {
                playerState.potionCount++;

            }
            RefreshAllButtonDisplays();

            return true;
        }
        else
        {

            return false;
        }
    }

    public void PurchaseItemButton(int itemId)
    {
        StoreItem item = FindItemById(itemId);
        if (!CanBuyItem(item, out string reason))
        {
            Debug.LogWarning($"Failed to initiate purchase for Item ID: {itemId}. Reason: {reason}");
            
            RefreshAllButtonDisplays();
            return;
        }
        bool success = BuyItem(itemId);
        if (success)
        {
            Debug.Log($"Successfully purchased Item ID: {itemId}. Shards remaining: {RiftShardManager.Instance.Amount}");
        }
        else
        {
            CanBuyItem(FindItemById(itemId), out reason);
            Debug.LogWarning($"Failed to purchase Item ID: {itemId}. Reason: {reason}");
        }


    }

    public StoreItem FindItemById(int id)
    {
        StoreItem foundItem = null;

        // Search Upgrades 
        foreach (StoreItem item in upgradeItems)
        {
            if (item.id == id)
            {
                foundItem = item;
                break;
            }
        }
        if (foundItem != null) return foundItem;

        // Search Consumables
        foreach (StoreItem item in consumableItems)
        {
            if (item.id == id)
            {
                foundItem = item;
                break;
            }
        }
        if (foundItem != null) return foundItem;

        return foundItem;

    }


    public void ExitStore()
    {
        if (storeUIPanel != null)
        {
            storeUIPanel.SetActive(false);

        }
        else
        {
            Debug.LogWarning("Store UI Panel reference is missing on the Store script. Cannot hide panel.");
        }
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
        if (gamemanager.instance != null)
        {
            gamemanager.instance.stateUnpause();
            Debug.Log("Store closed and game resumed.");
        }

    }
    public void SetStoreOpen()
    {
        RefreshAllButtonDisplays();
        if (storeUIPanel != null)
        {
            storeUIPanel.SetActive(true);
            
        }
    }
}