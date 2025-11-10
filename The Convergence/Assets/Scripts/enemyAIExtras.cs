using System.Collections;
using UnityEngine;

public class enemyAIExtras : MonoBehaviour
{
    [SerializeField] bool useShield; // Toggles the shield system
    [SerializeField] int shieldHP; // Current shield HP
    [SerializeField] int shieldMaxHP; // Maximum shield HP
    [SerializeField] float shieldRegenDelay; // Time before the shield starts regenerating
    [SerializeField] GameObject shieldPrefab; // Shield visual prefab in the scene
    [SerializeField] Color shieldFlashColor = Color.white; // Color to flash when hit
    [SerializeField] float shieldFlashDuration = 0.1f; // How long the flash lasts

    bool shieldActive; // True when shield is active
    bool shieldBroken; // True when shield HP reaches zero

    void Start()
    {
        // Initialize shield if enabled
        if (useShield)
        {
            shieldActive = true;
            shieldBroken = false;
            shieldHP = shieldMaxHP;
            if (shieldPrefab != null)
                shieldPrefab.SetActive(true);
        }
    }

    public bool IsShieldActive()
    {
        return useShield && shieldPrefab != null && shieldPrefab.activeSelf;
    }

    public void takeShieldDamage(int amount)
    {
        // Prevents shield damage when disabled or inactive
        if (!useShield || !shieldActive) return;

        // Reduces shield HP by incoming damage
        shieldHP -= amount;

        // Flash shield to indicate it's been hit
        if (shieldPrefab != null)
            StartCoroutine(FlashShield());

        if (shieldHP <= 0)
        {
            // Deactivates shield prefab and starts regen delay
            shieldBroken = true;
            shieldActive = false;
            shieldHP = 0;

            if (shieldPrefab != null)
            {
                shieldPrefab.SetActive(false);
                Collider shieldCol = shieldPrefab.GetComponent<Collider>();
                if (shieldCol != null)
                    shieldCol.enabled = false;
            }

            StartCoroutine(shieldRegen());
        }
    }

    IEnumerator FlashShield()
    {
        Renderer shieldRenderer = shieldPrefab.GetComponent<Renderer>();
        if (shieldRenderer != null)
        {
            // Save the original color
            Color originalColor = shieldRenderer.material.color;

            // Flash the shield color
            shieldRenderer.material.color = shieldFlashColor;

            // Wait briefly
            yield return new WaitForSeconds(shieldFlashDuration);

            // Restore the original color
            shieldRenderer.material.color = originalColor;
        }
    }

    IEnumerator shieldRegen()
    {
        // Waits for delay before regenerating shield
        yield return new WaitForSeconds(shieldRegenDelay);
        shieldHP = shieldMaxHP;
        shieldActive = true;
        shieldBroken = false;
        if (shieldPrefab != null)
            shieldPrefab.SetActive(true);
    }
}
