using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class enemyAIExtras : MonoBehaviour
{
    [SerializeField] enemyAI ai; // Reference to the main enemy AI script
    [SerializeField] NavMeshAgent agent; // NavMeshAgent for movement and patrol control

    [SerializeField] bool useShield; // Toggles the shield system
    [SerializeField] int shieldHP; // Current shield HP
    [SerializeField] int shieldMaxHP; // Maximum shield HP
    [SerializeField] float shieldRegenDelay; // Time before the shield starts regenerating
    [SerializeField] GameObject shieldPrefab; // Shield visual prefab in the scene
    [SerializeField] Color shieldFlashColor = Color.white; // color to flash when hit
    [SerializeField] float shieldFlashDuration = 0.1f;     // how long the flash lasts


    [SerializeField] bool useMelee; // Toggles melee attack system
    [SerializeField] int meleeDamage; // Damage dealt by melee attack
    [SerializeField] float meleeRange; // Range of melee attack
    [SerializeField] float meleeRate; // Time between melee attacks

    [SerializeField] bool usePatrol; // Toggles patrol system
    [SerializeField] Transform[] patrolPoints; // Patrol point locations
    [SerializeField] float patrolWaitTime; // Wait time before moving to next patrol point

    [SerializeField] bool useShooting; // Toggles the shooting system in the main AI script

    bool shieldActive; // True when shield is active
    bool shieldBroken; // True when shield HP reaches zero
    bool canMelee; // True when melee attack can be used
    bool isPatrolling; // True when patrol routine is running
    int patrolIndex; // Current patrol point index
    float meleeTimer; // Tracks melee attack cooldown

    void Start()
    {
        // Initialize shield if enabled
        if (useShield)
        {
            shieldActive = true;
            shieldBroken = false;
            shieldHP = shieldMaxHP;
            if (shieldPrefab != null)
                shieldPrefab.SetActive(true);
        }

        // Enable melee if toggled on
        if (useMelee)
        {
            canMelee = true;
        }

        // Start patrol if enabled and has valid points
        if (usePatrol && patrolPoints.Length > 0)
        {
            isPatrolling = true;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }

        // Disable shooting logic in main AI if toggled off
        if (ai != null && !useShooting)
        {
            ai.enabled = false;
        }
    }

    void Update()
    {
        // Handles melee attack timing and distance check
        if (useMelee && ai != null)
        {
            meleeTimer += Time.deltaTime;
            if (canMelee && meleeTimer >= meleeRate)
            {
                float dist = Vector3.Distance(transform.position, gamemanager.instance.player.transform.position);
                if (dist <= meleeRange)
                {
                    StartCoroutine(meleeAttack());
                }
            }
        }

        // Patrol loop that triggers once enemy reaches destination
        if (usePatrol && isPatrolling)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                StartCoroutine(patrolWait());
        }
    }

    public bool IsShieldActive()
    {
        return useShield && shieldPrefab != null && shieldPrefab.activeSelf;
    }

    public void takeShieldDamage(int amount)
    {
        // Prevents shield damage when disabled or inactive
        if (!useShield || !shieldActive) return;

        // Reduces shield HP by incoming damage
        shieldHP -= amount;

        // Flash shield to indicate it's been hit
        if (shieldPrefab != null)
            StartCoroutine(FlashShield());

        if (shieldHP <= 0)
        {
            // Deactivates shield prefab and starts regen delay
            shieldBroken = true;
            shieldActive = false;
            shieldHP = 0;

            if (shieldPrefab != null)
            {
                shieldPrefab.SetActive(false);
                Collider shieldCol = shieldPrefab.GetComponent<Collider>();
                if (shieldCol != null)
                    shieldCol.enabled = false;
            }

            StartCoroutine(shieldRegen());
        }
    }

    IEnumerator FlashShield()
    {
        Renderer shieldRenderer = shieldPrefab.GetComponent<Renderer>();
        if (shieldRenderer != null)
        {
            // Save the original color
            Color originalColor = shieldRenderer.material.color;

            // Flash the shield color
            shieldRenderer.material.color = Color.white;

            // Wait briefly
            yield return new WaitForSeconds(shieldFlashDuration);

            // Restore the original color
            shieldRenderer.material.color = originalColor;
        }
    }

    IEnumerator shieldRegen()
    {
        // Waits for delay before regenerating shield
        yield return new WaitForSeconds(shieldRegenDelay);
        shieldHP = shieldMaxHP;
        shieldActive = true;
        shieldBroken = false;
        if (shieldPrefab != null)
            shieldPrefab.SetActive(true);
    }

    IEnumerator meleeAttack()
    {
        // Handles melee attack cooldown and damage application
        meleeTimer = 0;
        canMelee = false;

        IDamage target = gamemanager.instance.player.GetComponent<IDamage>();
        if (target != null)
            target.takeDamage(meleeDamage);

        yield return new WaitForSeconds(meleeRate);
        canMelee = true;
    }

    IEnumerator patrolWait()
    {
        // Waits at patrol point before moving to next
        isPatrolling = false;
        yield return new WaitForSeconds(patrolWaitTime);

        patrolIndex++;
        if (patrolIndex >= patrolPoints.Length)
            patrolIndex = 0;

        agent.SetDestination(patrolPoints[patrolIndex].position);
        isPatrolling = true;
    }

    public void toggleShooting(bool state)
    {
        // Enables or disables shooting behavior dynamically
        useShooting = state;
        if (ai != null)
            ai.enabled = state;
    }
}
