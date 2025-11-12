using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsAudio : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider masterSlider;

    [Header("Audio")]
    [SerializeField] private AudioMixer masterMixer; // assign MasterMixer asset

    const string PREF_VOL = "audio_master_vol";     // 0..1
    const string MIXER_PARAM = "MasterVolume";      // exposed param name

    void OnEnable()
    {
        // Load saved
        float vol = PlayerPrefs.GetFloat(PREF_VOL, 0.8f);
        masterSlider.SetValueWithoutNotify(vol);
        ApplyMasterVolume(vol);

        // Hook slider
        masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
    }

    void OnDisable()
    {
        masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
    }

    void OnMasterSliderChanged(float v)
    {
        ApplyMasterVolume(v);
        PlayerPrefs.SetFloat(PREF_VOL, v);
        PlayerPrefs.Save();
    }

    // Convert to decibels.
    void ApplyMasterVolume(float linear)
    {
        // clamp to avoid log(0)
        linear = Mathf.Clamp(linear, 0.0001f, 1f);
        float dB = Mathf.Log10(linear) * 20f;
        masterMixer.SetFloat(MIXER_PARAM, dB);
    }

    // Optional: call this from an Apply button if you want manual apply.
    public void ApplySettingsNow()
    {
        OnMasterSliderChanged(masterSlider.value);
    }
}
