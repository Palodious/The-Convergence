using System;
using System.Collections.Generic;
using UnityEngine;

/*public enum GunType
{
    SMG,
    Rifle,
    AR,
    None,
}*/

/*public enum UpgradeStat
{
    Damage,
    Rate,
    Distance,
    Ammo,
    MaxHP,
    Heal,
}*/

/*public enum ItemType
{
    Upgrade,
    Consumable,
}*/

[System.Serializable]
public class StoreItem
{
    public int id;
    public string itemName;
    public int cost;
    public ItemType type; // Upgrade, Consumable

    [Header("Upgrade Target (Upgrades Only)")]
    public GunType gunType = GunType.None;
    public UpgradeStat upgradeStat;

    [Header("Upgrade Leveling (Upgrades Only)")]
    public int maxLevel = 1;
    public int baseCost = 0;
    public int costPerLevel = 0;

    public float baseAmount = 0f;
    public float amountPerLevel = 0f;

    [Header("Consumable (Consumables Only)")]
    public int quantity = 1;
}

[System.Serializable]
public class UpgradeLevelEntry 
{
    public int id;
    public int level;
}

[System.Serializable]
public class PlayerState
{
    // CONSUMABLES
    public int potionCount = 0;

    // Which weapon types the player has unlocked/picked up.
    public List<int> ownedGuns = new List<int>();
    public List<UpgradeLevelEntry> upgradeLevels = new List<UpgradeLevelEntry>();
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

    [Header("Starting Owned Guns")]
    [Tooltip("Weapon types that are considered owned at the start of a new game (used to unlock their upgrades).")]
    [SerializeField] private GunType[] startingOwnedGuns = new GunType[0];


    [Serializable]
    private struct StoreSaveData
    {
        public int potionCount;
        public List<UpgradeLevelEntry> upgradeLevels;
        public List<int> ownedGuns;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (playerState == null)
                playerState = new PlayerState();

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

    public void UnregisterButton(StoreButtonUI buttonUI)
    {
        if (buttonUI == null) return;
        registeredButtons.Remove(buttonUI);
    }

    private void RefreshAllButtonDisplays()
    {
        foreach (var button in registeredButtons)
            button.UpdateDisplay();
    }

    public StoreItem FindItemById(int id)
    {
        for (int i = 0; i < upgradeItems.Count; i++)
            if (upgradeItems[i].id == id)
                return upgradeItems[i];

        for (int i = 0; i < consumableItems.Count; i++)
            if (consumableItems[i].id == id)
                return consumableItems[i];

        return null;
    }

    public int GetUpgradeLevel(int upgradeId)
    {
        if (playerState == null || playerState.upgradeLevels == null)
            return 0;

        for (int i = 0; i < playerState.upgradeLevels.Count; i++)
        {
            if (playerState.upgradeLevels[i].id == upgradeId)
                return playerState.upgradeLevels[i].level;
        }

        return 0;
    }

    private void SetUpgradeLevel(int upgradeId, int newLevel)
    {
        if (playerState == null)
            playerState = new PlayerState();

        if (playerState.upgradeLevels == null)
            playerState.upgradeLevels = new List<UpgradeLevelEntry>();

        for (int i = 0; i < playerState.upgradeLevels.Count; i++)
        {
            if (playerState.upgradeLevels[i].id == upgradeId)
            {
                playerState.upgradeLevels[i].level = newLevel;
                return;
            }
        }

        playerState.upgradeLevels.Add(new UpgradeLevelEntry { id = upgradeId, level = newLevel });
    }

    public bool IsGunOwned(GunType type)
    {
        if (type == GunType.None)
            return true;

        if (playerState == null || playerState.ownedGuns == null)
            return false;

        return playerState.ownedGuns.Contains((int)type);
    }

