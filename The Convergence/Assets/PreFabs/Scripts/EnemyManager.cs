using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    public int activeEnemyCount { get; private set; } = 0;

    private void Awake()
    {
        // Set up the static instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterEnemy()
    {
        activeEnemyCount++;
    }

    public void UnregisterEnemy()
    {
        activeEnemyCount--;
    }
}
