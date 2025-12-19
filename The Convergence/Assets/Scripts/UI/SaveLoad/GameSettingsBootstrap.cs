using UnityEngine;
using UnityEngine.Audio;

public class GameSettingsBootstrap : MonoBehaviour
{
    [Header("Optional (only needed if you want audio applied even when Options menu never opened)")]
    [SerializeField] private AudioMixer masterMixer;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // Apply stored settings immediately on boot.
        GameSettings.ApplyVideo();
        GameSettings.ApplyAudio(masterMixer);
    }
}
