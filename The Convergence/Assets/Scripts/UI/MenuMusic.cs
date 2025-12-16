using UnityEngine;

public class MenuMusic : MonoBehaviour
{
    [SerializeField] GameObject menuRoot;        // the panel or canvas you toggle
    [SerializeField] AudioSource musicSource;    // dedicated source for menu music
    [SerializeField] AudioClip menuClip;

    bool wasOpen;

    void Start()
    {
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        wasOpen = menuRoot != null && menuRoot.activeSelf;
        ApplyState(wasOpen);
    }

    void Update()
    {
        if (menuRoot == null) return;

        bool isOpen = menuRoot.activeSelf;
        if (isOpen == wasOpen) return;

        wasOpen = isOpen;
        ApplyState(isOpen);
    }

    void ApplyState(bool open)
    {
        if (musicSource == null) return;

        if (open)
        {
            if (menuClip != null && musicSource.clip != menuClip)
                musicSource.clip = menuClip;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }
        else
        {
            if (musicSource.isPlaying)
                musicSource.Stop();
        }
    }
}