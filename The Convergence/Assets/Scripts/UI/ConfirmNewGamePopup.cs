using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ConfirmNewGamePopup : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Buttons")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    [Header("Safety")]
    [SerializeField] private float confirmInputDelay = 0.35f;

    private Coroutine enableConfirmRoutine;

    private void Awake()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Hide);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void Show()
    {
        if (popupRoot == null) return;

        popupRoot.SetActive(true);

        if (cancelButton != null)
            cancelButton.Select();

        if (enableConfirmRoutine != null)
            StopCoroutine(enableConfirmRoutine);

        enableConfirmRoutine = StartCoroutine(EnableConfirmAfterDelay());
    }

    public void Hide()
    {
        if (popupRoot == null) return;

        popupRoot.SetActive(false);
    }

    private IEnumerator EnableConfirmAfterDelay()
    {
        if (confirmButton != null)
            confirmButton.interactable = false;

        float t = 0f;
        while (t < confirmInputDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void Confirm()
    {

        Hide();

        Debug.Log("[ConfirmNewGamePopup] Confirm pressed (hook up wipe + new game next).");
    }
}
