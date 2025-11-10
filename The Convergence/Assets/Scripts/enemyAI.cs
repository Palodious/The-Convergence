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
    [SerializeField] bool hasShield = false; // Toggle shield on/off
    [SerializeField] int maxShield; // Maximum shield value
    [SerializeField] float shieldRegenRate; // Amount regenerated per second
    [SerializeField] float shieldRegenDelay; // Time before regen starts after taking damage
    [SerializeField] GameObject shieldVisual; // GameObject for shield visuals
    [SerializeField] Color shieldFlashColor = Color.white; // Color when shield flashes on damage
    [SerializeField] float shieldFlashDuration = 0.1f; // How long the shield flashes

    int currentShield;
    bool shieldBroken;
    bool canRegenShield;
    Coroutine regenCoroutine;
    Color shieldOrigColor; // Original color for shield flash

    // Patrol features
    [SerializeField] bool enablePatrol = false; // Toggle patrol on/off
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
    bool playerInTrigger;

    float shootTimer;
    float angleToPlayer;
    Vector3 playerDir;
    float stoppingDistOrig;

    void Start()
    {
        colorOrig = model.material.color;
        gamemanager.instance.updateGameGoal(1);
        stoppingDistOrig = agent.stoppingDistance;

        // Initialize shield if enabled
        if (hasShield)
        {
            currentShield = maxShield; // Initialize shield value
            shieldBroken = false; // Shield is not broken at start
            canRegenShield = true; // Allow regen initially

            // Enable shield visuals
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(true);
                shieldOrigColor = shieldVisual.GetComponent<Renderer>().material.color;
            }
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
            if (canCurrentlySeePlayer)
            {
                lostSightTimer += Time.deltaTime;
                if (lostSightTimer >= lostSightDuration)
                {
                    canCurrentlySeePlayer = false;
                    playerInTrigger = false; // Stop chasing, resume patrol
                    lostSightTimer = 0;

                    if (enablePatrol && !isRotating && patrolPoints.Length > 0)
                        StartCoroutine(RandomRotation());
                }
            }
        }
        else if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            Patrol(); // Run patrol when not chasing
        }

        // Handle shield regeneration (gradual)
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

    void Patrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolTimer += Time.deltaTime;

            if (!isRotating && patrolTimer < patrolWaitTime)
                StartCoroutine(RandomRotation());

            if (patrolTimer >= patrolWaitTime)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                patrolTimer = 0;
            }
        }
    }

    IEnumerator RandomRotation()
    {
        isRotating = true;

        float randomAngle = Random.Range(0f, 360f);
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, randomAngle, 0);

        float rotationTime = Random.Range(minRotationTime, maxRotationTime);
        float elapsed = 0f;

        while (elapsed < rotationTime)
        {
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / rotationTime);
            elapsed += Time.deltaTime * rotationSpeed;
            yield return null;
        }

        transform.rotation = endRot;
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
            playerInTrigger = true;
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
        if (hasShield && currentShield > 0)
        {
            currentShield -= amount;

            // Stop any existing regen delay
            if (regenCoroutine != null)
                StopCoroutine(regenCoroutine);

            canRegenShield = false;
            regenCoroutine = StartCoroutine(ShieldRegenDelay());

            // Flash shield visual
            if (shieldVisual != null)
                StartCoroutine(FlashShield());

            // Check if shield breaks
            if (currentShield <= 0)
            {
                currentShield = 0;
                shieldBroken = true;

                if (shieldVisual != null)
                    shieldVisual.SetActive(false);
            }

            return; // Exit early so HP is not reduced
        }

        // Apply damage to HP if shield is inactive
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

    IEnumerator ShieldRegenDelay()
    {
        yield return new WaitForSeconds(shieldRegenDelay);
        canRegenShield = true;

        // If shield was broken, reset shield
        if (shieldBroken)
        {
            shieldBroken = false;
            currentShield = maxShield;
            if (shieldVisual != null)
                shieldVisual.SetActive(true);
        }
    }

    IEnumerator FlashShield()
    {
        Renderer shieldRenderer = shieldVisual.GetComponent<Renderer>();
        if (shieldRenderer != null)
        {
            Color originalColor = shieldRenderer.material.color;
            shieldRenderer.material.color = shieldFlashColor;
            yield return new WaitForSeconds(shieldFlashDuration);
            shieldRenderer.material.color = originalColor;
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
            Instantiate(projectile, shootPOS.position, transform.rotation);
    }
}
