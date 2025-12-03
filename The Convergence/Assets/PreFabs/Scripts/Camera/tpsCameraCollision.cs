using UnityEngine;

// We can put this on our TPS camera to stop it clipping through walls.
public class tpsCameraCollision : MonoBehaviour
{
    [Header("~=~= Target ~=~= ")]
    [SerializeField] Transform target;                                 // What the camera orbits
    [SerializeField] Vector3 targetOffset = new Vector3(0f, 1.6f, 0f); // Aim point roughly

    [Header("~=~= Distance Settings ~=~= ")]
    [Range(0.5f, 10f)][SerializeField] float defaultDistance = 4f; // Normal camera distance
    [Range(0.01f, 10f)][SerializeField] float minDistance = 0.5f;   // Closest allowed
    [Range(0.01f, 10f)][SerializeField] float maxDistance = 4f;     // Furthest allowed

    [Header("~=~= Collision ~=~= ")]
    [Range(0.01f, 1f)][SerializeField] float sphereRadius = 0.25f;  // Size of the collision sphere
    [SerializeField] LayerMask collisionMask;                       // Walls / level geometry layers
    [Range(0f, 0.5f)][SerializeField] float distanceSmooth = 0.05f; // How fast distance adjusts
    [Range(0f, 0.5f)][SerializeField] float positionSmooth = 0.05f; // How smooth the position moves

    float currentDistance;     // Distance actually used this frame
    Vector3 positionVelocity;  // For SmoothDamp

    void Start()
    {
        currentDistance = defaultDistance;

        // Auto-hook the player if I forgot to assign the target.
        if (target == null && gamemanager.instance != null && gamemanager.instance.player != null)
        {
            target = gamemanager.instance.player.transform;
        }
    }

    // Use LateUpdate so this runs after player movement.
    void LateUpdate()
    {
        if (target == null) return;

        // Point the camera is focusing on.
        Vector3 focusPoint = target.position + targetOffset;

        // Assume some other script already set the camera rotation.
        Vector3 desiredDir = -transform.forward;

        // Fallback if forward is bad.
        if (desiredDir.sqrMagnitude < 0.0001f)
        {
            desiredDir = -target.forward;
        }

        float desiredDistance = defaultDistance;

        // Check for obstacles between the player and the camera.
        if (Physics.SphereCast(
                focusPoint,
                sphereRadius,
                desiredDir,
                out RaycastHit hit,
                defaultDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            // Pull camera in front of the wall with a little padding.
            desiredDistance = Mathf.Clamp(hit.distance - 0.1f, minDistance, maxDistance);
        }
        else
        {
            // No hit, go back toward default.
            desiredDistance = defaultDistance;
        }

        // Smooth distance changes so it doesn’t pop.
        currentDistance = Mathf.Lerp(
            currentDistance,
            desiredDistance,
            1f - Mathf.Exp(-distanceSmooth * (Time.deltaTime * 60f))
        );

        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // Final desired position along the collision safe direction.
        Vector3 targetCamPos = focusPoint + desiredDir * currentDistance;

        // SmoothDamp into place for nice camera motion.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetCamPos,
            ref positionVelocity,
            positionSmooth
        );
    }
}
