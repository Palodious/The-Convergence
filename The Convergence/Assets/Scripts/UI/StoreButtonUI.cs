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

    [SerializeField, Range(0.05f, 1f)] private float disabledAlpha = 0.35f;
    [SerializeField, Range(0.05f, 1f)] private float enabledAlpha = 1.0f;

    // Like "sold out" or "purchased"
    [Header("Optional Label Swap")]
    [SerializeField] private bool showPurchasedText = true;
    [SerializeField] private string purchasedLabel = "PURCHASED";
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

        if (Store.Instance != null)
        {
            Store.Instance.RegisterButton(this);
        }
        else
        {
            Debug.LogWarning($"StoreButtonUI on {gameObject.name}: Store.Instance is null at Awake(). " +
                                $"Make sure Store exists in the scene before UI loads.");
        }
    }

    public void UpdateDisplay()
    {

        if (button == null)
            return;

        // If Store missing, hard-disable + gray out
        if (Store.Instance == null)
        {
            SetInteractable(false);
            return;
        }

        StoreItem item = Store.Instance.FindItemById(itemID);

        if (item == null)
        {
            Debug.LogWarning($"StoreButtonUI on {gameObject.name}: No StoreItem found for ID {itemID}");
            SetInteractable(false);
            return;
        }

        bool canBuy = Store.Instance.CanBuyItem(item, out string reason);
        if (item.type == ItemType.Upgrade)
        {
            bool purchased = Store.Instance.playerState.purchasedIds.Contains(item.id);

            SetInteractable(!purchased && canBuy);
            if (labelText != null && showPurchasedText)
            {
                labelText.text = purchased ? purchasedLabel : (string.IsNullOrEmpty(originalLabel) ? labelText.text : originalLabel);
            }
        }
        else
        {
            // Consumables: always enabled if you can afford
            SetInteractable(canBuy);
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
        {
            Debug.LogWarning($"Click on {gameObject.name} blocked: Button is visually disabled.");
            return;
        }
        Store.Instance.PurchaseItemButton(itemID);
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
    }
}
