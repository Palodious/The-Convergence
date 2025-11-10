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

    // Shield features
    [SerializeField] bool hasShield; // Toggle shield on/off
    [SerializeField] int maxShield; // Maximum shield value
    [SerializeField] float shieldRegenRate; // Amount regenerated per second
    [SerializeField] float sRegenDelay; // Time before regen starts after taking damage
    [SerializeField] GameObject shieldVisual; // GameObject for shield visuals
    [SerializeField] Renderer shieldRenderer; // Renderer for flash effect

    int currentShield;
    bool shieldBroken;
    bool canRegenShield;
    Coroutine regenCoroutine;

    // Patrol features
    [SerializeField] bool enablePatrol; // Toggle patrol on/off
    [SerializeField] Transform[] patrolPoints;  // List of patrol points
    [SerializeField] float patrolWaitTime; // Time to wait at each patrol point

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
    Color shieldOrigColor;
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

        // Initialize shield if enabled
        if (hasShield)
        {
            currentShield = maxShield;
            shieldBroken = false;
            canRegenShield = true;

            if (shieldVisual != null)
                shieldVisual.SetActive(true);

            if (shieldRenderer != null)
                shieldOrigColor = shieldRenderer.material.color;
        }

        // Start patrol if enabled and waypoints exist
        if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.stoppingDistance = 0;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
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
                        StartCoroutine(RandomRotation());
                }
            }
        }
        else if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            Patrol(); // Run patrol when not chasing
        }

        // Handle shield regeneration
        if (hasShield && !shieldBroken && canRegenShield && currentShield < maxShield)
        {
            currentShield += Mathf.CeilToInt(shieldRegenRate * Time.deltaTime);
            if (currentShield > maxShield)
                currentShield = maxShield;
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
                agent.stoppingDistance = stoppingDistOrig;
                agent.SetDestination(gamemanager.instance.player.transform.position);

                if (shootTimer >= shootRate)
                    shoot();

                if (agent.remainingDistance <= stoppingDistOrig)
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
        }
    }

    public void takeDamage(int amount)
    {
        // Handle shield before HP
        if (hasShield && !shieldBroken && currentShield > 0)
        {
            currentShield -= amount;

            if (regenCoroutine != null)
                StopCoroutine(regenCoroutine);

            canRegenShield = false;
            regenCoroutine = StartCoroutine(shieldRegenDelay());

            if (currentShield <= 0)
            {
                currentShield = 0;
                shieldBroken = true;

                // Shield break visual or sound effect could go here
                if (shieldVisual != null)
                    shieldVisual.SetActive(false);
            }
            else
            {
                StartCoroutine(flashShield());
            }

            return; // Exit early so HP is not reduced
        }

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

    IEnumerator shieldRegenDelay()
    {
        yield return new WaitForSeconds(sRegenDelay);
        canRegenShield = true;

        // If shield was broken, reactivate visuals once regen starts
        if (hasShield && shieldBroken)
        {
            shieldBroken = false;
            if (shieldVisual != null)
                shieldVisual.SetActive(true);
        }
    }

    IEnumerator flashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;
    }

    IEnumerator flashShield()
    {
        if (shieldRenderer != null)
        {
            shieldRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            shieldRenderer.material.color = shieldOrigColor;
        }
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
