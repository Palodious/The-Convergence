using UnityEngine;
using UnityEngine.EventSystems;

public class UIAudioFeedback :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler,
    ISelectHandler,
    ISubmitHandler,
    ICancelHandler
{
    [Header("Sound Names (must exist in SFXManager)")]
    [SerializeField] private string hoverSound = "UI_Hover";
    [SerializeField] private string clickSound = "UI_Click";
    [SerializeField] private string backSound = "UI_Back";

    void Play(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (SFXManager.Instance == null) return;

        SFXManager.Instance.PlaySound(soundName);
    }

    // Mouse hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        Play(hoverSound);
    }

    // Mouse click
    public void OnPointerClick(PointerEventData eventData)
    {
        Play(clickSound);
    }

    // Keyboard / gamepad selection (navigating with arrows/WASD)
    public void OnSelect(BaseEventData eventData)
    {
        Play(hoverSound);
    }

    // Keyboard / gamepad submit (Enter/Space/A)
    public void OnSubmit(BaseEventData eventData)
    {
        Play(clickSound);
    }

    // Cancel / back (Esc/B button)
    public void OnCancel(BaseEventData eventData)
    {
        Play(backSound);
    }
}
