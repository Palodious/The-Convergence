using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] playerController controller;
    [SerializeField] CharacterController charController;

    // Rift Pulse
    [Range(10, 50)][SerializeField] int pulseDamage;
    [Range(5f, 15f)][SerializeField] float pulseRange;
    [Range(2f, 15f)][SerializeField] float pulseCooldown;

    // Rift Jump
    [Range(10f, 25f)][SerializeField] float jumpDistance;
    [Range(0.01f, 15f)][SerializeField] float jumpCooldown;

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
        jumpTimer = jumpCooldown;
    }

    void Update()
    {
        //prevention of using abilities in popup menu
        if (gamemanager.instance != null &&
       (gamemanager.instance.isPaused || directionalPopup.PopupIsOpen))
            return;


        // Update timers
        pulseTimer += Time.deltaTime;
        surgeTimer += Time.deltaTime;
        jumpTimer += Time.deltaTime;

        // Input handling
        if (Input.GetKeyDown(KeyCode.Q) && pulseTimer >= pulseCooldown)
            StartCoroutine(RiftPulse());

        if (Input.GetKeyDown(KeyCode.F) && jumpTimer >= jumpCooldown)
            StartCoroutine(RiftJump());
    }


    IEnumerator RiftPulse()
    {
        if (gamemanager.instance != null &&
        (gamemanager.instance.isPaused || directionalPopup.PopupIsOpen))
            yield break;

        pulseTimer = 0;

        // Visual effect
        GameObject pulseVFX = EffectsManager.Instance.Create("PulseCast", transform.position);
        SetEffectColor(pulseVFX, new Color(0.2f, 0.7f, 1f));

        // Detect and destroy pylons
        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRange, enemyMask);

        foreach (Collider hit in hits)
        {
            // Damage enemies
            IDamage dmg = hit.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(Mathf.RoundToInt(pulseDamage * controller.damageBoost));
                EffectsManager.Instance.Create("Lightning", hit.transform.position);
            }

            // Knockback rigidbodies
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 knockDir = (hit.transform.position - transform.position).normalized + Vector3.up * 0.3f;
                rb.AddForce(knockDir * 6f, ForceMode.Impulse);
            }

            // Destroy pylons
            PylonController pylon = hit.GetComponent<PylonController>();
            if (pylon != null)
            {
                pylon.OnPulseHit();
                EffectsManager.Instance.Create("PylonDestroy", hit.transform.position);
            }
        }

        yield break; 
    }

    IEnumerator RiftJump()
    {
        if (gamemanager.instance != null &&
            (gamemanager.instance.isPaused || directionalPopup.PopupIsOpen))
            yield break;

        if (isJumping) yield break;

        jumpTimer = 0;
        isJumping = true;

        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;
        float distance = jumpDistance;

        float radius = charController != null ? charController.radius : 0.5f;
        float height = charController != null ? charController.height : 2f;

        Vector3 point1 = startPos + Vector3.up * (height / 2 - radius);
        Vector3 point2 = startPos + Vector3.up * radius;

        // Use CapsuleCast instead of Raycast to prevent clipping
        if (Physics.CapsuleCast(point1, point2, radius, direction, out RaycastHit hit, distance, environmentMask))
        {
            distance = Mathf.Max(0f, hit.distance - 0.1f); // stop just before wall
        }

        Vector3 targetPos = startPos + direction * distance;

        EffectsManager.Instance.Create("JumpPrep", startPos);

        if (charController != null)
        {
            if (charController.enabled)
                charController.enabled = false;

            transform.position = targetPos;

            charController.enabled = true;
        }
        else
        {
            transform.position = targetPos;
        }

        EffectsManager.Instance.Create("JumpImpact", targetPos);

        isJumping = false;
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