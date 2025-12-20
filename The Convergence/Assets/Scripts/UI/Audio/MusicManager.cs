using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Sources (2)")]
    [SerializeField] private AudioSource sceneMusicSource;
    [SerializeField] private AudioSource battleMusicSource;

    [Header("Mixer Routing")]
    [Tooltip("Assign your AudioMixerGroup for MUSIC (controlled by the MusicVolume parameter).")]
    [SerializeField] private AudioMixerGroup musicOutputGroup;

    // Profile stack with priority support
    private struct StackEntry
    {
        public MusicProfile profile;
        public int priority;
        public int order; // tie-breaker: newer wins
    }

    private readonly List<StackEntry> stack = new List<StackEntry>();
    private int orderCounter = 0;
    private MusicProfile activeProfile;

    // Runtime state
    private float combatTimer;
    private float battleVolumeCurrent;
    private float sceneVolumeCurrent;

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
        ApplyMixerRouting();
    }

    private void Update()
    {
        if (activeProfile == null)
            return;

        // Combat linger timer (unscaled so pause menus don't freeze fades)
        if (combatTimer > 0f)
            combatTimer -= Time.unscaledDeltaTime;

        bool battleAllowed = activeProfile.enableBattleMusic && activeProfile.battleMusic != null;
        bool battleShouldBeActive = battleAllowed && combatTimer > 0f;

        // Targets
        float targetBattle = battleShouldBeActive ? activeProfile.battleMusicMaxVolume : 0f;
        float targetScene = battleShouldBeActive ? 0f : activeProfile.sceneMusicVolume;

        // Fade speeds
        float battleFadeTime = battleShouldBeActive ? activeProfile.battleFadeInTime : activeProfile.battleFadeOutTime;
        float sceneFadeTime = battleShouldBeActive ? activeProfile.sceneFadeOutTime : activeProfile.sceneFadeInTime;

        battleVolumeCurrent = MoveTowards(battleVolumeCurrent, targetBattle, battleFadeTime);
        sceneVolumeCurrent = MoveTowards(sceneVolumeCurrent, targetScene, sceneFadeTime);

        // Apply volumes
        if (battleMusicSource != null) battleMusicSource.volume = battleVolumeCurrent;
        if (sceneMusicSource != null) sceneMusicSource.volume = sceneVolumeCurrent;

        // Keep sources playing if clips exist (volumes decide what you hear)
        if (sceneMusicSource != null && sceneMusicSource.clip != null && !sceneMusicSource.isPlaying)
            sceneMusicSource.Play();

        if (battleMusicSource != null && battleMusicSource.clip != null && !battleMusicSource.isPlaying)
            battleMusicSource.Play();
    }

    /// <summary>
    /// Call this when an enemy spots/engages the player.
    /// Extends combat activity for the active profile's hold time.
    /// </summary>
    public void ReportCombat(float refreshTime = -1f)
    {
        if (activeProfile == null) return;
        if (!activeProfile.enableBattleMusic) return;
        if (activeProfile.battleMusic == null) return;

        float hold = refreshTime > 0f ? refreshTime : activeProfile.combatHoldTime;
        combatTimer = Mathf.Max(combatTimer, hold);
    }

    public void PushProfile(MusicProfile profile, int priority)
    {
        if (profile == null) return;

        stack.Add(new StackEntry
        {
            profile = profile,
            priority = priority,
            order = ++orderCounter
        });

        RecomputeActiveProfile();
    }

    public void PopProfile(MusicProfile profile)
    {
        if (profile == null) return;

        // Remove the most recent matching entry (so nested enables/disables behave)
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            if (stack[i].profile == profile)
            {
                stack.RemoveAt(i);
                break;
            }
        }

        RecomputeActiveProfile();
    }

    private void RecomputeActiveProfile()
    {
        MusicProfile best = null;
        int bestPriority = int.MinValue;
        int bestOrder = int.MinValue;

        for (int i = 0; i < stack.Count; i++)
        {
            var e = stack[i];
            if (e.profile == null) continue;

            if (e.priority > bestPriority || (e.priority == bestPriority && e.order > bestOrder))
            {
                best = e.profile;
                bestPriority = e.priority;
                bestOrder = e.order;
            }
        }

        if (best == activeProfile)
            return;

        ApplyProfile(best);
    }

    private void ApplyProfile(MusicProfile profile)
    {
        activeProfile = profile;

        // Reset combat state when switching profiles (prevents “carryover combat” into menus)
        combatTimer = 0f;
        battleVolumeCurrent = 0f;
        sceneVolumeCurrent = 0f;

        if (sceneMusicSource != null)
            sceneMusicSource.clip = activeProfile != null ? activeProfile.sceneMusic : null;

        if (battleMusicSource != null)
            battleMusicSource.clip = (activeProfile != null && activeProfile.enableBattleMusic) ? activeProfile.battleMusic : null;

        // Start/stop safely
        if (sceneMusicSource != null)
        {
            if (sceneMusicSource.clip != null) sceneMusicSource.Play();
            else sceneMusicSource.Stop();
        }

        if (battleMusicSource != null)
        {
            if (battleMusicSource.clip != null) battleMusicSource.Play();
            else battleMusicSource.Stop();
        }
    }

    private void EnsureSources()
    {
        var sources = GetComponents<AudioSource>();
        while (sources.Length < 2)
        {
            gameObject.AddComponent<AudioSource>();
            sources = GetComponents<AudioSource>();
        }

        sceneMusicSource = sources[0];
        battleMusicSource = sources[1];

        sceneMusicSource.loop = true;
        battleMusicSource.loop = true;

        sceneMusicSource.playOnAwake = false;
        battleMusicSource.playOnAwake = false;
    }

    private void ApplyMixerRouting()
    {
        if (musicOutputGroup == null) return;

        if (sceneMusicSource != null) sceneMusicSource.outputAudioMixerGroup = musicOutputGroup;
        if (battleMusicSource != null) battleMusicSource.outputAudioMixerGroup = musicOutputGroup;
    }

    private float MoveTowards(float current, float target, float timeToTarget)
    {
        if (timeToTarget <= 0f) return target;
        float maxDelta = Time.unscaledDeltaTime / timeToTarget;
        return Mathf.MoveTowards(current, target, maxDelta);
    }
}
