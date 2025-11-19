using UnityEngine;
using System.Collections;

public class damage : MonoBehaviour
{
    private enum damageType { moving, melee, DOT, homing }

    [Header("~=~= Damage Type =~=~")]
    [SerializeField] private damageType type;

    [Header("~=~= Physics =~=~")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private LayerMask ignoreLayer;

    [Header("~=~= Damage Settings =~=~")]
    [Range(1, 20)][SerializeField] private int damageAmount;
    [Range(0.1f, 20f)][SerializeField] private float damageRate;

    [Header("~=~= Movement & Lifetime =~=~")]
    [Range(1, 50)][SerializeField] private int speed;
    [Range(1, 20)][SerializeField] private int destroyTime;

    private bool isDamaging;


    void Start()
    {
        if (type == damageType.moving || type == damageType.homing || type == damageType.melee)
        {
            Destroy(gameObject, destroyTime);

            if (type == damageType.moving)
            {
                rb.linearVelocity = transform.forward * speed;
            }
        }
    }

    void Update()
    {
        if (type == damageType.homing)
        {
            rb.linearVelocity = (gamemanager.instance.player.transform.position - transform.position).normalized * speed * Time.deltaTime;
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
            Destroy(gameObject);
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