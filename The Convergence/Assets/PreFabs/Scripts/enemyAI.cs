using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour, IDamage, ISaveable
{
    public enum EnemyType
    {
        Melee,
        Shooter,
        Hybrid
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

    [Header("~=~= Stats =~=~")]
    [Range(1, 100)][SerializeField] int HP;
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

    bool isPlayingStep;

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

        // Play footsteps if moving
        if (agent.velocity.magnitude > 0.1f && !isPlayingStep)
        {
            StartCoroutine(playStep());
        }

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
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    bool canSeePlayer()
    {
        Vector3 playerPos = gamemanager.instance.player.transform.position;
        playerDir = playerPos - headPos.position;
        float distanceToPlayer = playerDir.magnitude;

        if (distanceToPlayer > sightRange)
        {
            agent.stoppingDistance = 0;
            return false;
        }

        angleToPlayer = Vector3.Angle(playerDir, transform.forward);
        if (angleToPlayer > FOV)
        {
            agent.stoppingDistance = 0;
            return false;
        }

        RaycastHit hit;
        if (Physics.Raycast(headPos.position, playerDir.normalized, out hit, sightRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
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

        if (audHurt.Length > 0 && aud != null)
            aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);

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
        Instantiate(projectile, shootPOS.position, transform.rotation);
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

        foreach (var hit in hitColliders)
        {
            IDamage dmgTarget = hit.GetComponent<IDamage>();
            if (dmgTarget != null && meleeDamage != null)
            {
                Instantiate(meleeDamage, meleePos.position, Quaternion.identity);
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

    [System.Serializable]
    private struct EnemyState
    {
        public int hp;
        public Vector3 pos;
    }

    public object CaptureState()
    {
        return new EnemyState
        {
            hp = HP,
            pos = transform.position
        };
    }

    public void RestoreState(object state)
    {
        EnemyState s = (EnemyState)state;

        if (agent != null)
            agent.Warp(s.pos);
        else
            transform.position = s.pos;

        HP = s.hp;

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }
}