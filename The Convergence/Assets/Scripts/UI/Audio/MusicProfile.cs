using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Music Profile", fileName = "MusicProfile_")]
public class MusicProfile : ScriptableObject
{
    [Header("Scene Music")]
    [Tooltip("Primary music for this scene/menu (plays while NOT in battle).")]
    public AudioClip sceneMusic;

    [Header("Battle Music")]
    [Tooltip("If enabled, MusicManager can fade in battle music when combat is reported.")]
    public bool enableBattleMusic = false;

    [Tooltip("Optional combat music clip. Only used when Enable Battle Music is true.")]
    public AudioClip battleMusic;

    [Header("Volumes")]
    [Range(0f, 1f)]
    [Tooltip("Volume of Scene Music when battle is NOT active.")]
    public float sceneMusicVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Target volume of Battle Music during combat/linger.")]
    public float battleMusicMaxVolume = 1f;

    [Header("Fades")]
    [Tooltip("Seconds to fade Battle Music IN when combat starts.")]
    public float battleFadeInTime = 0.7f;

    [Tooltip("Seconds to fade Battle Music OUT after linger ends.")]
    public float battleFadeOutTime = 1.2f;

    [Tooltip("Seconds to fade Scene Music OUT when battle fades in.")]
    public float sceneFadeOutTime = 0.5f;

    [Tooltip("Seconds to fade Scene Music IN when battle fades out.")]
    public float sceneFadeInTime = 1.0f;

    [Header("Combat Linger")]
    [Tooltip("Seconds to keep battle music active after combat ends.")]
    public float combatHoldTime = 5.0f;
}
