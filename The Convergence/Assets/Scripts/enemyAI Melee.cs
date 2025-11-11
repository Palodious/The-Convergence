using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class enemyAIMelee : MonoBehaviour, IDamage
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Renderer model;
    [SerializeField] Transform headPos;

    [SerializeField] int HP;
    [SerializeField] int FOV;
    [SerializeField] int faceTargetSpeed;

    [SerializeField] GameObject meleeObject; // Replaces projectile
    [SerializeField] float meleeRate; // Replaces shootRate
    [SerializeField] Transform meleePOS; // Replaces shootPOS

    // Patrol features
    [SerializeField] bool enablePatrol; // Toggle patrol on/off
    [SerializeField] Transform[] patrolPoints;  // List of patrol points
    [SerializeField] float patrolWaitTime; // Time to wait at each patrol point
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

    int currentPatrolIndex;
    float patrolTimer;

    Color colorOrig;
    bool playerInTrigger;

    float meleeTimer;
    float angleToPlayer;
    float stoppingDistOrig;
    Vector3 playerDir;

    void Start()
    {
        colorOrig = model.material.color;
        gamemanager.instance.updateGameGoal(1);
        stoppingDistOrig = agent.stoppingDistance;

        // Start patrol if enabled and waypoints exist
        if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.stoppingDistance = 0;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void Update()
    {
        meleeTimer += Time.deltaTime;

        // Try to detect and engage player
        if (playerInTrigger && canSeePlayer())
        {
            canCurrentlySeePlayer = true; // Reset lost sight tracking
            lostSightTimer = 0;
        }
        else if (playerInTrigger && !canSeePlayer())
        {
            // Player is in range but not visible; start counting down to resume patrol
            if (canCurrentlySeePlayer)
            {
                lostSightTimer += Time.deltaTime;
                if (lostSightTimer >= lostSightDuration)
                {
                    canCurrentlySeePlayer = false;
                    playerInTrigger = false; // Stop chasing, resume patrol
                    lostSightTimer = 0;

                    // Start random rotation only after fully stopped
                    if (enablePatrol && !isRotating && patrolPoints.Length > 0)
                    {
                        agent.stoppingDistance = 0;
                        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                        StartCoroutine(RandomRotation());
                    }
                }
            }
        }
        else if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            Patrol(); // Run patrol when not chasing
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

                if (meleeTimer >= meleeRate)
                    meleeAttack();

                if (agent.remainingDistance <= agent.stoppingDistance)
                    faceTarget();

                return true;
            }
        }
        return false;
    }

    // Handles moving between patrol points
    void Patrol()
    {
        // Only act when agent is fully stopped at patrol point
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Ensure patrol ignores stopping distance
            agent.stoppingDistance = 0;

            patrolTimer += Time.deltaTime; // Increment timer for waiting at current point

            // Only rotate while waiting at the patrol point, not when moving
            if (!isRotating && patrolTimer < patrolWaitTime)
            {
                StartCoroutine(RandomRotation()); // Start random rotation coroutine
            }

            // Move to next patrol point after waiting
            if (patrolTimer >= patrolWaitTime)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length; // Loop through patrol points
                agent.SetDestination(patrolPoints[currentPatrolIndex].position); // Move agent to next patrol point
                patrolTimer = 0; // Reset wait timer
            }
        }
    }

    // Coroutine to smoothly rotate randomly while stopped
    IEnumerator RandomRotation()
    {
        isRotating = true;

        // Pick a random angle between 0 and 360 degrees
        float randomAngle = Random.Range(0f, 360f);
        Quaternion startRot = transform.rotation; // Current rotation
        Quaternion endRot = Quaternion.Euler(0, randomAngle, 0); // Target random rotation

        float rotationTime = Random.Range(minRotationTime, maxRotationTime);
        float elapsed = 0f;

        while (elapsed < rotationTime)
        {
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / rotationTime); // Smoothly rotate
            elapsed += Time.deltaTime * rotationSpeed; // Increase elapsed based on rotation speed
            yield return null;
        }

        transform.rotation = endRot; // Ensure final rotation matches exactly
        isRotating = false;
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

            // Reset patrol stopping distance when leaving trigger
            if (enablePatrol)
                agent.stoppingDistance = 0;
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

    // Replaces projectile-based shooting system with melee instantiation
    void meleeAttack()
    {
        meleeTimer = 0;
        if (meleeObject != null && meleePOS != null)
        {
            // Instantiate the melee object (similar to projectile)
            Instantiate(meleeObject, meleePOS.position, transform.rotation);
        }
    }
}
