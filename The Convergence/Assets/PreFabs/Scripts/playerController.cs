using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class playerController : MonoBehaviour, IDamage, IPickup
{
    [Header("~=~= Components =~=~")]
    [SerializeField] CharacterController controller;
    [SerializeField] LayerMask ignoreLayer;

    [Header("~=~= Player Stats =~=~")]
    [Range(1, 1000)][SerializeField] int HP;
    [Range(1, 50)] public int speed;
    [Range(1, 10)][SerializeField] int sprintMod;
    [Range(1, 50)][SerializeField] int JumpSpeed;
    [Range(1, 10)][SerializeField] int maxJumps;
    [Range(1, 50)][SerializeField] int gravity;

    [Header("~=~= Shooting =~=~")]
    [Range(1, 100)][SerializeField] int shootDamage;
    [Range(1, 100)][SerializeField] int shootDist;
    [Range(0.01f, 5f)][SerializeField] float shootRate;

    [Header("~=~= Movement Modifiers =~=~")]
    [Range(0.1f, 50f)][SerializeField] float glideGravity;
    [Range(0.1f, 1f)][SerializeField] float crouchSpeedMod;
    [Range(0.1f, 5f)][SerializeField] float crouchHeight;

    [Header("~=~= Audio =~=~")]
    [SerializeField] AudioSource aud;
    [SerializeField] AudioClip[] audStep;
    [Range(0, 1)][SerializeField] float audStepVol;
    [SerializeField] AudioClip[] audJump;
    [Range(0, 1)][SerializeField] float audJumpVol;
    [SerializeField] AudioClip[] audHurt;
    [Range(0, 1)][SerializeField] float audHurtVol;

    public int ShootDamage => shootDamage;
    float originalHeight; // remember height for uncrouch  
    int originalSpeed; // store original speed  

    Vector3 moveDir;
    Vector3 playerVel;

    int jumpCount;
    int HPOrig;
    float shootTimer;

    bool isCrouching;  // crouch state  
    bool isGliding;    // glide state  
    bool isSprinting;
    bool isPlayingStep;

    [HideInInspector] public float damageBoost = 1f;

    [Header("~=~= Guns =~=~")]
    [SerializeField] List<gunStats> gunList = new List<gunStats>();
    [SerializeField] GameObject gunModel;
    int gunListPos;

    [Header("~=~= Medkit Settings =~=~")]
    private bool canUseMedkit = true;
    private float medkitCooldown = 0f;
    [Range(0.1f, 60f)][SerializeField] private float medkitUseCooldown = 5f; // Cooldown between medkit uses

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
    }

    void movement()
    {
        if (controller.isGrounded)
        {
            if (playerVel.y < 0) playerVel.y = -2f;
            jumpCount = 0;

            // play footstep audio if moving
            if (moveDir.normalized.magnitude > 0.3f && isPlayingStep)
            {
                StartCoroutine(playStep());
            }
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

    IEnumerator playStep()
    {
        isPlayingStep = true;
        aud.PlayOneShot(audStep[Random.Range(0, audStep.Length)], audStepVol);

        if (isSprinting) yield return new WaitForSeconds(0.3f);
        else yield return new WaitForSeconds(0.5f);

        isPlayingStep = false;
    }

    void sprint()
    {
        if (Input.GetButtonDown("Sprint")) { speed *= sprintMod; isSprinting = true; }
        else if (Input.GetButtonUp("Sprint")) { speed /= sprintMod; isSprinting = false; }
    }

    void jump()
    {
        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            playerVel.y = JumpSpeed;
            jumpCount++;
            aud.pitch = Random.Range(0.9f, 1.1f);
            aud.PlayOneShot(audJump[Random.Range(0, audJump.Length)], audJumpVol);
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

        if (gunList.Count > 0)
        {
            aud.pitch = Random.Range(0.9f, 1.1f);
            gunStats gunPos = gunList[gunListPos];
            aud.PlayOneShot(gunPos.shootSound[Random.Range(0, gunPos.shootSound.Length)], gunPos.shootSoundVol);
        }

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

        aud.pitch = Random.Range(0.9f, 1.1f);
        aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);

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
            UseMedkitFromPickup(med);
            return;
        }

        Debug.LogWarning("Picked up unknown item: " + item.name);
    }

    public void UseMedkitFromPickup(medkitStats medkit)
    {
        int healAmount = medkit.healAmount;
        HP += healAmount;
        if (HP > HPOrig) HP = HPOrig;

        updatePlayerUI();

        if (medkit.useEffect != null)
            Instantiate(medkit.useEffect, transform.position, Quaternion.identity);
    }

    public void UseMedkitInstantly(int healAmount)
    {
        if (!canUseMedkit) return;

        HP += healAmount;
        if (HP > HPOrig) HP = HPOrig;

        updatePlayerUI();

        canUseMedkit = false;
        medkitCooldown = medkitUseCooldown;
    }

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

    public bool CanUseMedkit()
    {
        return canUseMedkit && HP < HPOrig;
    }

    public void getGunStats(gunStats gun)
    {
        if (gunList.Contains(gun))
        {
            gunListPos = gunList.IndexOf(gun);
        }
        else
        {
            gunList.Add(gun);
            gunListPos = gunList.Count - 1;
        }

        changeGun();
    }

    void changeGun()
    {
        if (gunList.Count == 0) return;

        shootDamage = gunList[gunListPos].shootDamage;
        shootDist = gunList[gunListPos].shootDist;
        shootRate = gunList[gunListPos].shootRate;

        Transform[] children = new Transform[gunModel.transform.childCount];
        for (int i = 0; i < gunModel.transform.childCount; i++)
        {
            children[i] = gunModel.transform.GetChild(i);
        }

        foreach (Transform child in children)
        {
            Destroy(child.gameObject);
        }

        GameObject newGunModel = Instantiate(gunList[gunListPos].gunModel, gunModel.transform);
        newGunModel.transform.localPosition = Vector3.zero;
        newGunModel.transform.localRotation = Quaternion.identity;
    }

    void selectGun()
    {
        if (gunList.Count < 2) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f || scroll < 0f)
        {
            if (scroll > 0f)
            {
                gunListPos = (gunListPos + 1) % gunList.Count;
            }
            else
            {
                gunListPos = (gunListPos - 1 + gunList.Count) % gunList.Count;
            }

            changeGun();
        }
    }

    public void respawn()
    {
        controller.transform.position = gamemanager.instance.spawnPoint.transform.position;
        HP = HPOrig;
        updatePlayerUI();
    }

    [System.Serializable]
    public class PlayerControllerSaveData
    {
        public int hp;
        public bool isCrouching;
        public bool isGliding;
        public bool canUseMedkit;
        public float medkitCooldown;
    }

    public PlayerControllerSaveData CaptureState()
    {
        return new PlayerControllerSaveData
        {
            hp = HP,
            isCrouching = this.isCrouching,
            isGliding = this.isGliding,
            canUseMedkit = this.canUseMedkit,
            medkitCooldown = this.medkitCooldown
        };
    }

    public void RestoreState(object state)
    {
        var data = state as PlayerControllerSaveData;
        if (data == null) return;

        HP = data.hp;
        isCrouching = data.isCrouching;
        isGliding = data.isGliding;
        canUseMedkit = data.canUseMedkit;
        medkitCooldown = data.medkitCooldown;

        if (isCrouching)
        {
            controller.height = crouchHeight;
            speed = Mathf.RoundToInt(originalSpeed * crouchSpeedMod);
        }
        else
        {
            controller.height = originalHeight;
            speed = originalSpeed;
        }

        updatePlayerUI();
    }
}