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

    // Inventory medkit system
    medkitStats storedMedkit;// holds a medkit if storeInInventory = true
    bool medkitReady = true; // cooldown ready state
    float medkitCooldownTimer = 0f; // cooldown countdown timer

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

        // press H to use stored medkit
        if (Input.GetKeyDown(KeyCode.H))
            UseStoredMedkit();
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

        // Medkit handler
        if (item is medkitStats med)
        {
            if (!med.storeInInventory)
            {
                HP += med.healAmount;
                if (HP > HPOrig) HP = HPOrig;
                updatePlayerUI();
                return;
            }

            storedMedkit = med; //Store medkit for later use
            return;
        }

        Debug.LogWarning("Picked up unknown item: " + item.name);
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

        gunModel.GetComponent<MeshFilter>().sharedMesh =
            gunList[gunListPos].gunModel.GetComponent<MeshFilter>().sharedMesh;

        gunModel.GetComponent<MeshRenderer>().sharedMaterial =
            gunList[gunListPos].gunModel.GetComponent<MeshRenderer>().sharedMaterial;
    }

    void selectGun()
    {
        if (gunList.Count == 0) return;

        if (Input.GetAxis("Mouse ScrollWheel") > 0 && gunListPos < gunList.Count - 1)
        {
            gunListPos++;
            changeGun();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && gunListPos > 0)
        {
            gunListPos--;
            changeGun();
        }
    }

    //cooldown logic
    void HandleMedkitCooldown()
    {
        if (!medkitReady)
        {
            medkitCooldownTimer -= Time.deltaTime;

            if (medkitCooldownTimer <= 0)
                medkitReady = true;
        }
    }

    // use stored medkit
    void UseStoredMedkit()
    {
        if (storedMedkit == null) return;
        if (!medkitReady) return;

        HP += storedMedkit.healAmount;
        if (HP > HPOrig) HP = HPOrig;
        updatePlayerUI();

        medkitReady = false;
        medkitCooldownTimer = storedMedkit.cooldown;

        storedMedkit = null; // medkit destroyed after use
    }

    public void respawn()
    {
        controller.transform.position = gamemanager.instance.spawnPoint.transform.position;
        HP = HPOrig;
        updatePlayerUI();
    }
}
