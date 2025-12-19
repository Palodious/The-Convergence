using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeLevelEntry
{
    public int id; // Unique ID of StoreItem of upgrade
    public int level;
}

[System.Serializable]
public class PlayerState
{
    public int potionCount = 0;

    public List<int> ownedGuns = new List<int>();
    public List<UpgradeLevelEntry> upgradeLevels = new List<UpgradeLevelEntry>();
}

public class Store : MonoBehaviour, ISaveable
{
    public static Store Instance { get; private set; }

    [Header("Player State (Runtime)")]
    public PlayerState playerState = new PlayerState();

    [Header("Store Data")]
    [Tooltip("Drag all your StoreItem assets here that should be available in the store.")]
    public List<StoreItem> allStoreItems = new List<StoreItem>();

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
       
        if (playerState == null)
            playerState = new PlayerState();

       
        EnsureStartingOwnedGuns();

    }

    public void RegisterButton(StoreButtonUI button)
    {
        if (button != null && !registeredButtons.Contains(button))
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
        for (int i = 0; i < allStoreItems.Count; i++)
            if (allStoreItems[i].id == id)
                return allStoreItems[i];

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

        // Only apply if player has none yet.
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
            return Mathf.Max(0, item.baseCost);

        int lvl = GetUpgradeLevel(item.id);
        int cost = item.baseCost + (item.costPerLevel * lvl);
        return Mathf.Max(0, cost);
    }

    public float GetEffectiveAmount(StoreItem item)
    {
        if (item == null || item.type != ItemType.Upgrade) return 0f;

        int currentLevel = GetUpgradeLevel(item.id);
        float amount = item.baseAmount + (item.amountPerLevel * currentLevel);
        return amount;
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

        if (!CanBuyItem(item, out string reason))
        { /* Debug.Log($"Store.BuyItem: Cannot buy {item.itemName}. Reason: {reason}");*/ return false; }

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
            if (gamemanager.instance != null && gamemanager.instance.playerScript != null)
            {
                // Use baseAmount as heal amount for consumables (set this in the StoreItem asset)
                int healAmount = Mathf.RoundToInt(item.baseAmount);
                gamemanager.instance.playerScript.HealFromStore(healAmount);
            }
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

        gunStats gun = null;


        if (gamemanager.instance != null && gamemanager.instance.playerScript != null)
        {
            var p = gamemanager.instance.playerScript;

            if (p.activeGunStats != null && p.activeGunStats.gunType == item.gunType)
                gun = p.activeGunStats;
        }

        if (gun == null)
        {
            if (GunUpgradeManager.Instance == null)
                return;

            gun = GunUpgradeManager.Instance.GetGunStats(item.gunType);
        }

        if (gun == null)
            return;


        // Apply upgrade directly to gun stats
        //Debug.Log($"[UPGRADE] item={item.itemName} gun={gun.name} id={gun.GetInstanceID()} stat={item.upgradeStat} amount={amount} BEFORE dmg={gun.shootDamage} rate={gun.shootRate} dist={gun.shootDist} ammo={gun.ammoMax}");

        gun.ApplyUpgrade(item.upgradeStat, amount);

        //Debug.Log($"[UPGRADE] AFTER dmg={gun.shootDamage} rate={gun.shootRate} dist={gun.shootDist} ammo={gun.ammoMax}");

        // If the upgraded gun is currently equipped, rebuild the clone so gameplay updates immediately
        if (gamemanager.instance != null && gamemanager.instance.playerScript != null)
        {
            gamemanager.instance.playerScript.RefreshEquippedGunIfMatchesTemplate(gun);
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
  
     

    public void ResetStoreProgress()
    {
        if (playerState == null)
            playerState = new PlayerState();

        playerState.potionCount = 0;

        playerState.upgradeLevels.Clear();
        playerState.ownedGuns.Clear();
 
        EnsureStartingOwnedGuns();

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
                : new List<UpgradeLevelEntry>(),
            ownedGuns = (playerState != null && playerState.ownedGuns != null) // Also save owned guns
                ? new List<int>(playerState.ownedGuns)
                : new List<int>()
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
        playerState.ownedGuns = (s.ownedGuns != null) ? new List<int>(s.ownedGuns) : new List<int>(); // Restore owned guns

        RefreshAllButtonDisplays();
    }
}