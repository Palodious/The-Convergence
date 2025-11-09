using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class enemyAIExtras : MonoBehaviour
{
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
