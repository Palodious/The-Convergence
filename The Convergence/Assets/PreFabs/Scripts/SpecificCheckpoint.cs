using UnityEngine;
using System.Collections;

public class SpecificCheckpoint : MonoBehaviour
{
    [SerializeField] Renderer model;

    Color colorOrig;

    void Start()
    {
        colorOrig = model.material.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Check for the Player tag.
        if (other.CompareTag("Player"))
        {
            // 2. CHECK THE SPECIAL CONDITION: Enemy Count must be 1.
            //    (Requires EnemyManager.Instance.activeEnemyCount to be set up)
            if (EnemyManager.Instance != null && EnemyManager.Instance.activeEnemyCount == 1)
            {
                // 3. CHECK ORIGINAL CONDITION: Only update if the checkpoint is new.
                if (gamemanager.instance.spawnPoint.transform.position != transform.position)
                {
                    // ALL CONDITIONS MET: Activate the special checkpoint.
                    gamemanager.instance.spawnPoint.transform.position = transform.position;
                    StartCoroutine(feedback());
                }
            }
            // If the enemy count is NOT 1, the checkpoint is NOT activated, 
            // even if the player enters it.
        }
    }

    // Reuse your existing feedback coroutine
    IEnumerator feedback()
    {
        model.material.color = Color.red;
        gamemanager.instance.checkpointPopup.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        gamemanager.instance.checkpointPopup.SetActive(false);
        model.material.color = colorOrig;
    }
}

