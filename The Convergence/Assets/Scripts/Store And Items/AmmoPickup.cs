using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    [SerializeField] private AmmoStats ammo; // Use the ScriptableObject

    [Header("Pickup Visuals")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private bool isPickup = true;

    private Vector3 startPosition;
    private Light pickupLight;
    private AudioSource audioSource;

    private void Start()
    {
        if (!isPickup)
        {
            this.enabled = false;
            return;
        }

        startPosition = transform.position;

        // Add AudioSource for pickup sound
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.playOnAwake = false;

        // Setup light - use values from ammoStats if available, otherwise use defaults
        Color lightColor = (ammo != null && ammo.lightColor != Color.clear) ? ammo.lightColor : Color.yellow;
        float lightRange = (ammo != null && ammo.lightRange > 0) ? ammo.lightRange : 3f;
        float lightIntensity = (ammo != null && ammo.lightIntensity > 0) ? ammo.lightIntensity : 1.5f;

        pickupLight = GetComponent<Light>();
        if (pickupLight == null)
        {
            pickupLight = gameObject.AddComponent<Light>();
        }

        pickupLight.color = lightColor;
        pickupLight.range = lightRange;
        pickupLight.intensity = lightIntensity;
        pickupLight.shadows = LightShadows.Soft;
    }


    private void Update()
    {
        if (!isPickup) return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPickup) return;
        if (!other.CompareTag("Player")) return;

        playerController player = other.GetComponent<playerController>();
        if (player == null) return;

        // Pass the ammoStats ScriptableObject to player's GetItem method
        if (ammo != null)
        {
            player.GetItem(ammo);
            OnPickup();
        }
    }

    private void OnPickup()
    {
        // Play pickup sound
        if (ammo != null && ammo.pickupSound != null)
        {
            float volume = ammo.pickupSoundVol;
            if (audioSource != null)
            {
                audioSource.PlayOneShot(ammo.pickupSound, volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(ammo.pickupSound, transform.position, volume);
            }
        }

        // Spawn pickup effect
        if (ammo != null && ammo.pickupEffect != null)
        {
            Instantiate(ammo.pickupEffect, transform.position, Quaternion.identity);
        }

        // Disable the pickup immediately
        isPickup = false;

        // Hide the mesh but keep the gameobject alive briefly for sound to play
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        // Disable collider
        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        // Turn off the light
        if (pickupLight != null)
            pickupLight.enabled = false;

        // Destroy the gameobject after a short delay (to allow sound to play)
        Destroy(gameObject, .5f);
    }

    public void EnablePickup()
    {
        isPickup = true;
        this.enabled = true;
    }
}