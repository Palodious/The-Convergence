using UnityEngine;

public class spawner : MonoBehaviour
{
    [SerializeField] GameObject objectToSpawn;
    [SerializeField] int spawnAmount;
    [SerializeField] float spawnRate;
    [SerializeField] Transform[] spawnPos;

    [Header("Optional Patrol Points")]
    [SerializeField] Transform[] patrolPoints; // Enemies can use patrol points

    int spawnCount;
    float spawnTimer;

    bool startSpawning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        if (other.CompareTag("Player"))
        {
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
            ai.SetPatrolPoints(patrolPoints);
        }

        spawnCount++;
        spawnTimer = 0;
    }
}