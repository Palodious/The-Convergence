using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] playerController controller;
    [SerializeField] CharacterController charController;

    // Rift Pulse
    [SerializeField] int pulseDamage = 25;
    [SerializeField] float pulseRange = 6f;
    [SerializeField] float pulseCooldown = 2.5f;

    // Rift Surge
    [SerializeField] float surgeDuration = 30f;
    [SerializeField] float surgeDamageBoost = 1.5f;
    [SerializeField] float surgeCooldown = 10f;

    // Rift Jump
    [SerializeField] float jumpDistance = 15f;
    [SerializeField] float jumpCooldown = 3f;
    [SerializeField] float jumpPrepTime = 0.3f;

    // Layer masks
    [SerializeField] LayerMask enemyMask;
    [SerializeField] LayerMask environmentMask;

    // Timers
    float pulseTimer;
    float surgeTimer;
    float jumpTimer;

    // Ability states
    public bool isSurging;
    public bool isJumping;
    GameObject surgeEffect;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<playerController>();
        if (charController == null)
            charController = GetComponent<CharacterController>();

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
        SFXManager.Instance.PlayElementSound("Lightning");

        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRange, enemyMask);
        foreach (Collider hit in hits)
        {
            IDamage dmg = hit.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(Mathf.RoundToInt(pulseDamage * controller.damageBoost));
                EffectsManager.Instance.Create("Lightning", hit.transform.position);
            }

            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 knockDir = (hit.transform.position - transform.position).normalized + Vector3.up * 0.3f;
                rb.AddForce(knockDir * 6f, ForceMode.Impulse);
            }
        }

        yield return null;
    }

    private void CreateChainLightning(Vector3 start, Vector3 end)
    {
        GameObject arc = EffectsManager.Instance.Create("ChainLightning", start);
        if (arc == null) return;

        LineRenderer lr = arc.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }
        else
        {
            arc.transform.LookAt(end);
            Vector3 scale = arc.transform.localScale;
            scale.z = Vector3.Distance(start, end);
            arc.transform.localScale = scale;
        }

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
        controller.damageBoost = surgeDamageBoost;

        Debug.Log($"RIFT SURGE STARTED! Damage ×{surgeDamageBoost} for {surgeDuration:F0}s");

        if (gamemanager.instance.surgeOverlay != null)
        {
            gamemanager.instance.surgeOverlay.SetActive(true);
            Image overlayImage = gamemanager.instance.surgeOverlay.GetComponent<Image>();
            if (overlayImage != null)
                overlayImage.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        }

        SFXManager.Instance.PlaySound("SurgeStart");
        SFXManager.Instance.PlayLoopSound("SurgeLoop");

        surgeEffect = EffectsManager.Instance.Create("Surge", transform.position);
        if (surgeEffect != null)
        {
            surgeEffect.transform.SetParent(transform);
            surgeEffect.transform.localPosition = Vector3.zero;
            surgeEffect.transform.localRotation = Quaternion.identity;
        }

        yield return new WaitForSeconds(surgeDuration);

        EndSurge();
    }

    void EndSurge()
    {
        if (!isSurging) return;

        isSurging = false;
        controller.damageBoost = 1f;

        if (gamemanager.instance.surgeOverlay != null)
            gamemanager.instance.surgeOverlay.SetActive(false);

        SFXManager.Instance.StopLoopSound();

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
        isJumping = true;

        Debug.Log($"Rift Jump STARTED - Prep phase");

        GameObject prepEffect = EffectsManager.Instance.Create("JumpPrep", transform.position);
        SFXManager.Instance.PlaySound("JumpPrep");

        yield return new WaitForSeconds(jumpPrepTime);

        Vector3 targetPos = SafeJumpPosition();
        Debug.Log($"Jumping from {transform.position} to {targetPos} (Distance: {Vector3.Distance(transform.position, targetPos):F2}m)");

        // Temporarily disable controller to safely teleport
        charController.enabled = false;
        transform.position = targetPos;
        charController.enabled = true;

        EffectsManager.Instance.Create("JumpImpact", transform.position);
        SFXManager.Instance.PlaySound("JumpImpact");

        if (prepEffect != null && prepEffect.activeInHierarchy)
            EffectsManager.Instance.Return(prepEffect);

        isJumping = false;
        Debug.Log($"Rift Jump COMPLETE");
    }

    Vector3 SafeJumpPosition()
    {
        Vector3 startPos = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;
        float safeDistance = jumpDistance;

        float radius = charController.radius;

        if (Physics.SphereCast(startPos, radius, direction, out RaycastHit hit, jumpDistance, environmentMask))
        {
            safeDistance = Mathf.Max(0, hit.distance - radius - 0.5f);
        }

        Vector3 finalPos = startPos + direction * safeDistance;

        if (Physics.Raycast(finalPos + Vector3.up * 2f, Vector3.down, out RaycastHit groundHit, 5f, environmentMask))
            finalPos.y = groundHit.point.y;
        else
            finalPos.y = transform.position.y;

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
