using UnityEngine;

public class MusicSceneProfile : MonoBehaviour
{
    [Header("Scene Music")]
    [SerializeField] private AudioClip baseMusic;
    [SerializeField] private AudioClip battleMusic;

    private void Start()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMusicClips(baseMusic, battleMusic);
    }
}
