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

    [Header("**** Visual FOV Display ****")]
    [SerializeField] bool showFOV = true;
    [SerializeField] Color fovColor = new Color(1f, 0f, 0f, 0.3f); // RED with transparency (alpha 0.3)
    [SerializeField] Color detectionColor = new Color(1f, 0f, 0f, 0.6f); // Brighter red when detecting
    [Range(10, 50)][SerializeField] int fovSegments = 30;

    [Header("**** Effects ****")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip shootSound;
    [SerializeField] AudioClip detectionSound;
    [SerializeField] ParticleSystem muzzleFlash;
    [SerializeField] GameObject destructionEffect;

    [Header("**** Debug ****")]
    [SerializeField] bool debugMode = false;

    // State variables
    private Transform target;
    private float fireTimer;
    private bool isFiring = false;
    private bool isBursting = false;
    private Coroutine burstCoroutine;
    private Material fovMaterial;
    private Mesh fovMesh;
    private bool hasTarget = false;
    private Color currentFOVColor;
    private Quaternion initialRotation; // Store initial rotation for clamping

    void Start()
    {
        // Find player target
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
        else if (debugMode)
            Debug.LogWarning("Turret: No player found with tag 'Player'");

        // Store initial rotation for clamping
        initialRotation = transform.rotation;

        // Create FOV visualization if enabled
        if (showFOV)
            CreateFOVVisualization();

        currentFOVColor = fovColor;
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

                // Rotate entire turret towards target
                RotateTowardsTarget(directionToTarget);

                // Check if target is within FOV cone for shooting
                if (IsInFOV(directionToTarget))
                {
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

        // Update FOV visualization
        if (showFOV)
            UpdateFOVVisualization();
    }

    bool IsInFOV(Vector3 directionToTarget)
    {
        // Calculate angle between turret forward and direction to target
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

        // Check if within the cone defined by FOV
        return angleToTarget <= Mathf.Min(horizontalFOV, verticalFOV) / 2f;
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

        // Debug visualization
        if (debugMode)
        {
            Debug.DrawRay(transform.position, directionToTarget * range, Color.blue);
            Debug.DrawRay(transform.position, transform.forward * range, Color.green);
        }
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
        if (debugMode) Debug.Log("Turret: Target acquired!");

        if (audioSource != null && detectionSound != null)
            audioSource.PlayOneShot(detectionSound);

        currentFOVColor = detectionColor;
    }

    void OnTargetLost()
    {
        if (debugMode) Debug.Log("Turret: Target lost!");

        currentFOVColor = fovColor;
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

    // FOV Visualization
    void CreateFOVVisualization()
    {
        GameObject fovVisual = new GameObject("FOV_Visualization");
        fovVisual.transform.SetParent(transform);
        fovVisual.transform.localPosition = Vector3.zero;
        fovVisual.transform.localRotation = Quaternion.identity;

        MeshFilter meshFilter = fovVisual.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = fovVisual.AddComponent<MeshRenderer>();

        fovMesh = new Mesh();
        meshFilter.mesh = fovMesh;

        // Create material for FOV visualization
        fovMaterial = new Material(Shader.Find("Transparent/Diffuse"));
        fovMaterial.color = currentFOVColor;
        meshRenderer.material = fovMaterial;
    }

    void UpdateFOVVisualization()
    {
        if (fovMesh == null) return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float horizontalStep = horizontalFOV / fovSegments;
        float verticalStep = verticalFOV / fovSegments;
        float halfHorizontalFOV = horizontalFOV / 2f;
        float halfVerticalFOV = verticalFOV / 2f;

        // Create cone vertices
        vertices.Add(Vector3.zero); // Center point (tip of cone)

        for (int v = 0; v <= fovSegments; v++)
        {
            float verticalAngle = -halfVerticalFOV + (verticalStep * v);

            for (int h = 0; h <= fovSegments; h++)
            {
                float horizontalAngle = -halfHorizontalFOV + (horizontalStep * h);

                // Convert spherical coordinates to Cartesian
                float x = Mathf.Sin(horizontalAngle * Mathf.Deg2Rad);
                float y = Mathf.Sin(verticalAngle * Mathf.Deg2Rad);
                float z = Mathf.Cos(horizontalAngle * Mathf.Deg2Rad) * Mathf.Cos(verticalAngle * Mathf.Deg2Rad);

                Vector3 point = new Vector3(x, y, z) * range;
                vertices.Add(point);
            }
        }

        // Create triangles for cone
        int vertexCount = fovSegments + 1;

        for (int v = 0; v < fovSegments; v++)
        {
            for (int h = 0; h < fovSegments; h++)
            {
                int current = 1 + (v * vertexCount) + h;
                int next = current + 1;
                int below = current + vertexCount;
                int belowNext = below + 1;

                // Create two triangles for each quad
                triangles.Add(0);
                triangles.Add(current);
                triangles.Add(next);

                triangles.Add(0);
                triangles.Add(next);
                triangles.Add(belowNext);

                triangles.Add(0);
                triangles.Add(belowNext);
                triangles.Add(below);

                triangles.Add(0);
                triangles.Add(below);
                triangles.Add(current);
            }
        }

        fovMesh.Clear();
        fovMesh.vertices = vertices.ToArray();
        fovMesh.triangles = triangles.ToArray();
        fovMesh.RecalculateNormals();

        // Update material color
        if (fovMaterial != null)
            fovMaterial.color = currentFOVColor;

        // Position the FOV visualization with the turret
        GameObject fovVisual = GameObject.Find("FOV_Visualization");
        if (fovVisual != null)
        {
            fovVisual.transform.position = transform.position;
            fovVisual.transform.rotation = transform.rotation;
        }
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
}