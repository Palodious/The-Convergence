using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class playerController : MonoBehaviour, IDamage, IPickup
{
    [SerializeField] CharacterController controller;
    [SerializeField] LayerMask ignoreLayer;  // ignore layers for shooting  

    [SerializeField] int HP;
    public int speed;
    [SerializeField] int sprintMod;
    [SerializeField] int JumpSpeed;
    [SerializeField] int maxJumps;
    [SerializeField] int gravity;  // gravity applied each frame  

    [SerializeField] int shootDamage;
    [SerializeField] int shootDist;
    [SerializeField] float shootRate;  // time between shots  

    [SerializeField] float glideGravity;  // lower gravity while gliding  
    [SerializeField] float crouchSpeedMod;
    [SerializeField] float crouchHeight;

    float originalHeight;  // remember height for uncrouch  
    int originalSpeed;     // store original speed  

    Vector3 moveDir;
    Vector3 playerVel;

    int jumpCount;
    int HPOrig;
    float shootTimer;

    bool isCrouching;  // crouch state  
    bool isGliding;    // glide state  

    // Modified by playerAbilities during surge
    [HideInInspector] public float damageBoost = 3f;

 
    [SerializeField] List<gunStats> gunList = new List<gunStats>();
    [SerializeField] GameObject gunModel;
    int gunListPos;

    void Start()
    {
        HPOrig = HP;
        originalHeight = controller.height;
        originalSpeed = speed;

        updatePlayerUI(); // fill HP bar at start

        restart();
    }

    void Update()
    {
        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);
        shootTimer += Time.deltaTime;

        movement();
        sprint();
    }

    void movement()
    {
        // Ground check
        if (controller.isGrounded)
        {
            if (playerVel.y < 0) playerVel.y = -2f;
            jumpCount = 0;
        }
        else
        {
            if (isGliding)
            {
                // FIXED GLIDE — smooth slow descent instead of dropping
                float targetFallSpeed = -glideGravity;
                playerVel.y = Mathf.Lerp(playerVel.y, targetFallSpeed, Time.deltaTime * 2f);
            }
            else
            {
                playerVel.y -= gravity * Time.deltaTime;
            }
        }

        // Movement
        moveDir = Input.GetAxis("Horizontal") * transform.right + Input.GetAxis("Vertical") * transform.forward;
        controller.Move(moveDir * speed * Time.deltaTime);

        // Jump
        jump();
        controller.Move(playerVel * Time.deltaTime);

        // Crouch
        if (Input.GetKey(KeyCode.C)) crouch();
        else uncrouch();

        // Glide
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
            playerVel.y = -0.5f;  // softer start  
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

    // UNIVERSAL PICKUP HANDLING
    public void GetItem(ScriptableObject item)
    {
        // Gun pickup
        if (item is gunStats gun)
        {
            getGunStats(gun);
            return;
        }

        // Medkit pickup
        if (item is medkitStats medkit)
        {
            HP += medkit.healAmount;

            if (HP > HPOrig)
                HP = HPOrig;

            updatePlayerUI();
            return;
        }

        Debug.LogWarning("Picked up unknown item: " + item.name);
    }

    // Existing gun pickup method
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

    public void restart()
    {
        controller.transform.position = gamemanager.instance.spawnPoint.transform.position;
        HP = HPOrig;
        updatePlayerUI();
    }
}