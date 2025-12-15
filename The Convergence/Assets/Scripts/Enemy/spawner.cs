using UnityEngine;
using System.Collections.Generic;

public class spawner : MonoBehaviour, ISaveable
{
    // Global lookup so enemies can find their spawner by SaveEntity Id.
    static readonly Dictionary<string, spawner> registry = new Dictionary<string, spawner>();

    [SerializeField] GameObject objectToSpawn;
    [SerializeField] int spawnAmount;
    [SerializeField] float spawnRate;
    [SerializeField] Transform[] spawnPos;
    [SerializeField] bool hasTriggered;

    [Header("Optional Patrol Points")]
    [SerializeField] Transform[] patrolPoints; // Enemies can use patrol points

    [Header("**** NEW GAME+ NG+ SCALING ****")]
    [SerializeField] private bool enableNGPlusScaling = true;


    private int baseSpawnAmount;
    private float baseSpawnRate;

    private bool ngpBaseCached;
    private bool ngpApplied;

    int spawnCount;
    float spawnTimer;

    bool startSpawning;
    SaveEntity saveEntity;
    string saveId;

    void Awake()
    {
        // I register myself in a static lookup so enemies can find me later by my SaveEntity Id.
        saveEntity = GetComponent<SaveEntity>();
        if (saveEntity != null)
        {
            saveId = saveEntity.Id;
            if (!string.IsNullOrEmpty(saveId))
            {
                registry[saveId] = this;
            }
        }
    }

    void OnDestroy()
    {
        // If this spawner goes away, I clean up my registry entry.
        if (!string.IsNullOrEmpty(saveId) &&
            registry.TryGetValue(saveId, out var current) &&
            current == this)
        {
            registry.Remove(saveId);
        }
    }

    private void CacheNgpBaseStatsIfNeeded()
    {
        if (ngpBaseCached) return;

        baseSpawnAmount = spawnAmount;
        baseSpawnRate = spawnRate;

        ngpBaseCached = true;
    }

    private void ApplyNgpScalingIfNeeded()
    {
        if (!enableNGPlusScaling) return;
        if (ngpApplied) return;

        // Never scale spawners while restoring a save
        if (SaveManager.IsLoadingFromSave)
            return;

        CacheNgpBaseStatsIfNeeded();

        int cycle = 0;
        if (NewGamePlusManager.Instance != null)
            cycle = Mathf.Max(0, NewGamePlusManager.Instance.CurrentCycle);

        // Tunable difficulty curve
        float amountMult = 1f + (0.25f * cycle); // +25% enemies per NG+
        float rateMult = 1f + (0.15f * cycle); // +15% faster spawns per NG+

        spawnAmount = Mathf.Max(0, Mathf.RoundToInt(baseSpawnAmount * amountMult));

        // spawnRate = seconds between spawns - smaller = faster
        spawnRate = Mathf.Max(0.01f, baseSpawnRate / rateMult);

        ngpApplied = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ApplyNgpScalingIfNeeded();

        if (spawnAmount > 0)
        {
            gamemanager.instance.updateGameGoal(spawnAmount);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (startSpawning)
        {
            spawnTimer += Time.deltaTime;

            if (spawnCount < spawnAmount && spawnTimer >= spawnRate)
            {
                spawn();
            }

        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            startSpawning = true;
        }
    }

    void spawn()
    {
        GameObject newObj = Instantiate(
            objectToSpawn,
            spawnPos[Random.Range(0, spawnPos.Length)].transform.position,
            Quaternion.identity
        );

        // Assign patrol points if enemyAI is present
        enemyAI ai = newObj.GetComponent<enemyAI>();
        if (ai != null && patrolPoints != null && patrolPoints.Length > 0)
        {
            if (!string.IsNullOrEmpty(saveId))
            {
                ai.SetPatrolPoints(patrolPoints, saveId);
            }
            else
            {
                // Fallback if someone forgot the SaveEntity.
                ai.SetPatrolPoints(patrolPoints);
            }
        }

        spawnCount++;
        spawnTimer = 0;
    }

    public Transform[] GetPatrolPoints()
    {
        return patrolPoints;
    }

    public static spawner FindBySaveId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        registry.TryGetValue(id, out var result);
        return result;
    }


    [System.Serializable]
    private struct SpawnerState
    {
        public int spawnCount;
        public float spawnTimer;
        public bool hasTriggered;
        public bool startSpawning;
    }

        object ISaveable.CaptureState() => CaptureState();
        void ISaveable.RestoreState(object state) => RestoreState(state);

    public object CaptureState()
    {
        return new SpawnerState
        {
            spawnCount = this.spawnCount,
            spawnTimer = this.spawnTimer,
            hasTriggered = this.hasTriggered,
            startSpawning = this.startSpawning
        };
    }

    public void RestoreState(object state)
    {
        if (state is not SpawnerState s)
        {
            Debug.LogError($"spawner.RestoreState: expected SpawnerState, got {state?.GetType()} on {name}");
            return;
        }

        spawnCount = Mathf.Clamp(s.spawnCount, 0, spawnAmount);
        spawnTimer = Mathf.Max(0f, s.spawnTimer);
        hasTriggered = s.hasTriggered;
        startSpawning = s.startSpawning;
    }
}