using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreButtonUI : MonoBehaviour
{

    public int itemID;

    private Button button;



    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"StoreButtonUI on {gameObject.name} is missing a Button component! Button interactivity will fail.");
        }

        Store.Instance.RegisterButton(this);

    }

    public void UpdateDisplay()
    {
        StoreItem item = Store.Instance.FindItemById(itemID);

        if (item == null || button == null) return;

        if (item.type == ItemType.Upgrade)
        {
            bool alreadyPurchased = Store.Instance.playerState.purchasedIds.Contains(item.id);

            button.interactable = !alreadyPurchased;
        }
        else if (item.type == ItemType.Consumable)
        {
            button.interactable = true;
        }
    }

    public void OnButtonClick()
    {
        if (button != null && !button.interactable)
        {
            Debug.LogWarning($"Click on {gameObject.name} blocked: Button is visually disabled.");
            return;
        }
        Store.Instance.PurchaseItemButton(itemID);

    }
}
