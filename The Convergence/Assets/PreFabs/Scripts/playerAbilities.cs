
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerAbilities : MonoBehaviour
{
    [Header("~=~= References =~=~")]
    [SerializeField] playerController controller;
    [SerializeField] CharacterController charController;

    [Header("~=~=Rift Pulse =~=~")]
    [Range (15, 50)] int pulseDamage;
    [Range (5f, 10f)] float pulseRange;
    [Range (2f, 10f)] float pulseCooldown;

    [Header("~=~= Rift Surge =~=~")]
    [Range (15f, 50f)] float surgeDuration;
    [Range (1.25f, 5f)] float surgeDamageBoost;
    [Range (5f, 25f)] float surgeCooldown;

    [Header("~=~= Rift Jump =~=~")]
    [Range (10f, 25f)] float jumpDistance = 15f;
    [Range (2f, 10f)] float jumpCooldown = 3f;

    [Header("~=~= Layers =~=~")]
    [SerializeField] LayerMask enemyMask;
    [SerializeField] LayerMask environmentMask;

    [Header("~=~= Timers =~=~")]
    float pulseTimer;
    float surgeTimer;
    float jumpTimer;

    [Header("~=~= Ability states =~=~")]
    public bool isSurging = false;
    public bool isJumping = false;
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

        // Create pulse effect with sound
        GameObject pulseVFX = EffectsManager.Instance.Create("PulseCast", transform.position);
        SetEffectColor(pulseVFX, new Color(0.2f, 0.7f, 1f));

        // Find and damage enemies
        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRange, enemyMask);
        foreach (Collider hit in hits)
        {
            IDamage dmg = hit.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(Mathf.RoundToInt(pulseDamage * controller.damageBoost));

                // Create lightning impact with sound
                EffectsManager.Instance.Create("Lightning", hit.transform.position);
            }

            // Apply knockback
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 knockDir = (hit.transform.position - transform.position).normalized + Vector3.up * 0.3f;
                rb.AddForce(knockDir * 6f, ForceMode.Impulse);
            }
        }

        yield return null;
    }

    IEnumerator RiftSurge()
    {
        if (surgeTimer < surgeCooldown) yield break;

        surgeTimer = 0;
        isSurging = true;
        controller.damageBoost = surgeDamageBoost;

        Debug.Log($"RIFT SURGE STARTED! Damage ×{surgeDamageBoost} for {surgeDuration:F0}s");

        // UI overlay
        if (gamemanager.instance.surgeOverlay != null)
        {
            gamemanager.instance.surgeOverlay.SetActive(true);
            Image overlayImage = gamemanager.instance.surgeOverlay.GetComponent<Image>();
            if (overlayImage != null)
                overlayImage.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        }

        // Create surge VFX with sound (plays once when activated)
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

        // Play surge end sound
        EffectsManager.Instance.Create("SurgeEnd", transform.position);

        if (surgeEffect != null)
        {
            EffectsManager.Instance.Return(surgeEffect);
            surgeEffect = null;
        }

        Debug.Log("Rift Surge ENDED — damage boost removed");
    }

    IEnumerator RiftJump()
    {
        jumpTimer = 0;
        isJumping = true;

        Debug.Log("Rift Jump STARTED");

        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + transform.forward * jumpDistance;

        // Create jump prep with sound
        GameObject prepEffect = EffectsManager.Instance.Create("JumpPrep", startPos);

        // Teleport
        if (charController != null)
        {
            charController.enabled = false;
            transform.position = targetPos;
            charController.enabled = true;
        }
        else
        {
            transform.position = targetPos;
        }

        // Create jump impact with sound
        EffectsManager.Instance.Create("JumpImpact", targetPos);

        isJumping = false;
        Debug.Log($"Rift Jump COMPLETE - Jumped {Vector3.Distance(startPos, targetPos):F2}m");

        yield return null;
    }

    void SetEffectColor(GameObject effect, Color color)
    {
        if (effect == null) return;

        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }
    }
}