using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] playerController controller;
    [SerializeField] CharacterController charController;

    [Header("~=~= Rift Pulse =~=~")]
    [Range(1, 200)][SerializeField] int pulseDamage = 25;
    [Range(1, 50)][SerializeField] float pulseRange = 6f;
    [Range(0.1f, 30f)][SerializeField] float pulseCooldown = 2.5f;

    [Header("~=~= Rift Surge =~=~")]
    [Range(0.1f, 60f)][SerializeField] float surgeDuration = 30f;
    [Range(1f, 10f)][SerializeField] float surgeDamageBoost = 1.5f;
    [Range(0.1f, 60f)][SerializeField] float surgeCooldown = 10f;

    [Header("~=~= Rift Jump =~=~")]
    [Range(1f, 50f)][SerializeField] float jumpDistance = 15f;
    [Range(0.1f, 30f)][SerializeField] float jumpCooldown = 3f;

    [Header("~=~= Layer Masks =~=~")]
    [SerializeField] LayerMask enemyMask;
    [SerializeField] LayerMask environmentMask;
    [SerializeField] LayerMask ignoreLayer;

    [Header("~=~= Timers (Internal State) =~=~")]
    float pulseTimer;
    float surgeTimer;
    float jumpTimer;

    [Header("~=~= Ability States =~=~")]
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
        SFXManager.Instance.PlaySound("Lightning"); // Changed from PlayElementSound

        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRange, enemyMask & ~ignoreLayer);
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

    IEnumerator RiftSurge()
    {
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

        Debug.Log("Rift Surge ENDED — damage boost removed");
    }

    IEnumerator RiftJump()
    {
        jumpTimer = 0;
        isJumping = true;

        Debug.Log("Rift Jump STARTED");

        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + transform.forward * jumpDistance;

        GameObject prepEffect = EffectsManager.Instance.Create("JumpPrep", startPos);
        SFXManager.Instance.PlaySound("JumpPrep");

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

        EffectsManager.Instance.Create("JumpImpact", targetPos);
        SFXManager.Instance.PlaySound("JumpImpact");

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
