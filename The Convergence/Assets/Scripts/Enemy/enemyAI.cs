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
        Hybrid,
        Turret
    }

    [Header("~=~= Layers =~=~")]
    [SerializeField] LayerMask ignoreLayer;
    [Header("~=~= Enemy Type =~=~")]
    [SerializeField] EnemyType enemyType;

    [Header("~=~= Components =~=~")]
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator anim;
    [SerializeField] Renderer model;
    [SerializeField] Transform headPos;
    [SerializeField] Transform turretHead;

    [Header("~=~= Stats =~=~")]
    bool isAlive = true;
    [Range(1, 300)][SerializeField] int HP;
    [Range(1, 360)][SerializeField] int FOV;
    [Range(1, 360)][SerializeField] int faceTargetSpeed;
    [Range(1, 50)][SerializeField] int roamDist;
    [Range(0, 10)][SerializeField] int roamPauseTime;
    [Range(0.1f, 10f)][SerializeField] float animTransSpeed;

    [Header("~=~= Shooter Settings =~=~")]
    [SerializeField] GameObject projectile;
    [Range(0.1f, 10f)][SerializeField] float shootRate;
    [SerializeField] Transform shootPOS;

    [Header("~=~= Melee Settings =~=~")]
    [SerializeField] Transform meleePos;// Position from which melee attacks are measured
    [SerializeField] GameObject meleeDamage; // GameObject with damage.cs attached
    [Range(0.1f, 10f)][SerializeField] float meleeRange; // Distance at which enemy can hit player
    [Range(0.1f, 10f)][SerializeField] float attackRate;  // Cooldown between attacks
    [Range(1, 50)][SerializeField] int meleeDamageAmount;

    [Header("~=~= Turret Settings =~=~")]
    [SerializeField] bool enableTurretMode = false; // Toggle turret behavior
    [SerializeField] Transform[] turretRotationAxes; // Axes to rotate (e.g., head, base)
    [Range(0.1f, 100f)][SerializeField] float turretRotationSpeed = 5f;
    [SerializeField] float idleRotationSpeed = 10f; // Speed when idle
    [Range(-180, 180)][SerializeField] float minHorizontalAngle = -45f;
    [Range(-180, 180)][SerializeField] float maxHorizontalAngle = 45f;
    [Range(-90, 90)][SerializeField] float minVerticalAngle = -20f;
    [Range(-90, 90)][SerializeField] float maxVerticalAngle = 20f;
    [SerializeField] bool independentVerticalRotation = false; // Vertical rotates separately

    [Header("~=~= Jump Settings =~=~")]
    [SerializeField] bool canJumpAttack = false;
    [Range(0.1f, 10f)][SerializeField] float jumpForce;
    [Range(1f, 20f)][SerializeField] float jumpAttackRange; // Max distance to trigger jump
    [Range(0.1f, 4f)][SerializeField] float minHeightDifference; // How much higher player must be
    [Range(0.5f, 10f)][SerializeField] float jumpAttackCooldown;
    [Range(1, 30)][SerializeField] int jumpAttackDamage;
    [Range(0.5f, 5f)][SerializeField] float jumpAttackRadius; // Damage radius on landing
    [SerializeField] float jumpTimer;

    [Header("~=~= Dash Attack Settings =~=~")]
    [SerializeField] bool canDashAttack = false;
    [Range(2f, 25f)][SerializeField] float dashSpeed;
    [Range(0.1f, 2f)][SerializeField] float dashDuration;
    [Range(1f, 20f)][SerializeField] float dashAttackRange; // Max distance to trigger dash
    [Range(4f, 25f)][SerializeField] float minDashDistance; // Min distance (don't dash if too close)
    [Range(0.5f, 10f)][SerializeField] float dashAttackCooldown;
    [Range(1.1f, 5f)][SerializeField] float playerFleeingThreshold; // How fast player must be moving away

    //Burst Fire Settings
    [Header("~=~= Burst Fire Settings =~=~")]
    [SerializeField] bool useBurstFire = false;
    [Range(1, 10)][SerializeField] int bulletsPerBurst = 3;
    [Range(0.05f, 1f)][SerializeField] float timeBetweenBurstShots = 0.1f;
    [Range(0.5f, 5f)][SerializeField] float timeBetweenBursts = 1f;

    [Header("~=~= Behavior Toggles =~=~")]
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
    [SerializeField] AudioClip[] audDeath; // <-- NEW: Death audio
    [Range(0, 1)][SerializeField] float audDeathVol;
    [SerializeField] AudioClip[] audJumpAttack; // <-- NEW: Jump attack audio
    [Range(0, 1)][SerializeField] float audJumpAttackVol;
    [SerializeField] AudioClip[] audDash; // <-- NEW: Dash attack audio
    [Range(0, 1)][SerializeField] float audDashVol = 0.5f;

    [SerializeField] private ItemDrop itemDrop;

    public EnemyType EnemyTypeValue => enemyType;

    Color colorOrig;
    float sightRange = 20f;
    bool playerInTrigger;
    float shootTimer;
    float attackTimer;
    float roamTimer;
    float angleToPlayer;
    Vector3 playerDir;
    Vector3 startingPos;
    float stoppingDistOrig;

    [Header("~=~= Patrol Points =~=~")]
    [SerializeField] Transform[] patrolPoints; // Optional patrol points
    int patrolIndex = 0;
    [SerializeField] string patrolSourceId;

    bool isPlayingStep;

    //Turret variables
    private bool isTurretActive = false;
    private Vector3[] initialRotations;
    private float currentIdleRotation = 0f;
    private float idleRotationDirection = 1f;

    // Jump Attack variables
    private float jumpAttackTimer = 1f;
    private bool isJumpAttacking = false;
    private Rigidbody rb;
    private bool wasKinematic;
    private Vector3 jumpTarget;

    // Dash Attack variables
    private float dashAttackTimer = 1f;
    private bool isDashing = false;
    private Vector3 dashDirection;
    private float dashTimeRemaining;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;

    //Burst fire variables
    private bool isBursting = false;
    private int currentBurstCount = 0;
    private Coroutine burstCoroutine;

    //Turret state tracking
    private enum TurretState { Idle, Acquiring, Firing }
    private TurretState currentTurretState = TurretState.Idle;

    void Start()
    {
        colorOrig = model.material.color;
        stoppingDistOrig = agent.stoppingDistance;
        startingPos = transform.position;

        //rigidbody setup for jump attack
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
        }

        // Initialize patrol by setting the first patrol point as the destination
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[patrolIndex].position);

        if (itemDrop == null)
            itemDrop = GetComponent<ItemDrop>();

        //Initialize turret
        if (enableTurretMode)
        {
            InitializeTurret();
        }
    }

    void Update()
    {
        // Don't do anything if dead or in special state
        if (!isAlive) return;

        shootTimer += Time.deltaTime;
        attackTimer += Time.deltaTime;
        jumpAttackTimer += Time.deltaTime;
        dashAttackTimer += Time.deltaTime;

        // To track pplayer velocity for the dash attack
        TrackPlayerVelocity();

        // dash attack logic so enemy doesn't attack player if player movement is normal
        if (isDashing)
        {
            HandleDashMovement();
            return;
        }

        // Handling jump attack
        if (isJumpAttacking)
        {
            return;
        }

        // Play footsteps if moving
        if (!enableTurretMode && agent.velocity.magnitude > 0.1f && !isPlayingStep)
        {
            StartCoroutine(playStep());
        }

        // Update movement animation speed if enabled
        if (useAnimations && anim != null && !enableTurretMode)
        {
            float agentSpeedCur = agent.velocity.magnitude;
            float agentSpeedAnim = anim.GetFloat("Speed");
            anim.SetFloat("Speed", Mathf.Lerp(agentSpeedAnim, agentSpeedCur, Time.deltaTime * animTransSpeed));
        }

        // Track roam timer only when not moving
        if (!enableTurretMode && agent.remainingDistance < 0.01f)
            roamTimer += Time.deltaTime;
        // Handle different enemy behaviors based on type and mode
        if (enableTurretMode)
        {
            HandleTurretBehavior();
        }
        else
        {
            HandleMobileBehavior();
        }
    }
    //Separate behavior handler for turrets
    void HandleTurretBehavior()
    {
        // Turret doesn't move or roam
        if (playerInTrigger && canSeePlayer())
        {
            currentTurretState = TurretState.Acquiring;
            RotateTurretToTarget();

            // Check if we should start firing
            if (angleToPlayer <= FOV && shootTimer >= shootRate && !isBursting)
            {
                currentTurretState = TurretState.Firing;

                if (useBurstFire)
                {
                    if (burstCoroutine == null)
                    {
                        burstCoroutine = StartCoroutine(BurstFire());
                    }
                }
                else
                {
                    Shoot();
                    shootTimer = 0;
                }
            }
        }
        else
        {
            currentTurretState = TurretState.Idle;
            IdleTurretRotation();
        }
    }
    //Separate behavior handler for mobile enemies
    void HandleMobileBehavior()
    {
        if (playerInTrigger && !canSeePlayer())
        {
            checkRoamOrPatrol();
        }
        else if (!playerInTrigger)
        {
            checkRoamOrPatrol();
        }
    }
    //Initialize turret specific settings
    void InitializeTurret()
    {
        isTurretActive = true;
        agent.enabled = false; // Disable NavMeshAgent for turrets

        // Store initial rotations for each rotation axis
        if (turretRotationAxes != null && turretRotationAxes.Length > 0)
        {
            initialRotations = new Vector3[turretRotationAxes.Length];
            for (int i = 0; i < turretRotationAxes.Length; i++)
            {
                if (turretRotationAxes[i] != null)
                {
                    initialRotations[i] = turretRotationAxes[i].localEulerAngles;
                }
            }
        }

        // If no turret axes specified, use headPos
        if (turretRotationAxes == null || turretRotationAxes.Length == 0)
        {
            turretRotationAxes = new Transform[1] { headPos };
            initialRotations = new Vector3[1] { headPos.localEulerAngles };
        }
    }

    //Rotate turret when idle
    void IdleTurretRotation()
    {
        if (turretRotationAxes == null || turretRotationAxes.Length == 0)
            return;

        // Smooth idle rotation
        currentIdleRotation += idleRotationSpeed * Time.deltaTime * idleRotationDirection;

        // Reverse direction at limits
        if (currentIdleRotation >= maxHorizontalAngle || currentIdleRotation <= minHorizontalAngle)
        {
            idleRotationDirection *= -1;
            currentIdleRotation = Mathf.Clamp(currentIdleRotation, minHorizontalAngle, maxHorizontalAngle);
        }

        // Apply rotation to the first axis (typically horizontal)
        if (turretRotationAxes[0] != null)
        {
            Vector3 newRotation = turretRotationAxes[0].localEulerAngles;
            newRotation.y = initialRotations[0].y + currentIdleRotation;
            turretRotationAxes[0].localEulerAngles = newRotation;
        }
    }

    //Rotate turret to face player
    void RotateTurretToTarget()
    {
        if (turretRotationAxes == null || turretRotationAxes.Length == 0)
            return;

        Vector3 playerPos = gamemanager.instance.player.transform.position;

        // Calculate direction to player
        Vector3 directionToPlayer = playerPos - turretRotationAxes[0].position;
        directionToPlayer.y = 0; // Keep horizontal rotation separate

        // Calculate angles
        float targetHorizontalAngle = Mathf.Atan2(directionToPlayer.x, directionToPlayer.z) * Mathf.Rad2Deg;

        // Clamp horizontal rotation
        targetHorizontalAngle = Mathf.Clamp(
            targetHorizontalAngle,
            initialRotations[0].y + minHorizontalAngle,
            initialRotations[0].y + maxHorizontalAngle
        );

        // Apply horizontal rotation to first axis
        Quaternion targetHorizontalRotation = Quaternion.Euler(0, targetHorizontalAngle, 0);
        turretRotationAxes[0].rotation = Quaternion.Slerp(
            turretRotationAxes[0].rotation,
            targetHorizontalRotation,
            turretRotationSpeed * Time.deltaTime
        );

        // Handle vertical rotation if enabled
        if (independentVerticalRotation && turretRotationAxes.Length > 1 && turretRotationAxes[1] != null)
        {
            Vector3 directionToPlayerVertical = playerPos - turretRotationAxes[1].position;
            float verticalAngle = Vector3.Angle(turretRotationAxes[1].forward, directionToPlayerVertical);
            Vector3 cross = Vector3.Cross(turretRotationAxes[1].forward, directionToPlayerVertical);

            // Determine if angle is up or down
            if (cross.x > 0) verticalAngle = -verticalAngle;

            // Clamp vertical rotation
            verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);

            // Apply vertical rotation to second axis
            Quaternion targetVerticalRotation = Quaternion.Euler(verticalAngle, 0, 0);
            turretRotationAxes[1].localRotation = Quaternion.Slerp(
                turretRotationAxes[1].localRotation,
                targetVerticalRotation,
                turretRotationSpeed * Time.deltaTime
            );
        }
    }

    void TrackPlayerVelocity()
    {
        if (gamemanager.instance.player == null) return;

        Vector3 currentPlayerPos = gamemanager.instance.player.transform.position;
        playerVelocity = (currentPlayerPos - lastPlayerPosition) / Time.deltaTime;
        lastPlayerPosition = currentPlayerPos;
    }

    bool ShouldJumpAttack()
    {
        if (!canJumpAttack || isJumpAttacking || isDashing) return false;
        if (jumpAttackTimer < jumpAttackCooldown) return false;
        if (gamemanager.instance.player == null) return false;

        Vector3 playerPos = gamemanager.instance.player.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
        float heightDifference = playerPos.y - transform.position.y;

        // Check if player is above enemyAI and within range
        return heightDifference >= minHeightDifference && distanceToPlayer <= jumpAttackRange;
    }

    bool ShouldDashAttack()
    {
        // Enemies with type melee attack can dash
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

    void StartJumpAttack()
    {
        if (isJumpAttacking || gamemanager.instance.player == null) return;

        isJumpAttacking = true;
        jumpAttackTimer = 1f;
        jumpTarget = gamemanager.instance.player.transform.position;

        // Disable NavMeshAgent and enable rigidbody for physics-based jump
        agent.enabled = false;
        rb.isKinematic = false;
        rb.useGravity = true;

        // Calculate jump direction
        Vector3 direction = (jumpTarget - transform.position);
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        float verticalDistance = direction.y;

        // Calculate jump velocity for arc distance
        float timeToReach = horizontalDistance / (jumpForce * 0.7f);
        Vector3 horizontalVelocity = new Vector3(direction.x, 0, direction.z).normalized * jumpForce * 0.7f;
        float verticalVelocity = (verticalDistance / timeToReach) + (0.5f * Physics.gravity.magnitude * timeToReach);
        verticalVelocity = Mathf.Max(verticalVelocity, jumpForce); //Upward force

        rb.linearVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        // Play animation and sound
        if (useAnimations && anim != null)
            anim.SetTrigger("JumpAttack");
        if (audJumpAttack.Length > 0 && aud != null)
            aud.PlayOneShot(audJumpAttack[Random.Range(0, audJumpAttack.Length)], audJumpAttackVol);

        // Faces player during jump
        Vector3 lookDir = new Vector3(direction.x, 0, direction.z);
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    void StartDashAttack()
    {
        if (isDashing || gamemanager.instance.player == null) return;

        isDashing = true;
        dashAttackTimer = 1f;
        dashTimeRemaining = dashDuration;

        // Calculate dash direction towards player
        Vector3 playerPos = gamemanager.instance.player.transform.position;
        dashDirection = (playerPos - transform.position).normalized;
        dashDirection.y = 0; // Keep dash horizontal

        // Disable NavMeshAgent during dash
        agent.enabled = false;

        // Play animation and sound
        if (useAnimations && anim != null)
            anim.SetTrigger("DashAttack");
        if (audDash.Length > 0 && aud != null)
            aud.PlayOneShot(audDash[Random.Range(0, audDash.Length)], audDashVol);

        // Faces player during dash
        if (dashDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dashDirection);
    }

    void HandleDashMovement()
    {
        dashTimeRemaining -= Time.deltaTime;

        // Move in players directio to dash
        transform.position += dashDirection * dashSpeed * Time.deltaTime;

        // Check for a collision with the player during dash
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

        // End dash when time runs out
        if (dashTimeRemaining <= 0)
        {
            EndDash();
        }
    }

    void EndDash()
    {
        isDashing = false;

        // Re-enabling NavMeshAgent
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(transform.position, out navHit, 5f, NavMesh.AllAreas))
        {
            transform.position = navHit.position;
        }
        agent.enabled = true;

        // Resume chasing player
        if (gamemanager.instance.player != null)
            agent.SetDestination(gamemanager.instance.player.transform.position);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Handle landing after jump attack
        if (isJumpAttacking)
        {
            // Check if enemy hits the ground
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    LandJumpAttack();
                    break;
                }
            }
        }
    }

    void LandJumpAttack()
    {
        isJumpAttacking = false;

        // Deal damage in radius upon landing
        Collider[] hits = Physics.OverlapSphere(transform.position, jumpAttackRadius, ~ignoreLayer);
        foreach (var hit in hits)
        {
            IDamage dmg = hit.GetComponent<IDamage>();
            if (dmg != null && hit.CompareTag("Player"))
            {
                dmg.takeDamage(jumpAttackDamage);
            }
        }

        // Re-enabling NavMeshAgent
        rb.isKinematic = true;
        rb.useGravity = false; 
        rb.linearVelocity = Vector3.zero;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(transform.position, out navHit, 5f, NavMesh.AllAreas))
        {
            transform.position = navHit.position;
        }
        agent.enabled = true;

        // Play landing animation
        if (useAnimations && anim != null)
            anim.SetTrigger("Land");
    }

    //Burst fire coroutine
    IEnumerator BurstFire()
    {
        isBursting = true;
        currentBurstCount = 0;

        while (currentBurstCount < bulletsPerBurst && currentTurretState == TurretState.Firing)
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

    void checkRoamOrPatrol()
    {
        if (enableTurretMode) return; // Turrets don't roam

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

    void roam()
    {
        roamTimer = 0;
        agent.stoppingDistance = 0;

        Vector3 ranPos = Random.insideUnitSphere * roamDist;
        ranPos += startingPos;

        NavMeshHit hit;
        NavMesh.SamplePosition(ranPos, out hit, roamDist, 1);
        agent.SetDestination(hit.position);
    }

    void checkPatrol()
    {
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

    bool canSeePlayer()
    {
        Vector3 playerPos = gamemanager.instance.player.transform.position;

        // Use turret head position if available, otherwise use regular headPos
        Transform lookPoint = enableTurretMode && turretHead != null ? turretHead : headPos;
        playerDir = playerPos - lookPoint.position;
        float distanceToPlayer = playerDir.magnitude;

        if (distanceToPlayer > sightRange)
        {
            if (!enableTurretMode) agent.stoppingDistance = 0;
            return false;
        }

        angleToPlayer = Vector3.Angle(playerDir, lookPoint.forward);
        if (angleToPlayer > FOV)
        {
            if (!enableTurretMode) agent.stoppingDistance = 0;
            return false;
        }

        RaycastHit hit;
        if (Physics.Raycast(lookPoint.position, playerDir.normalized, out hit, sightRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                if (!enableTurretMode)
                {
                    //check for special attacks
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

                    agent.SetDestination(playerPos);
                    agent.stoppingDistance = stoppingDistOrig;

                    switch (enemyType)
                    {
                        case EnemyType.Melee:
                            if (distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                                meleeAttack();
                            break;
                        case EnemyType.Shooter:
                            if (shootTimer >= shootRate)
                                Shoot();
                            break;
                        case EnemyType.Hybrid:
                            if (distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                                meleeAttack();
                            else if (shootTimer >= shootRate)
                                Shoot();
                            break;
                    }

                    if (agent.remainingDistance <= agent.stoppingDistance)
                        faceTarget();
                }

                return true;
            }
        }

        if (!enableTurretMode) agent.stoppingDistance = 0;
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
            playerInTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            if (!enableTurretMode) agent.stoppingDistance = 0;
        }
    }

    public void takeDamage(int amount) // note the capitalization
    {
        // Prevent multiple death triggers
        if (!isAlive) return;

        // Subtract HP
        HP -= amount;

        // Make enemy aware of the player if not a turret
        if (!enableTurretMode && gamemanager.instance.player != null)
            agent.SetDestination(gamemanager.instance.player.transform.position);

        // Play hurt audio
        if (audHurt.Length > 0 && aud != null)
            aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);

        // Check for death
        if (HP <= 0)
        {
            // Mark enemy dead immediately
            isAlive = false;

            // Play death audio
            if (audDeath.Length > 0 && aud != null)
                aud.PlayOneShot(audDeath[Random.Range(0, audDeath.Length)], audDeathVol);

            // Update game goal
            gamemanager.instance.updateGameGoal(-1);

            // Drop items using the ItemDrop component
            if (itemDrop != null)
                itemDrop.TryDrop();

            // Stop movement and animations
            if (agent != null) agent.isStopped = true;
            if (anim != null) anim.enabled = false;

            Destroy(gameObject, 0.1f);
        }
        else
        {
            // Flash red to indicate damage
            StartCoroutine(flashRed());
        }
    }


    IEnumerator flashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;
    }

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

    public void createProjectile()
    {
        Instantiate(projectile, shootPOS.position, shootPOS.rotation);
    }

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

                // Optional: spawn visual effect
                if (meleeDamage != null)
                    Instantiate(meleeDamage, hit.transform.position, Quaternion.identity);
            }
        }
    }

    //Method to toggle turret mode at runtime
    public void SetTurretMode(bool active)
    {
        enableTurretMode = active;

        if (enableTurretMode)
        {
            InitializeTurret();
        }
        else
        {
            agent.enabled = true;
            isTurretActive = false;
        }
    }

    public void SetPatrolPoints(Transform[] points, string sourceId = null)
    {
        patrolPoints = points;
        patrolIndex = 0;

        if (!string.IsNullOrEmpty(sourceId))
            patrolSourceId = sourceId;

        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0 && agent != null)
            agent.SetDestination(patrolPoints[0].position);
    }

    //Gizmos for visualizing turret angles
    void OnDrawGizmosSelected()
    {
        if (enableTurretMode && turretRotationAxes != null && turretRotationAxes.Length > 0 && turretRotationAxes[0] != null)
        {
            // Draw FOV cone
            Gizmos.color = Color.yellow;
            float halfFOV = FOV / 2.0f;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.up);
            Vector3 leftRayDirection = leftRayRotation * turretRotationAxes[0].forward;
            Vector3 rightRayDirection = rightRayRotation * turretRotationAxes[0].forward;

            Gizmos.DrawRay(turretRotationAxes[0].position, leftRayDirection * sightRange);
            Gizmos.DrawRay(turretRotationAxes[0].position, rightRayDirection * sightRange);

            // Draw rotation limits
            Gizmos.color = Color.blue;
            Quaternion minRotation = Quaternion.Euler(0, minHorizontalAngle, 0);
            Quaternion maxRotation = Quaternion.Euler(0, maxHorizontalAngle, 0);
            Vector3 minDirection = minRotation * turretRotationAxes[0].forward;
            Vector3 maxDirection = maxRotation * turretRotationAxes[0].forward;

            Gizmos.DrawRay(turretRotationAxes[0].position, minDirection * sightRange);
            Gizmos.DrawRay(turretRotationAxes[0].position, maxDirection * sightRange);
        }
    }

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
        public bool isTurretActive;//save turret state
        public string patrolSourceId;
        public float jumpAttackTimer;
        public float dashAttackTimer;
    }

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
            isTurretActive = isTurretActive, //save turret state
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
        isTurretActive = s.isTurretActive;// save turret state

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
