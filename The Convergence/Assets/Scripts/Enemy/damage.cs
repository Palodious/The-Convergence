using UnityEngine;
using System.Collections;

public class damage : MonoBehaviour
{
    private enum damageType { moving, melee, DOT, homing }

    [Header("~=~= Damage Type =~=~")]
    [SerializeField] private damageType type;
    [SerializeField] private GameObject dotPrefab;

    [Header("~=~= Physics =~=~")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private LayerMask ignoreLayer;

    [Header("~=~= Damage Settings =~=~")]
    [Range(1, 20)][SerializeField] private int damageAmount;
    [Range(0.1f, 20f)][SerializeField] private float damageRate;

    [Header("~=~= Movement & Lifetime =~=~")]
    [Range(1, 50)][SerializeField] private int speed;
    [Range(1, 20)][SerializeField] private int destroyTime;

    [Header("~=~= Projectile Arc =~=~")]
    [SerializeField] private ProjectileMode projectileMode = ProjectileMode.Straight; // Projectile mode selection (straight or arc)

    [Header("Arc Settings")]
    [Range(0, 60)][SerializeField] private float launchAngle = 35f; // Upward tilt angle for arcing projectiles
    [Range(1, 50)][SerializeField] private float arcSpeed = 10f; // Speed of arcing projectile
    [Range(0f, 50f)][SerializeField] private float gravityScale = 1f; // Gravity multiplier for arc

    public enum ProjectileMode { Straight, Arc }

    private bool isDamaging;

    void Start()
    {
        if (type == damageType.moving || type == damageType.homing || type == damageType.melee)
        {
            Destroy(gameObject, destroyTime);

            if (type == damageType.moving)
            {
                LaunchProjectile(); // Set initial velocity based on projectile mode
            }
        }
    }

    void Update()
    {
        if (type == damageType.homing)
        {
            // Continuously move towards player for homing projectiles
            rb.linearVelocity = (gamemanager.instance.player.transform.position - transform.position).normalized * speed;
        }
    }

    private void LaunchProjectile()
    {
        if (rb == null) return;

        // Enable gravity only for arcing projectiles
        rb.useGravity = (projectileMode == ProjectileMode.Arc);

        if (projectileMode == ProjectileMode.Straight)
        {
            // Straight-line shot moves directly forward at set speed
            rb.linearVelocity = transform.forward * speed;
        }
        else
        {
            // Arcing shot tilts the forward direction upward to create a ballistic arc
            Vector3 direction = transform.forward;

            // Apply the launch angle to the forward vector to calculate initial velocity
            Vector3 launchVelocity =
                Quaternion.AngleAxis(-launchAngle, transform.right) * direction * arcSpeed;

            rb.linearVelocity = launchVelocity;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
            return;

        // Skip if layer should not be damaged
        if (((1 << other.gameObject.layer) & ignoreLayer) != 0)
            return;

        IDamage dmg = other.GetComponent<IDamage>();
        if (dmg != null && type != damageType.DOT)
        {
            dmg.takeDamage(damageAmount);
        }

        if (type == damageType.moving || type == damageType.homing)
        {
            if (dmg != null)
            {
                Destroy(gameObject);
            }
            // If it hit something NOT damaging (like the environment/ground) AND we have a DOT prefab...
            else if (dotPrefab != null)
            {
                // 1. Spawn the DOT object (the Acid Pool) at this location.
                Instantiate(dotPrefab, transform.position, Quaternion.identity);

                // 2. Destroy the original projectile.
                Destroy(gameObject);
            }
            // If we hit something non-damaging but have no DOT prefab, just destroy the projectile.
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.isTrigger)
            return;

        // Skip if layer should not be damaged
        if (((1 << other.gameObject.layer) & ignoreLayer) != 0)
            return;

        IDamage dmg = other.GetComponent<IDamage>();

        if (dmg != null && type == damageType.DOT && !isDamaging)
        {
            StartCoroutine(damageOther(dmg));
        }
    }

    IEnumerator damageOther(IDamage d)
    {
        isDamaging = true;
        d.takeDamage(damageAmount);
        yield return new WaitForSeconds(damageRate);
        isDamaging = false;
    }
}
