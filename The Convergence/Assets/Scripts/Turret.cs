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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
