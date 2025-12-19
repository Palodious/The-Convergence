using UnityEngine;
using UnityEngine.UI;

public class StoreExitButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnExitClick);
        }
    }

    public void OnExitClick()
    {
        if (Store.Instance != null)
        {
            Store.Instance.ExitStore();
        }
    }
}
