using UnityEngine;

[CreateAssetMenu(fileName = "New Store Item", menuName = "Store/Store Item")]
public class StoreItemSO : ScriptableObject
{
    [Header("***Identity***")]
    public int id;     //Must be unique; used for save/lookup
    public string itemName;
    [TextArea(3, 5)] public string description;  //Makes the text field larger in the Inspector for description
    public Sprite icon;
    public ItemType type; //upgrade or a consumable

    [Header("***Cost***")]
    public int baseCost = 0; //Initial cost for consumables, or first level for upgrades
    public int costPerLevel = 0; //Upgrades only (input 0 for consumables)

    [Header("***Upgrade Settings***")]
    public GunType gunType = GunType.None; //Target weapon - None for health upgrades
    public UpgradeStat upgradeStat; //Which stat upgrade modifies
    public int maxLevel = 1; //How many times can be purchased

    [Header("***Upgrade Amount***")]
    public float baseAmount = 0f; //amount applied to the stat when purchased - can be cumulative per level.
    public float amountPerLevel = 5f; //Extra per level for example +5% damage per level

    [Header("***Consumable Settings***")]
    public int quantity = 1; //how many health potions are granted per purchase
}