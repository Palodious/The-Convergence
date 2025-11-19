using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class playerAbilities : MonoBehaviour
{
    [Header("~=~= References =~=~")]
    [SerializeField] playerController controller;
    [SerializeField] CharacterController charController;
    [SerializeField] AudioSource audioSource;

    [Header("~=~= Rift Surge =~=~")]
    [Range(1f, 60f)][SerializeField] float surgeDuration = 5f;
    [Range(0.1f, 5f)][SerializeField] float surgeDamageBoost = 2f;
    [Range(0.1f, 30f)][SerializeField] float surgeCooldown = 8f;
    [SerializeField] GameObject surgeEffectPrefab;

    [Header("~=~= Rift Jump =~=~")]
    [Range(1f, 50f)][SerializeField] float jumpDistance = 8f;
    [Range(0.1f, 30f)][SerializeField] float jumpCooldown = 6f;
    [SerializeField] ParticleSystem jumpEffect;
    [SerializeField] AudioClip jumpSound;

    [Header("~=~= Internal State (Do Not Edit) =~=~")]
    float surgeEndTime;
    float jumpTimer;
    bool canSurge = true;
    bool canJump = true;
    bool isSurging = false;
    bool isJumping = false;
    int originalDamage;
    GameObject activeSurgeEffect;

    void Awake()
    {
        if (controller == null) controller = GetComponent<playerController>();
        if (charController == null) charController = GetComponent<CharacterController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        jumpTimer = jumpCooldown;

        if (controller != null)
        {
            originalDamage = controller.ShootDamage;
        }
    }

    void Update()
    {
        jumpTimer += Time.deltaTime;

        if (isSurging && Time.time >= surgeEndTime) EndSurge();

        // Input handling
        if (Input.GetKeyDown(KeyCode.E) && canSurge)
            TryActivateSurge();

        if (Input.GetKeyDown(KeyCode.F) && canJump)
            TryActivateJump();
    }

    void TryActivateSurge()
    {
        if (!canSurge) return;
        StartCoroutine(RiftSurge());
    }

    IEnumerator RiftSurge()
    {
        canSurge = false;
        isSurging = true;
        surgeEndTime = Time.time + surgeDuration;

        // Create surge effect
        if (surgeEffectPrefab != null)
        {
            activeSurgeEffect = Instantiate(surgeEffectPrefab, transform.position, Quaternion.identity);
            activeSurgeEffect.transform.SetParent(transform);
        }

        // Apply damage boost
        controller.damageBoost = surgeDamageBoost;

        yield return new WaitForSeconds(surgeDuration);
        EndSurge();

        yield return new WaitForSeconds(surgeCooldown);
        canSurge = true;
    }

    void EndSurge()
    {
        if (!isSurging) return;
        isSurging = false;

        // Reset stats
        if (controller != null)
        {
            controller.damageBoost = 1f;
        }

        // Clean up effects
        if (activeSurgeEffect != null)
        {
            EffectsManager.Instance.Return(activeSurgeEffect);
            activeSurgeEffect = null;
        }
    }

    void TryActivateJump()
    {
        if (!canJump) return;
        StartCoroutine(RiftJump());
    }

    IEnumerator RiftJump()
    {
        canJump = false;
        isJumping = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + transform.forward * jumpDistance;

        // Collider check
        float playerRadius = charController != null ? charController.radius : 0.5f;
        float playerHeight = charController != null ? charController.height : 2f;

        // Use a capsule check to see if the target position is free
        Vector3 point1 = targetPos + Vector3.up * (playerHeight / 2 - playerRadius);
        Vector3 point2 = targetPos + Vector3.up * playerRadius;
        if (Physics.CheckCapsule(point1, point2, playerRadius))
        {
            // Obstacle in the way, cancel jump
            Debug.Log("Jump blocked by obstacle!");
            isJumping = false;
            yield break;
        }

        // Jump prep effect
        if (jumpEffect != null)
            Instantiate(jumpEffect, startPos, Quaternion.identity);

        // Jump sound
        if (audioSource != null && jumpSound != null)
            audioSource.PlayOneShot(jumpSound);

        // Teleport player
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

        // Jump impact effect
        if (jumpEffect != null)
            Instantiate(jumpEffect, targetPos, Quaternion.identity);

        isJumping = false;

        yield return new WaitForSeconds(jumpCooldown);
        canJump = true;
    }
}