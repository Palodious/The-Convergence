using UnityEngine;

public class MusicContextBinder : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private MusicProfile profile;

    [Header("Behavior")]
    [Tooltip("If false, enabling this object will NOT change music (useful for minor popups).")]
    [SerializeField] private bool applyOnEnable = true;

    [Tooltip("Higher priority overrides lower (e.g., Pause Menu > Level).")]
    [SerializeField] private int priority = 0;

    private void OnEnable()
    {
        if (!applyOnEnable) return;
        if (profile == null) return;
        if (MusicManager.Instance == null) return;

        MusicManager.Instance.PushProfile(profile, priority);
    }

    private void OnDisable()
    {
        if (!applyOnEnable) return;
        if (profile == null) return;
        if (MusicManager.Instance == null) return;

        MusicManager.Instance.PopProfile(profile);
    }
    private void Start()
    {
        if (!applyOnEnable) return;
        if (profile == null) return;
        if (MusicManager.Instance == null) return;

        MusicManager.Instance.PushProfile(profile, priority);
    }
    private void OnDestroy()
    {
        if (!applyOnEnable) return;
        if (profile == null) return;
        if (MusicManager.Instance == null) return;

        MusicManager.Instance.PopProfile(profile);
    }

}