    public void UnlockGun(GunType type)
    {
        if (type == GunType.None)
            return;

        if (playerState == null)
            playerState = new PlayerState();

        if (playerState.ownedGuns == null)
            playerState.ownedGuns = new List<int>();

        int v = (int)type;
        if (!playerState.ownedGuns.Contains(v))
        {
            playerState.ownedGuns.Add(v);
            RefreshAllButtonDisplays();
        }
    }

    private void EnsureStartingOwnedGuns()
    {
        if (startingOwnedGuns == null || startingOwnedGuns.Length == 0)
            return;

        if (playerState == null)
            playerState = new PlayerState();

        if (playerState.ownedGuns == null)
            playerState.ownedGuns = new List<int>();

        // Only apply if we have none yet.
        if (playerState.ownedGuns.Count > 0)
            return;

        for (int i = 0; i < startingOwnedGuns.Length; i++)
        {
            GunType t = startingOwnedGuns[i];
            if (t == GunType.None) continue;

            int v = (int)t;
            if (!playerState.ownedGuns.Contains(v))
                playerState.ownedGuns.Add(v);
        }
    }

    public bool IsUpgradeMaxed(StoreItem item)
    {
        if (item == null) return true;
        if (item.type != ItemType.Upgrade) return false;

        int lvl = GetUpgradeLevel(item.id);
        return lvl >= Mathf.Max(1, item.maxLevel);
    }

    public int GetEffectiveCost(StoreItem item)
    {
        if (item == null) return int.MaxValue;

        if (item.type == ItemType.Consumable)
            return Mathf.Max(0, item.cost);

        int lvl = GetUpgradeLevel(item.id);
        int cost = item.baseCost + (item.costPerLevel * lvl);
        return Mathf.Max(0, cost);
    }

    public float GetEffectiveAmount(StoreItem item)
    {
        if (item == null) return 0f;

        if (item.type != ItemType.Upgrade)
            return 0f;

        int lvl = GetUpgradeLevel(item.id);
        float amt = item.baseAmount + (item.amountPerLevel * lvl);
        return amt;
    }

    public bool CanBuyItem(StoreItem item, out string reason)
    {
        if (item == null)
        {
            reason = "Invalid Item";
            return false;
        }

        if (RiftShardManager.Instance == null)
        {
            reason = "Currency System Missing";
            return false;
        }

        // Don't allow weapon-specific upgrades unless the player has unlocked that weapon.
        if (item.type == ItemType.Upgrade && item.gunType != GunType.None && !IsGunOwned(item.gunType))
        {
            reason = "Weapon Not Owned";
            return false;
        }

        if (item.type == ItemType.Upgrade && IsUpgradeMaxed(item))
        {
            reason = "Maxed";
            return false;
        }

        int cost = GetEffectiveCost(item);
        if (RiftShardManager.Instance.Amount < cost)
        {
            reason = "Not Enough Rift Shards";
            return false;
        }

        reason = "Available";
        return true;
    }

    public void PurchaseItemButton(int itemId)
    {
        BuyItem(itemId);
    }

    public bool BuyItem(int itemId)
    {
        StoreItem item = FindItemById(itemId);
        if (item == null) return false;

        if (!CanBuyItem(item, out _)) return false;

        int cost = GetEffectiveCost(item);

        if (!RiftShardManager.Instance.TrySpend(cost))
            return false;

        if (item.type == ItemType.Upgrade)
        {
            float amount = GetEffectiveAmount(item);

            ApplyUpgrade(item, amount);

            int currentLevel = GetUpgradeLevel(item.id);
            SetUpgradeLevel(item.id, currentLevel + 1);
        }
        else
        {
            playerState.potionCount++;
        }

        RefreshAllButtonDisplays();
        return true;
    }

