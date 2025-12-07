using UnityEngine;
using TMPro;

public class RiftShardHUD : MonoBehaviour
{
    [Header("~=~=UI Reference=~=~")]
    [SerializeField] private TMP_Text riftShardText; // Drag your TMP Text here

    private void Start()
    {
        if (RiftShardManager.Instance != null)
        {
            RiftShardManager.Instance.OnShardAmountChanged += UpdateHUD;
            UpdateHUD(RiftShardManager.Instance.Amount);
        }
    }

    private void OnDestroy()
    {
        if (RiftShardManager.Instance != null)
            RiftShardManager.Instance.OnShardAmountChanged -= UpdateHUD;
    }

    private void UpdateHUD(int amount)
    {
        if (riftShardText != null)
            riftShardText.text = amount.ToString();
    }
}
