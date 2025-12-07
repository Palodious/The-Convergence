using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem;

public class enemyAI : MonoBehaviour, IDamage, ISaveable
{
    public enum EnemyType
    {
        Melee,
        Shooter,
        Hybrid
    }

    [Header("**** Layers ****")]
    [SerializeField] LayerMask ignoreLayer;
    [Header("**** Enemy Type ****")]
    [SerializeField] EnemyType enemyType;

    [Header("**** Components ****")]
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator anim;
    [SerializeField] Renderer model;
    [SerializeField] Transform headPos;

    [Header("**** Stats ****")]
    bool isAlive = true;
    [Range(1, 300)][SerializeField] int HP;
    [Range(1, 360)][SerializeField] int FOV;
    [Range(1, 360)][SerializeField] int faceTargetSpeed;
    [Range(1, 50)][SerializeField] int roamDist;
    [Range(0, 10)][SerializeField] int roamPauseTime;
    [Range(0.1f, 10f)][SerializeField] float animTransSpeed;
    [Range(5f, 100f)][SerializeField] float sightRange = 20f;

    [Header("**** Shooter Settings ****")]
    [SerializeField] GameObject projectile;
    [Range(0.1f, 10f)][SerializeField] float shootRate;
    [SerializeField] Transform shootPOS;

    [Header("**** Melee Settings ****")]
    [SerializeField] Transform meleePos; // Position from which melee attacks are measured
    [SerializeField] GameObject meleeDamage; // GameObject with damage.cs attached
    [Range(0.1f, 10f)][SerializeField] float meleeRange; // Distance at which enemy can hit player
    [Range(0.1f, 10f)][SerializeField] float attackRate;  // Cooldown between attacks
    [Range(1, 50)][SerializeField] int meleeDamageAmount;

    [Header("**** Jump Attack Settings ****")]
    [SerializeField] bool canJumpAttack = false;
    [Range(0.1f, 10f)][SerializeField] float jumpForce;
    [Range(1f, 50f)][SerializeField] float jumpAttackRange; // Max distance to trigger jump
    [Range(0.1f, 10f)][SerializeField] float minHeightDifference; // How much higher player must be
    [Range(0.5f, 30f)][SerializeField] float jumpAttackCooldown;
    [Range(1, 100)][SerializeField] int jumpAttackDamage;
    [Range(0.5f, 15f)][SerializeField] float jumpAttackRadius; // Damage radius on landing
    [SerializeField] float jumpTimer;

    [Header("**** Jump Attack Arc Settings ****")]
    [Range(1f, 20f)][SerializeField] float jumpHeight = 3f; // Max height of jump arc
    [Range(0.1f, 5f)][SerializeField] float jumpDuration = 1f; // Total time of jump
    [SerializeField] AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Jump arc curve
    [SerializeField] GameObject jumpLandingEffect; // Optional effect when landing
    [Range(0f, 1f)][SerializeField] float jumpDamageDelay = 0.1f; // Delay before applying damage after landing
    [SerializeField] bool showJumpArcDebug = false; // Show debug line for jump arc
    [Range(0f, 1f)][SerializeField] float jumpWindupTime = 0.2f; // Time before jumping starts
    [Range(0f, 3f)][SerializeField] float landingDistanceFromPlayer = 0.5f; // How close to land to player (0 = on player, larger = further away)

    [Header("**** Dash Attack Settings ****")]
    [SerializeField] bool canDashAttack = false;
    [Range(2f, 50f)][SerializeField] float dashSpeed;
    [Range(0.1f, 5f)][SerializeField] float dashDuration;
    [Range(1f, 50f)][SerializeField] float dashAttackRange; // Max distance to trigger dash
    [Range(0.5f, 30f)][SerializeField] float minDashDistance; // Min distance (don't dash if too close)
    [Range(0.5f, 30f)][SerializeField] float dashAttackCooldown;
    [Range(1.1f, 10f)][SerializeField] float playerFleeingThreshold; // How fast player must be moving away

    [Header("**** Special Attack Tracking ****")]
    [Range(0.1f, 5f)][SerializeField] float specialAttackCheckInterval = 0.5f; // How often to check for special attacks
    [Range(10f, 200f)][SerializeField] float maxPlayerTrackingDistance = 50f; // Max distance to track player for special attacks

    [Header("**** Burst Fire Settings ****")]
    [SerializeField] bool useBurstFire = false;
    [Range(1, 20)][SerializeField] int bulletsPerBurst = 3;
    [Range(0.01f, 2f)][SerializeField] float timeBetweenBurstShots = 0.1f;
    [Range(0.1f, 10f)][SerializeField] float timeBetweenBursts = 1f;

    [Header("**** Behavior Toggles ****")]
    public bool useAnimations = true; // Toggle all animation logic on/off
    public bool usePatrol = true; // Toggle patrol behavior
    public bool useRoam = true; // Toggle roaming behavior

