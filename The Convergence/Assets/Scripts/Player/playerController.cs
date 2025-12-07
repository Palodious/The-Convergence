using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class playerController : MonoBehaviour, IDamage, IPickup, ISaveable
{
    [Header("~=~= Components =~=~")]
    [SerializeField] CharacterController controller;
    [SerializeField] LayerMask ignoreLayer;
    public Animator animator; // assign your model's Animator here (added for aim/animation)

    [Header("~=~= Player Stats =~=~")]
    [Range(1, 1000)][SerializeField] int HP;
    [Range(1, 50)] public int speed;
    [Range(1, 10)][SerializeField] int sprintMod;
    [Range(1, 50)][SerializeField] int JumpSpeed;
    [Range(1, 10)][SerializeField] int maxJumps;
    [Range(1, 50)][SerializeField] int gravity;

    

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
    [SerializeField] AudioClip audMedkit; // Sound for using a medkit
    [Range(0, 1)][SerializeField] float audMedkitVol = 1f; // Volume for medkit use

    // CHANGE: Replace static bool with static int for key count
    public static int keyCount = 0;

    public int ShootDamage
    {
        get
        {
            if(gunList.Count > 0) 
                return gunList[gunListPos].shootDamage;
            return 0;  
        }
    }
    float originalHeight; // remember height for uncrouch  
    int originalSpeed; // store original speed  

    Vector3 moveDir;
    Vector3 playerVel;

    int jumpCount;
    int HPOrig;
    float shootTimer;

    bool isCrouching;  // crouch state  
    bool isGliding; // glide state  
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

    [Header("~=~= TPS / Aiming Settings =~=~")]
    public Transform aimTarget; // assign empty AimTarget in front of player
    public Transform rightHandIKTarget; // assign empty child at gun grip
    [HideInInspector] public bool isAiming; // true while holding Fire2
    [Range(0f, 1f)] public float aimMoveSpeed = 8f; // how fast upper-body aims/IK blends

    void Start()
    {
        HPOrig = HP;
        originalHeight = controller.height;
        originalSpeed = speed;
        respawn();
    }

    void Update()
    {
        // read aiming input (hold-to-aim)
        isAiming = Input.GetButton("Fire2"); // right mouse by default

        // set animator param if available
        if (animator != null)
            animator.SetBool("IsAiming", isAiming);

        if (!gamemanager.instance.isPaused)
        {
            // helpful debug ray showing camera center forward (not required)
            Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);

            shootTimer += Time.deltaTime;
            movement();
        }

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
            if (moveDir.normalized.magnitude > 0.3f && !isPlayingStep)
            {
                StartCoroutine(playStep());
            }
        }
        else
        {
            if (isGliding)
            {
                playerVel.y = -glideGravity;
            }
            else
            {
                playerVel.y -= gravity * Time.deltaTime;
            }
        }

        //camera-relative movement (keeps lower-body independent)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 camF = Camera.main.transform.forward;
        Vector3 camR = Camera.main.transform.right;
        camF.y = 0f;
        camR.y = 0f;
        camF.Normalize();
        camR.Normalize();

        Vector3 camMove = camR * h + camF * v;
        moveDir = camMove.normalized;

        // Move the character (lower-body will face movement direction when not aiming)
        controller.Move(moveDir * speed * Time.deltaTime);

        // If not aiming: rotate lower-body to face movement direction (classic TPS)
        if (!isAiming)
        {
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
        // If aiming, do NOT forcibly rotate lower-body: upper-body (spine) will aim independently (via IK script)

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
        float currentShootRate = float.MaxValue;

        if (gunList.Count > 0)
        {
            // Get the live fire rate from the currently equipped gun's stats object
            currentShootRate = gunList[gunListPos].shootRate;
        }

        // Use the dynamic rate for the shoot check
        if (Input.GetButton("Fire1") && shootTimer >= currentShootRate)
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
        if (gunList.Count == 0) return;
        gunStats currentGun = gunList[gunListPos];

        if (shootTimer >= currentGun.shootRate)
        {
            shootTimer = 0;

            if (currentGun.ammoCur <= 0)
            {
                return;
            }

            currentGun.ammoCur--;

            aud.pitch = Random.Range(0.9f, 1.1f);

            aud.PlayOneShot(currentGun.shootSound[Random.Range(0, currentGun.shootSound.Length)], currentGun.shootSoundVol);

            int currentDamage = currentGun.shootDamage;
            int currentDistance = currentGun.shootDist;

            Vector3 rayOrigin;
            Vector3 rayDirection;

            // fire from gunModel toward the AimTarget
            if (aimTarget != null && gunModel != null)
            {
                rayOrigin = gunModel.transform.position;
                rayDirection = (aimTarget.position - rayOrigin).normalized;

            }
            else
            {
                rayOrigin = Camera.main.transform.position;
                rayDirection = Camera.main.transform.forward;
            }
            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, currentDistance, ~ignoreLayer))
            {
                Debug.Log($"Hit: {hit.collider.name}");

                IDamage dmg = hit.collider.GetComponent<IDamage>();
                if (dmg != null)
                {
                    // Apply damage using the gun's damage stat
                    dmg.takeDamage(Mathf.RoundToInt(currentDamage * damageBoost));
                }

                // Instantiate hit effect
                Instantiate(currentGun.hitEffect, hit.point, Quaternion.identity);
            }

        }
    }

    // IK is handled by a separate script (PlayerIKController) using rightHandIKTarget.
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
        var gm = gamemanager.instance;
        if (gm == null || gm.playerDamagePanel == null)
            yield break;

        var panel = gm.playerDamagePanel;

        if (panel != null)
            panel.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        if (panel != null)
            panel.SetActive(false);
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

    // Expose which gun slot I'm using so the save system can store it.
    public int GetCurrentGunIndex()
    {
        return gunListPos;
    }

    // After loading, call this to rebuild the visual gun model.
    public void RestoreGunVisual(int index)
    {
        if (gunList == null || gunList.Count == 0)
            return;

        gunListPos = Mathf.Clamp(index, 0, gunList.Count - 1);
        changeGun();
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

        if (item is keyStats key)
        {
            GiveKey(key);
            return;
        }

        Debug.LogWarning("Picked up unknown item: " + item.name);
    }

    // Updated to use key count instead of boolean
    public void GiveKey(keyStats key)
    {
        keyCount += key.keyCount; // Add the key count from the ScriptableObject
        Debug.Log($"Player picked up {key.keyCount} {key.keyName}(s)! Total keys: {keyCount}");

        // Play pickup effect if available
        if (key.pickupEffect != null)
        {
            Instantiate(key.pickupEffect, transform.position, Quaternion.identity);
        }
    }

    // Updated to use key count instead of boolean
    public bool UseKey()
    {
        if (keyCount > 0)
        {
            keyCount--;
            Debug.Log($"Player used a key! Keys remaining: {keyCount}");
            return true;
        }
        else
        {
            Debug.Log("No keys to use!");
            return false;
        }
    }

    public void UseMedkitFromPickup(medkitStats medkit)
    {
        int healAmount = medkit.healAmount;
        HP += healAmount;
        if (HP > HPOrig) HP = HPOrig;

        updatePlayerUI();

        if (medkit.useEffect != null)
            Instantiate(medkit.useEffect, transform.position, Quaternion.identity);

        // Play medkit audio
        if (audMedkit != null)
        {
            aud.pitch = Random.Range(0.95f, 1.05f); // slight pitch variation
            aud.PlayOneShot(audMedkit, audMedkitVol);
        }
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
        public int gunListPos;
        public int keyCount; // Save key count
    }

    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state);

    public PlayerControllerSaveData CaptureState()
    {
        return new PlayerControllerSaveData
        {
            hp = HP,
            isCrouching = this.isCrouching,
            isGliding = this.isGliding,
            canUseMedkit = this.canUseMedkit,
            medkitCooldown = this.medkitCooldown,
            gunListPos = this.gunListPos,
            keyCount = playerController.keyCount // Save key count
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

        if (gunList != null && gunList.Count > 0)
        {
            gunListPos = Mathf.Clamp(data.gunListPos, 0, gunList.Count - 1);
            changeGun();
        }
        // Restore key count
        playerController.keyCount = data.keyCount;

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