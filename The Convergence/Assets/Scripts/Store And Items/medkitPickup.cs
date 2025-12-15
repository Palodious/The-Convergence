using UnityEngine;

public class medkitPickup : MonoBehaviour
{
    [SerializeField] medkitStats medkit;
    [SerializeField] private bool isPickup = true;

    [Header("Pickup Visuals")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;

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

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        SetupPickupLight();
    }

    private void SetupPickupLight()
    {
        Color lightColor;
        if (medkit != null)
        {
            switch (medkit.lightColorType)
            {
                case LightColor.Red:
                    lightColor = Color.red;
                    break;
                case LightColor.Green:
                default:
                    lightColor = Color.green;
                    break;
            }
        }
        else
        {
            lightColor = Color.green;
        }

        float lightRange = (medkit != null && medkit.lightRange > 0) ? medkit.lightRange : 3f;
        float lightIntensity = (medkit != null && medkit.lightIntensity > 0) ? medkit.lightIntensity : 1.5f;

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

        IPickup pickup = other.GetComponent<IPickup>();
        if (pickup != null)
        {
            pickup.GetItem(medkit);
            OnPickup();
        }
    }

    private void OnPickup()
    {
        if (medkit != null && medkit.pickupSound != null)
        {
            float volume = medkit.pickupSoundVol;
            if (audioSource != null)
            {
                audioSource.PlayOneShot(medkit.pickupSound, volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(medkit.pickupSound, transform.position, volume);
            }
        }

        if (medkit != null && medkit.pickupEffect != null)
        {
            Instantiate(medkit.pickupEffect, transform.position, Quaternion.identity);
        }

        isPickup = false;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        if (pickupLight != null)
            pickupLight.enabled = false;

        Destroy(gameObject, 1f);
    }

    public void EnablePickup()
    {
        isPickup = true;
        this.enabled = true;
    }
}