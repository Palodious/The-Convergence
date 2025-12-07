using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StoreItem
{
    public int id;
    public string itemName;
    public int cost;
    public ItemType type; // Upgrade, Consumable

    
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
    public int Coin = 50;
    public PlayerState playerState = new PlayerState();

    [Header("Store Data")]
    public List<StoreItem> upgradeItems = new List<StoreItem>();
    public List<StoreItem> consumableItems = new List<StoreItem>();
    private List<StoreButtonUI> registeredButtons = new List<StoreButtonUI>();

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

        upgradeItems.Add(new StoreItem { id = 101, itemName = "SMG Upgrade I", cost = 5, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 102, itemName = "SMG A Upgrade II", cost = 10, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 103, itemName = "SMG A Upgrade III", cost = 15, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 104, itemName = "SMG A Upgrade IV (MAX)", cost = 20, type = ItemType.Upgrade });

        upgradeItems.Add(new StoreItem { id = 111, itemName = "Rifle Upgrade I", cost = 5, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 112, itemName = "Rifle Upgrade II", cost = 10, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 113, itemName = "Rifle Upgrade III", cost = 15, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 114, itemName = "Rifle Upgrade IV (MAX)", cost = 20, type = ItemType.Upgrade });

        upgradeItems.Add(new StoreItem { id = 121, itemName = "AR Upgrade I", cost = 5, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 122, itemName = "AR Upgrade II", cost = 10, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 123, itemName = "AR Upgrade III", cost = 15, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 124, itemName = "AR Upgrade IV (MAX)", cost = 20, type = ItemType.Upgrade });

        upgradeItems.Add(new StoreItem { id = 201, itemName = "Health Upgrade I", cost = 10, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 202, itemName = "Health Upgrade II", cost = 20, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 203, itemName = "Health Upgrade III", cost = 30, type = ItemType.Upgrade });
        upgradeItems.Add(new StoreItem { id = 204, itemName = "Health Upgrade IIII", cost = 40, type = ItemType.Upgrade });

        consumableItems.Add(new StoreItem { id = 301, itemName = "Health Potion (10c)", cost = 10, type = ItemType.Consumable, state = "potionCount" });
        consumableItems.Add(new StoreItem { id = 302, itemName = "Health Potion (20c)", cost = 20, type = ItemType.Consumable, state = "potionCount" });
        consumableItems.Add(new StoreItem { id = 303, itemName = "Health Potion (30c)", cost = 30, type = ItemType.Consumable, state = "potionCount" });
        consumableItems.Add(new StoreItem { id = 303, itemName = "Health Potion (40c)", cost = 40, type = ItemType.Consumable, state = "potionCount" });

    }


    public bool CanBuyItem(StoreItem item, out string reason)
    {

        if (Coin < item.cost)
        {
            reason = "Not Enough Coins";
            return false;
        }


        if (item.type == ItemType.Upgrade )
        {

            if (playerState.purchasedIds.Contains(item.id))
            {
                reason = "Already Purchased";
                return false;
            }

            if(item.quantity <= 0)
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

        if (CanBuyItem(item, out string reason))
        {
            // Deduct Coins
            Coin -= item.cost;

            // Apply Effect based on Type
            if (item.type == ItemType.Upgrade)
            {
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
        bool success = BuyItem(itemId);
        if (success)
        {
            Debug.Log($"Successfully purchased Item ID: {itemId}. Coins remaining: {Coin}");
        }
        else
        {
            CanBuyItem(FindItemById(itemId), out string reason);
            Debug.LogWarning($"Failed to purchase Item ID: {itemId}. Reason: {reason}");
        }

        
    }

    private StoreItem FindItemById(int id)
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

    public void OnStoreOpened()
    {
        RefreshAllButtonDisplays();
    }
}