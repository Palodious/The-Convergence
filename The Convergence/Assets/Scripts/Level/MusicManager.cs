using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Sources (2)")]
    [SerializeField] private AudioSource baseSource;    // normal level/menu music
    [SerializeField] private AudioSource battleSource;  // battle overlay

    [Header("Clips (set by scene profile)")]
    [SerializeField] private AudioClip baseClip;
    [SerializeField] private AudioClip battleClip;

    [Header("Mix Settings")]
    [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float battleMaxVolume = 1f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInTime = 1.0f;
    [SerializeField] private float fadeOutTime = 1.5f;
    [SerializeField] private float combatHoldTime = 3.0f; // how long after last detection before fading out

    private float combatTimer;
    private float battleVolumeCurrent;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSources();
        ApplyClips();
        SetImmediateVolumes();
    }

    private void Update()
    {
        // Decrease combat timer using unscaled time so pause menus don't break fades
        if (combatTimer > 0f)
            combatTimer -= Time.unscaledDeltaTime;

        bool shouldBeInCombat = combatTimer > 0f;

        float targetBattle = shouldBeInCombat ? battleMaxVolume : 0f;
        float fadeTime = shouldBeInCombat ? fadeInTime : fadeOutTime;

        // Smoothly move toward target
        battleVolumeCurrent = MoveTowards(battleVolumeCurrent, targetBattle, fadeTime);

        // Apply volumes
        battleSource.volume = battleVolumeCurrent;
        baseSource.volume = Mathf.Clamp01(baseVolume * (1f - battleVolumeCurrent));
    }

    // Call this when any enemy sees/attacks player
    public void ReportCombat(float refreshTime = -1f)
    {
        combatTimer = Mathf.Max(combatTimer, refreshTime > 0f ? refreshTime : combatHoldTime);

        // Ensure clips are playing
        if (baseSource.clip != null && !baseSource.isPlaying) baseSource.Play();
        if (battleSource.clip != null && !battleSource.isPlaying) battleSource.Play();
    }

    // Called by each scene to set which clips it wants
    public void SetMusicClips(AudioClip newBase, AudioClip newBattle)
    {
        baseClip = newBase;
        battleClip = newBattle;

        ApplyClips();

        // Keep currently playing position if we want?? Not sure.
        if (baseSource.clip != null) baseSource.Play();
        if (battleSource.clip != null) battleSource.Play();

        // Start battle silent
        combatTimer = 0f;
        battleVolumeCurrent = 0f;
        SetImmediateVolumes();
    }

    private void EnsureSources()
    {
        var sources = GetComponents<AudioSource>();

        if (sources.Length < 2)
        {
            while (sources.Length < 2)
            {
                gameObject.AddComponent<AudioSource>();
                sources = GetComponents<AudioSource>();
            }
        }

        baseSource = sources[0];
        battleSource = sources[1];

        baseSource.loop = true;
        battleSource.loop = true;

        baseSource.playOnAwake = false;
        battleSource.playOnAwake = false;
    }

    private void ApplyClips()
    {
        if (baseSource != null) baseSource.clip = baseClip;
        if (battleSource != null) battleSource.clip = battleClip;
    }

    private void SetImmediateVolumes()
    {
        if (battleSource != null) battleSource.volume = battleVolumeCurrent;
        if (baseSource != null) baseSource.volume = Mathf.Clamp01(baseVolume * (1f - battleVolumeCurrent));
    }

    private float MoveTowards(float current, float target, float timeToTarget)
    {
        if (timeToTarget <= 0f) return target;
        float maxDelta = Time.unscaledDeltaTime / timeToTarget;
        return Mathf.MoveTowards(current, target, maxDelta);
    }
}
