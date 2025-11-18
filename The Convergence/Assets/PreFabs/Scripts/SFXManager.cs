using UnityEngine;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [System.Serializable]
    public class Sound
    {
        public string soundName;
        public AudioClip clip;
    }

    [Header("General Game Sounds")]
    [SerializeField] public Sound[] sounds;

    [Header("Audio Sources")]
    [SerializeField] public AudioSource sfxSource;
    [SerializeField] public AudioSource loopSource;

    Dictionary<string, AudioClip> soundDict = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: persist across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupSounds();
    }

    void SetupSounds()
    {
        foreach (Sound s in sounds)
        {
            if (!string.IsNullOrEmpty(s.soundName) && s.clip != null)
            {
                if (!soundDict.ContainsKey(s.soundName))
                {
                    soundDict.Add(s.soundName, s.clip);
                }
                else
                {
                    Debug.LogWarning($"[SFXManager] Duplicate sound name: {s.soundName}");
                }
            }
        }

        Debug.Log($"[SFXManager] Loaded {soundDict.Count} sounds");
    }

    public void PlaySound(string soundName)
    {
        if (soundDict.ContainsKey(soundName))
        {
            sfxSource.PlayOneShot(soundDict[soundName]);
        }
        else
        {
            Debug.LogWarning($"[SFXManager] Sound not found: {soundName}");
        }
    }

    public void PlayLoopSound(string soundName)
    {
        if (soundDict.ContainsKey(soundName))
        {
            loopSource.clip = soundDict[soundName];
            loopSource.loop = true;
            loopSource.Play();
        }
        else
        {
            Debug.LogWarning($"[SFXManager] Loop sound not found: {soundName}");
        }
    }

    public void StopLoopSound()
    {
        if (loopSource != null && loopSource.isPlaying)
        {
            loopSource.Stop();
        }
    }
}
