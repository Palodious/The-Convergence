using UnityEngine;

public class spawner : MonoBehaviour
{
    [SerializeField] GameObject objectToSpawn;
    [SerializeField] int spawnAmount;
    [SerializeField] int spawnRate;
    [SerializeField] Transform[] spawnPos;

    int spawnCount;
    float spawnTimer;

    bool startSpawning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gamemanager.instance.updateGameGoal(spawnAmount);
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
        Instantiate(objectToSpawn, spawnPos[Random.Range(0, spawnPos.Length)].transform.position, Quaternion.identity);
        spawnCount++;
        spawnTimer = 0;
    }
}
