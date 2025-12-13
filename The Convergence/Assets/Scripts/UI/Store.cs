using System;
using System.Collections.Generic;
using UnityEngine;

public enum GunType
{
    SMG,
    Rifle,
    AR,
    None,
}

public enum UpgradeStat
{
    Damage,
    Rate,
    Distance,
    Ammo,
    MaxHP
}

[System.Serializable]
public class StoreItem
{
    public int id;
    public string itemName;
    public int cost;
    public ItemType type; // Upgrade, Consumable
    public GunType gunType = GunType.None;
    public UpgradeStat upgradeStat;
    public float upgradeAmount = 0f;
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



public class Store : MonoBehaviour, ISaveable
{
    public static Store Instance;

    [Header("Player State (Runtime)")]
    
    public PlayerState playerState = new PlayerState();

    [Header("Store Data")]
    public List<StoreItem> upgradeItems = new List<StoreItem>();
    public List<StoreItem> consumableItems = new List<StoreItem>();
    private readonly List<StoreButtonUI> registeredButtons = new();

    [Header("~=~= UI References =~=~")]
    [SerializeField] private GameObject storeUIPanel;

    [Serializable]
    private struct StoreSaveData
    {
        public int potionCount;
        public List<int> purchasedIds;
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

    public void RegisterButton(StoreButtonUI button)
    {
        if (!registeredButtons.Contains(button))
            registeredButtons.Add(button);
    }

    private void RefreshAllButtonDisplays()
    {
        foreach (var button in registeredButtons)
            button.UpdateDisplay();
    }

    private void InitializeStoreData()
    {
        upgradeItems.Add(new StoreItem { id = 101, itemName = "SMG Ammo Upgrade", cost = 5, type = ItemType.Upgrade, gunType = GunType.SMG, upgradeStat = UpgradeStat.Ammo, upgradeAmount = 5 });
        upgradeItems.Add(new StoreItem { id = 102, itemName = "SMG Damage Upgrade", cost = 10, type = ItemType.Upgrade, gunType = GunType.SMG, upgradeStat = UpgradeStat.Damage, upgradeAmount = 5 });
        upgradeItems.Add(new StoreItem { id = 103, itemName = "SMG Fire Rate Upgrade", cost = 15, type = ItemType.Upgrade, gunType = GunType.SMG, upgradeStat = UpgradeStat.Rate, upgradeAmount = 0.1f });
        upgradeItems.Add(new StoreItem { id = 104, itemName = "SMG Distance Upgrade", cost = 20, type = ItemType.Upgrade, gunType = GunType.SMG, upgradeStat = UpgradeStat.Distance, upgradeAmount = 5 });

        upgradeItems.Add(new StoreItem { id = 111, itemName = "Rifle Ammo Upgrade", cost = 5, type = ItemType.Upgrade, gunType = GunType.Rifle, upgradeStat = UpgradeStat.Ammo, upgradeAmount = 5 });
        upgradeItems.Add(new StoreItem { id = 112, itemName = "Rifle Damage Upgrade", cost = 10, type = ItemType.Upgrade, gunType = GunType.Rifle, upgradeStat = UpgradeStat.Damage, upgradeAmount = 5 });
        upgradeItems.Add(new StoreItem { id = 113, itemName = "Rifle Fire Rate Upgrade", cost = 15, type = ItemType.Upgrade, gunType = GunType.Rifle, upgradeStat = UpgradeStat.Rate, upgradeAmount = 0.1f });
        upgradeItems.Add(new StoreItem { id = 114, itemName = "Rifle Distance Upgrade", cost = 20, type = ItemType.Upgrade, gunType = GunType.Rifle, upgradeStat = UpgradeStat.Distance, upgradeAmount = 5 });

        upgradeItems.Add(new StoreItem { id = 121, itemName = "AR Ammo Upgrade", cost = 5, type = ItemType.Upgrade, gunType = GunType.AR, upgradeStat = UpgradeStat.Ammo, upgradeAmount = 5 });
        upgradeItems.Add(new StoreItem { id = 122, itemName = "AR Damage Upgrade", cost = 10, type = ItemType.Upgrade, gunType = GunType.AR, upgradeStat = UpgradeStat.Damage, upgradeAmount = 5 });
        upgradeItems.Add(new StoreItem { id = 123, itemName = "AR Fire Rate Upgrade", cost = 15, type = ItemType.Upgrade, gunType = GunType.AR, upgradeStat = UpgradeStat.Rate, upgradeAmount = 0.1f });
        upgradeItems.Add(new StoreItem { id = 124, itemName = "AR Distance Upgrade", cost = 20, type = ItemType.Upgrade, gunType = GunType.AR, upgradeStat = UpgradeStat.Distance, upgradeAmount = 5 });

        upgradeItems.Add(new StoreItem { id = 201, itemName = "Max HP Upgrade I", cost = 10, type = ItemType.Upgrade, gunType = GunType.None, upgradeStat = UpgradeStat.MaxHP, upgradeAmount = 25 });
        upgradeItems.Add(new StoreItem { id = 202, itemName = "Max HP Upgrade II", cost = 20, type = ItemType.Upgrade, gunType = GunType.None, upgradeStat = UpgradeStat.MaxHP, upgradeAmount = 50 });
        upgradeItems.Add(new StoreItem { id = 203, itemName = "Max HP Upgrade III", cost = 30, type = ItemType.Upgrade, gunType = GunType.None, upgradeStat = UpgradeStat.MaxHP, upgradeAmount = 75 });
        upgradeItems.Add(new StoreItem { id = 204, itemName = "Max HP Upgrade IV", cost = 40, type = ItemType.Upgrade, gunType = GunType.None, upgradeStat = UpgradeStat.MaxHP, upgradeAmount = 100 });

        consumableItems.Add(new StoreItem { id = 301, itemName = "Health Potion", cost = 10, type = ItemType.Consumable, gunType = GunType.None, upgradeStat = UpgradeStat.MaxHP, upgradeAmount = 0 });
        consumableItems.Add(new StoreItem { id = 302, itemName = "Health Potion+", cost = 20, type = ItemType.Consumable, gunType = GunType.None, upgradeStat = UpgradeStat.MaxHP, upgradeAmount = 0 });
        consumableItems.Add(new StoreItem { id = 303, itemName = "Health Potion++", cost = 30, type = ItemType.Consumable, gunType = GunType.None, upgradeStat = UpgradeStat.MaxHP, upgradeAmount = 0 });
        consumableItems.Add(new StoreItem { id = 304, itemName = "Health Potion MAX", cost = 40, type = ItemType.Consumable, gunType = GunType.None, upgradeStat = UpgradeStat.MaxHP, upgradeAmount = 0 });
    }


