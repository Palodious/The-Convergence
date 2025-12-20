using UnityEngine;
using UnityEngine.UI;

public class OptionsAudioUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string PREF_MUSIC = "audio_music_vol";
    private const string PREF_SFX = "audio_sfx_vol";
    private const float DEFAULT_VOL = 0.7f;

    public static event System.Action AudioSettingsChanged;

    private void OnEnable()
    {
        RefreshSlidersFromPrefs();
        AudioSettingsChanged += RefreshSlidersFromPrefs;
    }

    private void OnDisable()
    {
        AudioSettingsChanged -= RefreshSlidersFromPrefs;
    }

    public void OnMusicChanged(float v01)
    {
        v01 = Mathf.Clamp(v01, 0.0001f, 1f);
        PlayerPrefs.SetFloat(PREF_MUSIC, v01);
        PlayerPrefs.Save();

        if (GameSettingsBootstrap.Instance != null)
            GameSettingsBootstrap.Instance.ApplyAudioFromPrefs();

        AudioSettingsChanged?.Invoke();
    }

    public void OnSfxChanged(float v01)
    {
        v01 = Mathf.Clamp(v01, 0.0001f, 1f);
        PlayerPrefs.SetFloat(PREF_SFX, v01);
        PlayerPrefs.Save();

        if (GameSettingsBootstrap.Instance != null)
            GameSettingsBootstrap.Instance.ApplyAudioFromPrefs();

        AudioSettingsChanged?.Invoke();
    }

    private void RefreshSlidersFromPrefs()
    {
        float music = Mathf.Clamp(PlayerPrefs.GetFloat(PREF_MUSIC, DEFAULT_VOL), 0.0001f, 1f);
        float sfx = Mathf.Clamp(PlayerPrefs.GetFloat(PREF_SFX, DEFAULT_VOL), 0.0001f, 1f);

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);

        if (GameSettingsBootstrap.Instance != null)
            GameSettingsBootstrap.Instance.ApplyAudioFromPrefs();
    }
}
