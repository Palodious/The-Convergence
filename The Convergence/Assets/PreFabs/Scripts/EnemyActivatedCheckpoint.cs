using UnityEngine;

public class EnemyActivatedCheckpoint : MonoBehaviour
{
    private bool isActivated = false;

    [SerializeField] private  string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            if (EnemyManager.Instance != null && EnemyManager.Instance.activeEnemyCount == 1)
            {

                {
                    ActivateCheckpoint();
                }
            }

        }
    }
    private void ActivateCheckpoint()
    {
        isActivated = true;
    }
}
