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
    private const float DEFAULT_VOL = 0.7f;

    [SerializeField] private float moveSoundCooldown = 0.08f;
    private float lastMoveSoundTime = -999f;

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
        float saved = PlayerPrefs.GetFloat(PREF_VOL, DEFAULT_VOL);

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

        if (Time.unscaledTime - lastMoveSoundTime >= moveSoundCooldown)
        {
            lastMoveSoundTime = Time.unscaledTime;

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_MoveSlider");
        }
    }

        // Converts 0–1 linear slider to decibels and pushes it to the mixer.
        void ApplyMasterVolume(float linear)
        {
            if (masterMixer == null)
                return;

            linear = Mathf.Clamp(linear, 0.0001f, 1f);
            float dB = Mathf.Log10(linear) * 20f;
            masterMixer.SetFloat(MIXER_PARAM, dB);
        }

    // Apply button to force-apply.
    public void ApplySettingsNow()
    {
        if (masterSlider != null)
        {
            float value = masterSlider.value;

            // Lock in this value
            PlayerPrefs.SetFloat(PREF_VOL, value);
            PlayerPrefs.Save();

            // Make sure mixer is using the final value
            ApplyMasterVolume(value);
        }

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySound("UI_Apply");
        }
    }
}
