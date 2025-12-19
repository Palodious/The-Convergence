using UnityEngine;
using UnityEngine.Audio;

public static class GameSettings
{
    // PlayerPrefs keys
    public const string PREF_VOL = "audio_master_vol";
    public const string PREF_SENS = "mouse_sensitivity";
    public const string PREF_W = "video_width";
    public const string PREF_H = "video_height";
    public const string PREF_FS = "video_fullscreen";

    // Defaults
    public const float DEFAULT_VOL = 0.7f;
    public const float DEFAULT_SENS = 1.0f;

    // Mixer param
    public const string MIXER_PARAM = "MasterVolume";

    public static float GetMasterVolume01()
        => Mathf.Clamp(PlayerPrefs.GetFloat(PREF_VOL, DEFAULT_VOL), 0.0001f, 1f);

    public static void SetMasterVolume01(float v01)
    {
        v01 = Mathf.Clamp(v01, 0.0001f, 1f);
        PlayerPrefs.SetFloat(PREF_VOL, v01);
        PlayerPrefs.Save();
    }

    public static float GetMouseSensitivity()
        => Mathf.Clamp(PlayerPrefs.GetFloat(PREF_SENS, DEFAULT_SENS), 0.01f, 100f);

    public static void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat(PREF_SENS, value);
        PlayerPrefs.Save();
    }

    public static bool GetFullscreen(bool fallback)
        => PlayerPrefs.GetInt(PREF_FS, fallback ? 1 : 0) == 1;

    public static void SetFullscreen(bool fullscreen)
    {
        PlayerPrefs.SetInt(PREF_FS, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetResolution(int w, int h)
    {
        PlayerPrefs.SetInt(PREF_W, w);
        PlayerPrefs.SetInt(PREF_H, h);
        PlayerPrefs.Save();
    }

    public static bool TryGetResolution(out int w, out int h)
    {
        w = PlayerPrefs.GetInt(PREF_W, -1);
        h = PlayerPrefs.GetInt(PREF_H, -1);
        return w > 0 && h > 0;
    }

    // Apply helpers
    public static void ApplyAudio(AudioMixer mixer)
    {
        if (mixer == null) return;

        float linear = GetMasterVolume01();
        float dB = Mathf.Log10(linear) * 20f;
        mixer.SetFloat(MIXER_PARAM, dB);
    }

    public static void ApplyVideo()
    {
        // WebGL: you can't truly set resolution (it’s the browser canvas, I did not accout for this at all).
#if UNITY_WEBGL
        return;
#else
        bool isCurrentlyFullscreen =
#if UNITY_2019_1_OR_NEWER
            Screen.fullScreenMode != FullScreenMode.Windowed;
#else
            Screen.fullScreen;
#endif

        bool useFullscreen = GetFullscreen(isCurrentlyFullscreen);

        if (TryGetResolution(out int w, out int h))
        {
#if UNITY_2019_1_OR_NEWER
            var mode = useFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(w, h, mode);
#else
            Screen.SetResolution(w, h, useFullscreen);
#endif
        }
        else
        {
#if UNITY_2019_1_OR_NEWER
            Screen.fullScreenMode = useFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#else
            Screen.fullScreen = useFullscreen;
#endif
        }
#endif
    }
}
