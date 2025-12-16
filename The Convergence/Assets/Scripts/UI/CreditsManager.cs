using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreditsManager : MonoBehaviour
{
    [Header("****UI References****")]
    public GameObject creditsPanel;
    public ScrollRect scrollRect;
    public UnityEngine.UI.Button creditsButton;  // Fully qualified
    public UnityEngine.UI.Button closeButton;

    [Header("****Audio****")]
    public AudioSource creditsMusic;

    private bool isScrolling = false;
    private Coroutine scrollCoroutine;

    void Start()
    {
        // Wire the buttons
        if (creditsButton != null) creditsButton.onClick.AddListener(ShowCredits);
        if (closeButton != null) closeButton.onClick.AddListener(HideCredits);

        // Make sure credits panel starts hidden
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
        ResetScroll();

        if (creditsMusic != null && !creditsMusic.isPlaying)
        {
            creditsMusic.Play();
            Debug.Log("Started Playing CreditsMusic");
        }

        // Start auto-scroll
        if (!isScrolling)
        {
            scrollCoroutine = StartCoroutine(AutoScroll()); // Store the reference!
            Debug.Log("Started AutoScroll Coroutine");
        }
    }

    public void HideCredits()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }
        isScrolling = false;

        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (creditsMusic != null) creditsMusic.Stop();
    }

    private IEnumerator AutoScroll()
    {
        isScrolling = true;

        // Wait a frame for layout to update
        yield return null;

        Vector2 startPos = new Vector2(0, 1f); // Top
        Vector2 endPos = new Vector2(0, 0f);   // Bottom
        float duration = 60f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (scrollRect != null)
            {
                float t = elapsed / duration;
                scrollRect.normalizedPosition = Vector2.Lerp(startPos, endPos, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we end exactly at the bottom
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = endPos;
        }

        isScrolling = false;
        scrollCoroutine = null;
    }

    private void ResetScroll()
    {
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 1);
        }
    }
}