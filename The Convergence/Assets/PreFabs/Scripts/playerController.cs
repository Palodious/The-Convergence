using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class playerController : MonoBehaviour, IDamage, IPickup
{
    [SerializeField] CharacterController controller;
    [SerializeField] LayerMask ignoreLayer;

    [SerializeField] int HP;
    public int speed;
    [SerializeField] int sprintMod;
    [SerializeField] int JumpSpeed;
    [SerializeField] int maxJumps;
    [SerializeField] int gravity;

    [SerializeField] int shootDamage;
    [SerializeField] int shootDist;
    [SerializeField] float shootRate;

    [SerializeField] float glideGravity;  // lower gravity while gliding  
    [SerializeField] float crouchSpeedMod;
    [SerializeField] float crouchHeight;

    float originalHeight;// remember height for uncrouch  
    int originalSpeed; // store original speed  

    Vector3 moveDir;
    Vector3 playerVel;

    int jumpCount;
    int HPOrig;
    float shootTimer;

    bool isCrouching;  // crouch state  
    bool isGliding;// glide state  

    [HideInInspector] public float damageBoost = 1f;

    [SerializeField] List<gunStats> gunList = new List<gunStats>();
    [SerializeField] GameObject gunModel;
    int gunListPos;

   
    // Instant medkit use system
    private bool canUseMedkit = true;
    private float medkitCooldown = 0f;
    [SerializeField] private float medkitUseCooldown = 5f; // Cooldown between medkit uses
    void Start()
    {
        HPOrig = HP;
        originalHeight = controller.height;
        originalSpeed = speed;
        respawn();
    }

    void Update()
    {
        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);
        shootTimer += Time.deltaTime;

        movement();
        sprint();

        // handle medkit cooldown timer
        HandleMedkitCooldown();

        // Debug medkit use with H key
        if (Input.GetKeyDown(KeyCode.H) && canUseMedkit)
        {
            // For testing - create a temporary medkit
          UseMedkitInstantly(50); // Heal 50 HP
        }
    }

    void movement()
    {
        if (controller.isGrounded)
        {
            if (playerVel.y < 0) playerVel.y = -2f;
            jumpCount = 0;
        }
        else
        {
            if (isGliding)
            {
                float targetFallSpeed = -glideGravity;
                playerVel.y = Mathf.Lerp(playerVel.y, targetFallSpeed, Time.deltaTime * 2f);
            }
            else
            {
                playerVel.y -= gravity * Time.deltaTime;
            }
        }

        moveDir = Input.GetAxis("Horizontal") * transform.right + Input.GetAxis("Vertical") * transform.forward;
        controller.Move(moveDir * speed * Time.deltaTime);

        jump();
        controller.Move(playerVel * Time.deltaTime);

        if (Input.GetKey(KeyCode.C)) crouch();
        else uncrouch();

        if (!controller.isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.G)) StartGlide();
            if (Input.GetKeyUp(KeyCode.G)) StopGlide();
        }
        else if (isGliding) StopGlide();

        if (Input.GetButton("Fire1") && shootTimer >= shootRate)
        {
            shoot();
        }

        selectGun();
    }

    void sprint()
    {
        if (Input.GetButtonDown("Sprint")) speed *= sprintMod;
        else if (Input.GetButtonUp("Sprint")) speed /= sprintMod;
    }

    void jump()
    {
        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            playerVel.y = JumpSpeed;
            jumpCount++;
        }
    }

    void crouch()
    {
        if (!isCrouching)
        {
            isCrouching = true;
            controller.height = crouchHeight;
            speed = Mathf.RoundToInt(originalSpeed * crouchSpeedMod);
        }
    }

    void uncrouch()
    {
        if (isCrouching)
        {
            isCrouching = false;
            controller.height = originalHeight;
            speed = originalSpeed;
        }
    }

    void StartGlide()
    {
        if (!controller.isGrounded && !isGliding)
        {
            isGliding = true;
            playerVel.y = -0.5f;
        }
    }

    void StopGlide()
    {
        if (isGliding) isGliding = false;
    }

    void shoot()
    {
        shootTimer = 0;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, shootDist, ~ignoreLayer))
        {
            Debug.Log(hit.collider.name);

            IDamage dmg = hit.collider.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(Mathf.RoundToInt(shootDamage * damageBoost));
            }

            if (gunList.Count > 0)
                Instantiate(gunList[gunListPos].hitEffect, hit.point, Quaternion.identity);
        }
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        updatePlayerUI();
        StartCoroutine(screenFlashDamage());

        if (HP <= 0) gamemanager.instance.youLose();
    }

    public void updatePlayerUI()
    {
        if (gamemanager.instance.playerHPBar != null)
            gamemanager.instance.playerHPBar.fillAmount = (float)HP / HPOrig;
    }

    IEnumerator screenFlashDamage()
    {
        gamemanager.instance.playerDamagePanel.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        gamemanager.instance.playerDamagePanel.SetActive(false);
    }

    public int CurrentHP
    {
        get { return HP; }
        set { HP = value; }
    }

    public int GetHP() => HP;
    public void SetHP(int value)
    {
        HP = value;
        updatePlayerUI();
    }

    public void GetItem(ScriptableObject item)
    {
        if (item is gunStats gun)
        {
            getGunStats(gun);
            return;
        }

        if (item is medkitStats med)
        {
            // Auto-use medkit on pickup
            UseMedkitFromPickup(med);
            return;
        }

        Debug.LogWarning("Picked up unknown item: " + item.name);
    }
    // New method for instant medkit use from pickup
    public void UseMedkitFromPickup(medkitStats medkit)
    {
        int healAmount = medkit.healAmount;
        HP += healAmount;
        if (HP > HPOrig) HP = HPOrig;

        updatePlayerUI();

        Debug.Log($"Used medkit! Healed {healAmount} HP. Current HP: {HP}/{HPOrig}");

        if (medkit.useEffect != null)
            Instantiate(medkit.useEffect, transform.position, Quaternion.identity);
    }
    // Public method for instant medkit use with specified heal amount
    public void UseMedkitInstantly(int healAmount)
    {
        if (!canUseMedkit) return;

        HP += healAmount;
        if (HP > HPOrig) HP = HPOrig;

        updatePlayerUI();

        // Start cooldown
        canUseMedkit = false;
        medkitCooldown = medkitUseCooldown;

        Debug.Log($"Used medkit! Healed {healAmount} HP. Current HP: {HP}/{HPOrig}");
    }
    // Handle medkit cooldown
    public void HandleMedkitCooldown()
    {
        if (!canUseMedkit)
        {
            medkitCooldown -= Time.deltaTime;
            if (medkitCooldown <= 0f)
            {
                canUseMedkit = true;
                medkitCooldown = 0f;
            }
        }
    }

    // Check if medkit can be used
    public bool CanUseMedkit()
    {
        return canUseMedkit && HP < HPOrig;
    }
    public void getGunStats(gunStats gun)
    {
        gunList.Add(gun);
        gunListPos = gunList.Count - 1;
        changeGun();
    }

    void changeGun()
    {
        if (gunList.Count == 0) return;

        shootDamage = gunList[gunListPos].shootDamage;
        shootDist = gunList[gunListPos].shootDist;
        shootRate = gunList[gunListPos].shootRate;

        for (int i = gunModel.transform.childCount - 1; i >= 0; i--)
        {
            
            Destroy(gunModel.transform.GetChild(i).gameObject);
        }

        
        GameObject newGunModel = Instantiate(gunList[gunListPos].gunModel, gunModel.transform);

        
        newGunModel.transform.localPosition = Vector3.zero;
        newGunModel.transform.localRotation = Quaternion.identity;
    }

    void selectGun()
    {
        if (gunList.Count < 2) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

      
        if (scroll > 0f)
        {
          
            if (gunListPos < gunList.Count - 1)
            {
                gunListPos++;
                changeGun();
            }
        }
       
        else if (scroll < 0f)
        {
           
            if (gunListPos > 0)
            {
                gunListPos--;
                changeGun();
            }
        }
    }

    public void respawn()
    {
        controller.transform.position = gamemanager.instance.spawnPoint.transform.position;
        HP = HPOrig;
        updatePlayerUI();
    }
}
