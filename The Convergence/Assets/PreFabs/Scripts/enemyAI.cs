using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour, IDamage
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator anim;
    [SerializeField] Renderer model;
    [SerializeField] Transform headPos;

    [SerializeField] Collider weaponCol;

    [SerializeField] int HP;
    [SerializeField] int FOV;
    [SerializeField] int faceTargetSpeed;
    [SerializeField] int roamDist;
    [SerializeField] int roamPauseTime;
    [SerializeField] int animTransSpeed;

    [SerializeField] GameObject projectile;
    [SerializeField] float shootRate;
    [SerializeField] Transform shootPOS;

    [SerializeField] bool enableRoam; // Toggle roaming on/off in Inspector
    [SerializeField] bool enableAnimation; // Toggle animation on/off in Inspector

    [SerializeField] bool enablePatrol; // Toggle patrol on/off in Inspector
    [SerializeField] Transform[] patrolPoints; // Patrol points set in Inspector
    [SerializeField] float patrolPauseTime; // Pause time between patrol points

    [SerializeField] bool enableShooting; // Toggle shooting on/off in Inspector
    [SerializeField] float minShootRange; // Minimum distance required to shoot

    [SerializeField] bool enableMelee; // Toggle melee on/off in Inspector
    [SerializeField] float meleeRange; // Range to initiate melee attack
    [SerializeField] float meleeRate; // Cooldown time between melee attacks
    [SerializeField] GameObject meleeObj; // Melee GameObject to spawn
    [SerializeField] Transform meleePOS; // Melee position to spawn from

    Color colorOrig;

    bool playerInTrigger;
    bool waitingAtPatrolPoint;

    float shootTimer;
    float meleeTimer;
    float roamTimer;
    float angleToPlayer;
    float stoppingDistOrig;
    float patrolTimer;

    int currentPatrolIndex;

    Vector3 playerDir;
    Vector3 startingPos;

    void Start()
    {
        colorOrig = model.material.color;
        gamemanager.instance.updateGameGoal(1);
        stoppingDistOrig = agent.stoppingDistance;
        startingPos = transform.position;

        // Initialize patrol system if enabled and points exist
        if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            currentPatrolIndex = 0;
            agent.stoppingDistance = 0;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void Update()
    {
        shootTimer += Time.deltaTime;
        meleeTimer += Time.deltaTime;

        // Only handle animation logic if animation is enabled
        if (enableAnimation && anim != null)
        {
            float agentSpeedCur = agent.velocity.normalized.magnitude;
            float agentSpeedAnim = anim.GetFloat("Speed");
            anim.SetFloat("Speed", Mathf.Lerp(agentSpeedAnim, agentSpeedCur, Time.deltaTime * animTransSpeed));
        }

        if (agent.remainingDistance < 0.01f)
            roamTimer += Time.deltaTime;

        if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            handlePatrol();
        }
        else if (enableRoam) // Only check roam if roaming is on
        {
            if (playerInTrigger && !canSeePlayer())
            {
                checkRoam();
            }
            else if (!playerInTrigger)
            {
                checkRoam();
            }
        }
    }

    // Handles patrol logic if enabled
    void handlePatrol()
    {
        if (!enablePatrol || patrolPoints.Length == 0) return;

        if (!waitingAtPatrolPoint && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            waitingAtPatrolPoint = true;
            patrolTimer = 0;
        }

        if (waitingAtPatrolPoint)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolPauseTime)
            {
                waitingAtPatrolPoint = false;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }
    }

    void checkRoam()
    {
        if (!enableRoam) return; // Prevent roaming logic if off

        if (agent.remainingDistance < 0.01f && roamTimer >= roamPauseTime)
        {
            roam();
        }
    }

    void roam()
    {
        if (!enableRoam) return; // Double check before setting a roam destination

        roamTimer = 0;
        agent.stoppingDistance = 0;

        Vector3 ranPos = Random.insideUnitSphere * roamDist;
        ranPos += startingPos;

        NavMeshHit hit;
        NavMesh.SamplePosition(ranPos, out hit, roamDist, 1);
        agent.SetDestination(hit.position);
    }

    bool canSeePlayer()
    {
        playerDir = gamemanager.instance.player.transform.position - headPos.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        Debug.DrawRay(headPos.position, playerDir);

        RaycastHit hit;
        if (Physics.Raycast(headPos.position, playerDir, out hit))
        {
            if (angleToPlayer <= FOV && hit.collider.CompareTag("Player"))
            {
                agent.SetDestination(gamemanager.instance.player.transform.position);

                float distanceToPlayer = Vector3.Distance(transform.position, gamemanager.instance.player.transform.position);

                // Check if shooting is enabled and player is within range
                if (enableShooting && shootTimer >= shootRate && distanceToPlayer >= minShootRange)
                {
                    shoot();
                }

                // Check if melee is enabled and player is within melee range
                if (enableMelee && meleeTimer >= meleeRate && distanceToPlayer <= meleeRange)
                {
                    melee();
                }

                if (agent.remainingDistance <= stoppingDistOrig)
                    faceTarget();

                agent.stoppingDistance = stoppingDistOrig;
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
        {
            playerInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            agent.stoppingDistance = 0;

            // Resume patrol if enabled after player leaves trigger
            if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
            {
                waitingAtPatrolPoint = false;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
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

        // Only trigger shoot animation if enabled
        if (enableAnimation && anim != null)
            anim.SetTrigger("Shoot");
    }

    void melee()
    {
        meleeTimer = 0;

        // Only trigger melee animation if enabled
        if (enableAnimation && anim != null)
            anim.SetTrigger("Punch");
    }

    public void createProjectile()
    {
        Instantiate(projectile, shootPOS.position, transform.rotation);
    }

    public void createMelee()
    {
        if (meleeObj != null && meleePOS != null)
        {
            Instantiate(meleeObj, meleePOS.position, transform.rotation);
        }
    }

    public void weaponColOn()
    {
        weaponCol.enabled = true;
    }

    public void weaponColOff()
    {
        weaponCol.enabled = false;
    }
}