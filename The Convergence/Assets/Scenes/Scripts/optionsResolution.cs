using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OptionsResolution : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    // I keep a unique list of Width × Height pairs.
    private List<Vector2Int> availableResolutions;
    private bool hasBuiltList = false;

    // PlayerPrefs keys so my choices persist between sessions.
    private const string PREF_KEY_WIDTH = "video_width";
    private const string PREF_KEY_HEIGHT = "video_height";
    private const string PREF_KEY_FULLSCREEN = "video_fullscreen";

    void OnEnable()
    {
        // I only build the resolution list once.
        if (!hasBuiltList)
        {
            BuildResolutionList();
            BuildDropdownOptions();
            hasBuiltList = true;
        }

        // I restore saved (or current) resolution and fullscreen state.
        LoadSavedOrCurrentResolution();
        LoadSavedOrCurrentFullscreen();
    }

    void OnDisable()
    {
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.RemoveAllListeners();
    }

    // I gather unique resolution sizes and sort from largest to smallest.
    void BuildResolutionList()
    {
        availableResolutions = new List<Vector2Int>();
        HashSet<(int width, int height)> seen = new HashSet<(int width, int height)>();

        foreach (Resolution mode in Screen.resolutions)
        {
            var key = (mode.width, mode.height);
            if (seen.Add(key))
            {
                availableResolutions.Add(new Vector2Int(mode.width, mode.height));
            }
        }

        if (availableResolutions.Count == 0)
        {
            availableResolutions.Add(new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height));
        }

        availableResolutions.Sort((a, b) =>
        {
            int pixelsA = a.x * a.y;
            int pixelsB = b.x * b.y;
            return pixelsB.CompareTo(pixelsA); // largest first
        });
    }

    // I convert sizes to labels like "2560 × 1440" for the dropdown.
    void BuildDropdownOptions()
    {
        resolutionDropdown.ClearOptions();

        List<string> labels = new List<string>();
        foreach (Vector2Int size in availableResolutions)
        {
            labels.Add(size.x + " × " + size.y);
        }

        resolutionDropdown.AddOptions(labels);
    }

    // I pick the saved resolution if present; else I fall back to current; else first in list.
    void LoadSavedOrCurrentResolution()
    {
        int savedWidth = PlayerPrefs.GetInt(PREF_KEY_WIDTH, Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt(PREF_KEY_HEIGHT, Screen.currentResolution.height);

        int index = availableResolutions.FindIndex(r => r.x == savedWidth && r.y == savedHeight);

        if (index < 0)
        {
            Vector2Int current = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
            index = availableResolutions.FindIndex(r => r == current);
            if (index < 0) index = 0;
        }

        resolutionDropdown.SetValueWithoutNotify(index);
    }

    // I set the toggle from saved state; if nothing saved, I mirror the current mode.
    void LoadSavedOrCurrentFullscreen()
    {
#if UNITY_2019_1_OR_NEWER
        bool isCurrentlyFullscreen = Screen.fullScreenMode != FullScreenMode.Windowed;
#else
        bool isCurrentlyFullscreen = Screen.fullScreen;
#endif
        bool savedFullscreen = PlayerPrefs.GetInt(PREF_KEY_FULLSCREEN, isCurrentlyFullscreen ? 1 : 0) == 1;

        fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
    }

    // I apply both the selected resolution and the fullscreen/window mode.
    public void ApplyVideoSettings()
    {
        if (availableResolutions == null || availableResolutions.Count == 0)
        {
            return;
        }

        int selectedIndex = Mathf.Clamp(resolutionDropdown.value, 0, availableResolutions.Count - 1);
        Vector2Int selectedResolution = availableResolutions[selectedIndex];
        bool useFullscreen = fullscreenToggle.isOn;

#if UNITY_2019_1_OR_NEWER
        // ON = Borderless Fullscreen (plays nice with Alt-Tab); OFF = Windowed.
        FullScreenMode targetMode = useFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(selectedResolution.x, selectedResolution.y, targetMode);
#else
        // Legacy overload: I only have the bool for fullscreen.
        Screen.SetResolution(selectedResolution.x, selectedResolution.y, useFullscreen);
#endif

        // I save for next launch.
        PlayerPrefs.SetInt(PREF_KEY_WIDTH, selectedResolution.x);
        PlayerPrefs.SetInt(PREF_KEY_HEIGHT, selectedResolution.y);
        PlayerPrefs.SetInt(PREF_KEY_FULLSCREEN, useFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}