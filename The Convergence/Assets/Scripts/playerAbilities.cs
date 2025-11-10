using UnityEngine;
using System.Collections;

public class playerAbilities : MonoBehaviour
{
    [SerializeField] playerController controller; // Reference to playerController
    [SerializeField] GameObject surgeEffect; // Visual effect for surge
    [SerializeField] ParticleSystem jumpEffect;// Particle effect for jump
    [SerializeField] AudioClip jumpSound;   // Sound for jump

    AudioSource audioSource;
    CharacterController charController;

    [SerializeField] float surgeDuration ; // Duration of surge damage boost
    [SerializeField] float surgeDamageBoost ; // Multiplier for damage during surge
    [SerializeField] float surgeCooldown ;// Cooldown before next surge

    [SerializeField] float jumpDistance ;  // Distance jumped when using ability
    [SerializeField] float jumpCooldown; // Cooldown before next jump

    bool canSurge = true;
    bool canJump = true;
    bool isSurging = false;

    float surgeEndTime;

    void Awake()
    {
        controller = GetComponent<playerController>();
        audioSource = GetComponent<AudioSource>();
        charController = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Activate surge on E key
        if (Input.GetKeyDown(KeyCode.E) && canSurge)
            StartCoroutine(RiftSurge());

        // Activate jump on F key
        if (Input.GetKeyDown(KeyCode.F) && canJump)
            StartCoroutine(RiftJump());

        // End surge if duration expired
        if (isSurging && Time.time >= surgeEndTime)
            EndSurge();
    }

    IEnumerator RiftSurge()
    {
        canSurge = false;
        isSurging = true;
        surgeEndTime = Time.time + surgeDuration;

        if (surgeEffect != null)
            Instantiate(surgeEffect, transform.position, Quaternion.identity);

        // Apply damage boost
        controller.damageBoost = surgeDamageBoost;

        yield return new WaitForSeconds(surgeDuration);
        EndSurge();

        // Wait cooldown before allowing next surge
        yield return new WaitForSeconds(surgeCooldown);
        canSurge = true;
    }

    void EndSurge()
    {
        isSurging = false;
        if (controller != null)
        {
            controller.damageBoost = 1f; // Reset damage multiplier
        }
    }

    IEnumerator RiftJump()
    {
        canJump = false;

        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + transform.forward * jumpDistance;

        // Play particle effect at start
        if (jumpEffect != null)
            Instantiate(jumpEffect, startPos, Quaternion.identity);

        // Play jump sound
        if (audioSource != null && jumpSound != null)
            audioSource.PlayOneShot(jumpSound);

        // Move player
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

        // Play particle effect at landing
        if (jumpEffect != null)
            Instantiate(jumpEffect, targetPos, Quaternion.identity);

        // Wait cooldown before allowing next jump
        yield return new WaitForSeconds(jumpCooldown);
        canJump = true;
    }
}
