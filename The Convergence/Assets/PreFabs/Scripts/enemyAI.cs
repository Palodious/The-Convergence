using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour, IDamage
{
    public enum EnemyType
    {
        Melee,
        Shooter,
        Hybrid
    }

    [SerializeField] EnemyType enemyType;

    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator anim;
    [SerializeField] Renderer model;
    [SerializeField] Transform headPos;

    [SerializeField] int HP;
    [SerializeField] int FOV;
    [SerializeField] int faceTargetSpeed;
    [SerializeField] int roamDist;
    [SerializeField] int roamPauseTime;
    [SerializeField] float animTransSpeed;

    [SerializeField] GameObject projectile;
    [SerializeField] float shootRate;
    [SerializeField] Transform shootPOS;

    [SerializeField] Transform meleePos; // Position from which melee attacks are measured
    [SerializeField] GameObject meleeEffect;  // Optional visual effect for punches
    [SerializeField] float meleeRange; // Distance at which enemy can hit player
    [SerializeField] float attackRate;  // Cooldown between attacks
    [SerializeField] int meleeDamage; // Damage per punch

    public bool useAnimations = true; // Toggle all animation logic on/off
    public bool usePatrol = true; // Toggle patrol behavior
    public bool useRoam = true;  // Toggle roaming behavior
    public EnemyType EnemyTypeValue => enemyType;

    Color colorOrig;
    float sightRange = 20f; // max distance enemy can see
    bool playerInTrigger;
    float shootTimer;
    float attackTimer;
    float roamTimer;
    float angleToPlayer;
    Vector3 playerDir;
    Vector3 startingPos;
    float stoppingDistOrig;

    [SerializeField] Transform[] patrolPoints; // Optional patrol points
    int patrolIndex = 0;

    void Start()
    {
        colorOrig = model.material.color;
        stoppingDistOrig = agent.stoppingDistance;
        startingPos = transform.position;

        // Initialize patrol by setting the first patrol point as the destination
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    void Update()
    {
        shootTimer += Time.deltaTime;
        attackTimer += Time.deltaTime;

        // Update movement animation speed if enabled
        if (useAnimations && anim != null)
        {
            float agentSpeedCur = agent.velocity.magnitude;
            float agentSpeedAnim = anim.GetFloat("Speed");
            anim.SetFloat("Speed", Mathf.Lerp(agentSpeedAnim, agentSpeedCur, Time.deltaTime * animTransSpeed));
        }

        // Track roam timer only when not moving
        if (agent.remainingDistance < 0.01f)
            roamTimer += Time.deltaTime;

        // Use playerInTrigger as primary condition
        if (playerInTrigger && !canSeePlayer())
        {
            checkRoamOrPatrol();
        }
        else if (!playerInTrigger)
        {
            checkRoamOrPatrol();
        }
    }

    void checkRoamOrPatrol()
    {
        // Combined roaming and patrol check
        if (agent.remainingDistance < 0.01f && roamTimer >= roamPauseTime)
        {
            if (useRoam)
                roam();
            else if (usePatrol)
                checkPatrol();
        }

        // If we have patrol points and should be patrolling, make sure we have a destination
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
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    bool canSeePlayer()
    {
        Vector3 playerPos = gamemanager.instance.player.transform.position;
        playerDir = playerPos - headPos.position;
        float distanceToPlayer = playerDir.magnitude;

        // Check distance first
        if (distanceToPlayer > sightRange)
        {
            agent.stoppingDistance = 0;
            return false;
        }

        // Check FOV
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);
        if (angleToPlayer > FOV)
        {
            agent.stoppingDistance = 0;
            return false;
        }

        // Raycast for line-of-sight only
        RaycastHit hit;
        if (Physics.Raycast(headPos.position, playerDir.normalized, out hit, sightRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                agent.SetDestination(playerPos);
                agent.stoppingDistance = stoppingDistOrig;

                // Attack logic handled separately
                switch (enemyType)
                {
                    case EnemyType.Melee:
                        if (distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                            meleeAttack();
                        break;

                    case EnemyType.Shooter:
                        if (shootTimer >= shootRate)
                            shoot();
                        break;

                    case EnemyType.Hybrid:
                        if (distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                            meleeAttack();
                        else if (shootTimer >= shootRate)
                            shoot();
                        break;
                }

                if (agent.remainingDistance <= agent.stoppingDistance)
                    faceTarget();

                return true;
            }
        }

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
            agent.stoppingDistance = 0;
        }
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        agent.SetDestination(gamemanager.instance.player.transform.position);

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
        if (useAnimations && anim != null)
        {
            anim.SetTrigger("Shoot");
        }
        else
        {
            createProjectile(); // If no animations
        }
    }

    public void createProjectile()
    {
        Instantiate(projectile, shootPOS.position, transform.rotation);
    }

    void meleeAttack()
    {
        attackTimer = 0;

        if (useAnimations && anim != null)
        {
            anim.SetTrigger("Punch");

            // ApplyMeleeDamage() needs to be called via Animation Event
        }
        else
        {
            ApplyMeleeDamage();
        }
    }

    public void ApplyMeleeDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(meleePos.position, meleeRange);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                gamemanager.instance.playerScript.takeDamage(meleeDamage);

                if (meleeEffect != null)
                    Instantiate(meleeEffect, meleePos.position, Quaternion.identity);
            }
        }
    }

    public void SetPatrolPoints(Transform[] points)
    {
        patrolPoints = points;
        patrolIndex = 0;

        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);
    }
}