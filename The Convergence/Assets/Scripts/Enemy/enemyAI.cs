using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem;

[RequireComponent(typeof(Rigidbody))]
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
    [SerializeField] bool canJumpAttack = true;
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
    
    [Header("**** Wave Mode Settings ****")]
    [Range(0, 200)][SerializeField] float waveModeRange = 200f;

    [Header("**** Behavior Toggles ****")]
    public bool useAnimations = true; // Toggle all animation logic on/off
    public bool usePatrol = true; // Toggle patrol behavior
    public bool useRoam = true; // Toggle roaming behavior
    public bool waveModeActive = false; // toggle for wave mode
    public bool ignoreWaveMode;

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
    [Range(0, 1000)][SerializeField] private int currencyDropAmount = 10; // Amount of currency this enemy drops

    [Header("**** Boss Settings ****")]
    [SerializeField] bool isBoss = false;
    [SerializeField] GameObject bossDeathEffect; // Optional visual effect

    public bool IsBoss => isBoss;

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

    void Start()
    {
        colorOrig = model.material.color;
        stoppingDistOrig = agent.stoppingDistance;
        startingPos = transform.position;

      if (gamemanager.instance != null && gamemanager.instance.IsWaveModeActive)
      {
            waveModeActive = true;
      }
        
        // Rigidbody setup for jump attack (transform-arc or physics mode)
        if (canJumpAttack)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                Debug.LogWarning($"enemyAI on {name}: Rigidbody was missing and has been added for jump attacks.");
            }

            // Keep kinematic by default for transform-driven arc; if you switch to physics-based jumps,
            // set rb.isKinematic = false and rb.useGravity = true in the physics jump code path.
            rb.isKinematic = true;
            rb.useGravity = false;

            // Prevent unexpected rotation if physics ever gets enabled
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            wasKinematic = rb.isKinematic;
        }

        // Initialize player position tracking for dash safely
        if (gamemanager.instance != null && gamemanager.instance.player != null)
        {
            lastPlayerPosition = gamemanager.instance.player.transform.position;
        }

        // Initialize patrol by setting the first patrol point as the destination
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0 && agent != null && agent.isActiveAndEnabled)
            agent.SetDestination(patrolPoints[patrolIndex].position);

        if (itemDrop == null)
            itemDrop = GetComponent<ItemDrop>();
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
        float distanceToPlayer = Vector3.Distance(transform.position, gamemanager.instance.player.transform.position);
        // Handle attacks based on enemy type
        switch (enemyType)
        {
            case EnemyType.Melee:
                if (distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                    meleeAttack();
                break;
            case EnemyType.Shooter:
                if (CanShootAtPlayer())
                    Shoot();
                break;
            case EnemyType.Hybrid:
                if (distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                    meleeAttack();
                else if (CanShootAtPlayer())
                    Shoot();
                break;
        }
        // Already checked in Update(), but double-check for safety
        if (isJumpAttacking || isDashing) return;

        if (ShouldTargetPlayer())
        {
            // Set destination to player
            if (agent != null && agent.isActiveAndEnabled)
            {
                try
                {
                    agent.SetDestination(gamemanager.instance.player.transform.position);
                    agent.stoppingDistance = stoppingDistOrig;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not set destination: {e.Message}");
                }
            }

            // Check for special attacks
            if (ShouldJumpAttack())
            {
                StartJumpAttack();
                return;
            }

            if (ShouldDashAttack())
            {
                StartDashAttack();
                return;
            }

            // Handle attacks based on enemy type
           

            if (agent != null && agent.isActiveAndEnabled && agent.remainingDistance <= agent.stoppingDistance)
                faceTarget();
        }
        else
        {
            // If we shouldn't target the player, then roam or patrol
            checkRoamOrPatrol();
        }
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
        if (gamemanager.instance == null || gamemanager.instance.player == null) return false;

        Vector3 playerPos = gamemanager.instance.player.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
        float heightDifference = playerPos.y - transform.position.y;

        // Allow jump if player is sufficiently higher or close enough horizontally
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
        if (isJumpAttacking) return;
        if (gamemanager.instance == null || gamemanager.instance.player == null) return;
        if (!canJumpAttack) return;

        isJumpAttacking = true;
        jumpAttackTimer = 0f;
        hasLanded = false;
        jumpStartPosition = transform.position;
        jumpTarget = gamemanager.instance.player.transform.position;

        // Stop the agent and disable automatic position/rotation updates so transform movement is not overridden
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        // Windup audio
        if (aud != null && audJumpWindup != null)
            aud.PlayOneShot(audJumpWindup, audJumpWindupVol);

        // Face player during windup
        Vector3 lookDir = new Vector3(jumpTarget.x - transform.position.x, 0, jumpTarget.z - transform.position.z);
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // Start arc coroutine with safety timeout
        if (jumpCoroutine != null) StopCoroutine(jumpCoroutine);
        jumpCoroutine = StartCoroutine(PerformArcJumpAttack());
    }

    // Calculate launch velocity for jump attack
    Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float apexHeight)
    {
        // Gravity (negative)
        float g = Physics.gravity.y;

        // Ensure apex is above both points
        float highest = Mathf.Max(start.y, target.y) + apexHeight;

        // Time to maxHeight from start
        float timeUp = Mathf.Sqrt(2f * (highest - start.y) / -g);

        // Time from maxHeight to target
        float timeDown = Mathf.Sqrt(2f * (highest - target.y) / -g);

        float totalTime = timeUp + timeDown;
        if (totalTime <= 0.001f) totalTime = 0.5f;

        // Horizontal velocity
        Vector3 horizontalDisplacement = new Vector3(target.x - start.x, 0, target.z - start.z);
        Vector3 horizontalVelocity = horizontalDisplacement / totalTime;

        // Vertical velocity for ascent
        float verticalVelocity = Mathf.Sqrt(-2f * g * (highest - start.y));

        Vector3 result = horizontalVelocity + Vector3.up * verticalVelocity;
        return result;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isJumpAttacking) return;

        // Ignore collisions with other enemies or triggers
        if (collision.collider.CompareTag("Enemy") || collision.collider.isTrigger) return;

        // Check layer mask to ensure it's ground
        if (((1 << collision.gameObject.layer) & ignoreLayer) != 0) return;

        // Enemy has LANDED - apply landing logic once
        if (!hasLanded)
        {
            hasLanded = true;

            // Play landing sound and effect
            if (audJumpLanding != null && aud != null)
                aud.PlayOneShot(audJumpLanding, audJumpLandingVol);

            if (jumpLandingEffect != null)
                Instantiate(jumpLandingEffect, transform.position, Quaternion.identity);

            // Apply jumpDamage after a short delay to allow for landing impact
            StartCoroutine(ApplyJumpDamage());

            // Then end jump attack after a brief moment to allow for landing effects
            StartCoroutine(EndJumpAfterDelay(0.2f));
        }
    }

   
    IEnumerator EndJumpAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Re-enable agent and warp to current position
        if (agent != null)
        {
            try
            {
                agent.enabled = true;
                agent.Warp(transform.position);
                agent.isStopped = false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not re-enable agent after physics jump: {e.Message}");
            }
        }

        // Reset back to kinematic again to transform movement after landing
        if (rb != null)
        {
           rb.isKinematic = true;
        }

        isJumpAttacking = false;
        hasLanded = false;
    }


    // Coroutine to perform arc-style jump attack
    IEnumerator PerformArcJumpAttack()
    {
        // Safety guards
        if (gamemanager.instance == null || gamemanager.instance.player == null)
        {
            CancelJumpAttack();
            yield break;
        }

        // Windup
        yield return new WaitForSeconds(Mathf.Max(0f, jumpWindupTime));

        if (aud != null && audJumpAttack != null && audJumpAttack.Length > 0)
            aud.PlayOneShot(audJumpAttack[Random.Range(0, audJumpAttack.Length)], audJumpAttackVol);

        Vector3 startPos = transform.position;
        Vector3 playerPos = gamemanager.instance.player.transform.position;

        // Compute desired landing point near player
        Vector3 toPlayer = (playerPos - startPos);
        toPlayer.y = 0;
        Vector3 desiredEnd = playerPos;
        if (toPlayer.sqrMagnitude > 0.001f)
            desiredEnd = playerPos - toPlayer.normalized * Mathf.Max(0.1f, landingDistanceFromPlayer);

        // Find ground for desired end
        Vector3 finalLandingPos = FindGroundPosition(desiredEnd);

        // Try circle samples if initial ground search failed
        if (finalLandingPos == Vector3.zero)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector3 testPos = playerPos + Quaternion.Euler(0, angle, 0) * Vector3.forward * landingDistanceFromPlayer;
                finalLandingPos = FindGroundPosition(testPos);
                if (finalLandingPos != Vector3.zero) break;
            }
        }

        // Fallback to start position if still no ground
        if (finalLandingPos == Vector3.zero)
            finalLandingPos = startPos;

        // Try to snap landing to NavMesh if available
        Vector3 navLanding = finalLandingPos;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(finalLandingPos, out navHit, 2.0f, NavMesh.AllAreas))
            navLanding = navHit.position;

        // Horizontal path calculation
        Vector3 horizontalDirection = new Vector3(navLanding.x - startPos.x, 0, navLanding.z - startPos.z);
        float horizontalDistance = horizontalDirection.magnitude;
        if (horizontalDistance < 0.5f)
        {
            horizontalDirection = (playerPos - startPos);
            horizontalDirection.y = 0;
            horizontalDistance = Mathf.Max(1f, horizontalDirection.magnitude);
            horizontalDirection = horizontalDirection.normalized;
        }

        Vector3 horizontalNormalized = horizontalDirection.normalized;
        float actualJumpDuration = Mathf.Max(0.1f, jumpDuration * Mathf.Clamp(horizontalDistance / 10f, 0.5f, 2f));

        float elapsedTime = 0f;
        float maxDuration = Mathf.Max(5f, actualJumpDuration * 2f); // safety timeout

        while (elapsedTime < actualJumpDuration && isJumpAttacking && elapsedTime < maxDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / actualJumpDuration);

            // Horizontal interpolation
            float horizontalProgress = normalizedTime;
            Vector3 horizontalPosition = startPos + horizontalNormalized * (horizontalDistance * horizontalProgress);

            // Vertical using curve
            float verticalOffset = (jumpCurve != null ? jumpCurve.Evaluate(normalizedTime) : normalizedTime) * jumpHeight;
            Vector3 newPosition = horizontalPosition;
            newPosition.y = startPos.y + verticalOffset;

            transform.position = newPosition;

            // Face player while airborne if player still exists
            if (gamemanager.instance != null && gamemanager.instance.player != null)
            {
                Vector3 lookDirection = gamemanager.instance.player.transform.position - transform.position;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            yield return null;
        }

        // If coroutine timed out or player disappeared, cancel safely
        if (!isJumpAttacking)
            yield break;

        // Snap to landing position
        transform.position = navLanding;

        // Trigger landing logic
        LandJumpAttack();

        // Ensure agent is placed on navmesh and resumes
        if (agent != null)
        {
            try
            {
                if (!agent.isActiveAndEnabled) agent.enabled = true;
                agent.Warp(transform.position);
                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = true;
                // Resume chasing player if available
                if (gamemanager.instance != null && gamemanager.instance.player != null && isAlive)
                    agent.SetDestination(gamemanager.instance.player.transform.position);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"enemyAI: NavMesh warp/resume failed after jump: {e.Message}");
            }
        }

        // Clean up coroutine reference
        jumpCoroutine = null;
    }

    //Find ground position with detection
    Vector3 FindGroundPosition(Vector3 position)
    {
        // Validate inputs
        if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
            return Vector3.zero;

        float[] raycastHeights = { 10f, 5f, 2f, 0.5f };

        foreach (float height in raycastHeights)
        {
            Vector3 rayStart = position + Vector3.up * height;
            RaycastHit hit;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, height + 5f, ~ignoreLayer))
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.CompareTag("Enemy"))
                {
                    if (Vector3.Angle(hit.normal, Vector3.up) < 45f)
                        return hit.point;
                }
            }
        }

        // SphereCast fallback
        RaycastHit sphereHit;
        if (Physics.SphereCast(position + Vector3.up * 5f, 1f, Vector3.down, out sphereHit, 10f, ~ignoreLayer))
        {
            if (sphereHit.collider != null && sphereHit.collider.gameObject != gameObject && !sphereHit.collider.CompareTag("Enemy"))
                return sphereHit.point;
        }

        // No suitable ground found
        return Vector3.zero;
    }

    // Start dash attack
    void StartDashAttack()
    {
        if (isDashing || gamemanager.instance.player == null) return;

        isDashing = true;
        dashAttackTimer = 0f;
        dashTimeRemaining = dashDuration;

        // Use raycast to get direct path to player
        Vector3 playerPos = gamemanager.instance.player.transform.position;
        Vector3 rayDirection = (playerPos - transform.position).normalized;

        // Raycast to check for clear path to player
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, rayDirection, out hit, dashAttackRange * 2f, ~ignoreLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
                // Clear path to player, dash directly toward them
                dashDirection = rayDirection;
            }
            else
            {
                // Something in the way, dash in player's general direction
                dashDirection = rayDirection;
            }
        }
        else
        {
            // No hit, dash in player's direction
            dashDirection = rayDirection;
        }

        dashDirection.y = 0; // Keep dash horizontal

        // Stop the agent during dash
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // Play animation and sound - ONLY set Speed, no DashAttack trigger
        if (useAnimations && anim != null)
        {
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

        // Update dash direction each frame using raycast to follow player
        if (gamemanager.instance.player != null)
        {
            Vector3 playerPos = gamemanager.instance.player.transform.position;
            Vector3 rayDirection = (playerPos - transform.position).normalized;

            // Raycast to check line of sight to player
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, rayDirection, out hit, dashAttackRange * 2f, ~ignoreLayer))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    // Clear line of sight, update dash direction
                    dashDirection = Vector3.Slerp(dashDirection, rayDirection, 10f * Time.deltaTime);
                    dashDirection.y = 0;
                    dashDirection.Normalize();
                }
            }

            // Face the direction we're dashing
            if (dashDirection != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dashDirection);
        }

        // Check for wall collisions before moving
        if (!CheckWallCollision())
        {
            // Move with constant speed
            Vector3 movement = dashDirection * dashSpeed * Time.deltaTime;
            transform.position += movement;
        }
        else
        {
            // Hit a wall, end dash
            EndDash();
            return;
        }

        // Update animation speed during dash
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

        // End dash when time runs out
        if (dashTimeRemaining <= 0)
        {
            EndDash();
        }
    }

    // Check for wall collisions during dash
    bool CheckWallCollision()
    {
        // Raycast forward to check for walls
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dashDirection, out hit, 1f, ~ignoreLayer))
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

        if (aud != null && audJumpLanding != null)
            aud.PlayOneShot(audJumpLanding, audJumpLandingVol);

        if (jumpLandingEffect != null)
            Instantiate(jumpLandingEffect, transform.position, Quaternion.identity);

        // Apply damage after a short delay
        StartCoroutine(ApplyJumpDamage());
    }

    IEnumerator ApplyJumpDamage()
    {
        // Safety guard
        yield return new WaitForSeconds(Mathf.Max(0f, jumpDamageDelay));

        if (gamemanager.instance == null || gamemanager.instance.player == null)
        {
            Debug.LogWarning("enemyAI: ApplyJumpDamage aborted because player is missing");
        }
        else
        {
            bool playerDamaged = false;
            Vector3 playerPos = gamemanager.instance.player.transform.position;
            float distanceToPlayer = Vector3.Distance(transform.position, playerPos);

            // Direct distance check
            if (distanceToPlayer <= jumpAttackRadius * 1.2f)
            {
                IDamage dmg = gamemanager.instance.player.GetComponent<IDamage>();
                if (dmg != null)
                {
                    dmg.takeDamage(jumpAttackDamage);
                    playerDamaged = true;
                    Debug.Log($"Jump attack hit player via distance check: {distanceToPlayer} units away");
                }
            }

            // Overlap sphere fallback
            if (!playerDamaged)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, jumpAttackRadius, ~ignoreLayer);
                foreach (var hit in hits)
                {
                    if (hit != null && hit.CompareTag("Player"))
                    {
                        IDamage dmg = hit.GetComponent<IDamage>();
                        if (dmg != null)
                        {
                            dmg.takeDamage(jumpAttackDamage);
                            playerDamaged = true;
                            Debug.Log("Jump attack hit player via overlap sphere");
                            break;
                        }
                    }
                }
            }

            // Raycast fallback
            if (!playerDamaged)
            {
                RaycastHit hit;
                Vector3 rayDirection = (playerPos - transform.position).normalized;
                if (Physics.Raycast(transform.position, rayDirection, out hit, jumpAttackRadius * 2f, ~ignoreLayer))
                {
                    if (hit.collider != null && hit.collider.CompareTag("Player"))
                    {
                        IDamage dmg = hit.collider.GetComponent<IDamage>();
                        if (dmg != null)
                        {
                            dmg.takeDamage(jumpAttackDamage);
                            playerDamaged = true;
                            Debug.Log("Jump attack hit player via raycast");
                        }
                    }
                }
            }

            if (!playerDamaged)
                Debug.LogWarning($"Jump attack missed player. Distance: {Vector3.Distance(transform.position, gamemanager.instance.player.transform.position)}, Radius: {jumpAttackRadius}");
        }

        // Small wait to let effects play, then end jump
        yield return new WaitForSeconds(0.2f);
        EndJumpAttack();
    }


    // End jump attack and return to normal state
    void EndJumpAttack()
    {
        if (!isJumpAttacking) return;

        isJumpAttacking = false;
        hasLanded = false;

        // Restore enemyAI movement AND update flags
        if (agent != null)
        {
            try
            {
                if (!agent.isActiveAndEnabled) agent.enabled = true;
                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = true;

                // Resume chasing player if still alive
                if (gamemanager.instance != null && gamemanager.instance.player != null && isAlive)
                    agent.SetDestination(gamemanager.instance.player.transform.position);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"enemyAI: Could not restore agent after jump: {e.Message}");
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

        if (agent != null)
        {
            try
            {
                // Re-enable agent movement if it exists
                if (!agent.isActiveAndEnabled) agent.enabled = true;
                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"enemyAI: Could not re-enable agent after jump cancellation: {e.Message}");
            }
        }

        // Stop jump coroutine
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }
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
        playerDir = playerPos - headPos.position;
        float distanceToPlayer = playerDir.magnitude;

        if (distanceToPlayer > sightRange)
        {
            if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
                agent.stoppingDistance = 0;
            return false;
        }

        angleToPlayer = Vector3.Angle(playerDir, headPos.forward);
        if (angleToPlayer > FOV)
        {
            if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
                agent.stoppingDistance = 0;
            return false;
        }

        RaycastHit hit;
        if (Physics.Raycast(headPos.position, playerDir.normalized, out hit, sightRange, ~ignoreLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
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

                
                if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing &&
                    agent.remainingDistance <= agent.stoppingDistance)
                    faceTarget();
                return true;
            }
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
        if (!isAlive) return;

        HP -= amount;

        // Flash damage color
        if (isDashing || isJumpAttacking)
        {
            StartCoroutine(flashSpecialAttackColor());
        }
        else
        {
            StartCoroutine(flashRed());
        }

        // Make enemy aware of the player
        if (gamemanager.instance.player != null && agent != null && agent.isActiveAndEnabled)
        {
            try
            {
                agent.SetDestination(gamemanager.instance.player.transform.position);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not set destination after taking damage: {e.Message}");
            }
        }

        // Play hurt audio (even during special attacks)
        if (audHurt.Length > 0 && aud != null)
            aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);

        // Check for death
        if (HP <= 0)
        {
            Die();
        }
    }

    IEnumerator flashSpecialAttackColor()
    {
        model.material.color = Color.yellow; // Yellow for special attack resistance
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;
    }

    // Handle enemy death
    private void Die()
    {
        isAlive = false;

        // Cancel any special attacks
        if (isDashing) EndDash();
        if (isJumpAttacking) CancelJumpAttack();

        // Play death audio
        if (audDeath.Length > 0 && aud != null)
            aud.PlayOneShot(audDeath[Random.Range(0, audDeath.Length)], audDeathVol);

        // Check if this is the boss enemy
        if (isBoss)
        {
            Debug.Log("=== BOSS DEFEATED ===");

            // Show boss death effect if assigned
            if (bossDeathEffect != null)
                Instantiate(bossDeathEffect, transform.position, Quaternion.identity);

            // Call the special boss defeat method in gamemanager
            // This is the ONLY way to trigger the win condition in the entire game
            gamemanager.instance.OnLevel4BossDefeated();

            // Drop special boss loot
            if (itemDrop != null)
                itemDrop.TryDrop();

            // Drop currency (boss gives extra)
            if (currencyDropAmount > 0 && RiftShardManager.Instance != null)
            {
                RiftShardManager.Instance.Add(currencyDropAmount * 5); // Boss gives 5x currency
            }
        }
        else
        {
            // REGULAR ENEMY (NOT BOSS)
            // Update game goal count (for tracking only - no win condition)
            gamemanager.instance.updateGameGoal(-1);

            // Drop regular loot
            if (itemDrop != null)
                itemDrop.TryDrop();

            // Drop regular currency
            if (currencyDropAmount > 0 && RiftShardManager.Instance != null)
            {
                RiftShardManager.Instance.Add(currencyDropAmount);
            }
        }

        // Destroy the enemy object
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
        if (shootTimer < shootRate) return;

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
   
    public void EnableWaveMode()
    {
        waveModeActive = true;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.stoppingDistance = stoppingDistOrig;
        }
    }
    public void DisableWaveMode()
    {
        waveModeActive = false;
    }

    public void ToggleWaveMode()
    {
        waveModeActive = !waveModeActive;
    }

    // Optionally, add a method to set wave mode range
    public void SetWaveModeRange(float newRange)
    {
        waveModeRange = newRange;
    }
    bool ShouldTargetPlayer()
    {
        if (gamemanager.instance.player == null) return false;

        if (waveModeActive)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, gamemanager.instance.player.transform.position);
            if (distanceToPlayer <= waveModeRange)
                return true;
        }

        return playerInTrigger || canSeePlayer();
    }
    // Check if enemy can shoot at player
    bool CanShootAtPlayer()
    {
        if (gamemanager.instance == null || gamemanager.instance.player == null) return false;

        // Check if enough time has passed
        if (shootTimer < shootRate) return false;

        // Check if enemy has line of sight to player
        return HasLineOfSightToPlayer();
    }

    // Check if enemy has clear line of sight to player
    bool HasLineOfSightToPlayer()
    {
        if (gamemanager.instance == null || gamemanager.instance.player == null) return false;

        Vector3 playerPos = gamemanager.instance.player.transform.position;
        Vector3 directionToPlayer = (playerPos - shootPOS.position).normalized;
        float distanceToPlayer = Vector3.Distance(shootPOS.position, playerPos);

        RaycastHit hit;
        if (Physics.Raycast(shootPOS.position, directionToPlayer, out hit, distanceToPlayer, ~ignoreLayer))
        {
            return hit.collider.CompareTag("Player");
        }

        return false;
    }
}