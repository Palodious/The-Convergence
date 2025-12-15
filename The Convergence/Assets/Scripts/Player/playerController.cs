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

    [Header("~=~= Shooting =~=~")]
    [Range(1, 100)][SerializeField] int shootDamage;
    [Range(1, 100)][SerializeField] int shootDist;
    [Range(0.01f, 5f)][SerializeField] float shootRate;

    [Header("~=~= Movement Modifiers =~=~")]
    [Range(0.1f, 50f)][SerializeField] float glideGravity;

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
    int originalSpeed;

    Vector3 moveDir;
    Vector3 playerVel;

    [SerializeField] int currentMaxHP;
    int intialHP;
    float shootTimer;

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
        originalSpeed = speed;

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

    void Update()
    {
        if (!gamemanager.instance.isPaused)
        {
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

        if (Input.GetKeyDown(KeyCode.R))
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

        bool shouldSprint = Input.GetKey(KeyCode.LeftShift) &&
                           controller.isGrounded &&
                           forwardInput;

        if (shouldSprint && !isSprinting)
        {
            isSprinting = true;
            speed = originalSpeed * sprintMod;
        }
        else if (!shouldSprint && isSprinting)
        {
            isSprinting = false;
            speed = originalSpeed;
        }
    }

    void jump()
    {
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            playerVel.y = JumpSpeed;

            if (animator != null)
            {
                animator.SetTrigger("IsJumping");
            }

            if (aud != null && audJump.Length > 0)
            {
                aud.pitch = Random.Range(0.9f, 1.1f);
                aud.PlayOneShot(audJump[Random.Range(0, audJump.Length)], audJumpVol);
            }
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
        if (activeGunStats == null || isReloading || isDead) return;

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

        // Simple raycast from camera
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, activeGunStats.shootDist, ~ignoreLayer))
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

    public void Reload()
    {
        if (activeGunStats == null || isReloading || isDead) return;

        gunStats originalGunStats = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunType == originalGunStats);

        if (ammoData == null || ammoData.currentAmmo >= activeGunStats.ammoMax || ammoData.reserveAmmo <= 0)
            return;

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
        if (isDead) return;

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

    public void AddAmmo(int amount)
    {
        if (activeGunStats == null) return;

        GunAmmoData ammoData = gunAmmoInventory.Find(d => d.gunType == activeGunStats);
        if (ammoData == null) return;

        ammoData.reserveAmmo = Mathf.Min(
            ammoData.reserveAmmo + amount,
            GetMaxAmmo(activeGunStats)
        );

        UpdateAmmoDisplay();

        Debug.Log($"Added {amount} ammo to {activeGunStats.name}. Reserve now: {ammoData.reserveAmmo}");
    }

    public int GetCurrentAmmo(gunStats gunType)
    {
        GunAmmoData data = gunAmmoInventory.Find(d => d.gunType == gunType);
        return data != null ? data.currentAmmo : 0;
    }

    public int GetMaxAmmo(gunStats gunType)
    {
        return gunType.ammoMax * 10;
    }

    public bool CanAddAmmo()
    {
        if (activeGunStats == null) return false;

        GunAmmoData data = gunAmmoInventory.Find(d => d.gunType == activeGunStats);
        if (data == null) return false;

        return data.reserveAmmo < GetMaxAmmo(activeGunStats);
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
        return canUseMedkit && HP < currentMaxHP;
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

            gunAmmoInventory.Add(new GunAmmoData(gun));

            Debug.Log($"Picked up {gun.name}");
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

        UpdateAmmoDisplay();
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
        public bool isGliding;
        public bool canUseMedkit;
        public float medkitCooldown;
        public int gunListPos;
        public List<keyStats> keyList;
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
            isGliding = this.isGliding,
            canUseMedkit = this.canUseMedkit,
            medkitCooldown = this.medkitCooldown,
            gunListPos = this.gunListPos,
            keyList = new List<keyStats>(keyList),
            savedAmmoInventory = new List<GunAmmoData>(gunAmmoInventory)
        };
    }

    public void RestoreState(object state)
    {
        var data = state as PlayerControllerSaveData;
        if (data == null) return;

        HP = data.hp;
        currentMaxHP = data.currentMaxHP;
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

        if (data.savedAmmoInventory != null)
        {
            gunAmmoInventory = new List<GunAmmoData>(data.savedAmmoInventory);
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
}