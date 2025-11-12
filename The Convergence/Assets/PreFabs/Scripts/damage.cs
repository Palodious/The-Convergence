// damage.cs
using UnityEngine;
using System.Collections;

public class damage : MonoBehaviour
{
    enum damageType { moving, melee, DOT, homing }
    [SerializeField] damageType type;
    [SerializeField] Rigidbody rb;

    [SerializeField] int damageAmount;
    [SerializeField] float damageRate;
    [SerializeField] int speed;
    [SerializeField] int destroyTime;

    bool isDamaging;

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