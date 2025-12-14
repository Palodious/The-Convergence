using UnityEngine;

public class PylonOrbit : MonoBehaviour
{
    public Transform riftCenter;
    public float orbitSpeed = 45f;
    public float orbitRadius = 1.5f;
    public int pylonIndex = 0;  // Set this to 0, 1, or 2 for each pylon

    void Start()
    {
        // Automatically spread out 3 pylons
        float angle = pylonIndex * 120f; // 0°, 120°, 240°
        Vector3 startPos = riftCenter.position + Quaternion.Euler(0, angle, 0) * Vector3.forward * orbitRadius;
        transform.position = startPos;
    }

    void Update()
    {
        transform.RotateAround(riftCenter.position, Vector3.up, orbitSpeed * Time.deltaTime);
    }
}