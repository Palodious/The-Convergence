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
    [SerializeField] GameObject meleeA;
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
    [SerializeField] float shieldBreakDuration;
    [SerializeField] bool shieldBreakDisableOnBreak;


    Color colorOrig;
    Color shieldOrigColor;

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
        if (shieldVFX != null)
        {
            shieldInstance = Instantiate(shieldVFX, transform.position, Quaternion.identity, transform);
            shieldInstance.transform.localPosition = Vector3.zero;
            shieldInstance.SetActive(shieldEnabled && shieldCurrent > 0 && !isShieldBroken);

            Renderer r = shieldInstance.GetComponent<Renderer>();
            if (r != null)
                shieldOrigColor = r.material.color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        shootTimer += Time.deltaTime;
        meleeTimer += Time.deltaTime;

        if (playerInTrigger && canSeePlayer())
        {
            
        }
        else
        {
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
                agent.stoppingDistance = stoppingDistOrig;
                agent.SetDestination(gamemanager.instance.player.transform.position);

                float distanceToPlayer = Vector3.Distance(transform.position, gamemanager.instance.player.transform.position);

                if (meleeEnabled && meleeTimer >= meleeRate && distanceToPlayer <= meleeRange)   // melee priority
                {
                    melee();
                }
                else if (shootEnabled && shootTimer >= shootRate && distanceToPlayer > meleeRange) // shoot only if not melee range
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
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
        }
    }

    public void takeDamage(int amount)
    {
        if (shieldEnabled && !isShieldBroken && shieldCurrent > 0)
        {
            int left = amount - shieldCurrent;
            shieldCurrent -= amount;

            if (shieldInstance != null)
            {
                Renderer r = shieldInstance.GetComponent<Renderer>();
                if (r != null)
                    StartCoroutine(shieldFlash(r));
            }

            if (shieldCurrent <= 0)
            {
                shieldCurrent = 0;
                OnShieldBroken();

                if (left > 0)
                    HP -= left;
            }

            if (shieldInstance != null)
                shieldInstance.SetActive(shieldCurrent > 0 && !isShieldBroken);

            if (shieldRegenEnabled && regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
                regenCoroutine = null;
            }
            if (shieldRegenEnabled && !isShieldBroken)
                regenCoroutine = StartCoroutine(ShieldRegenDelay());

            StartCoroutine(flashRed());
            return;
        }
        // <<< END ADD

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

    IEnumerator shieldFlash(Renderer r)
    {
        r.material.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        r.material.color = shieldOrigColor;
    }

    void OnShieldBroken()
    {
        if (!shieldEnabled)
            return;

        if (shieldBreakEnabled)
        {
            if (breakCoroutine != null)
                StopCoroutine(breakCoroutine);

            breakCoroutine = StartCoroutine(DoShieldBreak());
        }
        else
        {
            if (shieldInstance != null)
                shieldInstance.SetActive(false);
        }
    }

    IEnumerator DoShieldBreak()
    {
        isShieldBroken = true;

        if (shieldInstance != null)
            shieldInstance.SetActive(false);

        if (shieldBreakDisableOnBreak && regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

        yield return new WaitForSeconds(shieldBreakDuration);

        shieldCurrent = shieldMax;
        isShieldBroken = false;

        if (shieldInstance != null)
            shieldInstance.SetActive(true);

        if (shieldRegenEnabled && regenCoroutine == null)
            regenCoroutine = StartCoroutine(ShieldRegenDelay());
    }

    IEnumerator ShieldRegenDelay()
    {
        yield return new WaitForSeconds(shieldRegenDelay);

        while (shieldCurrent < shieldMax && shieldRegenEnabled && !isShieldBroken)
        {
            shieldCurrent += shieldRegenAmount;
            if (shieldCurrent > shieldMax)
                shieldCurrent = shieldMax;

            if (shieldInstance != null)
                shieldInstance.SetActive(shieldCurrent > 0 && !isShieldBroken);

            yield return new WaitForSeconds(shieldRegenRate);
        }

        regenCoroutine = null;
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

    void melee()
    {
        meleeTimer = 0;
        Instantiate(meleeA, meleePOS.position, meleePOS.rotation);
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