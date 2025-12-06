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
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
