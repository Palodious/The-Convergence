using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour, IDamage
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Renderer model;
    [SerializeField] Transform headPOS;

    [SerializeField] int HP;
    [SerializeField] int FOV;
    [SerializeField] int faceTargetSpeed;

    [SerializeField] GameObject Projectile;
    [SerializeField] float shootRate;
    [SerializeField] Transform shootPOS;
    [SerializeField] bool shootEnabled;

    [SerializeField] bool meleeEnabled;
    [SerializeField] GameObject melee;
    [SerializeField] float meleeRate;
    [SerializeField] Transform meleePOS;
    [SerializeField] float meleeRange;

    [SerializeField] bool patrolEnabled;
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] float patrolSpeed;
    [SerializeField] float patrolWaitTime;

    [SerializeField] bool shieldEnabled;
    [SerializeField] int shieldMax;
    [SerializeField] GameObject shieldVFX;
    [SerializeField] bool shieldRegenEnabled;
    [SerializeField] float shieldRegenDelay;
    [SerializeField] int shieldRegenAmount;
    [SerializeField] float shieldRegenRate;
    [SerializeField] bool shieldBreakEnabled;
    [SerializeField] float shieldBrakDuration;
    [SerializeField] bool shieldBreakDisableOnBreak;


    Color colorOrig;

    bool playerInTrigger;

    float shootTimer;
    float angleToPlayer;
    float stoppingDistOrig;

    Vector3 playerDir;

    int shieldCurrent;
    bool isShieldBroken;
    GameObject shieldInstance;
    Coroutine regenCoroutine;
    Coroutine breakCoroutine;

    int patrolIndex;
    float agentSpeedOrig;
    bool isWaitingPatrol;

    float meleeTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        colorOrig = model.material.color;
        stoppingDistOrig = agent.stoppingDistance;
        agentSpeedOrig = agent.speed;

        shieldCurrent = shieldMax;

    }

    // Update is called once per frame
    void Update()
    {
        shootTimer += Time.deltaTime;

        if (playerInTrigger && canSeePlayer())
        {

        }
        else
        {
            // >>> ADDED: Patrol handling
            if (patrolEnabled && patrolPoints != null && patrolPoints.Length > 0)
            {
                Patrol();
            }
            else
            {
                agent.SetDestination(transform.position);
            }
        }
    }

    bool canSeePlayer()
    {
        playerDir = gamemanager.instance.player.transform.position - headPOS.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        Debug.DrawRay(headPOS.position, playerDir);

        RaycastHit hit;
        if (Physics.Raycast(headPOS.position, playerDir, out hit))
        {
            Debug.Log(hit.collider.name);

            if (angleToPlayer <= FOV && hit.collider.CompareTag("Player"))
            {
                agent.SetDestination(gamemanager.instance.player.transform.position);

                if (shootTimer >= shootRate)
                {
                    shoot();
                }
                if (agent.remainingDistance <= stoppingDistOrig)
                    faceTarget();

                return true;
            }
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
        {
            playerInTrigger = true;
            // agent.stoppingDistance = 1.5f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            // agent.stoppingDistance = stoppingDistOrig;
        }
    }

    public void takeDamage(int amount)
    {
        HP -= amount;

        if (HP <= 0)
        {
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

        Instantiate(Projectile, shootPOS.position, shootPOS.rotation);
    }
    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (isWaitingPatrol)
            return;

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            StartCoroutine(PatrolWait());
        }
    }

    IEnumerator PatrolWait()
    {
        isWaitingPatrol = true;

        if (patrolWaitTime > 0)
            yield return new WaitForSeconds(patrolWaitTime);

        agent.speed = patrolSpeed;
        agent.SetDestination(patrolPoints[patrolIndex].position);

        patrolIndex++;
        if (patrolIndex >= patrolPoints.Length)
            patrolIndex = 0;

        isWaitingPatrol = false;
    }
}