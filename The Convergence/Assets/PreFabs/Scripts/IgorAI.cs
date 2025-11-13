using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class IgorAI : MonoBehaviour, IDamage
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator anim;
    [SerializeField] Renderer model;
    [SerializeField] Transform headPos;

    [SerializeField] Collider weaponCol;

    [SerializeField] int HP;
    [SerializeField] float meleeRange;
    [SerializeField] float meleeRate;
    [SerializeField] Transform meleePOS;
    [SerializeField] GameObject meleeObj;

    [SerializeField] int faceTargetSpeed;
    [SerializeField] int roamDist;
    [SerializeField] int roamPauseTime;
    [SerializeField] int animTransSpeed;

    Color originalColor;

    bool playerInTrigger;

    float meleeTimer;
    float roamTimer;
    float angleToPlayer;
    float stoppingDistOrig;

    Vector3 playerDir;
    Vector3 startingPos;

    void Start()
    {
        originalColor = model.material.color;
        roamingSetup();
        startingPos = transform.position;
    }

    void roamingSetup()
    {
        meleeTimer = meleeRate; // Ready to attack immediately
        stoppingDistOrig = agent.stoppingDistance;
        startingPos = transform.position;
    }

    void Update()
    {
        meleeTimer += Time.deltaTime;

        if (agent.remainingDistance < 0.01f)
            roamTimer += Time.deltaTime;

        if (playerInTrigger && CanSeePlayer())
        {
            float distanceToPlayer = Vector3.Distance(transform.position, gamemanager.instance.player.transform.position);

            if (distanceToPlayer <= meleeRange && meleeTimer >= meleeRate)
            {
                MeleeAttack();
                meleeTimer = 0;
            }
            else
            {
                ChasePlayer();
            }

            if (agent.remainingDistance <= agent.stoppingDistance)
                FaceTarget();
        }
        else if (playerInTrigger && !CanSeePlayer())
        {
            CheckRoam();
        }
        else if (!playerInTrigger)
        {
            CheckRoam();
        }

        UpdateAnimatorMovement();
    }

    void CheckRoam()
    {
        if (agent.remainingDistance < 0.01f && roamTimer >= roamPauseTime)
        {
            Roam();
        }
    }

    void Roam()
    {
        roamTimer = 0;
        agent.stoppingDistance = 0;

        Vector3 randomDirection = Random.insideUnitSphere * roamDist;
        Vector3 roamPos = startingPos + randomDirection;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(roamPos, out hit, roamDist, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        Vector3 targetPos = gamemanager.instance.player.transform.position;
        agent.SetDestination(targetPos);
    }

    bool CanSeePlayer()
    {
        playerDir = gamemanager.instance.player.transform.position - headPos.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        Debug.DrawRay(headPos.position, playerDir);

        RaycastHit hit;
        if (Physics.Raycast(headPos.position, playerDir, out hit))
        {
            if (angleToPlayer <= 90 && hit.collider.CompareTag("Player"))
            {
                agent.SetDestination(gamemanager.instance.player.transform.position);

                agent.stoppingDistance = stoppingDistOrig;
                return true;
            }
        }
        agent.stoppingDistance = 0;
        return false;
    }

    void FaceTarget()
    {
        Quaternion rot = Quaternion.LookRotation(new Vector3(playerDir.x, 0, playerDir.z));
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, faceTargetSpeed * Time.deltaTime);
    }

    void UpdateAnimatorMovement()
    {
        float currentSpeed = agent.velocity.magnitude;
        float animSpeed = anim.GetFloat("Speed");
        anim.SetFloat("Speed", Mathf.Lerp(animSpeed, currentSpeed, Time.deltaTime * animTransSpeed));
    }

    void MeleeAttack()
    {
        anim.SetTrigger("Attack");
    }

    public void CreateMelee()
    {
        if (meleeObj != null && meleePOS != null)
        {
            Instantiate(meleeObj, meleePOS.position, transform.rotation);
        }
    }

    public void WeaponColOn()
    {
        if (weaponCol != null)
            weaponCol.enabled = true;
    }

    public void WeaponColOff()
    {
        if (weaponCol != null)
            weaponCol.enabled = false;
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
        if (!other.CompareTag("Player"))
            return;

        playerInTrigger = false;
        agent.stoppingDistance = 0;
        agent.isStopped = true;
        anim.SetFloat("Speed", 0);
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        if (HP <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(FlashRed());
        }
    }

    IEnumerator FlashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = originalColor;
    }

    void Die()
    {
        anim.SetTrigger("Die");
        agent.isStopped = true;
        Destroy(gameObject, 4f);
    }
}
