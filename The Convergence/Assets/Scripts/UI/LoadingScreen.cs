using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider progressBar;

    [Tooltip("Minimum time to show the loading screen, in seconds.")]
    [SerializeField] private float minimumDisplayTime = 0.5f;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Awake()
    {
        // Make sure game isn't stuck paused from previous scene
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
    {
        StartCoroutine(LoadTargetScene());
    }

    private IEnumerator LoadTargetScene()
    {
        // Fade from black -> visible loading UI
        if (fadeCanvasGroup != null)
            yield return StartCoroutine(Fade(1f, 0f));

        if (string.IsNullOrEmpty(SceneLoader.targetSceneName))
        {
          //  Debug.LogWarning("LoadingScreen: No targetSceneName set. Falling back to main gameplay scene.");

            // TODO: put your default gameplay scene name here
            SceneManager.LoadScene("Game Play Scene L1");
            yield break;
        }

        float timer = 0f;

        // Begin async load
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneLoader.targetSceneName);
        op.allowSceneActivation = false;

        // Load until Unity considers it "almost done" (0.9)
        while (op.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            yield return null;
        }

        // Ensure we show the loading screen for at least minimumDisplayTime
        while (timer < minimumDisplayTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Snap bar to full just before we go
        if (progressBar != null)
            progressBar.value = 1f;

        // Fade out loading UI (visible -> black)
        if (fadeCanvasGroup != null)
            yield return StartCoroutine(Fade(0f, 1f));

        // Now allow scene activation
        op.allowSceneActivation = true;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvasGroup == null || fadeDuration <= 0f)
            yield break;

        float t = 0f;
        fadeCanvasGroup.alpha = from;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / fadeDuration);
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, normalized);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }
}