    public bool CanBuyItem(StoreItem item, out string reason)
    {

        if (item == null)
        {
            reason = "Invalid Item";
            return false;
        }

        if (RiftShardManager.Instance == null || RiftShardManager.Instance.Amount < item.cost)
        {
            reason = "Not Enough Rift Shards";
            return false;
        }


        if (item.type == ItemType.Upgrade && playerState.purchasedIds.Contains(item.id))
        {
            reason = "Already Purchased";
            return false;
        }

        reason = "Available";
        return true;
    }
    public bool BuyItem(int itemId)
    {
        StoreItem item = FindItemById(itemId);
        if (item == null)
            return false;

        if (!CanBuyItem(item, out _))
            return false;

        if (!RiftShardManager.Instance.TrySpend(item.cost))
            return false;

        if (item.type == ItemType.Upgrade)
        {
            ApplyUpgrade(item);
            playerState.purchasedIds.Add(item.id);
        }
        else
        {
            playerState.potionCount++;
        }

        RefreshAllButtonDisplays();
        return true;
    }

    private void ApplyUpgrade(StoreItem item)
    {

        if (item == null) return;

        if (item.upgradeStat == UpgradeStat.MaxHP)
        {
            gamemanager.instance.playerScript.ApplyHealthUpgrade(item.upgradeAmount);
            return;
        }

        gunStats gun = GunUpgradeManager.Instance.GetGunStats(item.gunType);
        if (gun == null)
        {
            Debug.LogError($"Store: No gunStats found for {item.gunType}");
            return;
        }

        gun.ApplyUpgrade(item.upgradeStat, item.upgradeAmount);
    }

    public void PurchaseItemButton(int itemId)
    {
        BuyItem(itemId);
    }

    public StoreItem FindItemById(int id)
    {
        foreach (var item in upgradeItems)
            if (item.id == id)
                return item;

        foreach (var item in consumableItems)
            if (item.id == id)
                return item;

        return null;
    }

    public void SetStoreOpen()
    {
        RefreshAllButtonDisplays();

        if (storeUIPanel != null)
            storeUIPanel.SetActive(true);
    }

    public void ExitStore()
    {
        if (storeUIPanel != null)
            storeUIPanel.SetActive(false);

        gamemanager.instance.stateUnpause();
    }

    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state);

    public object CaptureState()
    {
        return new StoreSaveData
        {
            potionCount = playerState != null ? playerState.potionCount : 0,
            purchasedIds = playerState != null ? new List<int>(playerState.purchasedIds) : new List<int>()
        };
    }

    public void RestoreState(object state)
    {
        if (state is not StoreSaveData s)
        {
            Debug.LogError($"Store.RestoreState: expected StoreSaveData, got {state?.GetType()} on {name}");
            return;
        }

        if (playerState == null)
            playerState = new PlayerState();

        playerState.potionCount = s.potionCount;
        playerState.purchasedIds = (s.purchasedIds != null) ? new List<int>(s.purchasedIds) : new List<int>();
        RefreshAllButtonDisplays();

    }
}