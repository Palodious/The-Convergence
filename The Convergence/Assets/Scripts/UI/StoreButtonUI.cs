using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; 
using TMPro; 

public class StoreButtonUI : MonoBehaviour
{
    [Header("Item Reference")]
    public StoreItem assignedItem; // Drag the StoreItem asset here in the Inspector!

    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI levelText; //show level (ex "LvL [1]/[4]")

    private Button button;

    [Header("Gray Out Visuals")]
    [SerializeField] private Graphic buttonGraphic;
    [SerializeField] private List<Graphic> extraGraphicsToFade; // Any other graphics (e.g., borders, small details)
    [SerializeField, Range(0.05f, 1f)] private float disabledAlpha = 0.35f;
    [SerializeField, Range(0.05f, 1f)] private float enabledAlpha = 1.0f;

    [Header("Label Settings")]
    [SerializeField] private string levelFormat = "LvL [0]/[1]";
    [SerializeField] private string maxedLabel = "MAXED";
    [SerializeField] private string cannotAffordLabel = "NEED RIFT SHARDS";
    [SerializeField] private string lockedWeaponLabel = "LOCKED";
    [SerializeField] private string healthFullLabel = "FULL HEALTH";
    [SerializeField] private string unavailableLabel = "N/A";

    private List<Graphic> allFadableGraphics = new List<Graphic>();


    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            // Debug.LogError($"StoreButtonUI on {gameObject.name} is missing a Button component! Button interactivity will fail.");
        }

        if (buttonGraphic == null)
            buttonGraphic = GetComponent<Graphic>();

        PopulateFadableGraphicsList();
    }

    private void PopulateFadableGraphicsList()
    {
        allFadableGraphics.Clear();

        if (buttonGraphic != null) allFadableGraphics.Add(buttonGraphic);
        if (itemIcon != null) allFadableGraphics.Add(itemIcon);
        if (itemNameText != null) allFadableGraphics.Add(itemNameText);
        if (itemDescriptionText != null) allFadableGraphics.Add(itemDescriptionText);
        if (costText != null) allFadableGraphics.Add(costText);
        if (levelText != null) allFadableGraphics.Add(levelText);

        if (extraGraphicsToFade != null)
        {
            foreach (var g in extraGraphicsToFade)
            {
                if (g != null) allFadableGraphics.Add(g);
            }
        }
    }

    private void OnEnable()
    {
        if (Store.Instance != null)
            Store.Instance.RegisterButton(this);
        UpdateDisplay();
    }

    private void OnDisable()
    {
        if (Store.Instance != null)
            Store.Instance.UnregisterButton(this);
    }

    public void UpdateDisplay()
    {
        if (button == null) return;

        if (assignedItem == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        if (itemIcon != null) itemIcon.sprite = assignedItem.icon;
        if (itemNameText != null) itemNameText.text = assignedItem.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = assignedItem.description;

        if (Store.Instance == null)
        {
            SetInteractable(false);
            SetCostSafe("");
            SetLevelTextSafe(unavailableLabel);
            return;
        }

        int effectiveCost = Store.Instance.GetEffectiveCost(assignedItem);
        SetCostSafe(effectiveCost.ToString());

        bool isUpgrade = assignedItem.type == ItemType.Upgrade;
        bool isMaxed = isUpgrade && Store.Instance.IsUpgradeMaxed(assignedItem);

        string reasonCannotBuy = "";
        bool canBuy = Store.Instance.CanBuyItem(assignedItem, out reasonCannotBuy);

        if (isMaxed)
        {
            SetInteractable(false);
            SetLevelTextSafe(maxedLabel);
        }
        else
        {
            SetInteractable(canBuy);

            if (isUpgrade)
            {
                int currentLevel = Store.Instance.GetUpgradeLevel(assignedItem.id);
                int maxLevel = Mathf.Max(1, assignedItem.maxLevel);
                SetLevelTextSafe(string.Format(levelFormat, currentLevel, maxLevel));
            }
            else
            {
                SetLevelTextSafe("");
            }

            if (!canBuy)
            {
                switch (reasonCannotBuy)
                {
                    case "Not Enough Rift Shards":
                        SetLevelTextSafe(cannotAffordLabel);
                        break;
                    case "Weapon Not Owned":
                        SetLevelTextSafe(lockedWeaponLabel);
                        break;
                    default:
                        SetLevelTextSafe(unavailableLabel);
                        break;
                    case "Health Full":
                        SetLevelTextSafe(healthFullLabel);
                        break;
                }
            }
        }
    }

    public void OnButtonClick()
    {
        if (assignedItem == null)
        {
            // Debug.LogWarning($"StoreButtonUI on {gameObject.name}: Click ignored (no StoreItem assigned).");
            return;
        }

        if (Store.Instance == null)
        {
            // Debug.LogWarning($"StoreButtonUI on {gameObject.name}: Click ignored (Store.Instance is null).");
            return;
        }

        if (button != null && !button.interactable)
            return; 

        Store.Instance.PurchaseItemButton(assignedItem.id);

        UpdateDisplay();
    }

    private void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;

        float targetAlpha = value ? enabledAlpha : disabledAlpha;

        foreach (var g in allFadableGraphics)
        {
            if (g == null) continue;
            Color c = g.canvasRenderer.GetColor();
            c.a = targetAlpha;
            g.canvasRenderer.SetColor(c);
        }
    }

    private void SetCostSafe(string text)
    {
        if (costText != null)
            costText.text = text;
    }

    private void SetLevelTextSafe(string text)
    {
        if (levelText != null)
            levelText.text = text;
    }
}