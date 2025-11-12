using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour, IDamage
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Renderer model;
    [SerializeField] Transform headPos;

    [SerializeField] int HP;
    [SerializeField] int FOV;
    [SerializeField] int faceTargetSpeed;

    [SerializeField] GameObject projectile;
    [SerializeField] float shootRate;
    [SerializeField] Transform shootPOS;

    // Adjustable player stopping distance (applies only when chasing the player)
    [SerializeField] float playerStoppingDistance;

    // Lost sight features
    [SerializeField] float lostSightDuration; // Time before resuming patrol after losing sight
    float lostSightTimer; // Internal timer for losing sight
    bool canCurrentlySeePlayer; // Tracks if enemy currently sees player

    // Random rotation features
    [SerializeField] float rotationSpeed; // Speed of smooth rotation
    [SerializeField] float minRotationTime; // Minimum time to rotate
    [SerializeField] float maxRotationTime;  // Maximum time to rotate
    bool isRotating; // Tracks if currently rotating

    Color colorOrig;
    bool playerInTrigger;

    float shootTimer;
    float angleToPlayer;
    float stoppingDistOrig;
    Vector3 playerDir;

    void Start()
    {
        colorOrig = model.material.color;
        gamemanager.instance.updateGameGoal(1);
        stoppingDistOrig = agent.stoppingDistance;
    }

    void Update()
    {
        shootTimer += Time.deltaTime;

        // Try to detect and engage player
        if (playerInTrigger && canSeePlayer())
        {
            canCurrentlySeePlayer = true; // Reset lost sight tracking
            lostSightTimer = 0;
        }
        else if (playerInTrigger && !canSeePlayer())
        {
            // Player is in range but not visible; start counting down
            if (canCurrentlySeePlayer)
            {
                lostSightTimer += Time.deltaTime;
                if (lostSightTimer >= lostSightDuration)
                {
                    canCurrentlySeePlayer = false;
                    playerInTrigger = false; // Stop chasing
                    lostSightTimer = 0;
                }
            }
        }
    }

    bool canSeePlayer()
    {
        playerDir = gamemanager.instance.player.transform.position - headPos.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        Debug.DrawRay(headPos.position, playerDir, Color.red);

        RaycastHit hit;
        if (Physics.Raycast(headPos.position, playerDir.normalized, out hit, Mathf.Infinity, ~LayerMask.GetMask("Enemy")))
        {
            if (angleToPlayer <= FOV && hit.collider.CompareTag("Player"))
            {
                // Apply the adjustable stopping distance only for the player
                agent.stoppingDistance = playerStoppingDistance;
                agent.SetDestination(gamemanager.instance.player.transform.position);

                if (shootTimer >= shootRate)
                    shoot();

                if (agent.remainingDistance <= agent.stoppingDistance)
                    faceTarget();

                return true;
            }
        }
        return false;
    }

    void faceTarget()
    {
        Quaternion rot = Quaternion.LookRotation(new Vector3(playerDir.x, 0, playerDir.z));
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, faceTargetSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            canCurrentlySeePlayer = false;
            lostSightTimer = 0;
        }
    }

    public void takeDamage(int amount)
    {
        HP -= amount;

        if (HP <= 0)
        {
            gamemanager.instance.updateGameGoal(-1);
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(flashRed());
        }
    }

    IEnumerator flashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;
    }

    void shoot()
    {
        shootTimer = 0;
        if (projectile != null && shootPOS != null)
        {
            Instantiate(projectile, shootPOS.position, transform.rotation);
        }
    }
}
