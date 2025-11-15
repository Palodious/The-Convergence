using System.Collections;
using UnityEngine;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] playerController controller;
    [SerializeField] CharacterController charController;

 //rift Pulse
    [SerializeField] int pulseDamage = 25;
    [SerializeField] float pulseRange = 6f;
    [SerializeField] float pulseCooldown = 2.5f;

  //rift surge
    [SerializeField] float surgeDuration = 5f;
    [SerializeField] float surgeSpeedBoost = 1.5f;
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

    IEnumerator RiftSurge()
    {
        surgeTimer = 0;
        isSurging = true;

        // Create surge effect (follows player)
        surgeEffect = EffectsManager.Instance.Create("Surge", transform.position);
        surgeEffect.transform.SetParent(transform);

        // Play sounds
        SFXManager.Instance.PlaySound("SurgeStart");
        SFXManager.Instance.PlayLoopSound("SurgeLoop");

        // Apply buff
        controller.damageBoost = surgeDamageBoost;

        yield return new WaitForSeconds(surgeDuration);

        EndSurge();
    }

    void EndSurge()
    {
        isSurging = false;
        controller.damageBoost = 1f;

        // Stop effects
        SFXManager.Instance.StopLoopSound();
        if (surgeEffect != null)
            EffectsManager.Instance.Return(surgeEffect);
    }

    IEnumerator RiftJump()
    {
        jumpTimer = 0;

        // Create prep effect
        GameObject prepEffect = EffectsManager.Instance.Create("JumpPrep", transform.position);
        SFXManager.Instance.PlaySound("JumpPrep");

        yield return new WaitForSeconds(jumpPrepTime);

        // Get safe position
        Vector3 targetPos = GetSafeJumpPosition();

        // Teleport
        charController.enabled = false;
        transform.position = targetPos;
        charController.enabled = true;

        // Create impact effect
        EffectsManager.Instance.Create("JumpImpact", transform.position);
        SFXManager.Instance.PlaySound("JumpImpact");

        // Clean up prep effect
        EffectsManager.Instance.Return(prepEffect);
    }

    Vector3 GetSafeJumpPosition()
    {
        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;
        float safeDistance = jumpDistance;

        float radius = charController.radius;
        float height = charController.height;

        // Check at different heights
        float[] testHeights = { 0.1f, height / 2f, height - 0.1f };

        foreach (float testHeight in testHeights)
        {
            Vector3 testPoint = startPos + Vector3.up * testHeight;

            if (Physics.SphereCast(testPoint, radius, direction, out RaycastHit hit, jumpDistance, environmentMask))
            {
                if (hit.distance < safeDistance)
                    safeDistance = hit.distance - radius;
            }
        }

        // Safety buffer
        safeDistance = Mathf.Max(0, safeDistance - 0.2f);

        Vector3 finalPos = startPos + direction * safeDistance;

        // Snap to ground
        if (Physics.Raycast(finalPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 1f, environmentMask))
        {
            finalPos = groundHit.point;
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