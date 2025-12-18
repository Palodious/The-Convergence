using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Linq;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] playerController controller;
    [SerializeField] CharacterController charController;

    // Rift Pulse
    [Range(10, 50)][SerializeField] int pulseDamage;
    [Range(5f, 15f)][SerializeField] float pulseRange;
    [Range(2f, 15f)][SerializeField] float pulseCooldown;
    [Range(5f, 25f)][SerializeField] float pulseForce;


    // Rift Jump
    [Range(10f, 25f)][SerializeField] float jumpDistance;
    [Range(0.01f, 15f)][SerializeField] float jumpCooldown;

    // Layer masks
    [SerializeField] LayerMask enemyMask;
    [SerializeField] LayerMask environmentMask;

    // Timers
    float pulseTimer;
    float jumpTimer;

    // Ability states
    public bool isPulsing;
    public bool isJumping;
    GameObject pulseEffect;

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
        jumpTimer += Time.deltaTime;

        // Input handling
        if (Input.GetKeyDown(KeyCode.Q) && pulseTimer >= pulseCooldown)
            StartCoroutine(RiftPulse());

        if (Input.GetKeyDown(KeyCode.F) && jumpTimer >= jumpCooldown)
            StartCoroutine(RiftJump());
    }


    IEnumerator RiftPulse()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRange, enemyMask);
        Debug.Log($"Pulse fired. Hits: {hits.Length}");

        foreach (Collider hit in hits)
        {
            GameObject hitGO = hit.gameObject;
            Debug.Log($"Pulse hit: {hitGO.name} (layer: {LayerMask.LayerToName(hitGO.layer)})");

            // Robust IDamage lookup
            IDamage dmg = hitGO.GetComponent<IDamage>() ?? hitGO.GetComponentInParent<IDamage>();
            if (dmg == null)
            {
                foreach (var mb in hitGO.GetComponents<MonoBehaviour>())
                {
                    if (mb is IDamage) { dmg = (IDamage)mb; break; }
                }
            }

            int amount = Mathf.RoundToInt(pulseDamage * (controller != null ? controller.damageBoost : 1f));
            Debug.Log($"Computed damage: {amount}");

            if (dmg != null)
            {
                Debug.Log($"Applying {amount} damage to {hitGO.name}");
                dmg.takeDamage(amount);
                EffectsManager.Instance.Create("Lightning", hit.transform.position);
            }
            else
            {
                Debug.Log($"No IDamage on {hitGO.name}. Components: {string.Join(", ", hitGO.GetComponents<Component>().Select(c => c.GetType().Name))}");
            }

            // Knockback (use rb.velocity not linearVelocity)
            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                float desiredSpeed = pulseForce; // use your pulseForce or a tuned value
                Vector3 vDesired = dir * desiredSpeed;
                Vector3 deltaV = vDesired - rb.linearVelocity;
                Vector3 impulse = rb.mass * deltaV;
                rb.AddForce(impulse, ForceMode.Impulse);
                Debug.Log($"Applied knockback to {hitGO.name} impulse={impulse}");
            }

            // Pylon
            PylonController pylon = hit.GetComponent<PylonController>();
            if (pylon != null) { pylon.OnHit(); EffectsManager.Instance.Create("PylonDestroy", hit.transform.position); }
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
<<<<<<< Updated upstream
            distance = Mathf.Max(0f, hit.distance - 0.1f); // stop just before wall
=======
            // Stop just before the obstacle, reduce distance
            distance = hit.distance - 0.2f; // small offset so we don�t get stuck in the wall
>>>>>>> Stashed changes
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