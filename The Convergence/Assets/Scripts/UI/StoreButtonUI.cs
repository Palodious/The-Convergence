using UnityEngine;

using TMPro;

public class StoreButtonUI : MonoBehaviour
{
    
    public int itemID;

    
    public TextMeshProUGUI quantityText;

    private void Awake()
    {
        Store.Instance.RegisterButton(this);
    }

    public void UpdateDisplay()
    {
        StoreItem item = Store.Instance.FindItemById(itemID);

        if (item == null || quantityText == null) return;

        quantityText.text = item.quantity.ToString();


    }

}
