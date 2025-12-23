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
    [Range(1, 500)][SerializeField] private int damageAmount;
    [Range(0.1f, 20f)][SerializeField] private float damageRate;

    [Header("~=~= Movement & Lifetime =~=~")]
    [Range(1, 50)][SerializeField] private int speed;
    [Range(1, 5000)][SerializeField] private int destroyTime;

    [Header("~=~= Projectile Arc =~=~")]
    [SerializeField] private ProjectileMode projectileMode = ProjectileMode.Straight;

    [Header("Arc Settings")]
    [Range(0, 60)][SerializeField] private float launchAngle = 35f;
    [Range(1, 50)][SerializeField] private float arcSpeed = 10f;

    public enum ProjectileMode { Straight, Arc }

    private bool isDamaging;

    void Start()
    {
        if (type == damageType.moving || type == damageType.homing || type == damageType.melee || type == damageType.DOT)
        {
            Destroy(gameObject, destroyTime);

            if (type == damageType.moving)
            {
                LaunchProjectile();
            }
        }
    }

    void Update()
    {
        if (type == damageType.homing)
        {
            rb.linearVelocity =
                (gamemanager.instance.player.transform.position - transform.position).normalized * speed;
        }
    }

    private void LaunchProjectile()
    {
        if (rb == null) return;

        rb.useGravity = (projectileMode == ProjectileMode.Arc);

        if (projectileMode == ProjectileMode.Straight)
        {
            rb.linearVelocity = transform.forward * speed;
        }
        else
        {
            Vector3 direction = transform.forward;

            Vector3 launchVelocity =
                Quaternion.AngleAxis(-launchAngle, transform.right) * direction * arcSpeed;

            rb.linearVelocity = launchVelocity;
        }
    }

    private int baseDamageAmount;
    private bool baseDamageCached;

    private void CacheBaseDamageIfNeeded()
    {
        if (baseDamageCached) return;
        baseDamageAmount = damageAmount;
        baseDamageCached = true;
    }

    public void ApplyDamageMultiplier(float multiplier)
    {
        CacheBaseDamageIfNeeded();
        multiplier = Mathf.Max(0.01f, multiplier);
        damageAmount = Mathf.Max(1, Mathf.RoundToInt(baseDamageAmount * multiplier));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
            return;

        if (other.CompareTag("Enemy"))
            return;

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
            else if (dotPrefab != null)
            {
                Instantiate(dotPrefab, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
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

        if (other.CompareTag("Enemy"))
            return;

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
