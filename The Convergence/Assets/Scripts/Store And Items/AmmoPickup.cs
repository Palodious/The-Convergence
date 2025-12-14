using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    [SerializeField] private int ammoAmount = 30;

    [Header("Pickup Visuals")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private AudioClip pickupSound;
    [Range(0, 1)][SerializeField] private float pickupSoundVol = 1f;

    private Vector3 startPosition;
    private Light pickupLight;

    private void Start()
    {
        startPosition = transform.position;

        pickupLight = GetComponent<Light>();
        if (pickupLight == null)
        {
            pickupLight = gameObject.AddComponent<Light>();
        }

        pickupLight.color = Color.yellow;
        pickupLight.range = 3f;
        pickupLight.intensity = 1.5f;
    }


    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerController player = other.GetComponent<playerController>();
        if (player == null) return;

        if (!player.CanAddAmmo())
        {
            Debug.Log("Ammo pickup ignored — ammo already full.");
            return;
        }

        player.AddAmmo(ammoAmount);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupSoundVol);

        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        if (pickupLight != null)
            Destroy(pickupLight.gameObject);

        Destroy(gameObject);
    }
}
