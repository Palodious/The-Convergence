using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsAudio : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider masterSlider;

    [Header("Audio")]
    [SerializeField] private AudioMixer masterMixer;   // assign MasterMixer asset in Inspector

    private const string PREF_VOL = "audio_master_vol"; // 0..1
    private const string MIXER_PARAM = "MasterVolume";     // exposed param name in mixer

    void OnEnable()
    {
        if (masterSlider == null)
        {
            Debug.LogWarning("OptionsAudio: masterSlider is not assigned.");
        }

        if (masterMixer == null)
        {
            Debug.LogWarning("OptionsAudio: masterMixer is not assigned.");
        }

        // Load saved volume, default to full.
        float saved = PlayerPrefs.GetFloat(PREF_VOL, 1f);

        // Make sure the slider shows the saved value and listen for changes.
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
            masterSlider.value = saved;
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        // Apply immediately so mixer matches the saved value.
        ApplyMasterVolume(saved);
    }

    void OnDisable()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        }
    }

    // This is what the slider drives.
    public void OnMasterSliderChanged(float value)
    {
        ApplyMasterVolume(value);

        PlayerPrefs.SetFloat(PREF_VOL, value);
        PlayerPrefs.Save();
    }

    // Converts 0–1 linear slider to decibels and pushes it to the mixer.
    void ApplyMasterVolume(float linear)
    {
        if (masterMixer == null)
            return;

        linear = Mathf.Clamp(linear, 0.0001f, 1f);      // avoid log(0)
        float dB = Mathf.Log10(linear) * 20f;
        masterMixer.SetFloat(MIXER_PARAM, dB);
    }

    // Optional: if you ever want an "Apply" button to force-apply.
    public void ApplySettingsNow()
    {
        if (masterSlider != null)
        {
            OnMasterSliderChanged(masterSlider.value);
        }
    }
}
