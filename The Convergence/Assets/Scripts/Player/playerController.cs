using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class playerController : MonoBehaviour, IDamage, IPickup, ISaveable, IAmmo
{
    [Header("~=~= Components =~=~")]
    [SerializeField] CharacterController controller;
    [SerializeField] LayerMask ignoreLayer;
    public Animator animator;

    [Header("~=~= Player Stats =~=~")]
    [Range(1, 300)][SerializeField] int HP;
    [Range(1, 50)] public int speed;
    [Range(1, 10)][SerializeField] int sprintMod;
    [Range(1, 50)][SerializeField] int JumpSpeed;
    [Range(1, 50)][SerializeField] int gravity;
    bool hasJumped;

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
    [SerializeField] AudioClip audMedkit;
    [Range(0, 1)][SerializeField] float audMedkitVol = 1f;

    [Header("~=~= Keys =~=~")]
    [SerializeField] List<keyStats> keyList = new List<keyStats>();

    private static List<gunStats> persistentGunList = new List<gunStats>();
    private static List<keyStats> persistentKeyList = new List<keyStats>();
    private static int persistentGunListPos = 0;
    private static int persistentHP = 100;
    private static int persistentMaxHP = 100;
    private static int persistentHealthUpgradeTotal = 0;
    private static bool isNewGameSession = true;

    public int ShootDamage => shootDamage;
    float originalHeight;
    int originalSpeed;

    Vector3 moveDir;
    Vector3 playerVel;

    int jumpCount;
    [SerializeField] int currentMaxHP;
    int intialHP;
    float shootTimer;

    bool isCrouching;
    bool isGliding;
    bool isSprinting;
    bool isPlayingStep;

    [HideInInspector] public float damageBoost = 1f;

    [Header("~=~= Guns =~=~")]
    [SerializeField] List<gunStats> gunList = new List<gunStats>();
    [SerializeField] GameObject gunModel;
    int gunListPos;

    [HideInInspector] public gunStats activeGunStats;

    [System.Serializable]
    public class GunAmmoData
    {
        public gunStats gunType;
        public int currentAmmo;
        public int reserveAmmo;

        public GunAmmoData(gunStats gun)
        {
            gunType = gun;
            currentAmmo = gun.ammoMax;
            reserveAmmo = gun.ammoMax * 3;
        }
    }

    [SerializeField] private List<GunAmmoData> gunAmmoInventory = new List<GunAmmoData>();

    [Header("~=~= Medkit Settings =~=~")]
    private bool canUseMedkit = true;
    private float medkitCooldown = 0f;

    [Header("~=~= TPS / Aiming Settings =~=~")]
    public Transform aimTarget; // assign empty AimTarget in front of player
    public Transform rightHandIKTarget; // assign empty child at gun grip
    public Transform leftHandIKTarget; // assign empty child for left hand support
    private PlayerIKController ikController;

    [Header("~=~= Reload Settings =~=~")]
    [SerializeField] AudioClip reloadSound;
    [Range(0, 1)][SerializeField] float reloadSoundVol = 1f;
    [SerializeField] float reloadTime = 1.5f;
    private bool isReloading = false;
    private Coroutine reloadCoroutine;

    [Header("~=~= UI References =~=~")]
    [SerializeField] private TMPro.TextMeshProUGUI ammoTextDisplay;

    [Header("~=~= Death Settings =~=~")]
    [SerializeField] private float deathAnimationTime = 2f;
    [SerializeField] private AudioClip deathSound;
    [Range(0, 1)][SerializeField] private float deathSoundVol = 1f;
    private bool isDead = false;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // Check if we're starting a new game session
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex == 1 && isNewGameSession)
        {
            ResetStaticData();
            isNewGameSession = false;
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        intialHP = HP;
        currentMaxHP = intialHP + persistentHealthUpgradeTotal;
        originalHeight = controller.height;
        originalSpeed = speed;

        InitializeIKSystem();

        if (ammoTextDisplay == null)
        {
            // Look for ammo UI in the scene
            GameObject ammoUI = GameObject.FindGameObjectWithTag("AmmoUI");
            if (ammoUI != null)
                ammoTextDisplay = ammoUI.GetComponent<TMPro.TextMeshProUGUI>();
        }

        if (SaveManager.IsLoadingFromSave)
        {
            Debug.Log("playerController.Start: IsLoadingFromSave = true, skipping respawn/persistent init.");
            return;
        }

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex > 1 && (persistentGunList.Count > 0 || persistentKeyList.Count > 0))
        {
            // Load persistent data (but not on first scene)
            LoadPersistentData();
        }
        else
        {
            respawn();
        }
        updatePlayerUI();
        UpdateAmmoDisplay();
    }

    void InitializeIKSystem()
    {
        ikController = GetComponent<PlayerIKController>();
        if (ikController == null)
            ikController = GetComponentInChildren<PlayerIKController>();

        if (ikController == null)
        {
            Debug.LogWarning("No PlayerIKController found. Adding one...");
            ikController = gameObject.AddComponent<PlayerIKController>();
        }

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
            leftIK.transform.localPosition = new Vector3(-0.2f, 1.5f, 0.3f);
            leftHandIKTarget = leftIK.transform;
        }

        if (aimTarget == null && Camera.main != null)
        {
            GameObject aimObj = new GameObject("AimTarget");
            aimTarget = aimObj.transform;
            aimTarget.SetParent(Camera.main.transform);
            aimTarget.localPosition = Vector3.forward * 10f;
        }

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
        persistentHP = HP;
        persistentMaxHP = HP;
        persistentHealthUpgradeTotal = 0;
        Debug.Log("Static data reset for new game session");
    }

    void LoadPersistentData()
    {
        gunList.Clear();
        keyList.Clear();

        foreach (var gun in persistentGunList)
        {
            getGunStats(gun);
        }

        foreach (var key in persistentKeyList)
        {
            AddKeyToList(key);
        }

        if (persistentGunList.Count > 0)
        {
            gunListPos = Mathf.Clamp(persistentGunListPos, 0, persistentGunList.Count - 1);
            changeGun();
        }

        HP = persistentHP;
        currentMaxHP = intialHP + persistentHealthUpgradeTotal;

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
        persistentGunList = new List<gunStats>(gunList);
        persistentKeyList = new List<keyStats>(keyList);
        persistentGunListPos = gunListPos;
        persistentHP = HP;
        persistentMaxHP = currentMaxHP;
        persistentHealthUpgradeTotal = currentMaxHP - intialHP;
    }

    public void PrepareForSceneTransition()
    {
        SavePersistentData();
    }

    void Update()
    {
        if (!gamemanager.instance.isPaused)
        {
            Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);

            shootTimer += Time.deltaTime;
            movement();
        }

        sprint();
    }

    void movement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 inputDirection = new Vector3(h, 0, v);

        if (inputDirection.magnitude > 0.1f)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = Camera.main.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            Vector3 moveInput = (cameraForward * v + cameraRight * h).normalized;

            Vector3 localMove = transform.InverseTransformDirection(moveInput);

            float animX = localMove.x;
            float animY = localMove.z;

            if (animator != null)
            {
                animator.SetFloat("MoveX", animX, 0.1f, Time.deltaTime);
                animator.SetFloat("MoveY", isSprinting ? animY * 1.3f : animY, 0.1f, Time.deltaTime);
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetFloat("MoveX", 0, 0.1f, Time.deltaTime);
                animator.SetFloat("MoveY", 0, 0.1f, Time.deltaTime);
            }
        }

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveDir = camForward * v + camRight * h;

        if (controller.isGrounded)
        {
            if (playerVel.y < 0)
                playerVel.y = -2f;

            if (moveDir.magnitude > 0.3f && !isPlayingStep)
            {
                StartCoroutine(playStep());
            }
        }
        else
        {
            if (isGliding)
                playerVel.y = -glideGravity;
            else
                playerVel.y -= gravity * Time.deltaTime;
        }

        if (camForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        }

        jump();

        Vector3 horizontalMove = moveDir.normalized * speed * Mathf.Clamp01(new Vector2(h, v).magnitude);
        Vector3 finalMove = horizontalMove;
        finalMove.y = playerVel.y;

        controller.Move(finalMove * Time.deltaTime);

        if (Input.GetKey(KeyCode.C)) crouch();
        else uncrouch();

        if (!controller.isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.G)) StartGlide();
            if (Input.GetKeyUp(KeyCode.G)) StopGlide();
        }
        else if (isGliding)
        {
            StopGlide();
        }

        if (Input.GetButton("Fire1") && shootTimer >= shootRate && !isReloading)
        {
            shoot();
        }

        if (Input.GetKeyDown(KeyCode.R) && !isReloading && activeGunStats != null)
        {
            Reload();
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
        bool forwardInput = Input.GetAxis("Vertical") > 0.1f;

        isSprinting =
            Input.GetKey(KeyCode.LeftShift) &&
            controller.isGrounded &&
            forwardInput &&
            !isCrouching;

        speed = isSprinting ? originalSpeed * sprintMod : originalSpeed;
    }

    void jump()
    {
        if (Input.GetButtonDown("Jump") && controller.isGrounded && !hasJumped)
        {
            hasJumped = true;
            playerVel.y = JumpSpeed;

            if (animator != null)
            {
                animator.SetTrigger("IsJumping");
            }

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
        if (activeGunStats == null || isReloading) return;

        gunStats originalGunStats = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunType == originalGunStats);
        if (ammoData == null || ammoData.currentAmmo <= 0)
        {
            Reload();
            return;
        }

        if (shootTimer < activeGunStats.shootRate) return;
        shootTimer = 0;

        if (ammoData != null)
        {
            ammoData.currentAmmo--;
        }

        aud.pitch = Random.Range(0.9f, 1.1f);
        aud.PlayOneShot(activeGunStats.shootSound[Random.Range(0, activeGunStats.shootSound.Length)], activeGunStats.shootSoundVol);

        UpdateAmmoDisplay();

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

    public void Reload()
    {
        if (activeGunStats == null || isReloading) return;

        gunStats originalGunStats = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunType == originalGunStats);
        if (ammoData == null || ammoData.currentAmmo >= activeGunStats.ammoMax) return;

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }
        reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        if (reloadSound != null)
        {
            aud.pitch = Random.Range(0.95f, 1.05f);
            aud.PlayOneShot(reloadSound, reloadSoundVol);
        }

        if (animator != null)
        {
            animator.SetTrigger("Reload");
        }

        yield return new WaitForSeconds(reloadTime);

        // Calculate how much ammo to reload from reserve
        gunStats originalGunStats = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunType == originalGunStats);
        if (ammoData != null)
        {
            int ammoNeeded = activeGunStats.ammoMax - ammoData.currentAmmo;
            int ammoToTake = Mathf.Min(ammoNeeded, ammoData.reserveAmmo);

            ammoData.currentAmmo += ammoToTake;
            ammoData.reserveAmmo -= ammoToTake;
        }

        UpdateAmmoDisplay();

        isReloading = false;
        reloadCoroutine = null;
    }

    public void UpdateAmmoDisplay()
    {
        if (activeGunStats == null) return;

        if (ammoTextDisplay != null)
        {
            gunStats originalGunStats = gunList[gunListPos];
            GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunType == originalGunStats);
            if (ammoData != null)
            {
                ammoTextDisplay.text = $"{ammoData.currentAmmo}/{ammoData.reserveAmmo}";
            }
        }
        else
        {
            Debug.LogError("Ammo Text Display (TMPro component) is NULL in playerController!");
        }
    }

    public void takeDamage(int amount)
    {
        if (isDead) return; // Don't take damage if already dead

        HP -= amount;
        updatePlayerUI();
        StartCoroutine(screenFlashDamage());

        aud.pitch = Random.Range(0.9f, 1.1f);
        aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);

        if (HP <= 0 && !isDead)
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        isDead = true;

        controller.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        if (deathSound != null)
        {
            aud.pitch = 1f;
            aud.PlayOneShot(deathSound, deathSoundVol);
        }

        enabled = false;

        yield return new WaitForSeconds(deathAnimationTime);

        gamemanager.instance.youLose();
    }

    public void updatePlayerUI()
    {
        if (gamemanager.instance.playerHPBar != null)
            gamemanager.instance.playerHPBar.fillAmount = (float)HP / currentMaxHP;
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

    public int GetCurrentGunIndex()
    {
        return gunListPos;
    }

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

        // Handle ammo pickups
        if (item is AmmoStats ammo)
        {
            if (ammo.compatibleGun != null)
            {
                AddAmmo(ammo.ammoAmount);
            }
            return;
        }

        Debug.LogWarning("Picked up unknown item: " + item.name);
    }

    public void AddAmmo(int amount)
    {
        if (activeGunStats == null) return;

        gunStats originalGunStats = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunType == originalGunStats);
        if (ammoData != null)
        {
            ammoData.reserveAmmo = Mathf.Min(ammoData.reserveAmmo + amount, GetMaxAmmo(activeGunStats));
            UpdateAmmoDisplay();
            Debug.Log($"Added {amount} ammo for {activeGunStats.name}. Reserve: {ammoData.reserveAmmo}");
        }
    }

    public int GetCurrentAmmo(gunStats gunType)
    {
        gunStats originalGunStats = gunList[gunListPos];
        GunAmmoData data = gunAmmoInventory.Find(d => d.gunType == gunType);
        return data?.currentAmmo ?? 0;
    }

    public int GetMaxAmmo(gunStats gunType)
    {
        return gunType.ammoMax * 10;
    }

    public bool CanAddAmmo(gunStats gunType)
    {
        gunStats originalGunStats = gunList[gunListPos];
        GunAmmoData data = gunAmmoInventory.Find(d => d.gunType == gunType);
        if (data == null) return false;

        return data.reserveAmmo < GetMaxAmmo(gunType);
    }

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

    public bool HasKey(keyStats key)
    {
        return keyList.Contains(key);
    }

    public bool HasAnyKey()
    {
        return keyList.Count > 0;
    }

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

    public int GetKeyCount(keyStats key)
    {
        int count = 0;
        foreach (var k in keyList)
        {
            if (k == key) count++;
        }
        return count;
    }

    public int GetTotalKeyCount()
    {
        return keyList.Count;
    }

    public void UseMedkitFromPickup(medkitStats medkit)
    {
        int healAmount = medkit.healAmount;
        HP += healAmount;
        if (HP > currentMaxHP) HP = currentMaxHP;

        updatePlayerUI();

        if (medkit.useEffect != null)
            Instantiate(medkit.useEffect, transform.position, Quaternion.identity);

        if (audMedkit != null)
        {
            aud.pitch = Random.Range(0.95f, 1.05f);
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

            // Initialize ammo data for new gun
            gunAmmoInventory.Add(new GunAmmoData(gun));
        }

        changeGun();
    }

    public void changeGun()
    {
        if (gunList.Count == 0) return;

        gunStats originalStats = gunList[gunListPos];
        activeGunStats = Instantiate(originalStats);

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

        UpdateIKTargetsFromGun(newGunModel);

        UpdateAmmoDisplay();
    }

    void UpdateIKTargetsFromGun(GameObject gunObject)
    {
        if (gunObject == null || ikController == null) return;

        // Find IK targets on the gun using recursive search
        Transform gunRightHandIK = FindDeepChild(gunObject.transform, "RightHandIK");
        Transform gunLeftHandIK = FindDeepChild(gunObject.transform, "LeftHandIK");

        ikController.UpdateGunIKTargets(gunRightHandIK, gunLeftHandIK);

        if (gunRightHandIK == null)
        {
            // Position the right hand IK target at a reasonable default
            rightHandIKTarget.SetParent(gunObject.transform);
            rightHandIKTarget.localPosition = new Vector3(0.05f, -0.03f, 0.2f);
            rightHandIKTarget.localRotation = Quaternion.identity;

            ikController.UpdateGunIKTargets(rightHandIKTarget, null);
        }

        if (gunLeftHandIK == null)
        {
            // Position the left hand IK target at a reasonable default
            leftHandIKTarget.SetParent(gunObject.transform);
            leftHandIKTarget.localPosition = new Vector3(-0.05f, -0.02f, 0.4f);
            leftHandIKTarget.localRotation = Quaternion.identity;

            ikController.UpdateGunIKTargets(null, leftHandIKTarget);
        }
    }

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
        HP = currentMaxHP;
        updatePlayerUI();
    }

    [System.Serializable]
    public class PlayerControllerSaveData
    {
        public int hp;
        public int currentMaxHP;
        public bool isCrouching;
        public bool isGliding;
        public bool canUseMedkit;
        public float medkitCooldown;
        public int gunListPos;
        public List<keyStats> keyList;
        // Add ammo data
        public List<GunAmmoData> savedAmmoInventory;
    }

    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state);

    public PlayerControllerSaveData CaptureState()
    {
        return new PlayerControllerSaveData
        {
            hp = HP,
            currentMaxHP = this.currentMaxHP,
            isCrouching = this.isCrouching,
            isGliding = this.isGliding,
            canUseMedkit = this.canUseMedkit,
            medkitCooldown = this.medkitCooldown,
            gunListPos = this.gunListPos,
            keyList = new List<keyStats>(keyList),
            // Save ammo inventory
            savedAmmoInventory = new List<GunAmmoData>(gunAmmoInventory)
        };
    }

    public void RestoreState(object state)
    {
        var data = state as PlayerControllerSaveData;
        if (data == null) return;

        HP = data.hp;
        currentMaxHP = data.currentMaxHP;
        isCrouching = data.isCrouching;
        isGliding = data.isGliding;
        canUseMedkit = data.canUseMedkit;
        medkitCooldown = data.medkitCooldown;

        if (gunList != null && gunList.Count > 0)
        {
            gunListPos = Mathf.Clamp(data.gunListPos, 0, gunList.Count - 1);
            changeGun();
        }

        if (data.keyList != null)
        {
            keyList = new List<keyStats>(data.keyList);
        }

        // Restore ammo inventory
        if (data.savedAmmoInventory != null)
        {
            gunAmmoInventory = new List<GunAmmoData>(data.savedAmmoInventory);
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

    public void ApplyHealthUpgrade(float amount)
    {
        int increase = Mathf.RoundToInt(amount);

        currentMaxHP += increase;


        HP += increase;

        persistentHealthUpgradeTotal += increase;

        if (HP > currentMaxHP) HP = currentMaxHP;



        updatePlayerUI();
        Debug.Log($"CONFIRM: Max HP upgraded by {increase}. New Max HP: {currentMaxHP}");
    }


}