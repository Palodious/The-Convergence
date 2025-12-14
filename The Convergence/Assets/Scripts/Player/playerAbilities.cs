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
    [Range(2f, 15f)][SerializeField] float jumpCooldown;

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
        pulseTimer = 0;
        Debug.Log($"<color=cyan> PULSE ACTIVATED! Range: {pulseRange}m</color>");

        // Visualize the pulse range in Scene view
        Debug.DrawRay(transform.position, Vector3.forward * pulseRange, Color.red, 1f);
        Debug.DrawRay(transform.position, -Vector3.forward * pulseRange, Color.red, 1f);

        GameObject pulseVFX = EffectsManager.Instance.Create("PulseCast", transform.position);
        SetEffectColor(pulseVFX, new Color(0.2f, 0.7f, 1f));

        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRange, enemyMask);
        Debug.Log($"<color=yellow> Found {hits.Length} enemies</color>");

        foreach (Collider hit in hits)
        {
            Debug.Log($"<color=green> HIT: {hit.gameObject.name}</color>");

            PylonController pylon = hit.GetComponent<PylonController>();
            if (pylon != null)
            {
                Debug.Log($"<color=red> DESTROYING PYLON: {hit.gameObject.name}</color>");
                pylon.OnPulseHit();
                EffectsManager.Instance.Create("PylonDestroy", hit.transform.position);
            }
        }

        yield return null;
    }


    /* IEnumerator RiftPulse()
     {
         pulseTimer = 0;

         GameObject pulseVFX = EffectsManager.Instance.Create("PulseCast", transform.position);
         SetEffectColor(pulseVFX, new Color(0.2f, 0.7f, 1f));

         Collider[] hits = Physics.OverlapSphere(transform.position, pulseRange, enemyMask);
         foreach (Collider hit in hits)
         {
             Debug.Log("Pulse hit: " + hit.gameObject.name);

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

             PylonController pylon = hit.GetComponent<PylonController>();
             if (pylon != null)
             {
                 pylon.OnPulseHit();
                 EffectsManager.Instance.Create("PylonDestroy", hit.transform.position);
             }
         }

         yield return null;
     }*/

    IEnumerator RiftJump()
    {
        jumpTimer = 0;
        isJumping = true;

        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;
        float distance = jumpDistance;

        // Raycast to detect obstacles
        if (Physics.Raycast(startPos, direction, out RaycastHit hit, distance, environmentMask))
        {
            // Stop just before the obstacle, reduce distance
            distance = hit.distance - 0.2f; // small offset so we don’t get stuck in the wall
        }

        Vector3 targetPos = startPos + direction * distance;

        // Visual
        EffectsManager.Instance.Create("JumpPrep", startPos);

        if (charController != null)
        {
            charController.enabled = false;
            transform.position = targetPos;
            charController.enabled = true;
        }
        else
            transform.position = targetPos;

        EffectsManager.Instance.Create("JumpImpact", targetPos);

        isJumping = false;
        Debug.Log($"Rift Jump COMPLETE - Jumped {distance:F2}m");
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