    private void ApplyUpgrade(StoreItem item, float amount)
    {
        if (item == null) return;

        if (item.upgradeStat == UpgradeStat.MaxHP)
        {
            if (gamemanager.instance != null && gamemanager.instance.playerScript != null)
            {
                gamemanager.instance.playerScript.ApplyHealthUpgrade(amount);
            }
            else
            {
              //  Debug.LogError("Store.ApplyUpgrade(MaxHP): gamemanager/playerScript missing.");
            }
            return;
        }

        if (GunUpgradeManager.Instance == null)
        {
           // Debug.LogError("Store.ApplyUpgrade: GunUpgradeManager.Instance is null.");
            return;
        }

        gunStats gun = GunUpgradeManager.Instance.GetGunStats(item.gunType);

        if (gun == null)
        {
          //  Debug.LogError($"Store.ApplyUpgrade: No gunStats found for {item.gunType}");
            return;
        }

        // Apply upgrade directly to gun stats
        switch (item.upgradeStat)
        {
            case UpgradeStat.Damage:
                gun.shootDamage += Mathf.RoundToInt(amount);
                break;

            case UpgradeStat.Rate:
                gun.shootRate = Mathf.Max(0.05f, gun.shootRate - amount);
                break;

            case UpgradeStat.Distance:
                gun.shootDist += Mathf.RoundToInt(amount);
                break;

            case UpgradeStat.Ammo:
                gun.ammoMax += Mathf.RoundToInt(amount);
                break;
        }
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

        if (gamemanager.instance != null)
            gamemanager.instance.stateUnpause();
    }


