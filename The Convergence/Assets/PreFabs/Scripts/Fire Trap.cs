using UnityEngine;
using System.Collections;

public class FireTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    public float damage = 10f;
    public float damageInterval = 1f;
    public float trapDuration = 3f;
    public float activationRange = 5f;

    [Header("Visual Effects")]
    public ParticleSystem fireEffect;
    public Light fireLight;
    public AudioClip fireSound;

    private bool isActive = false;
    private bool playerInRange = false;
    private AudioSource audioSource;
    private Coroutine damageCoroutine;
    private GameObject player;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        player = GameObject.FindGameObjectWithTag("Player");

        // Initially disable effects
        if (fireEffect != null)
            fireEffect.Stop();

        if (fireLight != null)
            fireLight.enabled = false;
    }

    void Update()
    {
        // Check if player is in activation range
        if (!isActive && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer <= activationRange)
            {
                ActivateTrap();
            }
        }
    }

    public void ActivateTrap()
    {
        if (isActive) return;

        isActive = true;
        StartCoroutine(TrapSequence());
    }

    private IEnumerator TrapSequence()
    {
        // Start visual and audio effects
        if (fireEffect != null)
            fireEffect.Play();

        if (fireLight != null)
            fireLight.enabled = true;

        if (fireSound != null && audioSource != null)
            audioSource.PlayOneShot(fireSound);

        // Start damaging player if in range
        damageCoroutine = StartCoroutine(ApplyDamage());

        // Run trap for specified duration
        yield return new WaitForSeconds(trapDuration);

        // Deactivate trap
        DeactivateTrap();
    }

    private IEnumerator ApplyDamage()
    {
        while (isActive)
        {
            if (playerInRange && player != null)
            {
                // Apply damage to player
                HP hp = player.GetComponent<HP>();
                if (hp != null)
                {
                    hp.TakeDamage(damage);
                    Debug.Log($"Player took {damage} damage from fire trap!");
                }
            }
            yield return new WaitForSeconds(damageInterval);
        }
    }:

    private void DeactivateTrap()
    {
        isActive = false;

        // Stop effects
        if (fireEffect != null)
            fireEffect.Stop();

        if (fireLight != null)
            fireLight.enabled = false;

        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}