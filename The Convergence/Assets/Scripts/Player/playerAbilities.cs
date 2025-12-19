using UnityEngine;
using System.Collections;

public class PlayerAbilities : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private playerController controller;
    [SerializeField] private CharacterController charController;

    [Header("Rift Pulse Settings")]
    [Range(10, 50)][SerializeField] private int pulseDamage = 20;
    [Range(5f, 15f)][SerializeField] private float pulseRange = 10f;
    [Range(2f, 15f)][SerializeField] private float pulseCooldown = 5f;
    [Range(5f, 25f)][SerializeField] private float pulseForce = 10f;

    [Header("Rift Jump Settings")]
    [Range(10f, 25f)][SerializeField] private float jumpDistance = 15f;
    [SerializeField] private float forcedJumpLock = 5f; // hard lock for 5 seconds

    [Header("Enemy Settings")]
    [SerializeField] private LayerMask enemyMask;

    // Cooldowns
    private float pulseTimer;

    // Jump lock
    private bool jumpLocked = false;

    // Ability states
    public bool isPulsing { get; private set; }
    public bool isJumping { get; private set; }

    void Start()
    {
        if (controller == null)
            controller = GetComponent<playerController>();

        if (charController == null)
            charController = GetComponent<CharacterController>();

        pulseTimer = pulseCooldown;
    }

    void Update()
    {
        // Block abilities if game is paused or popup is open
        if (gamemanager.instance != null && (gamemanager.instance.isPaused || directionalPopup.PopupIsOpen))
            return;

        // Update pulse timer
        if (pulseTimer < pulseCooldown)
            pulseTimer += Time.deltaTime;

        // Rift Pulse input
        if (Input.GetKeyDown(KeyCode.Q) && pulseTimer >= pulseCooldown)
            StartCoroutine(RiftPulse());

        // Rift Jump input (only if not locked)
        if (Input.GetKeyDown(KeyCode.F) && !jumpLocked)
        {
            StartCoroutine(RiftJump());
        }
    }

    IEnumerator RiftPulse()
    {
        pulseTimer = 0f;
        isPulsing = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRange, enemyMask);

        foreach (Collider hit in hits)
        {
            GameObject hitGO = hit.gameObject;

            // Deal damage
            IDamage dmg = hitGO.GetComponent<IDamage>() ?? hitGO.GetComponentInParent<IDamage>();
            int amount = Mathf.RoundToInt(pulseDamage * (controller != null ? controller.damageBoost : 1f));
            if (dmg != null)
            {
                dmg.takeDamage(amount);
                EffectsManager.Instance.Create("Lightning", hit.transform.position);
            }

            // Knockback
            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                rb.AddForce(dir * pulseForce, ForceMode.Impulse);
            }

            // Pylon hit
            PylonController pylon = hit.GetComponent<PylonController>();
            if (pylon != null)
            {
                pylon.OnHit();
                EffectsManager.Instance.Create("PylonDestroy", hit.transform.position);
            }
        }

        isPulsing = false;
        yield break;
    }

    IEnumerator RiftJump()
    {
        if (isJumping)
            yield break;

        isJumping = true;
        jumpLocked = true; // lock the jump immediately

        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward.normalized;
        float distance = jumpDistance;

        float radius = charController.radius;
        float height = charController.height;
        Vector3 center = charController.center;

        Vector3 bottom = startPos + center + Vector3.up * radius;
        Vector3 top = startPos + center + Vector3.up * (height - radius);

        // CapsuleCast to stop at obstacles
        if (Physics.CapsuleCast(bottom, top, radius, direction, out RaycastHit hit, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(0f, hit.distance - 0.15f); // safe buffer
        }

        if (distance <= 0.05f)
        {
            isJumping = false;
            StartCoroutine(JumpUnlockTimer());
            yield break;
        }

        Vector3 targetPos = startPos + direction * distance;

        // Pre-teleport effect
        EffectsManager.Instance.Create("JumpPrep", startPos);

        // Teleport safely
        charController.enabled = false;
        transform.position = targetPos;
        charController.enabled = true;

        // Post-teleport effect
        EffectsManager.Instance.Create("JumpImpact", targetPos);

        isJumping = false;

        // Start unlock timer
        StartCoroutine(JumpUnlockTimer());
        yield return null;
    }

    IEnumerator JumpUnlockTimer()
    {
        yield return new WaitForSeconds(forcedJumpLock);
        jumpLocked = false;
    }
}