    [Header("~=~= Audio =~=~")]
    [SerializeField] AudioSource aud;
    [SerializeField] AudioClip[] audStep;
    [Range(0, 1)][SerializeField] float audStepVol;
    [SerializeField] AudioClip[] audHurt;
    [Range(0, 1)][SerializeField] float audHurtVol;
    [SerializeField] AudioClip[] audShoot;
    [Range(0, 1)][SerializeField] float audShootVol;
    [SerializeField] AudioClip[] audMelee;
    [Range(0, 1)][SerializeField] float audMeleeVol;
    [SerializeField] AudioClip[] audDeath; // Death audio
    [Range(0, 1)][SerializeField] float audDeathVol;
    [SerializeField] AudioClip[] audJumpAttack; // Jump attack audio
    [Range(0, 1)][SerializeField] float audJumpAttackVol;
    [SerializeField] AudioClip audJumpWindup; // Windup before jump
    [Range(0, 1)][SerializeField] float audJumpWindupVol = 0.5f;
    [SerializeField] AudioClip audJumpLanding; // Landing impact sound
    [Range(0, 1)][SerializeField] float audJumpLandingVol = 0.7f;
    [SerializeField] AudioClip[] audDash; // Dash attack audio
    [Range(0, 1)][SerializeField] float audDashVol = 0.5f;

    [SerializeField] private ItemDrop itemDrop;

    [Header("**** Currency Drop ****")]
    [Range(0,1000)][SerializeField] private int currencyDropAmount = 10; // Amount of currency this enemy drops

    public EnemyType EnemyTypeValue => enemyType;

    Color colorOrig;
    bool playerInTrigger;
    float shootTimer;
    float attackTimer;
    float roamTimer;
    float angleToPlayer;
    Vector3 playerDir;
    Vector3 startingPos;
    float stoppingDistOrig;

    [Header("**** Patrol Points ****")]
    [SerializeField] Transform[] patrolPoints; // Optional patrol points
    int patrolIndex = 0;
    [SerializeField] string patrolSourceId;

    bool isPlayingStep;

    // Jump Attack variables
    private float jumpAttackTimer = 1f;
    private bool isJumpAttacking = false;
    private Rigidbody rb;
    private bool wasKinematic;
    private Vector3 jumpTarget;
    private Coroutine jumpCheckCoroutine;
    private Coroutine jumpCoroutine; // For arc jump coroutine
    private Vector3 jumpStartPosition;
    private bool hasLanded = false;

    // Dash Attack variables
    private float dashAttackTimer = 1f;
    private bool isDashing = false;
    private Vector3 dashDirection;
    private float dashTimeRemaining;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;

    // Burst fire variables
    private bool isBursting = false;
    private int currentBurstCount = 0;
    private Coroutine burstCoroutine;

    // Special attack tracking variables
    private float specialAttackCheckTimer = 0f;
    private bool playerTracked = false;
    private Vector3 lastKnownPlayerPosition;

    void Start()
    {
        colorOrig = model.material.color;
        stoppingDistOrig = agent.stoppingDistance;
        startingPos = transform.position;

        // Rigidbody setup for jump attack
        if (canJumpAttack)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            wasKinematic = rb.isKinematic;
        }

        // Initialize player position tracking for dash
        if (gamemanager.instance.player != null)
        {
            lastPlayerPosition = gamemanager.instance.player.transform.position;
            lastKnownPlayerPosition = lastPlayerPosition;
        }

