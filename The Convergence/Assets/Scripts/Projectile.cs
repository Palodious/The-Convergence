using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] int damage = 25;
    [SerializeField] float speed = 20f;
    [SerializeField] float lifetime = 5f;
    [SerializeField] LayerMask hitLayer;
    [SerializeField] GameObject impactEffect;

    private float timer = 0f;

    public void Initialize(int dmg, float spd, LayerMask layer)
    {
        damage = dmg;
        speed = spd;
        hitLayer = layer;
    }

    void Update()
    {
        // Move forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Lifetime check
        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if we hit something on the target layer
        if (((1 << other.gameObject.layer) & hitLayer) != 0)
        {
            // Apply damage
            IDamage damageable = other.GetComponent<IDamage>();
            if (damageable != null)
            {
                damageable.takeDamage(damage);
            }

            // Spawn impact effect
            if (impactEffect != null)
                Instantiate(impactEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
        // Destroy on hitting anything that's not the shooter
        else if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}