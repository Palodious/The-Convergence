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
    [SerializeField] Transform turretBase; // Rotates horizontally (Y axis)
    [SerializeField] Transform turretHead; // Rotates vertically (X axis)
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
    private Vector3 baseStartRotation;
    private Vector3 headStartRotation;
    private Color currentFOVColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find player target
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
        else if (debugMode)
            Debug.LogWarning("Turret: No player found with tag 'Player'");

        // Initialize rotations
        if (turretBase != null)
            baseStartRotation = turretBase.localEulerAngles;
        if (turretHead != null)
            headStartRotation = turretHead.localEulerAngles;

        // Create FOV visualization if enabled
        if (showFOV)
            CreateFOVVisualization();

        currentFOVColor = fovColor;
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Check if target is in range
        if (distanceToTarget <= range)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            // Check if target is within FOV cone
            if (IsInFOV(directionToTarget))
            {
                // Check line of sight
                if (HasLineOfSight())
                {
                    if (!hasTarget)
                    {
                        OnTargetAcquired();
                        hasTarget = true;
                    }

                    // Rotate to face target
                    RotateTowardsTarget(directionToTarget);

                    // Check firing conditions
                    if (!isFiring && fireTimer >= 1f / fireRate)
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
        }
        else if (hasTarget)
        {
            OnTargetLost();
            hasTarget = false;
        }
        // Update fire timer
        if (!isFiring && !isBursting)
            fireTimer += Time.deltaTime;

        // Update FOV visualization color
        if (showFOV)
            UpdateFOVVisualization();
    }

    bool IsInFOV(Vector3 directionToTarget)
    {
        if (turretHead == null) return false;
        // Calculate horizontal angle
        Vector3 flatDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z);
        Vector3 flatForward = new Vector3(turretHead.forward.x, 0, turretHead.forward.z);
        float horizontalAngle = Vector3.Angle(flatForward, flatDirection);

        // Calculate vertical angle
        float verticalAngle = Vector3.Angle(turretHead.forward, directionToTarget);

        return horizontalAngle <= horizontalFOV / 2f && verticalAngle <= verticalFOV / 2f;
    }
    bool HasLineOfSight()
    {
        if (target == null || turretHead == null) return false;

        Vector3 rayOrigin = turretHead.position;
        Vector3 direction = (target.position - rayOrigin).normalized;
        float distance = Vector3.Distance(rayOrigin, target.position);

        // Raycast to check for obstacles
        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, distance, obstacleLayer))
        {
            // Check if we hit the player through the target layer
            if (((1 << hit.collider.gameObject.layer) & targetLayer) != 0)
            {
                return true;
            }
            return false;
        }

        return true;
    }
    void RotateTowardsTarget(Vector3 directionToTarget)
    {
        if (turretBase == null || turretHead == null) return;

        // Horizontal rotation (Y axis)
        Vector3 flatDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z);
        if (flatDirection != Vector3.zero)
        {
            Quaternion targetBaseRotation = Quaternion.LookRotation(flatDirection);

            // Clamp horizontal rotation
            float targetY = targetBaseRotation.eulerAngles.y;
            float currentY = turretBase.eulerAngles.y;

            // Convert to -180 to 180 range
            if (targetY > 180) targetY -= 360;
            if (currentY > 180) currentY -= 360;

            targetY = Mathf.Clamp(targetY, minHorizontalAngle, maxHorizontalAngle);

            Quaternion clampedBaseRotation = Quaternion.Euler(0, targetY, 0);
            turretBase.rotation = Quaternion.Slerp(turretBase.rotation, clampedBaseRotation, rotationSpeed * Time.deltaTime);
        }

        // Vertical rotation (X axis)
        float verticalAngle = Mathf.Asin(directionToTarget.y / directionToTarget.magnitude) * Mathf.Rad2Deg;
        verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);

        Quaternion targetHeadRotation = Quaternion.Euler(-verticalAngle, 0, 0);
        turretHead.localRotation = Quaternion.Slerp(turretHead.localRotation, targetHeadRotation, rotationSpeed * Time.deltaTime);
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
        if (fovMesh == null || turretHead == null) return;

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

        // Position and rotate the FOV visualization with the turret head
        if (fovMesh != null)
        {
            GameObject fovVisual = GameObject.Find("FOV_Visualization");
            if (fovVisual != null)
            {
                fovVisual.transform.position = turretHead.position;
                fovVisual.transform.rotation = turretHead.rotation;
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        if (!debugMode) return;

        // Draw range sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);

        // Draw FOV cone
        if (turretHead != null)
        {
            Gizmos.color = hasTarget ? Color.red : Color.green;

            // Draw horizontal FOV
            float halfFOV = horizontalFOV / 2f;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, turretHead.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, turretHead.up);
            Vector3 leftRayDirection = leftRayRotation * turretHead.forward;
            Vector3 rightRayDirection = rightRayRotation * turretHead.forward;

            Gizmos.DrawRay(turretHead.position, leftRayDirection * range);
            Gizmos.DrawRay(turretHead.position, rightRayDirection * range);

            // Draw vertical FOV
            Quaternion upRayRotation = Quaternion.AngleAxis(-verticalFOV / 2f, turretHead.right);
            Quaternion downRayRotation = Quaternion.AngleAxis(verticalFOV / 2f, turretHead.right);
            Vector3 upRayDirection = upRayRotation * turretHead.forward;
            Vector3 downRayDirection = downRayRotation * turretHead.forward;

            Gizmos.DrawRay(turretHead.position, upRayDirection * range);
            Gizmos.DrawRay(turretHead.position, downRayDirection * range);
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

    public int GetHealth() => health;
    public int GetMaxHealth() => 100; // make this configurable?
    public bool HasTarget() => hasTarget;
}