using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsAudio : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Audio")]
    [SerializeField] private AudioMixer masterMixer;   // assign MasterMixer asset in Inspector

    private const string PREF_MUSIC = "audio_music_vol";
    private const string PREF_SFX = "audio_sfx_vol";

    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SfxVolume";

    private const float DEFAULT_VOL = 0.7f;

    [SerializeField] private float moveSoundCooldown = 0.08f;
    private float lastMoveSoundTime = -999f;

    void OnEnable()
    {

        float music = PlayerPrefs.GetFloat(PREF_MUSIC, DEFAULT_VOL);
        float sfx = PlayerPrefs.GetFloat(PREF_SFX, DEFAULT_VOL);

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            musicSlider.value = music;
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            //  Debug.LogWarning("OptionsAudio: masterSlider is not assigned.");
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            sfxSlider.value = sfx;
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            //  Debug.LogWarning("OptionsAudio: masterMixer is not assigned.");
        }

        ApplyVolumes(music, sfx);
    }

    void OnDisable()
    {
        if(musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
    }

    public void OnMusicChanged(float value)
    {
        PlayerPrefs.SetFloat(PREF_MUSIC, value);
        PlayerPrefs.Save();

        ApplyMusic(value);
        PlayMoveSound();
    }

    public void OnSfxChanged(float value)
    {
        PlayerPrefs.SetFloat(PREF_SFX, value);
        PlayerPrefs.Save();

        ApplySfx(value);
        PlayMoveSound();
    }

    void ApplyVolumes(float music, float sfx)
    {
        ApplyMusic(music);
        ApplySfx(sfx);
    }

    void ApplyMusic(float linear)
    {
        if (masterMixer == null) return;

        float dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        masterMixer.SetFloat(MUSIC_PARAM, dB);
    }

    void ApplySfx(float linear)
    {
        if (masterMixer == null) return;

        float dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        masterMixer.SetFloat(SFX_PARAM, dB);
    }

    void PlayMoveSound()
    {
        if (Time.unscaledTime - lastMoveSoundTime < moveSoundCooldown)
            return;

        lastMoveSoundTime = Time.unscaledTime;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_MoveSlider");
    }
}
