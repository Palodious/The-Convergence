using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    [SerializeField] private int ammoAmount = 30;
    [SerializeField] private gunStats[] compatibleGuns; // Array of guns this works for

    [Header("Pickup Visuals")]
    [SerializeField] private bool isPickup = true;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private AudioClip pickupSound;
    [Range(0, 1)][SerializeField] private float pickupSoundVol = 1f;

    [Header("Visual Appearance")]
    [SerializeField] private Color pickupColor = Color.yellow;

    private Vector3 startPosition;
    private Light pickupLight;

    private void Start()
    {
        if (!isPickup)
        {
            enabled = false;
            return;
        }

        startPosition = transform.position;

        // Set visual appearance
        if (TryGetComponent<Renderer>(out var renderer))
        {
            renderer.material.color = pickupColor;
        }

        // Get or add light
        pickupLight = GetComponent<Light>();
        if (pickupLight == null)
        {
            pickupLight = gameObject.AddComponent<Light>();
            pickupLight.color = pickupColor;
            pickupLight.range = 3f;
            pickupLight.intensity = 1.5f;
        }
        else
        {
            pickupLight.color = pickupColor;
        }
    }

    private void Update()
    {
        if (!isPickup) return;

        // Simple animation
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPickup) return;
        if (!other.CompareTag("Player")) return;

        playerController player = other.GetComponent<playerController>();
        if (player != null)
        {
            // Detach light before pickup
            if (pickupLight != null)
            {
                pickupLight.transform.SetParent(null);
                Destroy(pickupLight.gameObject);
            }

            bool ammoAdded = false;

            if (compatibleGuns != null && compatibleGuns.Length > 0)
            {
                // Try to add ammo to any of the compatible guns
                foreach (var gun in compatibleGuns)
                {
                    if (gun != null && player.CanAddAmmo(gun))
                    {
                        // FIXED: Actually add the ammo!
                        player.AddAmmo(ammoAmount);
                        ammoAdded = true;
                        Debug.Log($"Added {ammoAmount} ammo to {gun.name}");
                        break; // Stop after adding to first valid gun
                    }
                }

                if (!ammoAdded)
                {
                    Debug.Log("All compatible guns are full!");
                    return; // Don't destroy if no ammo was added
                }
            }
            else
            {
                // No specific guns assigned, add to current weapon
                player.AddAmmo(ammoAmount);
                ammoAdded = true;
            }

            if (ammoAdded)
            {
                PickupEffect();
                Destroy(gameObject);
            }
        }
    }

    private void PickupEffect()
    {
        // Play sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupSoundVol);
        }

        // Spawn effect
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
    }

    public void EnablePickup()
    {
        isPickup = true;
        enabled = true;
    }
}