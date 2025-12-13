using UnityEngine;
using System.Collections;

public class AmmoPickup : MonoBehaviour
{
    [SerializeField] AmmoStats ammoStats;
    [SerializeField] float rotationSpeed = 50f;
    [SerializeField] float bobSpeed = 2f;
    [SerializeField] float bobHeight = 0.2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;

        if (TryGetComponent<Renderer>(out var renderer))
        {
            renderer.material.color = ammoStats.ammoUIColor;
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        IAmmo ammoUser = other.GetComponent<IAmmo>();

        if (ammoUser != null && ammoStats.compatibleGun != null)
        {
            if (ammoUser.CanAddAmmo(ammoStats.compatibleGun))
            {
                ammoUser.AddAmmo(ammoStats.ammoAmount);
                StartCoroutine(DelayedDestroy());
            }
        }
    }


    IEnumerator DelayedDestroy()
    {
        GetComponent<Collider>().enabled = false;
        if (TryGetComponent<Renderer>(out var renderer))
        {
            renderer.enabled = false;
        }

        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }
}