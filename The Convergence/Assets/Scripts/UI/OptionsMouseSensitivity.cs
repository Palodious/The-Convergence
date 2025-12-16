using UnityEngine;
using UnityEngine.UI;

public class OptionsMouseSensitivity : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider sensitivitySlider;

    [Header("Range")]
    [SerializeField] private float minSensitivity = 0.2f;
    [SerializeField] private float maxSensitivity = 3.0f;

    private const string PREF_KEY = "mouse_sensitivity";
    private const float DEFAULT_SENSITIVITY = 1.0f;

    [SerializeField] private float moveSoundCooldown = 0.08f;
    private float lastMoveSoundTime = -999f;

    void OnEnable()
    {
        if (sensitivitySlider == null)
        {
          //  Debug.LogWarning("OptionsMouseSensitivity: sensitivitySlider is not assigned.");
            return;
        }

        // Configure the slider range
        sensitivitySlider.minValue = minSensitivity;
        sensitivitySlider.maxValue = maxSensitivity;

        // Load saved sensitivity
        float saved = PlayerPrefs.GetFloat(PREF_KEY, DEFAULT_SENSITIVITY);
        saved = Mathf.Clamp(saved, minSensitivity, maxSensitivity);

        // Apply to slider without firing the event
        sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        sensitivitySlider.value = saved;
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    void OnDisable()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        }
    }

    public void OnSensitivityChanged(float value)
    {
        // Save it
        PlayerPrefs.SetFloat(PREF_KEY, value);
        PlayerPrefs.Save();

        // UI tick sound
        if (Time.unscaledTime - lastMoveSoundTime >= moveSoundCooldown)
        {
            lastMoveSoundTime = Time.unscaledTime;

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_MoveSlider");
        }
    }

    // Apply to set
    public void ApplySettingsNow()
    {
        if (sensitivitySlider == null)
            return;

        float value = sensitivitySlider.value;
        PlayerPrefs.SetFloat(PREF_KEY, value);
        PlayerPrefs.Save();

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Apply");
    }
}
