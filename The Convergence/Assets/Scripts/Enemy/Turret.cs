using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour, IDamage
{
    [System.Serializable]
    public enum FireMode
    {
        Single,
        Burst
    }

    [Header("**** Core Components ****")]
    [SerializeField] Transform projectileSpawnPoint;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] LayerMask targetLayer;
    [SerializeField] LayerMask obstacleLayer;

    [Header("**** Stats ****")]
    [Range(1, 500)][SerializeField] int health = 100;
    [Range(1, 500)][SerializeField] int damage = 20;
    [Range(5, 100)][SerializeField] float range = 25f;
    [Range(0.1f, 10f)][SerializeField] float fireRate = 1f;
    [Range(1, 10)][SerializeField] float projectileSpeed = 20f;

    [Header("**** Rotation Settings ****")]
    [Range(1, 100)][SerializeField] float rotationSpeed = 5f;
    [Range(1, 180)][SerializeField] float horizontalFOV = 120f; // Horizontal field of view
    [Range(1, 90)][SerializeField] float verticalFOV = 60f; // Vertical field of view
    [Range(-180, 180)][SerializeField] float minHorizontalAngle = -90f;
    [Range(-180, 180)][SerializeField] float maxHorizontalAngle = 90f;
    [Range(-90, 90)][SerializeField] float minVerticalAngle = -30f;
    [Range(-90, 90)][SerializeField] float maxVerticalAngle = 30f;

    [Header("**** Firing Settings ****")]
    [SerializeField] FireMode fireMode = FireMode.Single;
    [SerializeField] bool burstFireEnabled = false;
    [Range(1, 10)][SerializeField] int burstCount = 3;
    [Range(0.05f, 1f)][SerializeField] float burstDelay = 0.1f;
    [Range(0.1f, 3f)][SerializeField] float burstCooldown = 1f;

    [Header("**** Effects ****")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip shootSound;
    [SerializeField] AudioClip detectionSound;
    [SerializeField] ParticleSystem muzzleFlash;
    [SerializeField] GameObject destructionEffect;

    // State variables
    private Transform target;
    private float fireTimer;
    private bool isFiring = false;
    private bool isBursting = false;
    private Coroutine burstCoroutine;
    private bool hasTarget = false;
    private Quaternion initialRotation; // Store initial rotation for clamping

    void Start()
    {
        // Find player target
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;

        // Store initial rotation for clamping
        initialRotation = transform.rotation;
    }

    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Check if target is in range
        if (distanceToTarget <= range)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            // Check line of sight
            if (HasLineOfSight())
            {
                if (!hasTarget)
                {
                    OnTargetAcquired();
                    hasTarget = true;
                }

                // Check if target is within FOV cone for shooting
                if (IsInFOV(directionToTarget))
                {
                    // Rotate turret towards target within limits
                    RotateTowardsTarget(directionToTarget);

                    // Check firing conditions
                    if (!isFiring && !isBursting && fireTimer >= 1f / fireRate)
                    {
                        if (fireMode == FireMode.Burst && burstFireEnabled)
                        {
                            if (!isBursting)
                                burstCoroutine = StartCoroutine(BurstFire());
                        }
                        else
                        {
                            Fire();
                        }
                    }
                }
            }
            else if (hasTarget)
            {
                OnTargetLost();
                hasTarget = false;
            }
        }
        else if (hasTarget)
        {
            OnTargetLost();
            hasTarget = false;
        }

        // Update fire timer
        if (!isFiring && !isBursting)
            fireTimer += Time.deltaTime;
    }

    bool IsInFOV(Vector3 directionToTarget)
    {
        // Convert target direction to local space
        Vector3 localTargetDir = transform.InverseTransformDirection(directionToTarget);

        // Calculate horizontal angle (around Y axis)
        float horizontalAngle = Mathf.Atan2(localTargetDir.x, localTargetDir.z) * Mathf.Rad2Deg;

        // Calculate vertical angle (around X axis)
        float verticalAngle = Mathf.Atan2(-localTargetDir.y, localTargetDir.z) * Mathf.Rad2Deg;

        // Check if within both horizontal and vertical FOV
        bool inHorizontalFOV = Mathf.Abs(horizontalAngle) <= horizontalFOV / 2f;
        bool inVerticalFOV = Mathf.Abs(verticalAngle) <= verticalFOV / 2f;

        return inHorizontalFOV && inVerticalFOV;
    }

    bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 rayOrigin = transform.position;
        Vector3 direction = (target.position - rayOrigin).normalized;
        float distance = Vector3.Distance(rayOrigin, target.position);

        // Raycast to check for obstacles
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, direction, out hit, distance, obstacleLayer | targetLayer))
        {
            // Check if we hit the target (player)
            if (((1 << hit.collider.gameObject.layer) & targetLayer) != 0)
            {
                // Make sure it's actually the player
                if (hit.collider.CompareTag("Player") || hit.collider.transform == target)
                {
                    return true;
                }
            }
            return false;
        }

        return false;
    }

    void RotateTowardsTarget(Vector3 directionToTarget)
    {
        // Calculate target rotation
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

        // Extract local rotation relative to initial rotation
        Quaternion localTargetRotation = Quaternion.Inverse(initialRotation) * targetRotation;
        Vector3 localEuler = localTargetRotation.eulerAngles;

        // Normalize angles to -180 to 180 range
        float targetY = localEuler.y;
        float targetX = localEuler.x;

        if (targetY > 180) targetY -= 360;
        if (targetX > 180) targetX -= 360;

        // Clamp horizontal rotation (Y axis)
        float clampedY = Mathf.Clamp(targetY, minHorizontalAngle, maxHorizontalAngle);

        // Clamp vertical rotation (X axis)
        float clampedX = Mathf.Clamp(targetX, minVerticalAngle, maxVerticalAngle);

        // Create clamped rotation
        Quaternion clampedRotation = Quaternion.Euler(clampedX, clampedY, 0);

        // Convert back to world space
        Quaternion finalRotation = initialRotation * clampedRotation;

        // Smoothly rotate towards target
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            finalRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    void Fire()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null) return;

        fireTimer = 0f;
        isFiring = true;

        // Create projectile
        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);

        // Configure projectile
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.Initialize(damage, projectileSpeed, targetLayer);
        }
        else
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = projectileSpawnPoint.forward * projectileSpeed;
        }

        // Play effects
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        if (muzzleFlash != null)
            muzzleFlash.Play();

        // Reset firing state
        StartCoroutine(ResetFiringState());
    }

    IEnumerator BurstFire()
    {
        isBursting = true;

        for (int i = 0; i < burstCount; i++)
        {
            Fire();
            yield return new WaitForSeconds(burstDelay);
        }

        yield return new WaitForSeconds(burstCooldown);
        isBursting = false;
    }

    IEnumerator ResetFiringState()
    {
        yield return new WaitForSeconds(0.1f);
        isFiring = false;
    }

    void OnTargetAcquired()
    {
        if (audioSource != null && detectionSound != null)
            audioSource.PlayOneShot(detectionSound);
    }

    void OnTargetLost()
    {
        // Nothing needed here for now
    }

    // IDamage interface implementation
    public void takeDamage(int amount)
    {
        health -= amount;

        if (health <= 0)
        {
            DestroyTurret();
        }
    }

    void DestroyTurret()
    {
        if (destructionEffect != null)
            Instantiate(destructionEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    // Public methods for external control
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetFireMode(FireMode mode)
    {
        fireMode = mode;
    }

    public void ToggleBurstFire(bool enabled)
    {
        burstFireEnabled = enabled;
    }

    public void SetRotationLimits(float minHoriz, float maxHoriz, float minVert, float maxVert)
    {
        minHorizontalAngle = minHoriz;
        maxHorizontalAngle = maxHoriz;
        minVerticalAngle = minVert;
        maxVerticalAngle = maxVert;
    }

    public int GetHealth() => health;
    public int GetMaxHealth() => 100;
    public bool HasTarget() => hasTarget;
}