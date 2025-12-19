using System;
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

    [Header("UI SFX")]
    [SerializeField] private float moveSoundCooldown = 0.08f;
    private float lastMoveSoundTime = -999f;

    public static float CurrentSensitivity { get; private set; } = DEFAULT_SENSITIVITY;
    public static event Action<float> OnSensitivityUpdated;

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
        sensitivitySlider.onValueChanged.RemoveListener(HandleSliderChanged);
        sensitivitySlider.value = saved;
        sensitivitySlider.onValueChanged.AddListener(HandleSliderChanged);

        ApplySensitivity(saved, save: false, playMoveSound: false);
    }

    void OnDisable()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.RemoveListener(HandleSliderChanged);
    }

    private void HandleSliderChanged(float value)
    {
        ApplySensitivity(value, save: true, playMoveSound: true);
    }

    private void ApplySensitivity(float value, bool save, bool playMoveSound)
    {
        value = Mathf.Clamp(value, minSensitivity, maxSensitivity);

        CurrentSensitivity = value;
        OnSensitivityUpdated?.Invoke(value);

        if (save)
        {
            PlayerPrefs.SetFloat(PREF_KEY, value);
            PlayerPrefs.Save();
        }

        if (playMoveSound && Time.unscaledTime - lastMoveSoundTime >= moveSoundCooldown)
        {
            lastMoveSoundTime = Time.unscaledTime;

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_MoveSlider");
        }
    }

    public static float LoadSavedSensitivity()
    {
        float saved = PlayerPrefs.GetFloat(PREF_KEY, DEFAULT_SENSITIVITY);
        saved = Mathf.Clamp(saved, 0.01f, 100f);
        CurrentSensitivity = saved;
        return saved;
    }
}