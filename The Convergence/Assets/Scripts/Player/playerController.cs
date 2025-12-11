using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

    [Header("~=~= Keys =~=~")]
    [SerializeField] List<keyStats> keyList = new List<keyStats>();

    // Add static variables to persist data between scenes
    private static List<gunStats> persistentGunList = new List<gunStats>();
    private static List<keyStats> persistentKeyList = new List<keyStats>(); // NEW: Persistent key list
    private static int persistentGunListPos = 0;
    private static int persistentHP = 100;

    private static bool isNewGameSession = true;

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

    [HideInInspector] public gunStats activeGunStats;

    [Header("~=~= Medkit Settings =~=~")]
    private bool canUseMedkit = true;
    private float medkitCooldown = 0f;

    [Header("~=~= TPS / Aiming Settings =~=~")]
    public Transform aimTarget; // assign empty AimTarget in front of player
    public Transform rightHandIKTarget; // assign empty child at gun grip
    public Transform leftHandIKTarget; // assign empty child for left hand support
    private PlayerIKController ikController; // Reference to IK controller
    [HideInInspector] public bool isAiming; // true while holding Fire2
    [Range(0f, 1f)] public float aimMoveSpeed = 8f; // how fast upper-body aims/IK blends

    void Awake()
    {
        // Check if we're starting a new game session
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex == 1 && isNewGameSession)
        {
            // Reset all static data when starting from first scene
            ResetStaticData();
            isNewGameSession = false;
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        HPOrig = HP;
        originalHeight = controller.height;
        originalSpeed = speed;

        // Initialize IK system
        InitializeIKSystem();

        if (SaveManager.IsLoadingFromSave)
        {
            Debug.Log("playerController.Start: IsLoadingFromSave = true, skipping respawn/persistent init.");
            return;
        }

        // Check if we have persistent data (from previous scene)
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex > 1 && (persistentGunList.Count > 0 || persistentKeyList.Count > 0))
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

    void InitializeIKSystem()
    {
        // Get or add IK controller
        ikController = GetComponent<PlayerIKController>();
        if (ikController == null)
            ikController = GetComponentInChildren<PlayerIKController>();

        if (ikController == null)
        {
            Debug.LogWarning("No PlayerIKController found. Adding one...");
            ikController = gameObject.AddComponent<PlayerIKController>();
        }

        // Create IK target objects if they don't exist
        if (rightHandIKTarget == null)
        {
            GameObject rightIK = new GameObject("RightHandIKTarget");
            rightIK.transform.SetParent(transform);
            rightIK.transform.localPosition = new Vector3(0.2f, 1.5f, 0.3f); // Default position
            rightHandIKTarget = rightIK.transform;
        }

        if (leftHandIKTarget == null)
        {
            GameObject leftIK = new GameObject("LeftHandIKTarget");
            leftIK.transform.SetParent(transform);
            leftIK.transform.localPosition = new Vector3(-0.2f, 1.5f, 0.3f); // Default position
            leftHandIKTarget = leftIK.transform;
        }

        // Set up aim target if not assigned
        if (aimTarget == null && Camera.main != null)
        {
            GameObject aimObj = new GameObject("AimTarget");
            aimTarget = aimObj.transform;
            aimTarget.SetParent(Camera.main.transform);
            aimTarget.localPosition = Vector3.forward * 10f;
        }

        // Sync with IK controller
        if (ikController != null)
        {
            if (ikController.animator == null && animator != null)
                ikController.animator = animator;

            if (ikController.rightHandIKTarget == null)
                ikController.rightHandIKTarget = rightHandIKTarget;

            if (ikController.leftHandIKTarget == null)
                ikController.leftHandIKTarget = leftHandIKTarget;

            if (ikController.aimTarget == null)
                ikController.aimTarget = aimTarget;
        }
    }

    void ResetStaticData()
    {
        persistentGunList.Clear();
        persistentKeyList.Clear();
        persistentGunListPos = 0;
        persistentHP = HP; // Use the HP from inspector
        Debug.Log("Static data reset for new game session");
    }

    void LoadPersistentData()
    {
        // Clear current guns
        gunList.Clear();
        // Clear current keys
        keyList.Clear();

        // Add persistent guns
        foreach (var gun in persistentGunList)
        {
            getGunStats(gun);
        }

        // Add persistent keys
        foreach (var key in persistentKeyList)
        {
            AddKeyToList(key);
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
        persistentKeyList = new List<keyStats>(keyList);
        persistentGunListPos = gunListPos;
        persistentHP = HP;
    }

    // Call this when transitioning to a new scene
    public void PrepareForSceneTransition()
    {
        SavePersistentData();
    }

    void Update()
    {
        // read aiming input (hold-to-aim)
        isAiming = Input.GetButton("Fire2"); // right mouse by default

        // set animator param if available
        if (animator != null)
            animator.SetBool("IsAiming", isAiming);

        // Update IK based on aiming state
        if (isAiming)
            UpdateIKDuringAim();
        else
            UpdateIKDuringHipFire();

        if (!gamemanager.instance.isPaused)
        {
            // helpful debug ray showing camera center forward
            Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);

            shootTimer += Time.deltaTime;
            movement();
        }

        sprint();
    }

    void UpdateIKDuringAim()
    {
        if (!isAiming || ikController == null) return;

        // Set IK weights for aiming
        ikController.SetIKWeights(1f, 1f, 0.5f);

        // Update animator parameter for blend trees
        if (animator != null)
            animator.SetFloat("AimBlend", Mathf.Lerp(animator.GetFloat("AimBlend"), 1f, Time.deltaTime * aimMoveSpeed));
    }

    void UpdateIKDuringHipFire()
    {
        if (isAiming || ikController == null) return;

        // Set IK weights for hip fire (lower weights)
        ikController.SetIKWeights(0.3f, 0.1f, 0f);

        // Update animator parameter for blend trees
        if (animator != null)
            animator.SetFloat("AimBlend", Mathf.Lerp(animator.GetFloat("AimBlend"), 0f, Time.deltaTime * aimMoveSpeed));
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
        if (activeGunStats == null) return;

        if (shootTimer < activeGunStats.shootRate) return;
        shootTimer = 0;

        aud.pitch = Random.Range(0.9f, 1.1f);
        aud.PlayOneShot(activeGunStats.shootSound[Random.Range(0, activeGunStats.shootSound.Length)], activeGunStats.shootSoundVol);


        // fire from gunModel toward the AimTarget
        if (aimTarget != null && gunModel != null)
        {
            Vector3 shootDir = (aimTarget.position - gunModel.transform.position).normalized;
            if (Physics.Raycast(gunModel.transform.position, shootDir, out RaycastHit hit, activeGunStats.shootDist, ~ignoreLayer))
            {
                Debug.Log(hit.collider.name);

                IDamage dmg = hit.collider.GetComponent<IDamage>();
                if (dmg != null)
                {
                    dmg.takeDamage(Mathf.RoundToInt(activeGunStats.shootDamage * damageBoost));
                }

                Instantiate(activeGunStats.hitEffect, hit.point, Quaternion.identity);
            }
        }
        else
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit2, activeGunStats.shootDist, ~ignoreLayer))
            {
                IDamage dmg = hit2.collider.GetComponent<IDamage>();
                if (dmg != null)
                {
                    dmg.takeDamage(Mathf.RoundToInt(activeGunStats.shootDamage * damageBoost));
                }

                Instantiate(activeGunStats.hitEffect, hit2.point, Quaternion.identity);
            }
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
            AddKeyToList(key);
            return;
        }

        Debug.LogWarning("Picked up unknown item: " + item.name);
    }

    // Add key to list (similar to getGunStats for guns)
    void AddKeyToList(keyStats key)
    {
        if (!keyList.Contains(key))
        {
            keyList.Add(key);
            Debug.Log($"Player picked up {key.keyName}! Total keys: {keyList.Count}");

            // Remove any non-player lights
            Light[] allLights = GetComponentsInChildren<Light>();
            foreach (Light light in allLights)
            {
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
        else
        {
            Debug.Log($"Player already has {key.keyName}");
        }
    }

    // Check if player has a specific key
    public bool HasKey(keyStats key)
    {
        return keyList.Contains(key);
    }

    // Check if player has any key
    public bool HasAnyKey()
    {
        return keyList.Count > 0;
    }

    // Use/remove a specific key from the list
    public bool UseKey(keyStats key)
    {
        if (keyList.Contains(key))
        {
            keyList.Remove(key);
            Debug.Log($"Player used {key.keyName}! Keys remaining: {keyList.Count}");
            return true;
        }
        else
        {
            Debug.Log($"Player doesn't have {key.keyName}!");
            return false;
        }
    }

    // Use/remove any key
    public bool UseAnyKey()
    {
        if (keyList.Count > 0)
        {
            keyStats usedKey = keyList[0]; // Use the first key
            keyList.RemoveAt(0);
            Debug.Log($"Player used a key ({usedKey.keyName})! Keys remaining: {keyList.Count}");
            return true;
        }
        else
        {
            Debug.Log("No keys to use!");
            return false;
        }
    }

    // Get count of specific key type
    public int GetKeyCount(keyStats key)
    {
        int count = 0;
        foreach (var k in keyList)
        {
            if (k == key) count++;
        }
        return count;
    }

    // Get total key count
    public int GetTotalKeyCount()
    {
        return keyList.Count;
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

        activeGunStats = gunList[gunListPos];

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

        // Update IK targets from the new gun
        UpdateIKTargetsFromGun(newGunModel);
    }

    void UpdateIKTargetsFromGun(GameObject gunObject)
    {
        if (gunObject == null || ikController == null) return;

        // Find IK targets on the gun using recursive search
        Transform gunRightHandIK = FindDeepChild(gunObject.transform, "RightHandIK");
        Transform gunLeftHandIK = FindDeepChild(gunObject.transform, "LeftHandIK");

        // Pass the gun's IK targets to the IK controller
        ikController.UpdateGunIKTargets(gunRightHandIK, gunLeftHandIK);

        // If gun doesn't have IK targets, use our default targets
        if (gunRightHandIK == null)
        {
            // Position the right hand IK target at a reasonable default
            rightHandIKTarget.SetParent(gunObject.transform);
            rightHandIKTarget.localPosition = new Vector3(0.05f, -0.03f, 0.2f);
            rightHandIKTarget.localRotation = Quaternion.identity;

            // Tell IK controller to use our default target
            ikController.UpdateGunIKTargets(rightHandIKTarget, null);
        }

        if (gunLeftHandIK == null)
        {
            // Position the left hand IK target at a reasonable default
            leftHandIKTarget.SetParent(gunObject.transform);
            leftHandIKTarget.localPosition = new Vector3(-0.05f, -0.02f, 0.4f);
            leftHandIKTarget.localRotation = Quaternion.identity;

            // Tell IK controller to use our default target
            ikController.UpdateGunIKTargets(null, leftHandIKTarget);
        }
    }

    // Recursive helper to find deep children
    Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }
        return null;
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
        public List<keyStats> keyList; // Save key list
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
            keyList = new List<keyStats>(keyList) // Save key list
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

        // Restore key list
        if (data.keyList != null)
        {
            keyList = new List<keyStats>(data.keyList);
        }

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