using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreButtonUI : MonoBehaviour
{
    public int itemID;
    private Button button;

    [Header("Gray Out Visuals")]
    [SerializeField] private Graphic buttonGraphic;
    [SerializeField] private Graphic[] extraGraphicsToFade;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI costText;

    [SerializeField, Range(0.05f, 1f)] private float disabledAlpha = 0.35f;
    [SerializeField, Range(0.05f, 1f)] private float enabledAlpha = 1.0f;

    // Like "sold out" or "purchased"
    [Header("Label Settings")]
    [SerializeField] private bool showLevelOnUpgrades = true;
    [SerializeField] private string levelFormat = "LEVEL {0}/{1}";
    [SerializeField] private string maxedLabel = "MAXED";
    [SerializeField] private string cannotAffordLabel = "NEED SHARDS";

    private string originalLabel;


    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"StoreButtonUI on {gameObject.name} is missing a Button component! Button interactivity will fail.");
            return;
        }

        if (buttonGraphic == null)
            buttonGraphic = GetComponent<Graphic>();

        if (labelText != null)
            originalLabel = labelText.text;
    }

    private void OnEnable()
    {
        // Register when the UI actually becomes active.
        if (Store.Instance != null)
            Store.Instance.RegisterButton(this);

        // Also refresh the display when it shows up.
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {

        if (button == null)
            return;

        // If Store missing, hard disable + gray out
        if (Store.Instance == null)
        {
            SetInteractable(false);
            SetLabelSafe("STORE MISSING");
            SetCostSafe("");
            return;
        }

        StoreItem item = Store.Instance.FindItemById(itemID);

        if (item == null)
        {
            Debug.LogWarning($"StoreButtonUI on {gameObject.name}: No StoreItem found for ID {itemID}");
            SetInteractable(false);
            SetLabelSafe("INVALID");
            SetCostSafe("");
            return;
        }

        int effectiveCost = Store.Instance.GetEffectiveCost(item);
        SetCostSafe(effectiveCost.ToString());

        bool isMaxed = (item.type == ItemType.Upgrade) && Store.Instance.IsUpgradeMaxed(item);


        bool canBuy = Store.Instance.CanBuyItem(item, out string reason);


        if (isMaxed)
        {
            SetInteractable(false);
            if (showLevelOnUpgrades)
                SetLabelSafe(maxedLabel);
            else
                SetLabelSafe(originalLabel);

            return;
        }

        //Only enable if player can buy, not maxed
        SetInteractable(canBuy);

        if (item.type == ItemType.Upgrade && showLevelOnUpgrades)
        {
            int lvl = Store.Instance.GetUpgradeLevel(item.id);
            int max = Mathf.Max(1, item.maxLevel);

            // Show level status
            SetLabelSafe(string.Format(levelFormat, lvl, max));

            if (!canBuy && reason == "Not Enough Rift Shards")
                SetLabelSafe(cannotAffordLabel);
        }
        else
        {
            SetLabelSafe(originalLabel);

            if (!canBuy && reason == "Not Enough Rift Shards")
                SetLabelSafe(cannotAffordLabel);
        }
    }

    public void OnButtonClick()
    {

        if (Store.Instance == null)
        {
            Debug.LogWarning($"StoreButtonUI on {gameObject.name}: Click ignored (Store.Instance is null).");
            return;
        }

        if (button != null && !button.interactable)
            return;

        Store.Instance.PurchaseItemButton(itemID);

        // Update immediately
        UpdateDisplay();
    }

    private void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;

        float targetAlpha = value ? enabledAlpha : disabledAlpha;

        if (buttonGraphic != null)
        {
            Color c = buttonGraphic.color;
            c.a = targetAlpha;
            buttonGraphic.color = c;
        }

        if (extraGraphicsToFade != null)
        {
            for (int i = 0; i < extraGraphicsToFade.Length; i++)
            {
                var g = extraGraphicsToFade[i];
                if (g == null) continue;

                Color c = g.color;
                c.a = targetAlpha;
                g.color = c;
            }
        }

        if (labelText != null)
        {
            Color c = labelText.color;
            c.a = targetAlpha;
            labelText.color = c;
        }
        if (costText != null)
        {
            Color c = costText.color;
            c.a = targetAlpha;
            costText.color = c;
        }
    }

    private void SetLabelSafe(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }

    private void SetCostSafe(string text)
    {
        if (costText != null)
            costText.text = text;
    }
}
