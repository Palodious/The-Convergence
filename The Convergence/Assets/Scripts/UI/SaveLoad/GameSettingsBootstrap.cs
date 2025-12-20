using UnityEngine;
using UnityEngine.Audio;

public class GameSettingsBootstrap : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer masterMixer;

    public static GameSettingsBootstrap Instance { get; private set; }

    private const string PREF_MUSIC = "audio_music_vol";
    private const string PREF_SFX = "audio_sfx_vol";
    private const float DEFAULT_VOL = 0.7f;

    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SfxVolume";

    private const string PREF_MOUSE_SENS = "mouse_sensitivity";
    private const float DEFAULT_MOUSE_SENS = 1.0f;

    private static bool bootstrapped;

    private void Awake()
    {
        if (bootstrapped)
        {
            Destroy(gameObject);
            return;
        }

        bootstrapped = true;
        Instance = this;

        DontDestroyOnLoad(gameObject);

        ApplyAudioFromPrefs();
        ApplyMouseSensitivityFromPrefs();
    }

    public void ApplyAudioFromPrefs()
    {
        if (masterMixer == null)
            return;

        float music = Mathf.Clamp(PlayerPrefs.GetFloat(PREF_MUSIC, DEFAULT_VOL), 0.0001f, 1f);
        float sfx = Mathf.Clamp(PlayerPrefs.GetFloat(PREF_SFX, DEFAULT_VOL), 0.0001f, 1f);

        masterMixer.SetFloat(MUSIC_PARAM, Mathf.Log10(music) * 20f);
        masterMixer.SetFloat(SFX_PARAM, Mathf.Log10(sfx) * 20f);
    }

    private void ApplyMouseSensitivityFromPrefs()
    {
        float sens = PlayerPrefs.GetFloat(PREF_MOUSE_SENS, DEFAULT_MOUSE_SENS);
        OptionsMouseSensitivity.LoadSavedSensitivity();
    }
}