    // Store Data Init
    private void InitializeStoreData()
    {

        upgradeItems.Clear();
        consumableItems.Clear();

        // ---- SMG Upgrades ----
        upgradeItems.Add(new StoreItem
        {
            id = 101,
            itemName = "SMG Ammo Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.SMG,
            upgradeStat = UpgradeStat.Ammo,

            maxLevel = 4,
            baseCost = 5,
            costPerLevel = 5,
            baseAmount = 5,
            amountPerLevel = 0
        });

        upgradeItems.Add(new StoreItem
        {
            id = 102,
            itemName = "SMG Damage Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.SMG,
            upgradeStat = UpgradeStat.Damage,

            maxLevel = 4,
            baseCost = 10,
            costPerLevel = 5,
            baseAmount = 5,
            amountPerLevel = 0
        });

        upgradeItems.Add(new StoreItem
        {
            id = 103,
            itemName = "SMG Fire Rate Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.SMG,
            upgradeStat = UpgradeStat.Rate,

            maxLevel = 4,
            baseCost = 15,
            costPerLevel = 5,
            baseAmount = 0.1f,
            amountPerLevel = 0f
        });

        upgradeItems.Add(new StoreItem
        {
            id = 104,
            itemName = "SMG Distance Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.SMG,
            upgradeStat = UpgradeStat.Distance,

            maxLevel = 4,
            baseCost = 20,
            costPerLevel = 5,
            baseAmount = 5,
            amountPerLevel = 0f
        });

        // ---- Rifle Upgrades ----
        upgradeItems.Add(new StoreItem
        {
            id = 111,
            itemName = "Rifle Ammo Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.Rifle,
            upgradeStat = UpgradeStat.Ammo,

            maxLevel = 4,
            baseCost = 5,
            costPerLevel = 5,
            baseAmount = 5,
            amountPerLevel = 0f
        });

        upgradeItems.Add(new StoreItem
        {
            id = 112,
            itemName = "Rifle Damage Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.Rifle,
            upgradeStat = UpgradeStat.Damage,

            maxLevel = 4,
            baseCost = 10,
            costPerLevel = 5,
            baseAmount = 5,
            amountPerLevel = 0f
        });

        upgradeItems.Add(new StoreItem
        {
            id = 113,
            itemName = "Rifle Fire Rate Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.Rifle,
            upgradeStat = UpgradeStat.Rate,

            maxLevel = 4,
            baseCost = 15,
            costPerLevel = 5,
            baseAmount = 0.1f,
            amountPerLevel = 0f
        });

        upgradeItems.Add(new StoreItem
        {
            id = 114,
            itemName = "Rifle Distance Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.Rifle,
            upgradeStat = UpgradeStat.Distance,

            maxLevel = 4,
            baseCost = 20,
            costPerLevel = 5,
            baseAmount = 5,
            amountPerLevel = 0f
        });

        // ---- AR Upgrades ----
        upgradeItems.Add(new StoreItem
        {
            id = 121,
            itemName = "AR Ammo Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.AR,
            upgradeStat = UpgradeStat.Ammo,

            maxLevel = 4,
            baseCost = 5,
            costPerLevel = 5,
            baseAmount = 5,
            amountPerLevel = 0f
        });

        upgradeItems.Add(new StoreItem
        {
            id = 122,
            itemName = "AR Damage Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.AR,
            upgradeStat = UpgradeStat.Damage,

            maxLevel = 4,
            baseCost = 10,
            costPerLevel = 5,
            baseAmount = 5,
            amountPerLevel = 0f
        });

        upgradeItems.Add(new StoreItem
        {
            id = 123,
            itemName = "AR Fire Rate Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.AR,
            upgradeStat = UpgradeStat.Rate,

            maxLevel = 4,
            baseCost = 15,
            costPerLevel = 5,
            baseAmount = 0.1f,
            amountPerLevel = 0f
        });

        upgradeItems.Add(new StoreItem
        {
            id = 124,
            itemName = "AR Distance Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.AR,
            upgradeStat = UpgradeStat.Distance,

            maxLevel = 4,
            baseCost = 20,
            costPerLevel = 5,
            baseAmount = 5,
            amountPerLevel = 0f
        });

        // ---- Max HP Upgrade, single button, multi-level ----
        upgradeItems.Add(new StoreItem
        {
            id = 201,
            itemName = "Max HP Upgrade",
            type = ItemType.Upgrade,
            gunType = GunType.None,
            upgradeStat = UpgradeStat.MaxHP,

            maxLevel = 4,
            baseCost = 10,
            costPerLevel = 10,
            baseAmount = 25,
            amountPerLevel = 25
        });

        // ---- Consumables ----
        consumableItems.Add(new StoreItem
        {
            id = 301,
            itemName = "Health Potion",
            type = ItemType.Consumable,
            cost = 10
        });

        consumableItems.Add(new StoreItem
        {
            id = 302,
            itemName = "Health Potion+",
            type = ItemType.Consumable,
            cost = 20
        });

        consumableItems.Add(new StoreItem
        {
            id = 303,
            itemName = "Health Potion++",
            type = ItemType.Consumable,
            cost = 30
        });

        consumableItems.Add(new StoreItem
        {
            id = 304,
            itemName = "Health Potion MAX",
            type = ItemType.Consumable,
            cost = 40
        });
    }

    public void ResetStoreProgress()
    {
        if (playerState == null)
            playerState = new PlayerState();

        playerState.potionCount = 0;

        if (playerState.upgradeLevels != null)
            playerState.upgradeLevels.Clear();
        else
            playerState.upgradeLevels = new List<UpgradeLevelEntry>();

        RefreshAllButtonDisplays();
    }

    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state);

    public object CaptureState()
    {
        return new StoreSaveData
        {
            potionCount = playerState != null ? playerState.potionCount : 0,
            upgradeLevels = (playerState != null && playerState.upgradeLevels != null)
                ? new List<UpgradeLevelEntry>(playerState.upgradeLevels)
                : new List<UpgradeLevelEntry>()
        };
    }

    public void RestoreState(object state)
    {
        if (state is not StoreSaveData s)
        {
          //  Debug.LogError($"Store.RestoreState: expected StoreSaveData, got {state?.GetType()} on {name}");
            return;
        }

        if (playerState == null)
            playerState = new PlayerState();

        playerState.potionCount = s.potionCount;
        playerState.upgradeLevels = (s.upgradeLevels != null) ? new List<UpgradeLevelEntry>(s.upgradeLevels) : new List<UpgradeLevelEntry>();

        RefreshAllButtonDisplays();
    }
}