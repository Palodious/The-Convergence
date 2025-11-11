using UnityEngine;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    public class Sound
    {
        [SerializeField] public string soundName;
        [SerializeField] public AudioClip clip;
    }
    public class ElementSound
    {
        [SerializeField] public string elementType;
        [SerializeField] public AudioClip clip;
    }
    //general game sounds
    [SerializeField] public Sound[] sounds;

    //element sounds
    [SerializeField] public ElementSound[] elementSounds;

    //audio sources
    [SerializeField] public AudioSource sfxSource;
    [SerializeField] public AudioSource loopSource;

    Dictionary<string, AudioClip> soundDict = new Dictionary<string, AudioClip>();
    Dictionary<string, AudioClip> elementDict = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
            soundDict.Add(s.soundName, s.clip);
        }

        foreach (ElementSound es in elementSounds)
        {
            elementDict.Add(es.elementType, es.clip);
        }
    }

    public void PlaySound(string soundName)
    {
        if (soundDict.ContainsKey(soundName))
        {
            sfxSource.PlayOneShot(soundDict[soundName]);
        }
        else
        {
            Debug.LogWarning("Sound not found: " + soundName);
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
    }

    public void StopLoopSound()
    {
        loopSource.Stop();
    }

    public void PlayElementSound(string elementType)
    {
        if (elementDict.ContainsKey(elementType))
        {
            sfxSource.PlayOneShot(elementDict[elementType]);
        }
        else
        {
            Debug.LogWarning("Element sound not found: " + elementType);
        }
    }
}
