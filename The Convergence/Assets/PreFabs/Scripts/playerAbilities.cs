using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] playerController controller;
    [SerializeField] CharacterController charController;

 //rift Pulse
    [SerializeField] int pulseDamage = 25;
    [SerializeField] float pulseRange = 6f;
    [SerializeField] float pulseCooldown = 2.5f;

  //rift surge
    [SerializeField] float surgeDuration = 30f;
    [SerializeField] float surgeDamageBoost = 1.5f;
    [SerializeField] float surgeCooldown = 10f;

    //rift jump
    [SerializeField] float jumpDistance = 15f;
    [SerializeField] float jumpCooldown = 3f;
    [SerializeField] float jumpPrepTime = 0.3f;

    //masking layers
    [SerializeField] LayerMask enemyMask;
    [SerializeField] LayerMask environmentMask;

    // Timers
    float pulseTimer;
    float surgeTimer;
    float jumpTimer;

    // In surge
    bool isSurging;
    GameObject surgeEffect;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<playerController>();
        if (charController == null)
            charController = GetComponent<CharacterController>();


        // Set timers ready
        pulseTimer = pulseCooldown;
        surgeTimer = surgeCooldown;
        jumpTimer = jumpCooldown;
    }

    void Update()
    {
        // Update timers
        pulseTimer += Time.deltaTime;
        surgeTimer += Time.deltaTime;
        jumpTimer += Time.deltaTime;

        // Input handling
        if (Input.GetKeyDown(KeyCode.Q) && pulseTimer >= pulseCooldown)
            StartCoroutine(RiftPulse());

        if (Input.GetKeyDown(KeyCode.E) && surgeTimer >= surgeCooldown)
            StartCoroutine(RiftSurge());

        if (Input.GetKeyDown(KeyCode.F) && jumpTimer >= jumpCooldown)
            StartCoroutine(RiftJump());
    }

    IEnumerator RiftPulse()
    {
        pulseTimer = 0;
        GameObject pulseVFX = EffectsManager.Instance.Create("PulseCast", transform.position);
        SetEffectColor(pulseVFX, new Color(0.2f, 0.7f, 1f)); // Electric blue

        SFXManager.Instance.PlaySound("PulseCast");
        SFXManager.Instance.PlayElementSound("Lightning"); // Or use a unique "RiftPulse" SFX later

        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRange, enemyMask);
        foreach (Collider hit in hits)
        {
            // Deal damage
            IDamage dmg = hit.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(pulseDamage);

                // Spawn lightning impact
                EffectsManager.Instance.Create("Lightning", hit.transform.position);
            }

            // Apply knockback
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 knockDir = (hit.transform.position - transform.position).normalized;
                knockDir += Vector3.up * 0.3f; // Lift for stagger
                rb.AddForce(knockDir * 6f, ForceMode.Impulse);
            }
        }

        yield return null;
    }
    private void CreateChainLightning(Vector3 start, Vector3 end)
    {
        GameObject arc = EffectsManager.Instance.Create("ChainLightning", start);

        if (arc == null) return;
         
        // Try to set LineRenderer or Transform to stretch between points
        LineRenderer lr = arc.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }
        else
        {
            // Fallback: use transform forward or just let prefab handle it
            arc.transform.LookAt(end);
            Vector3 scale = arc.transform.localScale;
            scale.z = Vector3.Distance(start, end);
            arc.transform.localScale = scale;
        }

        // Auto-return after 0.15 seconds (duration of VFX)
        StartCoroutine(ReturnAfterDelay(arc, 0.15f));
    }
    private IEnumerator ReturnAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        EffectsManager.Instance.Return(effect);
    }

    IEnumerator RiftSurge()
    {
        if (surgeTimer < surgeCooldown) yield break;

        surgeTimer = 0;
        isSurging = true;

        //Apply damage boost
        controller.damageBoost = surgeDamageBoost;
        Debug.Log($"RIFT SURGE STARTED! Damage ×{surgeDamageBoost} for {surgeDuration:F0}s"); // Fixed string interpolation

        if (gamemanager.instance.surgeOverlay != null)
        {
            gamemanager.instance.surgeOverlay.SetActive(true);
            Image overlayImage = gamemanager.instance.surgeOverlay.GetComponent<Image>(); // Now correctly uses UnityEngine.UI.Image
            if (overlayImage != null)
            {
                overlayImage.color = new Color(0.2f, 0.6f, 1f, 0.3f); // Changed from .tintColor to .color
            }
        }
        else
        {
            Debug.LogWarning("surgeOverlay is not assigned in gamemanager");
        }

        //Play sounds
        SFXManager.Instance.PlaySound("SurgeStart");
        SFXManager.Instance.PlayLoopSound("SurgeLoop");

        //Spawn persistent VFX
        surgeEffect = EffectsManager.Instance.Create("Surge", transform.position);
        if (surgeEffect != null)
        {
            surgeEffect.transform.SetParent(transform);
            surgeEffect.transform.localPosition = Vector3.zero;
            surgeEffect.transform.localRotation = Quaternion.identity;
        }

        // Wait full duration
        yield return new WaitForSeconds(surgeDuration); // Simplified - no need for manual elapsed time tracking

        EndSurge();
    }

    void EndSurge()
    {
        if (!isSurging) return;

        isSurging = false;
        controller.damageBoost = 1f;

        if (gamemanager.instance.surgeOverlay != null)
        {
            gamemanager.instance.surgeOverlay.SetActive(false);
        }

        //Stop audio
        SFXManager.Instance.StopLoopSound();

        //Return VFX
        if (surgeEffect != null)
        {
            EffectsManager.Instance.Return(surgeEffect);
            surgeEffect = null;
        }

        Debug.Log($"Rift Surge ENDED — damage boost removed");
    }

    IEnumerator RiftJump()
    {
        jumpTimer = 0;

        Debug.Log($"Rift Jump STARTED - Prep phase");

        // Create prep effect
        GameObject prepEffect = EffectsManager.Instance.Create("JumpPrep", transform.position);
        SFXManager.Instance.PlaySound("JumpPrep");

        yield return new WaitForSeconds(jumpPrepTime);

        // Get safe position
        Vector3 targetPos = SafeJumpPosition();
        Debug.Log($"Jumping from {transform.position} to {targetPos} (Distance: {Vector3.Distance(transform.position, targetPos):F2}m)");

        // Teleport using CharacterController.Move instead of disabling
        Vector3 displacement = targetPos - transform.position;
        charController.Move(displacement);

        // Create impact effect
        EffectsManager.Instance.Create("JumpImpact", transform.position);
        SFXManager.Instance.PlaySound("JumpImpact");

        // Clean up prep effect (if it still exists and hasn't auto-returned)
        if (prepEffect != null && prepEffect.activeInHierarchy)
        {
            EffectsManager.Instance.Return(prepEffect);
        }

        Debug.Log($"Rift Jump COMPLETE");
    }

    Vector3 SafeJumpPosition()
    {
        Vector3 startPos = transform.position + Vector3.up * 0.5f; // Start slightly above ground
        Vector3 direction = transform.forward;
        float safeDistance = jumpDistance;

        float radius = charController.radius;
        float height = charController.height;

        Debug.Log($"Checking jump path: Direction={direction}, MaxDistance={jumpDistance}");

        // Single SphereCast from center of character
        if (Physics.SphereCast(startPos, radius, direction, out RaycastHit hit, jumpDistance, environmentMask))
        {
            safeDistance = Mathf.Max(0, hit.distance - radius - 0.5f); // Buffer of 0.5m
            Debug.Log($"Obstacle detected at {hit.distance:F2}m. Safe distance: {safeDistance:F2}m");
        }
        else
        {
            Debug.Log($"No obstacles - full distance jump");
        }

        Vector3 finalPos = startPos + direction * safeDistance;

        // Ground check - cast down from target position
        if (Physics.Raycast(finalPos + Vector3.up * 2f, Vector3.down, out RaycastHit groundHit, 5f, environmentMask))
        {
            finalPos.y = groundHit.point.y; // Snap to ground level
            Debug.Log($"Snapped to ground at Y={finalPos.y}");
        }
        else
        {
            Debug.LogWarning($"No ground detected at jump target! Keeping original Y position");
            finalPos.y = transform.position.y; // Fallback to current height
        }

        return finalPos;
    }

    void SetEffectColor(GameObject effect, Color color)
    {
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }
    }
}