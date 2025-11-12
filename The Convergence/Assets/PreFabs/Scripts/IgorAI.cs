// IgorAI.cs
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
    [SerializeField] int animTransSpeed;

    Color originalColor;

    bool playerInTrigger;
    float meleeTimer;

    Vector3 playerDir;
    float angleToPlayer;

    void Start()
    {
        originalColor = model.material.color;
        meleeTimer = meleeRate; // Ready to attack immediately
    }

    void Update()
    {
        meleeTimer += Time.deltaTime;

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
        else
        {
            // Stop moving if player not detected
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                anim.SetFloat("Speed", 0);
            }
        }

        UpdateAnimatorMovement();
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(gamemanager.instance.player.transform.position);
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
                return true;
            }
        }
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
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            agent.isStopped = true;
            anim.SetFloat("Speed", 0);
        }
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
        // Disable colliders or other components as needed
        Destroy(gameObject, 4f);
    }
}