        // Initialize patrol by setting the first patrol point as the destination
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0 && agent != null && agent.isActiveAndEnabled)
            agent.SetDestination(patrolPoints[patrolIndex].position);

        if (itemDrop == null)
            itemDrop = GetComponent<ItemDrop>();

        // Start continuous player tracking for special attacks
        if (canJumpAttack || canDashAttack)
        {
            StartCoroutine(ContinuousPlayerTracking());
        }
    }

    void Update()
    {
        // Don't do anything if dead
        if (!isAlive) return;

        // Update timers
        shootTimer += Time.deltaTime;
        attackTimer += Time.deltaTime;
        jumpAttackTimer += Time.deltaTime;
        dashAttackTimer += Time.deltaTime;
        specialAttackCheckTimer += Time.deltaTime;

        // Track player velocity for dash attack
        TrackPlayerVelocity();

        // Handle dash attack logic - during dash, only update dash movement
        if (isDashing)
        {
            HandleDashMovement();
            return; // Skip other updates during dash
        }

        // Handle jump attack - during jump, only maintain facing direction
        if (isJumpAttacking)
        {
            // Update facing direction during jump to face player
            if (gamemanager.instance.player != null)
            {
                Vector3 lookDir = (gamemanager.instance.player.transform.position - transform.position);
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDir);
            }
            return; // Skip other updates during jump
        }

        // Periodically check for special attacks even when not in direct combat
        if (specialAttackCheckTimer >= specialAttackCheckInterval && playerTracked)
        {
            CheckSpecialAttacks(lastKnownPlayerPosition,
                Vector3.Distance(transform.position, lastKnownPlayerPosition));
            specialAttackCheckTimer = 0f;
        }

        // Play footsteps if moving and not performing special attacks
        if (!isJumpAttacking && !isDashing &&
            agent != null && agent.isActiveAndEnabled && agent.velocity.magnitude > 0.1f && !isPlayingStep)
        {
            StartCoroutine(playStep());
        }

        // Update movement animation speed if enabled and not performing special attacks
        if (useAnimations && anim != null && !isJumpAttacking && !isDashing &&
            agent != null && agent.isActiveAndEnabled)
        {
            float agentSpeedCur = agent.velocity.magnitude;
            float agentSpeedAnim = anim.GetFloat("Speed");
            anim.SetFloat("Speed", Mathf.Lerp(agentSpeedAnim, agentSpeedCur, Time.deltaTime * animTransSpeed));
        }

        // Track roam timer only when not moving and not performing special attacks
        if (!isJumpAttacking && !isDashing &&
            agent != null && agent.isActiveAndEnabled && agent.remainingDistance < 0.01f)
            roamTimer += Time.deltaTime;

        // Handle mobile behavior if not in special attack
        if (!isJumpAttacking && !isDashing) // Only handle mobile behavior if not in special attack
        {
            HandleMobileBehavior();
        }
    }
    
    // Separate behavior handler for mobile enemies
    void HandleMobileBehavior()
    {
        // Already checked in Update(), but double-check for safety
        if (isJumpAttacking || isDashing) return;
        
        if (playerInTrigger && !canSeePlayer())
        {
            checkRoamOrPatrol();
        }
        else if (!playerInTrigger)
        {
            checkRoamOrPatrol();
        }
    }

    // Continuously track player position even when not in direct sight
    IEnumerator ContinuousPlayerTracking()
    {
        while (isAlive)
        {
            if (gamemanager.instance.player != null)
            {
                Vector3 playerPos = gamemanager.instance.player.transform.position;
                float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
                
                // Track player if within max tracking distance
                if (distanceToPlayer <= maxPlayerTrackingDistance)
                {
                    playerTracked = true;
                    lastKnownPlayerPosition = playerPos;
                    
                    // Check for special attacks even without direct line of sight
                    CheckSpecialAttacks(playerPos, distanceToPlayer);
                }
                else
                {
                    playerTracked = false;
                }
            }
            yield return new WaitForSeconds(0.3f); // Check every 0.3 seconds
        }
    }

    // Check if special attacks should be triggered
    void CheckSpecialAttacks(Vector3 playerPos, float distanceToPlayer)
    {
        // Don't check if already performing a special attack or if dead
        if (isDashing || isJumpAttacking || !isAlive) return;
        
        // Check for jump attack opportunity
        if (canJumpAttack && !isJumpAttacking && jumpAttackTimer >= jumpAttackCooldown)
        {
            float heightDifference = playerPos.y - transform.position.y;
            
            // Check if player is significantly above enemy (on an object/ledge)
            if (heightDifference >= minHeightDifference && distanceToPlayer <= jumpAttackRange)
            {
                // Check if there's a clear path for jump
                if (HasJumpPathToPlayer(playerPos))
                {
                    StartJumpAttack();
                    return;
                }
            }
        }
        
        // Check for dash attack opportunity
        if (canDashAttack && !isDashing && dashAttackTimer >= dashAttackCooldown)
        {
            // Check distance conditions
            if (distanceToPlayer >= minDashDistance && distanceToPlayer <= dashAttackRange)
            {
                // Check if player is moving away or if we need to close distance quickly
                Vector3 directionToEnemy = (transform.position - playerPos).normalized;
                float fleeingSpeed = Vector3.Dot(playerVelocity, directionToEnemy);
                
                // Dash if player is fleeing OR if we need to close distance to a ranged target
                if (fleeingSpeed < -playerFleeingThreshold || 
                    (enemyType == EnemyType.Melee && distanceToPlayer > meleeRange * 2f))
                {
                    StartDashAttack();
                    return;
                }
            }
        }
    }

    // Check if there's a viable jump path to the player
    bool HasJumpPathToPlayer(Vector3 playerPos)
    {
        // Simple check: if player is directly above or has minimal horizontal obstruction
        Vector3 horizontalDir = new Vector3(playerPos.x - transform.position.x, 0, playerPos.z - transform.position.z);
        float horizontalDistance = horizontalDir.magnitude;
        
        // If player is almost directly above, allow jump
        if (horizontalDistance < 2f)
            return true;
        
        // Check for obstacles between enemy and player
        Vector3 checkPos = transform.position + Vector3.up * 1f; // Start check from chest height
        Vector3 targetPos = playerPos + Vector3.up * 0.5f; // Check to player's midsection
        
        RaycastHit hit;
        if (Physics.Raycast(checkPos, (targetPos - checkPos).normalized, out hit, jumpAttackRange, ~ignoreLayer))
        {
            // If we hit the player directly, path is clear
            if (hit.collider.CompareTag("Player"))
                return true;
            
            // If we hit something else, check if we can jump over it
            float obstacleHeight = hit.point.y - transform.position.y;
            if (obstacleHeight < jumpHeight * 0.8f) // Can jump over obstacles up to 80% of jump height
                return true;
        }
        
        return false;
    }

    // Track player velocity for dash attack prediction
    void TrackPlayerVelocity()
    {
        if (gamemanager.instance.player == null) return;

        Vector3 currentPlayerPos = gamemanager.instance.player.transform.position;
        playerVelocity = (currentPlayerPos - lastPlayerPosition) / Time.deltaTime;
        lastPlayerPosition = currentPlayerPos;
    }

    // Determine if jump attack should be triggered
    bool ShouldJumpAttack()
    {
        if (!canJumpAttack || isJumpAttacking || isDashing) return false;
        if (jumpAttackTimer < jumpAttackCooldown) return false;
        if (gamemanager.instance.player == null) return false;

        Vector3 playerPos = gamemanager.instance.player.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
        float heightDifference = playerPos.y - transform.position.y;

        // More aggressive: allow jump even if player is slightly lower but within range
        return (heightDifference >= minHeightDifference || distanceToPlayer <= jumpAttackRange * 0.5f) 
               && distanceToPlayer <= jumpAttackRange;
    }

    // Determine if dash attack should be triggered
    bool ShouldDashAttack()
    {
        // Only melee or hybrid enemies can dash
        if (!canDashAttack || isDashing || isJumpAttacking) return false;
        if (enemyType != EnemyType.Melee && enemyType != EnemyType.Hybrid) return false;
        if (dashAttackTimer < dashAttackCooldown) return false;
        if (gamemanager.instance.player == null) return false;

        Vector3 playerPos = gamemanager.instance.player.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);

        // Check if player is within dash attack range
        if (distanceToPlayer < minDashDistance || distanceToPlayer > dashAttackRange) return false;

        // Check if player is moving away from enemy
        Vector3 directionToEnemy = (transform.position - playerPos).normalized;
        float fleeingSpeed = Vector3.Dot(playerVelocity, directionToEnemy);

        // Negative dot product means player is moving away
        return fleeingSpeed < -playerFleeingThreshold;
    }

    // Start jump attack
    void StartJumpAttack()
    {
        if (isJumpAttacking || gamemanager.instance.player == null) return;

        isJumpAttacking = true;
        jumpAttackTimer = 0f;
        hasLanded = false;
        jumpStartPosition = transform.position;
        jumpTarget = gamemanager.instance.player.transform.position;

        // Instead of disabling, just stop the agent and clear velocity
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            
            // Clear the path without using ResetPath()
            try
            {
                agent.SetDestination(transform.position);
            }
            catch (System.Exception e)
            {
                // If that fails, just log it and continue
                Debug.Log($"Could not clear agent path: {e.Message}");
            }
        }

        // Play windup animation and sound
        if (useAnimations && anim != null)
            anim.SetTrigger("JumpWindup");
        
        if (audJumpWindup != null && aud != null)
            aud.PlayOneShot(audJumpWindup, audJumpWindupVol);

        // Face player during jump windup
        Vector3 lookDir = new Vector3(jumpTarget.x - transform.position.x, 0, jumpTarget.z - transform.position.z);
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // Start the jump coroutine
        if (jumpCoroutine != null) StopCoroutine(jumpCoroutine);
        jumpCoroutine = StartCoroutine(PerformArcJumpAttack());
    }

    // Coroutine to perform arc-style jump attack
    IEnumerator PerformArcJumpAttack()
    {
        // Wait for windup animation
        yield return new WaitForSeconds(jumpWindupTime);

        // Play jump animation
        if (useAnimations && anim != null)
            anim.SetTrigger("JumpAttack");

        // Play jump sound
        if (audJumpAttack.Length > 0 && aud != null)
            aud.PlayOneShot(audJumpAttack[Random.Range(0, audJumpAttack.Length)], audJumpAttackVol);

        Vector3 startPos = transform.position;
        Vector3 playerPos = gamemanager.instance.player.transform.position;

        // Calculate direction to player
        Vector3 toPlayer = (playerPos - startPos).normalized;

        // Calculate landing position: Use the adjustable landingDistanceFromPlayer
        Vector3 endPos = playerPos - (toPlayer * Mathf.Max(0.1f, landingDistanceFromPlayer));

        // Ensure the player will be within damage radius
        float distanceToPlayerAfterLanding = Vector3.Distance(endPos, playerPos);
        if (distanceToPlayerAfterLanding > jumpAttackRadius * 0.7f)
        {
            // Adjust landing position to ensure player is within damage radius
            endPos = playerPos - (toPlayer * (jumpAttackRadius * 0.5f));
        }

        // Use more accurate ground finding
        Vector3 finalLandingPos = FindGroundPosition(endPos);

        // If no ground found, try positions around the player
        if (finalLandingPos == Vector3.zero)
        {
            // Try positions in a circle around the player
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector3 testPos = playerPos + Quaternion.Euler(0, angle, 0) * Vector3.forward * landingDistanceFromPlayer;
                finalLandingPos = FindGroundPosition(testPos);
                if (finalLandingPos != Vector3.zero)
                    break;
            }

            // If still no ground, use start position
            if (finalLandingPos == Vector3.zero)
                finalLandingPos = startPos;
        }

        endPos = finalLandingPos;

        // Calculate horizontal distance
        Vector3 horizontalDirection = new Vector3(endPos.x - startPos.x, 0, endPos.z - startPos.z);
        float horizontalDistance = horizontalDirection.magnitude;

        // If distance is very small, add minimum jump
        if (horizontalDistance < 0.5f)
        {
            horizontalDirection = toPlayer;
            horizontalDistance = 1f;
            endPos = startPos + horizontalDirection * horizontalDistance;
            endPos.y = FindGroundPosition(endPos).y;
        }

        Vector3 horizontalNormalized = horizontalDirection.normalized;

        // Adjust jump duration based on distance
        float actualJumpDuration = jumpDuration * Mathf.Clamp(horizontalDistance / 10f, 0.5f, 2f);

        float elapsedTime = 0f;

        while (elapsedTime < actualJumpDuration && isJumpAttacking)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / actualJumpDuration;

            // Calculate position along horizontal path
            float horizontalProgress = normalizedTime;
            Vector3 horizontalPosition = startPos + horizontalNormalized * (horizontalDistance * horizontalProgress);

            // Calculate vertical position using jump curve
            float verticalOffset = jumpCurve.Evaluate(normalizedTime) * jumpHeight;

            // Combine positions
            Vector3 newPosition = horizontalPosition;
            newPosition.y = startPos.y + verticalOffset;

            // Apply position
            transform.position = newPosition;

            // Apply rotation during jump (look at target)
            Vector3 lookDirection = (playerPos - transform.position);
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDirection);

            yield return null;
        }

        // Ensure we end at the calculated landing position
        if (isJumpAttacking)
        {
            // Final ground snap to prevent sliding
            Vector3 groundPos = FindGroundPosition(endPos);
            if (groundPos != Vector3.zero)
                endPos = groundPos;

            transform.position = endPos;
            LandJumpAttack();
        }
    }

    //Find ground position with detection
    Vector3 FindGroundPosition(Vector3 position)
    {
        // Try downward raycast first
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out hit, 10f, ~ignoreLayer))
        {
            return hit.point;
        }

        // Try sphere cast for better detection
        if (Physics.SphereCast(position + Vector3.up * 3f, 0.5f, Vector3.down, out hit, 10f, ~ignoreLayer))
        {
            return hit.point;
        }

        return Vector3.zero;
    }

    // Start dash attack
    void StartDashAttack()
    {
        if (isDashing || gamemanager.instance.player == null) return;

        isDashing = true;
        dashAttackTimer = 0f;
        dashTimeRemaining = dashDuration;

        // Calculate dash direction with prediction
        Vector3 playerPos = gamemanager.instance.player.transform.position;
        Vector3 playerPredictedPos = playerPos + playerVelocity * 0.3f; // Predict player movement
        dashDirection = (playerPredictedPos - transform.position).normalized;
        dashDirection.y = 0; // Keep dash horizontal

        // Stop the agent during dash
        if (agent != null && agent.isActiveAndEnabled) 
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // Play animation and sound
        if (useAnimations && anim != null)
        {
            anim.SetTrigger("DashAttack");
            anim.SetFloat("Speed", dashSpeed); // Set animation speed to match dash
        }
        
        if (audDash.Length > 0 && aud != null)
            aud.PlayOneShot(audDash[Random.Range(0, audDash.Length)], audDashVol);

        // Face dash direction
        if (dashDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dashDirection);
    }

    // Handle dash movement
    void HandleDashMovement()
    {
        if (!isDashing) return;
        
        dashTimeRemaining -= Time.deltaTime;

        // Move with constant speed
        Vector3 movement = dashDirection * dashSpeed * Time.deltaTime;
        transform.position += movement;

        // Update animation speed during dash to keep enemy "running"
        if (useAnimations && anim != null)
            anim.SetFloat("Speed", dashSpeed);

        // Check for collisions with player during dash
        if (meleePos != null)
        {
            Collider[] hits = Physics.OverlapSphere(meleePos.position, meleeRange, ~ignoreLayer);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    IDamage dmg = hit.GetComponent<IDamage>();
                    if (dmg != null)
                    {
                        dmg.takeDamage(meleeDamageAmount);
                        EndDash();
                        return;
                    }
                }
            }
        }

        // End dash when time runs out or if we hit a wall
        if (dashTimeRemaining <= 0 || CheckWallCollision())
        {
            EndDash();
        }
    }

    // Check for wall collisions during dash
    bool CheckWallCollision()
    {
        // Simple wall check using raycast
        RaycastHit hit;
        if (Physics.Raycast(transform.position, dashDirection, out hit, 1f, ~ignoreLayer))
        {
            if (!hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
                return true;
        }
        return false;
    }

    // End dash attack and return to normal state
    void EndDash()
    {
        if (!isDashing) return;
        
        isDashing = false;

        // Reset animation speed
        if (useAnimations && anim != null)
            anim.SetFloat("Speed", 0);

        // Re-enable agent movement
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            
            // Resume chasing player if still alive
            if (gamemanager.instance.player != null && isAlive)
            {
                try
                {
                    agent.SetDestination(gamemanager.instance.player.transform.position);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not set destination after dash: {e.Message}");
                }
            }
        }
    }

    // Handle landing after jump attack
    void LandJumpAttack()
    {
        if (hasLanded || !isJumpAttacking) return;
        
        hasLanded = true;

        // Play landing animation
        if (useAnimations && anim != null)
            anim.SetTrigger("JumpLand");
        
        // Play landing sound
        if (audJumpLanding != null && aud != null)
            aud.PlayOneShot(audJumpLanding, audJumpLandingVol);

        // Spawn landing effect
        if (jumpLandingEffect != null)
            Instantiate(jumpLandingEffect, transform.position, Quaternion.identity);

        // Apply damage after a small delay
        StartCoroutine(ApplyJumpDamage());
    }

    // Apply damage from jump attack with delay
    IEnumerator ApplyJumpDamage()
    {
        yield return new WaitForSeconds(jumpDamageDelay);

        bool playerDamaged = false;
        Vector3 playerPos = gamemanager.instance.player.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);

        // Direct distance check
        if (distanceToPlayer <= jumpAttackRadius * 1.2f) // Slightly larger radius for safety
        {
            IDamage dmg = gamemanager.instance.player.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(jumpAttackDamage);
                playerDamaged = true;
                Debug.Log($"Jump attack hit player via distance check: {distanceToPlayer} units away");
            }
        }

        // Overlap sphere
        if (!playerDamaged)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, jumpAttackRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    IDamage dmg = hit.GetComponent<IDamage>();
                    if (dmg != null)
                    {
                        dmg.takeDamage(jumpAttackDamage);
                        playerDamaged = true;
                        Debug.Log($"Jump attack hit player via overlap sphere");
                        break;
                    }
                }
            }
        }

        // Raycast to player
        if (!playerDamaged)
        {
            RaycastHit hit;
            Vector3 rayDirection = (playerPos - transform.position).normalized;
            if (Physics.Raycast(transform.position, rayDirection, out hit, jumpAttackRadius * 2f, ~ignoreLayer))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    IDamage dmg = hit.collider.GetComponent<IDamage>();
                    if (dmg != null)
                    {
                        dmg.takeDamage(jumpAttackDamage);
                        Debug.Log($"Jump attack hit player via raycast");
                    }
                }
            }
        }

        // Log if no damage was deal
        if (!playerDamaged)
        {
            Debug.LogWarning($"Jump attack missed player! Distance: {distanceToPlayer}, Radius: {jumpAttackRadius}");
        }

        // Re-enable agent movement after jump
        yield return new WaitForSeconds(0.2f);

        EndJumpAttack();
    }

    // End jump attack and return to normal state
    void EndJumpAttack()
    {
        if (!isJumpAttacking) return;
        
        isJumpAttacking = false;
        hasLanded = false;

        // Re-enable agent movement
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            
            // Resume chasing player if still alive
            if (gamemanager.instance.player != null && isAlive)
            {
                try
                {
                    agent.SetDestination(gamemanager.instance.player.transform.position);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not set destination after jump: {e.Message}");
                }
            }
        }

        // Stop jump coroutine
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }
    }

    // Cancel jump attack
    void CancelJumpAttack()
    {
        if (!isJumpAttacking) return;
        
        isJumpAttacking = false;
        hasLanded = false;
        
        // Re-enable agent movement if it exists
        if (agent != null && agent.isActiveAndEnabled)
        {
            try
            {
                agent.isStopped = false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not re-enable agent after jump cancellation: {e.Message}");
            }
        }
        
        // Stop jump coroutine
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }
    }

    // Burst fire coroutine
    IEnumerator BurstFire()
    {
        isBursting = true;
        currentBurstCount = 0;

        while (currentBurstCount < bulletsPerBurst)
        {
            Shoot();
            currentBurstCount++;

            if (currentBurstCount < bulletsPerBurst)
            {
                yield return new WaitForSeconds(timeBetweenBurstShots);
            }
        }

        shootTimer = 0;
        yield return new WaitForSeconds(timeBetweenBursts);
        isBursting = false;
        burstCoroutine = null;
    }

    // Play step sound
    IEnumerator playStep()
    {
        isPlayingStep = true;
        if (audStep.Length > 0 && aud != null)
        {
            aud.PlayOneShot(audStep[Random.Range(0, audStep.Length)], audStepVol);
        }
        yield return new WaitForSeconds(0.5f); // step rate
        isPlayingStep = false;
    }

    // Check if should roam or patrol
    void checkRoamOrPatrol()
    {
        if (agent == null || !agent.isActiveAndEnabled || isJumpAttacking || isDashing) return;

        if (agent.remainingDistance < 0.01f && roamTimer >= roamPauseTime)
        {
            if (useRoam)
                roam();
            else if (usePatrol)
                checkPatrol();
        }
        else if (usePatrol && patrolPoints != null && patrolPoints.Length > 0 && !agent.hasPath)
        {
            checkPatrol();
        }
    }

    // Roam to random position
    void roam()
    {
        if (agent == null || !agent.isActiveAndEnabled || isJumpAttacking || isDashing) return;
        
        roamTimer = 0;
        agent.stoppingDistance = 0;

        Vector3 ranPos = Random.insideUnitSphere * roamDist;
        ranPos += startingPos;

        NavMeshHit hit;
        NavMesh.SamplePosition(ranPos, out hit, roamDist, 1);
        agent.SetDestination(hit.position);
    }

    // Patrol between points
    void checkPatrol()
    {
        if (agent == null || !agent.isActiveAndEnabled || isJumpAttacking || isDashing) return;
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (agent.remainingDistance < 0.01f)
        {
            int safety = patrolPoints.Length;
            do
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                safety--;
            }
            while (safety > 0 && patrolPoints[patrolIndex] == null);

            if (patrolPoints[patrolIndex] != null)
            {
                agent.SetDestination(patrolPoints[patrolIndex].position);
            }
            else
            {
                Debug.LogWarning($"enemyAI on {name} has only null patrolPoints.");
            }
        }
    }

    // Check if enemy can see player
    bool canSeePlayer()
    {
        if (gamemanager.instance.player == null) return false;

        Vector3 playerPos = gamemanager.instance.player.transform.position;
        lastKnownPlayerPosition = playerPos; // Update last known position
        playerTracked = true;

        // Use head position
        Transform lookPoint = headPos;
        playerDir = playerPos - lookPoint.position;
        float distanceToPlayer = playerDir.magnitude;

        if (distanceToPlayer > sightRange)
        {
            if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
                agent.stoppingDistance = 0;
            return false;
        }

        angleToPlayer = Vector3.Angle(playerDir, lookPoint.forward);
        if (angleToPlayer > FOV)
        {
            if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
                agent.stoppingDistance = 0;
            return false;
        }

        RaycastHit hit;
        if (Physics.Raycast(lookPoint.position, playerDir.normalized, out hit, sightRange, ~ignoreLayer))
        {
            if (hit.collider.CompareTag("Player"))
            
                // Check for special attacks
                if (ShouldJumpAttack())
                {
                    StartJumpAttack();
                    return true;
                }

                if (ShouldDashAttack())
                {
                    StartDashAttack();
                    return true;
                }

                // Set destination to player position if agent is active and not in special attack
                if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
                {
                    try
                    {
                        agent.SetDestination(playerPos);
                        agent.stoppingDistance = stoppingDistOrig;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Could not set destination: {e.Message}");
                    }
                }

                // Handle attacks based on enemy type
                switch (enemyType)
                {
                    case EnemyType.Melee:
                        if (distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                            meleeAttack();
                        break;
                    case EnemyType.Shooter:
                        if (shootTimer >= shootRate && !isBursting)
                            Shoot();
                        break;
                    case EnemyType.Hybrid:
                        if (distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                            meleeAttack();
                        else if (shootTimer >= shootRate && !isBursting)
                            Shoot();
                        break;
                }
                if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing &&
                    agent.remainingDistance <= agent.stoppingDistance)
                    faceTarget();
                return true;
        }
        if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
            agent.stoppingDistance = 0;
        return false;
    }

    // Face the target (player)
    void faceTarget()
    {
        Quaternion rot = Quaternion.LookRotation(new Vector3(playerDir.x, 0, playerDir.z));
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, faceTargetSpeed * Time.deltaTime);
    }

    // Trigger enter for player detection
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = true;
    }

    // Trigger exit for player detection
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
                agent.stoppingDistance = 0;
        }
    }

    // Handle taking damage
    public void takeDamage(int amount)
    {
        // Prevent multiple death triggers or damage during special states
        if (!isAlive || isDashing || isJumpAttacking) return;

        HP -= amount;

        // Make enemy aware of the player
        if (gamemanager.instance.player != null && agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
        {
            // Only set destination if agent is active and not in special attack
            try
            {
                agent.SetDestination(gamemanager.instance.player.transform.position);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not set destination after taking damage: {e.Message}");
            }
        }

        // Play hurt audio
        if (audHurt.Length > 0 && aud != null)
            aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);

        // Check for death
        if (HP <= 0)
        {
            Die();
        }
        else
        {
            // Flash red to indicate damage
            StartCoroutine(flashRed());
        }
    }

    // Handle enemy death
    private void Die()
    {
        isAlive = false;

        // Cancel any special attacks
        if (isDashing) EndDash();
        if (isJumpAttacking) CancelJumpAttack();
        if (isBursting && burstCoroutine != null) StopCoroutine(burstCoroutine);

        // Play death audio
        if (audDeath.Length > 0 && aud != null)
            aud.PlayOneShot(audDeath[Random.Range(0, audDeath.Length)], audDeathVol);

        // Update game goal
        gamemanager.instance.updateGameGoal(-1);

        // Drop items
        if (itemDrop != null)
            itemDrop.TryDrop();

        // Drop currency (automatically added to player)
        if (currencyDropAmount > 0 && RiftShardManager.Instance != null)
        {
            RiftShardManager.Instance.Add(currencyDropAmount);
        }

        // Destroy immediately without animation or delay
        Destroy(gameObject);
    }

    IEnumerator flashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;
    }

    // Shoot projectile
    void Shoot()
    {
        shootTimer = 0;

        if (audShoot.Length > 0 && aud != null)
            aud.PlayOneShot(audShoot[Random.Range(0, audShoot.Length)], audShootVol);

        if (useAnimations && anim != null)
            anim.SetTrigger("Shoot");
        else
            createProjectile();
    }

    // Create projectile
    public void createProjectile()
    {
        Instantiate(projectile, shootPOS.position, shootPOS.rotation);
    }

    // Perform melee attack
    void meleeAttack()
    {
        attackTimer = 0;

        if (audMelee.Length > 0 && aud != null)
            aud.PlayOneShot(audMelee[Random.Range(0, audMelee.Length)], audMeleeVol);

        if (useAnimations && anim != null)
            anim.SetTrigger("Punch");
        else
            ApplyMeleeDamage();
    }

    // Apply melee damage to targets in range
    public void ApplyMeleeDamage()
    {
        // Check all colliders in range
        Collider[] hitColliders = Physics.OverlapSphere(meleePos.position, meleeRange, ~ignoreLayer);

        foreach (var hit in hitColliders)
        {
            // Look for objects implementing IDamage
            IDamage dmgTarget = hit.GetComponent<IDamage>();
            if (dmgTarget != null)
            {
                dmgTarget.takeDamage(meleeDamageAmount); // Apply damage

                // spawn visual effect
                if (meleeDamage != null)
                    Instantiate(meleeDamage, hit.transform.position, Quaternion.identity);
            }
        }
    }

    // Set patrol points
    public void SetPatrolPoints(Transform[] points, string sourceId = null)
    {
        patrolPoints = points;
        patrolIndex = 0;

        if (!string.IsNullOrEmpty(sourceId))
            patrolSourceId = sourceId;

        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0 && agent != null && agent.isActiveAndEnabled)
            agent.SetDestination(patrolPoints[0].position);
    }

    // Draw gizmos for visualization
    void OnDrawGizmosSelected()
    {
        // Draw jump attack range and radius
        if (canJumpAttack)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, jumpAttackRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, jumpAttackRadius);
        }
    }

    // Save state structure
    [System.Serializable]
    private struct EnemyState
    {
        public int hp;
        public Vector3 pos;
        public Vector3 startingPos;
        public float roamTimer;
        public int patrolIndex;
        public bool hasDestination;
        public Vector3 destination;
        public bool isTurretActive; // Save turret state
        public string patrolSourceId;
        public float jumpAttackTimer;
        public float dashAttackTimer;
    }

    // Capture current state for saving
    public object CaptureState()
    {
        var state = new EnemyState
        {
            hp = HP,
            pos = transform.position,
            startingPos = startingPos,
            roamTimer = roamTimer,
            patrolIndex = patrolIndex,
            hasDestination = false,
            destination = Vector3.zero,
            patrolSourceId = patrolSourceId,
            jumpAttackTimer = this.jumpAttackTimer,
            dashAttackTimer = this.dashAttackTimer
        };

        if (agent != null && agent.hasPath)
        {
            state.hasDestination = true;
            state.destination = agent.destination;
        }

        return state;
    }

    // Restore state from save
    public void RestoreState(object state)
    {
        if (state is not EnemyState s)
        {
            Debug.LogError($"enemyAI.RestoreState: expected EnemyState, got {state?.GetType()} on {name}");
            return;
        }

        jumpAttackTimer = s.jumpAttackTimer;
        dashAttackTimer = s.dashAttackTimer;

        // Restore position first
        if (agent != null)
            agent.Warp(s.pos);
        else
            transform.position = s.pos;

        // Restore combat state
        HP = s.hp;

        // Restore roam / patrol internals
        startingPos = s.startingPos;
        roamTimer = s.roamTimer;
        patrolSourceId = s.patrolSourceId;

        if ((patrolPoints == null || patrolPoints.Length == 0) && !string.IsNullOrEmpty(patrolSourceId))
        {
            var src = spawner.FindBySaveId(patrolSourceId);
            if (src != null)
            {
                var points = src.GetPatrolPoints();
                if (points != null && points.Length > 0)
                {
                    SetPatrolPoints(points);
                }
            }
        }

        // Only clamp patrolIndex if we actually have patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
            patrolIndex = Mathf.Clamp(s.patrolIndex, 0, patrolPoints.Length - 1);
        else
            patrolIndex = 0;

        // Restore movement destination (if it was valid when saved)
        if (agent != null && s.hasDestination)
        {
            agent.SetDestination(s.destination);
        }

        // Clean up animation state
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }
}