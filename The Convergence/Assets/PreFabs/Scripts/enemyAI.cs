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
    [SerializeField] int animTransSpeed;

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
    float stoppingDistOrig;
    Vector3 playerDir;
    Vector3 startingPos;

    [SerializeField] Transform[] patrolPoints; // Optional patrol points
    int patrolIndex = 0;

    void Start()
    {
        colorOrig = model.material.color;
        gamemanager.instance.updateGameGoal(1);
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
        roamTimer += Time.deltaTime;

        // Update movement animation speed if enabled
        if (useAnimations && anim != null)
        {
            float agentSpeedCur = agent.velocity.magnitude;
            float agentSpeedAnim = anim.GetFloat("Speed");
            anim.SetFloat("Speed", Mathf.Lerp(agentSpeedAnim, agentSpeedCur, Time.deltaTime * animTransSpeed));
        }

        bool playerVisible = canSeePlayer();

        if (playerVisible)
        {
            agent.SetDestination(gamemanager.instance.player.transform.position);

            float distanceToPlayer = Vector3.Distance(transform.position, gamemanager.instance.player.transform.position);

            // Melee has priority
            if (enemyType == EnemyType.Melee && distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                meleeAttack();
            else if (enemyType == EnemyType.Shooter && shootTimer >= shootRate)
                shoot();
            else if (enemyType == EnemyType.Hybrid)
            {
                if (distanceToPlayer <= meleeRange && attackTimer >= attackRate)
                    meleeAttack();
                else if (shootTimer >= shootRate)
                    shoot();
            }

            if (agent.remainingDistance <= stoppingDistOrig)
                faceTarget();
        }
        else
        {
            if (useRoam) checkRoam();
            if (usePatrol) checkPatrol();
        }
    }

    void checkRoam()
    {
        if (agent.remainingDistance < 0.01f && roamTimer >= roamPauseTime)
            roam();
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
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (playerDir.magnitude > sightRange) return false; // too far

        RaycastHit hit;
        if (Physics.Raycast(headPos.position, playerDir.normalized, out hit, sightRange))
        {
            if (hit.collider.CompareTag("Player") && angleToPlayer <= FOV)
                return true;
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
            anim.SetTrigger("Shoot");
        else
            createProjectile();
    }

    public void createProjectile()
    {
        Instantiate(projectile, shootPOS.position, transform.rotation);
    }

    void meleeAttack()
    {
        attackTimer = 0;

        if (useAnimations && anim != null)
            anim.SetTrigger("Punch");
        else if (useAnimations && anim != null)
            anim.SetTrigger("Claw");
    }

    public void ApplyMeleeDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(meleePos.position, meleeRange);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                gamemanager.instance.controller.takeDamage(meleeDamage);

                if (meleeEffect != null)
                    Instantiate(meleeEffect, meleePos.position, Quaternion.identity);
            }
        }
    }
}