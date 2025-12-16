using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreditsManager : MonoBehaviour
{
    [Header("****UI References****")]
    public GameObject creditsPanel;
    public UnityEngine.UI.ScrollRect scrollRect;
    public UnityEngine.UI.Button creditsButton;
    public UnityEngine.UI.Button closeButton;

    private bool isScrolling = false;
    private Coroutine scrollCoroutine;
    void Start()
    {
        // wire the buttons
        if (creditsButton != null) creditsButton.onClick.AddListener(ShowCredits);
        if (closeButton != null) closeButton.onClick.AddListener(HideCredits);
    }

    public void ShowCredits()
    {
        creditsPanel.SetActive(true);
        if (!isScrolling)
        {
            isScrolling = true;
            StartCoroutine(AutoScroll());
        }
    }

    public void HideCredits()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
        }
        ResetScroll();
        creditsPanel.SetActive(false);
        isScrolling = false;
    }

    private IEnumerator AutoScroll()
    {
        isScrolling = true;
        Vector2 startPos = new Vector2(0,1f); //starts at the top
        Vector2 endPos = new Vector2(0,0f); //ends at the bottom
        float duration = 60f; //duration of the scroll in seconds

        for (float t =0; t < 1f; t+= Time.deltaTime / duration)
        {
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = Vector2.Lerp(startPos, endPos, t);
            }
            yield return null;
        }
        ResetScroll();
        isScrolling = false;
    }

    private void ResetScroll()
    {
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
}
