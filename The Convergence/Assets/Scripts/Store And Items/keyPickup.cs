using System.Collections.Generic;
using UnityEngine;

public class keyPickup : MonoBehaviour
{
    [Header("Key Settings")]
    [SerializeField] private keyStats key;
    [SerializeField] private string uniqueKeyID;
    public static HashSet<string> collectedKeys = new HashSet<string>();

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
        if (!string.IsNullOrEmpty(uniqueKeyID) && collectedKeys.Contains(uniqueKeyID))
        {
            Destroy(gameObject);
            return;
        }

        if (!isPickup)
        {
            enabled = false;
            return;
        }

        startPosition = transform.position;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        Color lightColor = (key != null && key.lightColor != Color.clear) ? key.lightColor : Color.yellow;
        float lightRange = (key != null && key.lightRange > 0) ? key.lightRange : 3f;
        float lightIntensity = (key != null && key.lightIntensity > 0) ? key.lightIntensity : 1.5f;

        pickupLight = GetComponent<Light>();
        if (pickupLight == null)
            pickupLight = gameObject.AddComponent<Light>();

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
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPickup) return;
        if (!other.CompareTag("Player")) return;

        playerController player = other.GetComponent<playerController>();
        if (player == null) return;

        if (key != null)
        {
            player.GetItem(key);
            OnPickup();
        }
    }

    private void OnPickup()
    {
        if (!string.IsNullOrEmpty(uniqueKeyID))
        {
            collectedKeys.Add(uniqueKeyID);
        }

        if (key != null && key.pickupSound != null)
        {
            float volume = key.pickupSoundVol;
            if (audioSource != null)
                audioSource.PlayOneShot(key.pickupSound, volume);
            else
                AudioSource.PlayClipAtPoint(key.pickupSound, transform.position, volume);
        }

        if (key != null && key.pickupEffect != null)
        {
            Instantiate(key.pickupEffect, transform.position, Quaternion.identity);
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