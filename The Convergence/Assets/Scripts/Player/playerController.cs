using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Add this for SceneManager

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
    [SerializeField] AudioClip audMedkit; // Sound for using a medkit
    [Range(0, 1)][SerializeField] float audMedkitVol = 1f; // Volume for medkit use

    // CHANGE: Replace static bool with static int for key count
    public static int keyCount = 0;

    // Add static variables to persist data between scenes
    private static List<gunStats> persistentGunList = new List<gunStats>();
    private static int persistentGunListPos = 0;
    private static int persistentHP = 100;
    private static int persistentKeyCount = 0;

    // Track if we're starting from the first scene
    private static int lastSceneIndex = -1;

    public int ShootDamage => shootDamage;
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

    [Header("~=~= TPS / Aiming Settings =~=~")]
    public Transform aimTarget; // assign empty AimTarget in front of player
    public Transform rightHandIKTarget; // assign empty child at gun grip
    [HideInInspector] public bool isAiming; // true while holding Fire2
    [Range(0f, 1f)] public float aimMoveSpeed = 8f; // how fast upper-body aims/IK blends

    void Awake()
    {
        // Get the current scene index
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex == 1)
        {
            // This will persist between Editor play sessions
            int lastPlayedScene = PlayerPrefs.GetInt("LastPlayedScene", -1);

            // If we were not in scene 0 last time we played, reset persistent data
            if (lastPlayedScene != 1 && lastPlayedScene != -1)
            {
                ResetPersistentData();
            }

            // Update PlayerPrefs
            PlayerPrefs.SetInt("LastPlayedScene", currentSceneIndex);
            PlayerPrefs.Save();
        }
        else if (currentSceneIndex > 0)
        {
            // Save current scene to PlayerPrefs
            PlayerPrefs.SetInt("LastPlayedScene", currentSceneIndex);
            PlayerPrefs.Save();
        }

        lastSceneIndex = currentSceneIndex;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        HPOrig = HP;
        originalHeight = controller.height;
        originalSpeed = speed;

        // Check if we have persistent data (from previous scene)
        if (persistentGunList.Count > 0 && SceneManager.GetActiveScene().buildIndex > 0)
        {
            // Load persistent data (but not on first scene)
            LoadPersistentData();
        }
        else
        {
            // First scene or no persistent data - initialize fresh
            respawn();
        }
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
            // helpful debug ray showing camera center forward
            Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);

            shootTimer += Time.deltaTime;
            movement();
        }

        sprint();
    }

    void ResetPersistentData()
    {
        persistentGunList.Clear();
        persistentGunListPos = 0;
        persistentHP = HP; // Use the HP from inspector, not 100
        persistentKeyCount = 0;
        keyCount = 0;
        Debug.Log("Persistent data reset for new game session");
    }

    void LoadPersistentData()
    {
        // Clear current guns
        gunList.Clear();

        // Add persistent guns
        foreach (var gun in persistentGunList)
        {
            getGunStats(gun);
        }

        // Set current gun
        if (persistentGunList.Count > 0)
        {
            gunListPos = Mathf.Clamp(persistentGunListPos, 0, persistentGunList.Count - 1);
            changeGun();
        }

        // Set HP
        HP = persistentHP;
        if (HP > HPOrig) HP = HPOrig;

        // Set key count
        keyCount = persistentKeyCount;

        // Move to spawn point
        if (gamemanager.instance != null && gamemanager.instance.spawnPoint != null)
        {
            controller.enabled = false;
            transform.position = gamemanager.instance.spawnPoint.transform.position;
            controller.enabled = true;
        }

        updatePlayerUI();
    }

    void SavePersistentData()
    {
        // Save to static variables
        persistentGunList = new List<gunStats>(gunList);
        persistentGunListPos = gunListPos;
        persistentHP = HP;
        persistentKeyCount = keyCount;
        Debug.Log($"Saved persistent data: {gunList.Count} guns, {HP} HP, {keyCount} keys");
    }

    // Call this when transitioning to a new scene
    public void PrepareForSceneTransition()
    {
        SavePersistentData();
    }

    void movement()
    {
        // ... [rest of your movement code remains exactly the same] ...
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

        // Get input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Get camera vectors
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Calculate movement direction relative to camera
        moveDir = (camForward * v + camRight * h).normalized;

        // Apply movement
        controller.Move(moveDir * speed * Time.deltaTime);

        if (isAiming)
        {
            Vector3 aimDirection = camForward;
            if (aimDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
            }
        }
        else
        {
            if (v > 0.1f) // Moving forward
            {
                // Rotate to face movement direction
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }

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
        // ... [rest of playStep remains the same] ...
        isPlayingStep = true;
        aud.PlayOneShot(audStep[Random.Range(0, audStep.Length)], audStepVol);

        if (isSprinting) yield return new WaitForSeconds(0.3f);
        else yield return new WaitForSeconds(0.5f);

        isPlayingStep = false;
    }

    void sprint()
    {
        // ... [rest of sprint remains the same] ...
        if (Input.GetButtonDown("Sprint")) { speed *= sprintMod; isSprinting = true; }
        else if (Input.GetButtonUp("Sprint")) { speed /= sprintMod; isSprinting = false; }
    }

    void jump()
    {
        // ... [rest of jump remains the same] ...
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
        // ... [rest of crouch remains the same] ...
        if (!isCrouching)
        {
            isCrouching = true;
            controller.height = crouchHeight;
            speed = Mathf.RoundToInt(originalSpeed * crouchSpeedMod);
        }
    }

    void uncrouch()
    {
        // ... [rest of uncrouch remains the same] ...
        if (isCrouching)
        {
            isCrouching = false;
            controller.height = originalHeight;
            speed = originalSpeed;
        }
    }

    void StartGlide()
    {
        // ... [rest of StartGlide remains the same] ...
        if (!controller.isGrounded && !isGliding)
        {
            isGliding = true;
            playerVel.y = -0.5f;
        }
    }

    void StopGlide()
    {
        // ... [rest of StopGlide remains the same] ...
        if (isGliding) isGliding = false;
    }

    void shoot()
    {
        // ... [rest of shoot remains the same] ...
        if (gunList.Count == 0) return;

        shootTimer = 0;

        if (gunList.Count > 0)
        {
            aud.pitch = Random.Range(0.9f, 1.1f);
            gunStats gunPos = gunList[gunListPos];
            aud.PlayOneShot(gunPos.shootSound[Random.Range(0, gunPos.shootSound.Length)], gunPos.shootSoundVol);
        }

        // fire from gunModel toward the AimTarget
        if (aimTarget != null && gunModel != null)
        {
            Vector3 shootDir = (aimTarget.position - gunModel.transform.position).normalized;
            if (Physics.Raycast(gunModel.transform.position, shootDir, out RaycastHit hit, shootDist, ~ignoreLayer))
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
        else
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit2, shootDist, ~ignoreLayer))
            {
                IDamage dmg = hit2.collider.GetComponent<IDamage>();
                if (dmg != null)
                {
                    dmg.takeDamage(Mathf.RoundToInt(shootDamage * damageBoost));
                }

                if (gunList.Count > 0)
                    Instantiate(gunList[gunListPos].hitEffect, hit2.point, Quaternion.identity);
            }
        }
    }

    // IK is handled by a separate script (PlayerIKController) using rightHandIKTarget.
    public void takeDamage(int amount)
    {
        // ... [rest of takeDamage remains the same] ...
        HP -= amount;
        updatePlayerUI();
        StartCoroutine(screenFlashDamage());

        aud.pitch = Random.Range(0.9f, 1.1f);
        aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);

        if (HP <= 0) gamemanager.instance.youLose();
    }

    public void updatePlayerUI()
    {
        // ... [rest of updatePlayerUI remains the same] ...
        if (gamemanager.instance.playerHPBar != null)
            gamemanager.instance.playerHPBar.fillAmount = (float)HP / HPOrig;
    }

    IEnumerator screenFlashDamage()
    {
        // ... [rest of screenFlashDamage remains the same] ...
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
        // ... [rest of RestoreGunVisual remains the same] ...
        if (gunList == null || gunList.Count == 0)
            return;

        gunListPos = Mathf.Clamp(index, 0, gunList.Count - 1);
        changeGun();
    }

    public void GetItem(ScriptableObject item)
    {
        // ... [rest of GetItem remains the same] ...
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
        // ... [rest of GiveKey remains the same] ...
        keyCount += key.keyCount;
        Debug.Log($"Player picked up {key.keyCount} {key.keyName}(s)! Total keys: {keyCount}");

        // Look for any lights that weren't originally on the player
        Light[] allLights = GetComponentsInChildren<Light>();
        foreach (Light light in allLights)
        {
            // Check if this light is part of the player's original setup
            if (light != null && !light.gameObject.CompareTag("PlayerLight"))
            {
                light.enabled = false;
                Destroy(light);
            }
        }

        if (key.pickupEffect != null)
        {
            Instantiate(key.pickupEffect, transform.position, Quaternion.identity);
        }
    }

    // Updated to use key count instead of boolean
    public bool UseKey()
    {
        // ... [rest of UseKey remains the same] ...
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
        // ... [rest of UseMedkitFromPickup remains the same] ...
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

    public void getGunStats(gunStats gun)
    {
        // ... [rest of getGunStats remains the same] ...
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
        // ... [rest of changeGun remains the same] ...
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
        // ... [rest of selectGun remains the same] ...
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
        // ... [rest of respawn remains the same] ...
        Debug.Log("controller = " + controller);
        Debug.Log("gamemanager.instance = " + gamemanager.instance);
        Debug.Log("spawnPoint = " + gamemanager.instance?.spawnPoint);

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