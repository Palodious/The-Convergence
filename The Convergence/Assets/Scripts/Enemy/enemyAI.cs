using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEngine.XR;

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
    [Range(1, 1500)][SerializeField] int maxHP = 100;
    private int currentHP;
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
    [SerializeField] Transform meleePos;
    [SerializeField] GameObject meleeDamage;
    [Range(0.1f, 30f)][SerializeField] float meleeRange;
    [Range(0.1f, 10f)][SerializeField] float attackRate;
    [Range(1, 50)][SerializeField] int meleeDamageAmount;

    [Header("**** Jump Attack Settings ****")]
    [SerializeField] bool canJumpAttack = true;
    [Range(0.1f, 10f)][SerializeField] float jumpForce;
    [Range(1f, 50f)][SerializeField] float jumpAttackRange;
    [Range(0.1f, 10f)][SerializeField] float minHeightDifference;
    [Range(0.5f, 30f)][SerializeField] float jumpAttackCooldown;
    [Range(1, 100)][SerializeField] int jumpAttackDamage;
    [Range(0.5f, 15f)][SerializeField] float jumpAttackRadius;
    [SerializeField] float jumpTimer;

    [Header("**** Jump Attack Arc Settings ****")]
    [Range(1f, 20f)][SerializeField] float jumpHeight = 3f;
    [Range(0.1f, 5f)][SerializeField] float jumpDuration = 1f;
    [SerializeField] AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] GameObject jumpLandingEffect;
    [Range(0f, 1f)][SerializeField] float jumpDamageDelay = 0.1f;
    [Range(0f, 1f)][SerializeField] float jumpWindupTime = 0.2f;
    [Range(0f, 3f)][SerializeField] float landingDistanceFromPlayer = 0.5f;

    [Header("**** Dash Attack Settings ****")]
    [SerializeField] bool canDashAttack = false;
    [Range(2f, 50f)][SerializeField] float dashSpeed;
    [Range(0.1f, 5f)][SerializeField] float dashDuration;
    [Range(1f, 50f)][SerializeField] float dashAttackRange;
    [Range(0.5f, 30f)][SerializeField] float minDashDistance;
    [Range(0.5f, 30f)][SerializeField] float dashAttackCooldown;
    [Range(1.1f, 10f)][SerializeField] float playerFleeingThreshold;

    [Header("**** Wave Mode Settings ****")]
    [Range(0, 200)][SerializeField] float waveModeRange = 200f;

    [Header("**** Behavior Toggles ****")]
    public bool useAnimations = true;
    public bool usePatrol = true;
    public bool useRoam = true;
    public bool waveModeActive = false;
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
    [SerializeField] AudioClip[] audDeath;
    [Range(0, 1)][SerializeField] float audDeathVol;
    [SerializeField] AudioClip[] audJumpAttack;
    [Range(0, 1)][SerializeField] float audJumpAttackVol;
    [SerializeField] AudioClip audJumpWindup;
    [Range(0, 1)][SerializeField] float audJumpWindupVol = 0.5f;
    [SerializeField] AudioClip audJumpLanding;
    [Range(0, 1)][SerializeField] float audJumpLandingVol = 0.7f;
    [SerializeField] AudioClip[] audDash;
    [Range(0, 1)][SerializeField] float audDashVol = 0.5f;

    [SerializeField] private ItemDrop itemDrop;

    [Header("**** Currency Drop ****")]
    [Range(0, 1000)][SerializeField] private int currencyDropAmount = 10;

    [Header("**** Boss Settings ****")]
    [SerializeField] bool isBoss = false;
    [SerializeField] GameObject bossDeathEffect;

    [Header("**** Death Animation Settings ****")]
    [SerializeField] float deathAnimationDuration = 2f;
    [SerializeField] bool useDeathAnimation = true;

    [Header("**** NEW GAME+ NG+ SCALING ****")]
    [SerializeField] private bool enableNGPlusScaling = true;

    private int baseMaxHP;
    private int baseMeleeDamage;
    private int baseJumpAttackDamage;

    private float baseAgentSpeed;
    private float baseAgentAccel;

    private bool ngpBaseCached;
    private bool ngpApplied;


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
    [SerializeField] Transform[] patrolPoints;
    int patrolIndex = 0;
    [SerializeField] string patrolSourceId;

    bool isPlayingStep;

    private float jumpAttackTimer = 1f;
    private bool isJumpAttacking = false;
    private Rigidbody rb;
    private bool wasKinematic;
    private Vector3 jumpTarget;
    private Coroutine jumpCoroutine;
    private Vector3 jumpStartPosition;
    private bool hasLanded = false;

    private float dashAttackTimer = 1f;
    private bool isDashing = false;
    private Vector3 dashDirection;
    private float dashTimeRemaining;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;

    private float GetNgpDamageMultiplier()
    {
        if (!enableNGPlusScaling) return 1f;
        if (NewGamePlusManager.Instance == null) return 1f;

        return Mathf.Max(0.01f, NewGamePlusManager.Instance.GetEnemyDamageMultiplier());
    }

    private int GetScaledDamage(int baseDamage)
    {
        float mult = GetNgpDamageMultiplier();
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * mult));
    }

    private void CacheNgpBaseStatsIfNeeded()
    {
        if (ngpBaseCached) return;

        baseMaxHP = maxHP;
        baseMeleeDamage = meleeDamageAmount;
        baseJumpAttackDamage = jumpAttackDamage;

        if (agent != null)
        {
            baseAgentSpeed = agent.speed;
            baseAgentAccel = agent.acceleration;
        }

        ngpBaseCached = true;
    }

    private void ApplyNgpScalingIfNeeded()
    {
        if (!enableNGPlusScaling) return;
        if (ngpApplied) return;

        if (SaveManager.IsLoadingFromSave)
            return;

        CacheNgpBaseStatsIfNeeded();

        float hpMult = 1f;
        float dmgMult = 1f;
        float speedMult = 1f;

        if (NewGamePlusManager.Instance != null)
        {
            hpMult = Mathf.Max(0.01f, NewGamePlusManager.Instance.GetEnemyHealthMultiplier());
            dmgMult = Mathf.Max(0.01f, NewGamePlusManager.Instance.GetEnemyDamageMultiplier());
            speedMult = Mathf.Max(0.01f, NewGamePlusManager.Instance.GetEnemySpeedMultiplier());
        }

        maxHP = Mathf.Max(1, Mathf.RoundToInt(baseMaxHP * hpMult));
        currentHP = maxHP;
        meleeDamageAmount = Mathf.Max(1, Mathf.RoundToInt(baseMeleeDamage * dmgMult));
        jumpAttackDamage = Mathf.Max(1, Mathf.RoundToInt(baseJumpAttackDamage * dmgMult));

        if (agent != null)
        {
            agent.speed = baseAgentSpeed * speedMult;
            agent.acceleration = baseAgentAccel * speedMult;
        }

        ngpApplied = true;
    }

    void Start()
    {
        if (SaveManager.IsLoadingFromSave)
            return;

        colorOrig = model.material.color;
        stoppingDistOrig = agent.stoppingDistance;
        startingPos = transform.position;

        if (gamemanager.instance != null && gamemanager.instance.IsWaveModeActive)
        {
            waveModeActive = true;
        }

        if (canJumpAttack)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;

            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            wasKinematic = rb.isKinematic;
        }

        if (gamemanager.instance != null && gamemanager.instance.player != null)
        {
            lastPlayerPosition = gamemanager.instance.player.transform.position;
        }

        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0 && agent != null && agent.isActiveAndEnabled)
            agent.SetDestination(patrolPoints[patrolIndex].position);

        if (itemDrop == null)
            itemDrop = GetComponent<ItemDrop>();

        if (!SaveManager.IsLoadingFromSave)
            currentHP = Mathf.Max(1, maxHP);

        ApplyNgpScalingIfNeeded();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy != gameObject)
            {
                Collider myCollider = GetComponent<Collider>();
                Collider enemyCollider = enemy.GetComponent<Collider>();

                if (myCollider != null && enemyCollider != null)
                {
                    Physics.IgnoreCollision(myCollider, enemyCollider, true);
                }
            }
        }
    }

    void Update()
    {
        if (!isAlive) return;

        shootTimer += Time.deltaTime;
        attackTimer += Time.deltaTime;
        jumpAttackTimer += Time.deltaTime;
        dashAttackTimer += Time.deltaTime;

        HandleShootCooldown();
        TrackPlayerVelocity();

        if (isDashing)
        {
            HandleDashMovement();
            return;
        }

        if (isJumpAttacking)
        {
            if (gamemanager.instance.player != null)
            {
                Vector3 lookDir = (gamemanager.instance.player.transform.position - transform.position);
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDir);
            }
            return;
        }

        if (!isJumpAttacking && !isDashing &&
            agent != null && agent.isActiveAndEnabled && agent.velocity.magnitude > 0.1f && !isPlayingStep)
        {
            StartCoroutine(playStep());
        }

        if (useAnimations && anim != null && !isJumpAttacking && !isDashing &&
            agent != null && agent.isActiveAndEnabled)
        {
            float agentSpeedCur = agent.velocity.magnitude;
            float agentSpeedAnim = anim.GetFloat("Speed");
            anim.SetFloat("Speed", Mathf.Lerp(agentSpeedAnim, agentSpeedCur, Time.deltaTime * animTransSpeed));
        }

        if (!isJumpAttacking && !isDashing &&
            agent != null && agent.isActiveAndEnabled && agent.remainingDistance < 0.01f)
            roamTimer += Time.deltaTime;

        bool shouldTarget = ShouldTargetPlayer();

        if (shouldTarget)
        {
            roamTimer = 0;

            if (!isJumpAttacking && !isDashing)
            {
                HandleMobileBehavior();
            }
        }
        else
        {
            checkRoamOrPatrol();
        }
    }

    void HandleMobileBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, gamemanager.instance.player.transform.position);
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

        if (isJumpAttacking || isDashing) return;

        if (ShouldTargetPlayer())
        {
            if (agent != null && agent.isActiveAndEnabled)
            {
                try
                {
                    agent.SetDestination(gamemanager.instance.player.transform.position);
                    agent.stoppingDistance = stoppingDistOrig;
                }
                catch (System.Exception)
                {

                }
            }

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

            if (agent != null && agent.isActiveAndEnabled && agent.remainingDistance <= agent.stoppingDistance)
                faceTarget();
        }
        else
        {
            checkRoamOrPatrol();
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
        if (gamemanager.instance == null || gamemanager.instance.player == null) return false;

        Vector3 playerPos = gamemanager.instance.player.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
        float heightDifference = playerPos.y - transform.position.y;

        return (heightDifference >= minHeightDifference || distanceToPlayer <= jumpAttackRange * 0.5f)
               && distanceToPlayer <= jumpAttackRange;
    }

    bool ShouldDashAttack()
    {
        if (!canDashAttack || isDashing || isJumpAttacking) return false;
        if (enemyType != EnemyType.Melee && enemyType != EnemyType.Hybrid) return false;
        if (dashAttackTimer < dashAttackCooldown) return false;
        if (gamemanager.instance.player == null) return false;

        Vector3 playerPos = gamemanager.instance.player.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);

        if (distanceToPlayer < minDashDistance || distanceToPlayer > dashAttackRange) return false;

        Vector3 directionToEnemy = (transform.position - playerPos).normalized;
        float fleeingSpeed = Vector3.Dot(playerVelocity, directionToEnemy);

        return fleeingSpeed < -playerFleeingThreshold;
    }

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

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        if (aud != null && audJumpWindup != null)
            aud.PlayOneShot(audJumpWindup, audJumpWindupVol);

        if (useAnimations && anim != null)
        {
            anim.SetBool("IsJumping", true);
            anim.SetBool("IsAscending", true);
        }

        Vector3 lookDir = new Vector3(jumpTarget.x - transform.position.x, 0, jumpTarget.z - transform.position.z);
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);

        if (jumpCoroutine != null) StopCoroutine(jumpCoroutine);
        jumpCoroutine = StartCoroutine(PerformArcJumpAttack());
    }

    Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float apexHeight)
    {
        float g = Physics.gravity.y;

        float highest = Mathf.Max(start.y, target.y) + apexHeight;

        float timeUp = Mathf.Sqrt(2f * (highest - start.y) / -g);

        float timeDown = Mathf.Sqrt(2f * (highest - target.y) / -g);

        float totalTime = timeUp + timeDown;
        if (totalTime <= 0.001f) totalTime = 0.5f;

        Vector3 horizontalDisplacement = new Vector3(target.x - start.x, 0, target.z - start.z);
        Vector3 horizontalVelocity = horizontalDisplacement / totalTime;

        float verticalVelocity = Mathf.Sqrt(-2f * g * (highest - start.y));

        Vector3 result = horizontalVelocity + Vector3.up * verticalVelocity;
        return result;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isJumpAttacking) return;

        if (collision.collider.CompareTag("Enemy") || collision.collider.isTrigger) return;

        if (((1 << collision.gameObject.layer) & ignoreLayer) != 0) return;

        if (!hasLanded)
        {
            hasLanded = true;

            if (audJumpLanding != null && aud != null)
                aud.PlayOneShot(audJumpLanding, audJumpLandingVol);

            if (jumpLandingEffect != null)
                Instantiate(jumpLandingEffect, transform.position, Quaternion.identity);

            StartCoroutine(ApplyJumpDamage());

            StartCoroutine(EndJumpAfterDelay(0.2f));
        }
    }

    IEnumerator EndJumpAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (agent != null)
        {
            try
            {
                agent.enabled = true;
                agent.Warp(transform.position);
                agent.isStopped = false;
            }
            catch (System.Exception)
            {
            }
        }

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        isJumpAttacking = false;
        hasLanded = false;
    }

    IEnumerator PerformArcJumpAttack()
    {
        if (gamemanager.instance == null || gamemanager.instance.player == null)
        {
            CancelJumpAttack();
            yield break;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, jumpWindupTime));

        if (aud != null && audJumpAttack != null && audJumpAttack.Length > 0)
            aud.PlayOneShot(audJumpAttack[Random.Range(0, audJumpAttack.Length)], audJumpAttackVol);

        Vector3 startPos = transform.position;
        Vector3 playerPos = gamemanager.instance.player.transform.position;

        Vector3 toPlayer = (playerPos - startPos);
        toPlayer.y = 0;
        Vector3 desiredEnd = playerPos;
        if (toPlayer.sqrMagnitude > 0.001f)
            desiredEnd = playerPos - toPlayer.normalized * Mathf.Max(0.1f, landingDistanceFromPlayer);

        Vector3 finalLandingPos = FindGroundPosition(desiredEnd);

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

        if (finalLandingPos == Vector3.zero)
            finalLandingPos = startPos;

        Vector3 navLanding = finalLandingPos;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(finalLandingPos, out navHit, 2.0f, NavMesh.AllAreas))
            navLanding = navHit.position;

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
        float maxDuration = Mathf.Max(5f, actualJumpDuration * 2f);

        while (elapsedTime < actualJumpDuration && isJumpAttacking && elapsedTime < maxDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / actualJumpDuration);

            float horizontalProgress = normalizedTime;
            Vector3 horizontalPosition = startPos + horizontalNormalized * (horizontalDistance * horizontalProgress);

            float verticalOffset = (jumpCurve != null ? jumpCurve.Evaluate(normalizedTime) : normalizedTime) * jumpHeight;
            Vector3 newPosition = horizontalPosition;
            newPosition.y = startPos.y + verticalOffset;

            transform.position = newPosition;

            if (gamemanager.instance != null && gamemanager.instance.player != null)
            {
                Vector3 lookDirection = gamemanager.instance.player.transform.position - transform.position;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            yield return null;
        }

        if (!isJumpAttacking)
            yield break;

        transform.position = navLanding;

        LandJumpAttack();

        if (agent != null)
        {
            try
            {
                if (!agent.isActiveAndEnabled) agent.enabled = true;
                agent.Warp(transform.position);
                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = true;
                if (gamemanager.instance != null && gamemanager.instance.player != null && isAlive)
                    agent.SetDestination(gamemanager.instance.player.transform.position);
            }
            catch (System.Exception)
            {
            }
        }

        jumpCoroutine = null;
    }

    Vector3 FindGroundPosition(Vector3 position)
    {
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

        RaycastHit sphereHit;
        if (Physics.SphereCast(position + Vector3.up * 5f, 1f, Vector3.down, out sphereHit, 10f, ~ignoreLayer))
        {
            if (sphereHit.collider != null && sphereHit.collider.gameObject != gameObject && !sphereHit.collider.CompareTag("Enemy"))
                return sphereHit.point;
        }

        return Vector3.zero;
    }

    void StartDashAttack()
    {
        if (isDashing || gamemanager.instance.player == null) return;

        isDashing = true;
        dashAttackTimer = 0f;
        dashTimeRemaining = dashDuration;

        Vector3 playerPos = gamemanager.instance.player.transform.position;
        Vector3 rayDirection = (playerPos - transform.position).normalized;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, rayDirection, out hit, dashAttackRange * 2f, ~ignoreLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
                dashDirection = rayDirection;
            }
            else
            {
                dashDirection = rayDirection;
            }
        }
        else
        {
            dashDirection = rayDirection;
        }

        dashDirection.y = 0;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (useAnimations && anim != null)
        {
            anim.SetFloat("Speed", dashSpeed);
        }

        if (audDash.Length > 0 && aud != null)
            aud.PlayOneShot(audDash[Random.Range(0, audDash.Length)], audDashVol);

        if (dashDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dashDirection);
    }

    void HandleDashMovement()
    {
        if (!isDashing) return;

        dashTimeRemaining -= Time.deltaTime;

        if (gamemanager.instance.player != null)
        {
            Vector3 playerPos = gamemanager.instance.player.transform.position;
            Vector3 rayDirection = (playerPos - transform.position).normalized;

            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, rayDirection, out hit, dashAttackRange * 2f, ~ignoreLayer))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    dashDirection = Vector3.Slerp(dashDirection, rayDirection, 10f * Time.deltaTime);
                    dashDirection.y = 0;
                    dashDirection.Normalize();
                }
            }

            if (dashDirection != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dashDirection);
        }

        if (!CheckWallCollision())
        {
            Vector3 movement = dashDirection * dashSpeed * Time.deltaTime;
            transform.position += movement;
        }
        else
        {
            EndDash();
            return;
        }

        if (useAnimations && anim != null)
            anim.SetFloat("Speed", dashSpeed);

        if (meleePos != null)
        {
            Collider[] hits = Physics.OverlapSphere(meleePos.position, meleeRange, ~ignoreLayer);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy") && hit.gameObject != gameObject)
                    continue;

                if (hit.CompareTag("Player"))
                {
                    IDamage dmg = hit.GetComponent<IDamage>();
                    if (dmg != null)
                    {
                        dmg.takeDamage(GetScaledDamage(baseMeleeDamage));
                        EndDash();
                        return;
                    }
                }
            }
        }

        if (dashTimeRemaining <= 0)
        {
            EndDash();
        }
    }

    bool CheckWallCollision()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dashDirection, out hit, 1f, ~ignoreLayer))
        {
            if (!hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
                return true;
        }
        return false;
    }

    void EndDash()
    {
        if (!isDashing) return;

        isDashing = false;

        if (useAnimations && anim != null)
            anim.SetFloat("Speed", 0);

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;

            if (gamemanager.instance.player != null && isAlive)
            {
                try
                {
                    agent.SetDestination(gamemanager.instance.player.transform.position);
                }
                catch (System.Exception)
                {
                }
            }
        }
    }

    void LandJumpAttack()
    {
        if (hasLanded || !isJumpAttacking) return;

        hasLanded = true;

        if (aud != null && audJumpLanding != null)
            aud.PlayOneShot(audJumpLanding, audJumpLandingVol);

        if (jumpLandingEffect != null)
            Instantiate(jumpLandingEffect, transform.position, Quaternion.identity);

        StartCoroutine(ApplyJumpDamage());
    }

    IEnumerator ApplyJumpDamage()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, jumpDamageDelay));

        if (gamemanager.instance == null || gamemanager.instance.player == null)
        {
        }
        else
        {
            bool playerDamaged = false;
            Vector3 playerPos = gamemanager.instance.player.transform.position;
            float distanceToPlayer = Vector3.Distance(transform.position, playerPos);

            if (distanceToPlayer <= jumpAttackRadius * 1.2f)
            {
                IDamage dmg = gamemanager.instance.player.GetComponent<IDamage>();
                if (dmg != null)
                {
                    dmg.takeDamage(GetScaledDamage(baseJumpAttackDamage));
                    playerDamaged = true;
                }
            }

            if (!playerDamaged)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, jumpAttackRadius, ~ignoreLayer);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Enemy") && hit.gameObject != gameObject)
                        continue;

                    if (hit != null && hit.CompareTag("Player"))
                    {
                        IDamage dmg = hit.GetComponent<IDamage>();
                        if (dmg != null)
                        {
                            dmg.takeDamage(GetScaledDamage(baseJumpAttackDamage));
                            playerDamaged = true;
                            break;
                        }
                    }
                }
            }

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
                            dmg.takeDamage(GetScaledDamage(baseJumpAttackDamage));
                            playerDamaged = true;
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        if (useAnimations && anim != null)
        {
            anim.SetBool("IsJumping", false);
            anim.SetBool("IsAscending", false);
        }

        EndJumpAttack();
    }

    void EndJumpAttack()
    {
        if (!isJumpAttacking) return;

        isJumpAttacking = false;
        hasLanded = false;

        if (useAnimations && anim != null)
        {
            anim.SetBool("IsJumping", false);
            anim.SetBool("IsAscending", false);
        }

        if (agent != null)
        {
            try
            {
                if (!agent.isActiveAndEnabled) agent.enabled = true;
                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = true;

                if (gamemanager.instance != null && gamemanager.instance.player != null && isAlive)
                    agent.SetDestination(gamemanager.instance.player.transform.position);
            }
            catch (System.Exception)
            {
            }
        }

        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }
    }

    void CancelJumpAttack()
    {
        if (!isJumpAttacking) return;

        isJumpAttacking = false;
        hasLanded = false;

        if (useAnimations && anim != null)
        {
            anim.SetBool("IsJumping", false);
            anim.SetBool("IsAscending", false);
        }

        if (agent != null)
        {
            try
            {
                if (!agent.isActiveAndEnabled) agent.enabled = true;
                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = true;
            }
            catch (System.Exception)
            {
            }
        }

        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }
    }

    IEnumerator playStep()
    {
        isPlayingStep = true;
        if (audStep.Length > 0 && aud != null)
        {
            aud.PlayOneShot(audStep[Random.Range(0, audStep.Length)], audStepVol);
        }
        yield return new WaitForSeconds(0.5f);
        isPlayingStep = false;
    }

    void checkRoamOrPatrol()
    {
        if (canSeePlayer()) return;

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
        }
    }

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

                if (MusicManager.Instance != null)
                    MusicManager.Instance.ReportCombat();

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
                if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
                {
                    try
                    {
                        agent.SetDestination(playerPos);
                        agent.stoppingDistance = stoppingDistOrig;
                    }
                    catch (System.Exception)
                    {
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
            if (agent != null && agent.isActiveAndEnabled && !isJumpAttacking && !isDashing)
                agent.stoppingDistance = 0;
        }
    }

    public void takeDamage(int amount)
    {
        if (!isAlive) return;

        currentHP -= amount;

        if (isDashing || isJumpAttacking)
        {
            StartCoroutine(flashSpecialAttackColor());
        }
        else
        {
            StartCoroutine(flashRed());
        }

        if (gamemanager.instance.player != null && agent != null && agent.isActiveAndEnabled)
        {
            try
            {
                agent.SetDestination(gamemanager.instance.player.transform.position);
            }
            catch (System.Exception)
            {
            }
        }

        if (audHurt.Length > 0 && aud != null)
            aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    IEnumerator flashSpecialAttackColor()
    {
        model.material.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;
    }

    private void Die()
    {
        if (!isAlive) return;

        isAlive = false;

        if (isDashing) EndDash();
        if (isJumpAttacking) CancelJumpAttack();

        if (audDeath.Length > 0 && aud != null)
            aud.PlayOneShot(audDeath[Random.Range(0, audDeath.Length)], audDeathVol);

        if (useAnimations && useDeathAnimation && anim != null)
        {
            SafeSetBool("IsJumping", false);
            SafeSetBool("GoingUp", false);

            SafeSetBool("IsDead", true);
        }

        HandleDeathRewards();

        StartCoroutine(DestroyAfterDeathAnimation());
    }
    private void HandleDeathRewards()
    {
        if (isBoss)
        {
            if (bossDeathEffect != null)
                Instantiate(bossDeathEffect, transform.position, Quaternion.identity);

            gamemanager.instance.OnLevel4BossDefeated();

            if (itemDrop != null)
                itemDrop.TryDrop();

            if (currencyDropAmount > 0 && RiftShardManager.Instance != null)
            {
                RiftShardManager.Instance.Add(currencyDropAmount * 5);
            }
        }
        else
        {
            gamemanager.instance.updateGameGoal(-1);

            if (itemDrop != null)
                itemDrop.TryDrop();

            if (currencyDropAmount > 0 && RiftShardManager.Instance != null)
            {
                RiftShardManager.Instance.Add(currencyDropAmount);
            }
        }
    }

    private IEnumerator DestroyAfterDeathAnimation()
    {
        if (useAnimations && useDeathAnimation && anim != null)
        {
            yield return new WaitForSeconds(deathAnimationDuration);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        Destroy(gameObject);
    }
    public void OnDeathAnimationComplete()
    {
        Destroy(gameObject);
    }

    IEnumerator flashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = colorOrig;
    }

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

    public void createProjectile()
    {
        GameObject proj = Instantiate(projectile, shootPOS.position, shootPOS.rotation);

        float dmgMult = GetNgpDamageMultiplier();

        var dmg = proj.GetComponent<damage>();
        if (dmg != null)
            dmg.ApplyDamageMultiplier(dmgMult);
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
        Collider[] hitColliders = Physics.OverlapSphere(meleePos.position, meleeRange, ~ignoreLayer);

        int scaledMeleeDamage = GetScaledDamage(baseMeleeDamage);

        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Enemy") && hit.gameObject != gameObject)
                continue;

            if (hit.isTrigger)
                continue;

            IDamage dmgTarget = hit.GetComponent<IDamage>();
            if (dmgTarget != null)
            {
                dmgTarget.takeDamage(meleeDamageAmount);

                if (meleeDamage != null)
                    Instantiate(meleeDamage, hit.transform.position, Quaternion.identity);
            }
        }
    }

    public void SetPatrolPoints(Transform[] points, string sourceId = null)
    {
        patrolPoints = points;
        patrolIndex = 0;

        if (!string.IsNullOrEmpty(sourceId))
            patrolSourceId = sourceId;

        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0 && agent != null && agent.isActiveAndEnabled)
            agent.SetDestination(patrolPoints[0].position);
    }

    void OnDrawGizmosSelected()
    {
        if (canJumpAttack)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, jumpAttackRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, jumpAttackRadius);
        }
    }

    [System.Serializable]
    private struct EnemyState
    {
        public int hp;
        public int maxHp;
        public Vector3 pos;
        public Vector3 startingPos;
        public float roamTimer;
        public int patrolIndex;
        public bool hasDestination;
        public Vector3 destination;
        public string patrolSourceId;
        public float jumpAttackTimer;
        public float dashAttackTimer;
    }

    public object CaptureState()
    {
        var state = new EnemyState
        {
            hp = currentHP,
            maxHp = maxHP,
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

    public void RestoreState(object state)
    {
        if (state is not EnemyState s)
        {
            return;
        }

        jumpAttackTimer = s.jumpAttackTimer;
        dashAttackTimer = s.dashAttackTimer;

        if (agent != null)
            agent.Warp(s.pos);
        else
            transform.position = s.pos;

        maxHP = Mathf.Max(1, s.maxHp);
        currentHP = Mathf.Clamp(s.hp, 0, maxHP);

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

        if (patrolPoints != null && patrolPoints.Length > 0)
            patrolIndex = Mathf.Clamp(s.patrolIndex, 0, patrolPoints.Length - 1);
        else
            patrolIndex = 0;

        if (agent != null && s.hasDestination)
        {
            agent.SetDestination(s.destination);
        }

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

    public void SetWaveModeRange(float newRange)
    {
        waveModeRange = newRange;
    }

    bool ShouldTargetPlayer()
    {
        if (gamemanager.instance.player == null) return false;

        if (canSeePlayer())
            return true;

        if (waveModeActive)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, gamemanager.instance.player.transform.position);
            if (distanceToPlayer <= waveModeRange)
                return true;
        }

        return playerInTrigger;
    }

    bool CanShootAtPlayer()
    {
        if (gamemanager.instance == null || gamemanager.instance.player == null) return false;

        if (shootTimer < shootRate) return false;

        Vector3 checkPosition = headPos != null ? headPos.position : transform.position;
        float distanceToPlayer = Vector3.Distance(checkPosition, gamemanager.instance.player.transform.position);
        if (distanceToPlayer > sightRange) return false;

        return HasLineOfSightToPlayer();
    }

    bool HasLineOfSightToPlayer()
    {
        if (gamemanager.instance == null || gamemanager.instance.player == null) return false;

        if (enemyType == EnemyType.Melee) return false;

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

    void HandleShootCooldown()
    {
        if (gamemanager.instance == null || gamemanager.instance.player == null) return;

        if (!HasLineOfSightToPlayer() && shootTimer > shootRate * 2f)
        {
            shootTimer = shootRate;
        }
    }

    private void SafeSetBool(string paramName, bool value)
    {
        if (anim == null) return;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool(paramName, value);
                return;
            }
        }
    }
}