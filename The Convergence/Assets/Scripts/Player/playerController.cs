using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class playerController : MonoBehaviour, IDamage, IPickup, ISaveable
{
    [Header("~=~= Components =~=~")]
    [SerializeField] CharacterController controller;
    [SerializeField] LayerMask ignoreLayer;
    public Animator animator;

    [Header("~=~= IK Controller =~=~")]
    [SerializeField] private PlayerIKController ikController;

    [Header("~=~= Player Stats =~=~")]
    [Range(1, 300)][SerializeField] int HP;
    [Range(1, 50)] public int speed;
    [Range(1, 10)][SerializeField] int sprintMod;
    [Range(1, 50)][SerializeField] int JumpSpeed;
    [Range(1, 10)][SerializeField] int maxJumps;
    [Range(1, 50)][SerializeField] int gravity;

    [Header("~=~= Movement Modifiers =~=~")]
    [Range(0.1f, 50f)][SerializeField] float glideGravity;

    [Header("~=~= Shooting =~=~")]
    [Range(1, 100)][SerializeField] int shootDamage;
    [Range(1, 100)][SerializeField] int shootDist;
    [Range(0.01f, 0.1f)][SerializeField] float trailDuration = 0.05f;
    [Range(0.01f, 0.5f)][SerializeField] float trailWidth = 0.05f;
    [SerializeField] Gradient trailGradient = new Gradient();
    private LineRenderer bulletTrail;

    [Header("~=~= Guns =~=~")]
    [SerializeField] List<gunStats> gunList = new List<gunStats>();
    [SerializeField] GameObject gunModel;
    int gunListPos;
    [HideInInspector] public gunStats activeGunStats;

    [Header("~=~= Reload Settings =~=~")]
    [SerializeField] AudioClip reloadSound;
    [Range(0, 1)][SerializeField] float reloadSoundVol = 1f;
    [SerializeField] float reloadTime = 1.5f;

    [Header("~=~= Medkit Settings =~=~")]
    [SerializeField] private bool enableNGPlusPlayerHpScaling = true;

    [Header("~=~= Death Settings =~=~")]
    [SerializeField] private float deathAnimationTime = 2f;
    [SerializeField] private AudioClip deathSound;
    [Range(0, 1)][SerializeField] private float deathSoundVol = 1f;

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

    [Header("~=~= Ammo Pickup History =~=~")]
    [SerializeField] private List<AmmoStats> ammoPickupHistory = new List<AmmoStats>();
    private static Dictionary<string, Vector2Int> persistentAmmoCounts = new Dictionary<string, Vector2Int>();

    [Header("~=~= UI References =~=~")]
    [SerializeField] private TMPro.TextMeshProUGUI ammoTextDisplay;

    [System.Serializable]
    public class GunAmmoData
    {
        public gunStats gunTemplate;
        public int currentAmmo;
        public int reserveAmmo;

        public GunAmmoData(gunStats gun)
        {
            gunTemplate = gun;
            currentAmmo = gun.ammoMax;
            reserveAmmo = gun.ammoMax * 3;
        }
    }

    [SerializeField] private List<GunAmmoData> gunAmmoInventory = new List<GunAmmoData>();

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
        public List<AmmoStats> ammoPickupHistory;
        public List<GunAmmoData> savedAmmoInventory;
    }

    private static List<gunStats> persistentGunList = new List<gunStats>();
    private static List<keyStats> persistentKeyList = new List<keyStats>();
    private static List<AmmoStats> persistentAmmoPickupHistory = new List<AmmoStats>();
    private static int persistentGunListPos = 0;
    private static int persistentHP = 100;
    private static int persistentMaxHP = 100;
    private static int persistentHealthUpgradeTotal = 0;
    private static bool isNewGameSession = true;

    [Header("~=~= Enemy Collision Prevention =~=~")]
    [SerializeField] private float enemyPushForce = 15f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float enemyCheckRadius = 0.8f;
    [SerializeField] private float enemyCheckHeight = 0.5f;
    [SerializeField] private float enemyCheckDownwardDistance = 1.5f;
    [SerializeField] private float maxEnemyStandHeight = 0.2f;

    private Collider[] enemyOverlapResults = new Collider[10];
    private List<GameObject> nearbyEnemies = new List<GameObject>();

    public int ShootDamage => shootDamage;
    int originalSpeed;
    Vector3 moveDir;
    Vector3 playerVel;
    [SerializeField] int currentMaxHP;
    int intialHP;
    private int ngpBaseMaxHP;
    private bool ngpBaseCached;
    private bool ngpApplied;
    float fireCooldown;
    bool isGliding;
    bool isSprinting;
    bool isPlayingStep;

    [HideInInspector] public float damageBoost = 1f;
    private bool canUseMedkit = true;
    private float medkitCooldown = 0f;
    private bool isReloading = false;
    private Coroutine reloadCoroutine;
    private bool isDead = false;
    int jumpCount;

    void Awake()
    {
        if (isNewGameSession)
        {
            ResetAllRuntimePersistence(); // only if flagged as new game
            isNewGameSession = false;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

    }

    void Start()
    {
        if (SaveManager.IsLoadingFromSave)
            return;

        controller = GetComponent<CharacterController>();

        intialHP = HP;
        currentMaxHP = intialHP + persistentHealthUpgradeTotal;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        ApplyNgpPlayerHpScalingIfNeeded(healToFull: currentSceneIndex == 1);
        originalSpeed = speed;

        if (ikController == null)
            ikController = GetComponent<PlayerIKController>();

        if (ikController != null)
            ikController.SetPlayerController(this);

        if (ammoTextDisplay == null)
        {
            GameObject ammoUI = GameObject.FindGameObjectWithTag("AmmoUI");
            if (ammoUI != null)
                ammoTextDisplay = ammoUI.GetComponent<TMPro.TextMeshProUGUI>();
        }

        if (bulletTrail == null)
        {
            GameObject trailObj = new GameObject("BulletTrail");
            trailObj.transform.SetParent(transform);
            bulletTrail = trailObj.AddComponent<LineRenderer>();
            bulletTrail.useWorldSpace = true;
            bulletTrail.startWidth = trailWidth;
            bulletTrail.endWidth = trailWidth * 0.5f;
            bulletTrail.material = new Material(Shader.Find("Sprites/Default"));
            bulletTrail.colorGradient = trailGradient;
            bulletTrail.enabled = false;
        }

        if (SaveManager.IsLoadingFromSave)
        {
            return;
        }

        if (currentSceneIndex == 1)
        {
            respawn();
        }
        else if (currentSceneIndex >= 2 && currentSceneIndex <= 5)
        {
            if (persistentGunList.Count > 0 || persistentKeyList.Count > 0 || persistentAmmoPickupHistory.Count > 0)
            {
                LoadPersistentData();
            }
            else
            {
                respawn();
            }
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
        if (isDead) return;

        if (!gamemanager.instance.isPaused)
        {
            fireCooldown += Time.deltaTime;

            movement();
            sprint();
        }

        HandleMedkitCooldown();
        UpdateIKState();

        CheckAndPreventEnemyStanding();
    }

    void UpdateIKState()
    {
        if (ikController == null) return;

        bool shouldAim = (Input.GetButton("Fire1") && !directionalPopup.PopupIsOpen) || isReloading;
        ikController.SetAiming(shouldAim);

        if (activeGunStats != null && gunModel != null)
        {
            ikController.SetGunTransform(gunModel.transform);
        }
    }

    void CheckAndPreventEnemyStanding()
    {
        if (isDead || controller == null || !controller.enabled) return;

        nearbyEnemies.Clear();

        Vector3 checkCenter = transform.position + Vector3.up * enemyCheckHeight;
        int numEnemies = Physics.OverlapSphereNonAlloc(checkCenter, enemyCheckRadius, enemyOverlapResults, enemyLayer);

        for (int i = 0; i < numEnemies; i++)
        {
            Collider enemyCollider = enemyOverlapResults[i];
            if (enemyCollider != null && enemyCollider.gameObject != gameObject)
            {
                nearbyEnemies.Add(enemyCollider.gameObject);

                Vector3 enemyTop = enemyCollider.bounds.max;
                Vector3 playerBottom = controller.bounds.min;

                if (playerBottom.y - enemyTop.y < maxEnemyStandHeight &&
                    playerBottom.y > enemyTop.y - 0.5f)
                {
                    Vector3 pushDirection = (transform.position - enemyCollider.transform.position).normalized;

                    pushDirection.y = 0.3f;
                    pushDirection.Normalize();

                    controller.Move(pushDirection * enemyPushForce * Time.deltaTime);

                    if (playerVel.y > -2f)
                    {
                        playerVel.y = -2f;
                    }
                }
            }
        }

        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit,
            enemyCheckDownwardDistance, enemyLayer))
        {
            Vector3 pushDirection = (transform.position - hit.transform.position).normalized;
            pushDirection.y = 0.3f;
            pushDirection.Normalize();

            controller.Move(pushDirection * enemyPushForce * Time.deltaTime * 2f);

            playerVel.y = Mathf.Min(playerVel.y, -5f);
        }
    }

    void movement()
    {
        if (isDead) return;
        if (controller == null || !controller.enabled) return;

        if (controller.isGrounded)
        {
            playerVel = Vector3.zero;
            jumpCount = 0;

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

        if (Input.GetButton("Fire1") && !directionalPopup.PopupIsOpen)
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
        if (aud != null && audStep != null && audStep.Length > 0)
        {
            aud.PlayOneShot(audStep[Random.Range(0, audStep.Length)], audStepVol);
        }

        if (isSprinting) yield return new WaitForSecondsRealtime(0.3f);
        else yield return new WaitForSecondsRealtime(0.5f);

        isPlayingStep = false;
    }

    void sprint()
    {
        if (Input.GetButtonDown("Sprint"))
        {
            isSprinting = true;
            speed = originalSpeed * sprintMod;
        }
        else if (Input.GetButtonUp("Sprint"))
        {
            isSprinting = false;
            speed = originalSpeed;
        }
    }

    void jump()
    {
        if (controller == null || !controller.enabled) return;

        bool isNearEnemy = false;
        if (nearbyEnemies.Count > 0)
        {
            foreach (GameObject enemy in nearbyEnemies)
            {
                if (enemy != null)
                {
                    float verticalDistance = transform.position.y - enemy.transform.position.y;
                    float horizontalDistance = Vector3.Distance(
                        new Vector3(transform.position.x, 0, transform.position.z),
                        new Vector3(enemy.transform.position.x, 0, enemy.transform.position.z)
                    );

                    if (verticalDistance < 2f && horizontalDistance < 1.5f)
                    {
                        isNearEnemy = true;
                        break;
                    }
                }
            }
        }

        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            if (isNearEnemy)
            {
                playerVel.y = JumpSpeed * 1.3f;

                if (nearbyEnemies.Count > 0)
                {
                    GameObject nearestEnemy = nearbyEnemies[0];
                    Vector3 pushDirection = (transform.position - nearestEnemy.transform.position).normalized;
                    pushDirection.y = 0;
                    if (pushDirection.magnitude > 0.1f)
                    {
                        controller.Move(pushDirection * enemyPushForce * 0.5f * Time.deltaTime);
                    }
                }
            }
            else
            {
                playerVel.y = JumpSpeed;
            }

            jumpCount++;

            if (animator != null)
            {
                animator.SetTrigger("IsJumping");
            }

            if (aud != null && audJump != null && audJump.Length > 0)
            {
                aud.pitch = Random.Range(0.9f, 1.1f);
                aud.PlayOneShot(audJump[Random.Range(0, audJump.Length)], audJumpVol);
            }
        }
    }

    void StartGlide()
    {
        if (controller == null || !controller.enabled) return;
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
        if (gunModel == null) return;

        if (fireCooldown < activeGunStats.shootRate) return;
        fireCooldown = 0;

        gunStats originalGunTemplate = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunTemplate == originalGunTemplate);

        if (ammoData == null || ammoData.currentAmmo <= 0)
        {
            Reload();
            return;
        }

        ammoData.currentAmmo--;

        if (aud != null && activeGunStats.shootSound != null && activeGunStats.shootSound.Length > 0)
        {
            aud.pitch = Random.Range(0.9f, 1.1f);
            aud.PlayOneShot(activeGunStats.shootSound[Random.Range(0, activeGunStats.shootSound.Length)], activeGunStats.shootSoundVol);
        }

        UpdateAmmoDisplay();

        Vector3 startPos = GetGunMuzzlePosition();
        Vector3 endPos;
        bool hitSomething = false;

        Vector3 shootDirection = GetGunMuzzleDirection();

        if (Physics.Raycast(startPos, shootDirection, out RaycastHit hit, activeGunStats.shootDist, ~ignoreLayer))
        {
            IDamage dmg = hit.collider.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(Mathf.RoundToInt(activeGunStats.shootDamage * damageBoost));
            }

            if (activeGunStats.hitEffect != null)
            {
                Instantiate(activeGunStats.hitEffect, hit.point, Quaternion.identity);
            }
            endPos = hit.point;
            hitSomething = true;
        }
        else
        {
            endPos = startPos + shootDirection * activeGunStats.shootDist;
        }

        StartCoroutine(ShowBulletTrail(startPos, endPos, hitSomething));
    }

    private Vector3 GetGunMuzzlePosition()
    {
        if (gunModel == null) return transform.position;

        Transform[] allChildren = gunModel.GetComponentsInChildren<Transform>();
        Transform muzzleTransform = allChildren.FirstOrDefault(t => t.name == "MuzzlePoint");

        if (muzzleTransform != null)
        {
            return muzzleTransform.position;
        }

        return gunModel.transform.position;
    }

    private Vector3 GetGunMuzzleDirection()
    {
        if (gunModel == null) return transform.forward;

        Transform[] allChildren = gunModel.GetComponentsInChildren<Transform>();
        Transform muzzleTransform = allChildren.FirstOrDefault(t => t.name == "MuzzlePoint");

        if (muzzleTransform != null)
        {
            return muzzleTransform.forward;
        }

        return gunModel.transform.forward;
    }

    IEnumerator ShowBulletTrail(Vector3 start, Vector3 end, bool hitTarget)
    {
        if (bulletTrail == null) yield break;

        bulletTrail.enabled = true;
        bulletTrail.SetPosition(0, start);
        bulletTrail.SetPosition(1, end);

        if (hitTarget)
        {
            bulletTrail.colorGradient = trailGradient;
        }
        else
        {
            bulletTrail.startColor = Color.white;
            bulletTrail.endColor = new Color(1, 1, 1, 0.5f);
        }

        yield return new WaitForSecondsRealtime(trailDuration);

        bulletTrail.enabled = false;
    }

    public void Reload()
    {
        if (activeGunStats == null || isReloading || isDead) return;

        gunStats originalGunTemplate = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunTemplate == originalGunTemplate);

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

        if (aud != null && reloadSound != null)
        {
            aud.pitch = Random.Range(0.95f, 1.05f);
            aud.PlayOneShot(reloadSound, reloadSoundVol);
        }

        if (animator != null)
        {
            animator.SetTrigger("Reload");
        }

        yield return new WaitForSecondsRealtime(reloadTime);

        gunStats originalGunTemplate = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunTemplate == originalGunTemplate);
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
            gunStats originalGunTemplate = gunList[gunListPos];
            GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunTemplate == originalGunTemplate);
            if (ammoData != null)
            {
                ammoTextDisplay.text = $"{ammoData.currentAmmo}/{ammoData.reserveAmmo}";
            }
        }
    }

    public void takeDamage(int amount)
    {
        if (isDead) return;

        HP -= amount;
        updatePlayerUI();
        StartCoroutine(screenFlashDamage());

        if (aud != null && audHurt != null && audHurt.Length > 0)
        {
            aud.pitch = Random.Range(0.9f, 1.1f);
            aud.PlayOneShot(audHurt[Random.Range(0, audHurt.Length)], audHurtVol);
        }

        if (HP <= 0 && !isDead)
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        if (isDead) yield break;
        isDead = true;

        if (controller != null)
        {
            controller.enabled = false;
            controller.detectCollisions = false;
        }

        if (ikController != null)
            ikController.SetAiming(false);

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        if (aud != null && deathSound != null)
        {
            aud.pitch = 1f;
            aud.PlayOneShot(deathSound, deathSoundVol);
        }

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            isReloading = false;
        }

        yield return new WaitForSecondsRealtime(deathAnimationTime);

        if (gamemanager.instance != null)
            gamemanager.instance.youLose();
    }

    public void updatePlayerUI()
    {
        if (gamemanager.instance != null && gamemanager.instance.playerHPBar != null)
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

        yield return new WaitForSecondsRealtime(0.1f);

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

        if (item is AmmoStats ammo)
        {
            GetAmmoFromPickup(ammo);
            return;
        }
    }

    public void GetAmmoFromPickup(AmmoStats ammo)
    {
        if (ammo == null) return;

        if (!ammoPickupHistory.Contains(ammo))
        {
            ammoPickupHistory.Add(ammo);
        }

        bool ammoAdded = false;

        if (ammo.gunType != null && ammo.gunType.Length > 0)
        {
            foreach (gunStats gunType in ammo.gunType)
            {
                if (gunType == null) continue;

                GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunTemplate == gunType);
                if (ammoData != null)
                {
                    int maxAmmo = GetMaxAmmo(gunType);
                    int newAmmo = Mathf.Min(ammoData.reserveAmmo + ammo.ammoAmount, maxAmmo);
                    int added = newAmmo - ammoData.reserveAmmo;
                    ammoData.reserveAmmo = newAmmo;

                    if (added > 0)
                    {
                        ammoAdded = true;
                    }
                }
            }
        }
        else
        {
            if (activeGunStats != null)
            {
                gunStats originalGunTemplate = gunList[gunListPos];
                GunAmmoData ammoData = gunAmmoInventory.Find(data => data.gunTemplate == originalGunTemplate);
                if (ammoData != null)
                {
                    int maxAmmo = GetMaxAmmo(activeGunStats);
                    int newAmmo = Mathf.Min(ammoData.reserveAmmo + ammo.ammoAmount, maxAmmo);
                    int added = newAmmo - ammoData.reserveAmmo;
                    ammoData.reserveAmmo = newAmmo;

                    if (added > 0)
                    {
                        ammoAdded = true;
                    }
                }
            }
        }

        if (ammoAdded)
        {
            UpdateAmmoDisplay();
        }
    }

    public void AddAmmo(int amount)
    {
        if (activeGunStats == null) return;

        gunStats originalGunTemplate = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(d => d.gunTemplate == originalGunTemplate);
        if (ammoData == null) return;

        ammoData.reserveAmmo = Mathf.Min(
            ammoData.reserveAmmo + amount,
            GetMaxAmmo(activeGunStats)
        );

        UpdateAmmoDisplay();
    }

    public int GetCurrentAmmo(gunStats gunType)
    {
        GunAmmoData data = gunAmmoInventory.Find(d => d.gunTemplate == gunType);
        return data != null ? data.currentAmmo : 0;
    }

    public int GetMaxAmmo(gunStats gunType)
    {
        return gunType.ammoMax * 10;
    }

    public bool CanAddAmmo()
    {
        if (activeGunStats == null) return false;

        gunStats originalGunTemplate = gunList[gunListPos];
        GunAmmoData data = gunAmmoInventory.Find(d => d.gunTemplate == originalGunTemplate);
        if (data == null) return false;

        return data.reserveAmmo < GetMaxAmmo(activeGunStats);
    }

    void AddKeyToList(keyStats key)
    {
        if (keyList.Contains(key))
        {
            return;
        }

        keyList.Add(key);

        if (key.pickupEffect != null)
        {
            Instantiate(key.pickupEffect, transform.position, Quaternion.identity);
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
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool UseAnyKey()
    {
        if (keyList.Count > 0)
        {
            keyStats usedKey = keyList[0];
            keyList.RemoveAt(0);
            return true;
        }
        else
        {
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

        if (aud != null && audMedkit != null)
        {
            aud.pitch = Random.Range(0.95f, 1.05f);
            aud.PlayOneShot(audMedkit, audMedkitVol);
        }
    }

    public void HandleMedkitCooldown()
    {
        if (isDead) return;

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
        if (gun == null) return;

        gunStats existingGun = gunList.Find(g => g != null && g.gunType == gun.gunType);


        if (existingGun != null)
        {
            gunListPos = gunList.IndexOf(existingGun);
        }
        else
        {
            gunStats gunToAdd = gun;

            gunList.Add(gunToAdd);
            gunListPos = gunList.Count - 1;

            GunAmmoData ammoData = new GunAmmoData(gunToAdd);
            gunAmmoInventory.Add(ammoData);
        }

        changeGun();
        SavePersistentData();
    }

    public void changeGun()
    {
        if (gunList.Count == 0) return;
        if (gunModel == null) return;

        gunStats originalStats = gunList[gunListPos];

        activeGunStats = originalStats;

        Transform[] children = new Transform[gunModel.transform.childCount];
        for (int i = 0; i < gunModel.transform.childCount; i++)
        {
            children[i] = gunModel.transform.GetChild(i);
        }

        foreach (Transform child in children)
        {
            Destroy(child.gameObject);
        }

        GameObject newGunModel = Instantiate(originalStats.gunModel, gunModel.transform);
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
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isDead = false;

        if (controller != null)
        {
            controller.enabled = false;
            yield return null;
            controller.transform.position = gamemanager.instance.spawnPoint.transform.position;
            controller.enabled = true;
            controller.detectCollisions = true;
        }

        if (ikController != null)
            ikController.SetAiming(false);

        HP = currentMaxHP;
        playerVel = Vector3.zero;
        updatePlayerUI();
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
            ammoPickupHistory = new List<AmmoStats>(ammoPickupHistory),
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

        if (data.ammoPickupHistory != null)
        {
            ammoPickupHistory = new List<AmmoStats>(data.ammoPickupHistory);
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
    }

    public static void ResetAllRuntimePersistence()
    {
        persistentGunList.Clear();
        persistentKeyList.Clear();
        persistentAmmoPickupHistory.Clear();
        persistentAmmoCounts.Clear();

        persistentGunListPos = 0;
        persistentHealthUpgradeTotal = 0;
        persistentHP = 0;
        persistentMaxHP = 0;

        isNewGameSession = true;
    }

    void LoadPersistentData()
    {
        gunList.Clear();
        keyList.Clear();
        ammoPickupHistory.Clear();
        gunAmmoInventory.Clear();

        foreach (var gun in persistentGunList)
        {
            gunList.Add(gun);

            GunAmmoData ammoData = new GunAmmoData(gun);

            string gunName = gun.name;
            if (persistentAmmoCounts.ContainsKey(gunName))
            {
                Vector2Int savedAmmo = persistentAmmoCounts[gunName];
                ammoData.currentAmmo = savedAmmo.x;
                ammoData.reserveAmmo = savedAmmo.y;
            }

            gunAmmoInventory.Add(ammoData);
        }

        foreach (var key in persistentKeyList)
        {
            keyList.Add(key);
        }

        if (persistentGunList.Count > 0)
        {
            gunListPos = Mathf.Clamp(persistentGunListPos, 0, persistentGunList.Count - 1);
            changeGun();
        }

        HP = Mathf.Min(persistentHP, currentMaxHP);

        updatePlayerUI();
        UpdateAmmoDisplay();
    }

    void SavePersistentData()
    {
        persistentGunList = new List<gunStats>(gunList);
        persistentKeyList = new List<keyStats>(keyList);
        persistentGunListPos = gunListPos;
        persistentHP = HP;
        persistentMaxHP = currentMaxHP;

        persistentAmmoCounts.Clear();
        foreach (var ammoData in gunAmmoInventory)
        {
            string gunName = ammoData.gunTemplate.name;
            persistentAmmoCounts[gunName] = new Vector2Int(ammoData.currentAmmo, ammoData.reserveAmmo);
        }
    }

    public void PrepareForSceneTransition()
    {
        SavePersistentData();
    }

    public bool IsReloading()
    {
        return isReloading;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public Transform GetGunModelTransform()
    {
        return gunModel != null ? gunModel.transform : null;
    }

    private void CachePlayerHpBaseIfNeeded()
    {
        if (ngpBaseCached) return;

        ngpBaseMaxHP = currentMaxHP;
        ngpBaseCached = true;
    }

    private void ApplyNgpPlayerHpScalingIfNeeded(bool healToFull)
    {
        if (!enableNGPlusPlayerHpScaling) return;
        if (ngpApplied) return;

        if (SaveManager.IsLoadingFromSave)
            return;

        CachePlayerHpBaseIfNeeded();

        float mult = 1f;
        if (NewGamePlusManager.Instance != null)
            mult = Mathf.Max(0.01f, NewGamePlusManager.Instance.GetPlayerHealthMultiplier());

        int scaledMax = Mathf.Max(1, Mathf.RoundToInt(ngpBaseMaxHP * mult));

        currentMaxHP = scaledMax;

        if (healToFull)
            HP = currentMaxHP;
        else if (HP > currentMaxHP)
            HP = currentMaxHP;

        ngpApplied = true;
    }

    // ==== Store Upgrade Sync ====
    public void RefreshEquippedGunIfMatchesTemplate(gunStats upgradedTemplate)
    {
        if (upgradedTemplate == null) return;
        if (gunList == null || gunList.Count == 0) return;

        // Is the currently selected gun using this template?
        gunStats currentTemplate = gunList[gunListPos];
        bool isSameTemplate =
    currentTemplate != null &&
    upgradedTemplate != null &&
    currentTemplate.gunType == upgradedTemplate.gunType;

        if (!isSameTemplate) return;

        // Preserve current mag ammo before rebuilding the clone
        gunStats originalGunTemplate = gunList[gunListPos];
        GunAmmoData ammoData = gunAmmoInventory.Find(d => d.gunTemplate == originalGunTemplate);

        int curAmmo = ammoData != null ? ammoData.currentAmmo : 0;
        int resAmmo = ammoData != null ? ammoData.reserveAmmo : 0;

        // Rebuild clone + visuals using your existing path
        changeGun();

        // Restore inventory ammo
        if (ammoData != null)
        {
            ammoData.currentAmmo = curAmmo;
            ammoData.reserveAmmo = resAmmo;
        }

        UpdateAmmoDisplay();
    }

    // ==== Store Healing ====
    public void HealFromStore(int healAmount)
    {
        if (healAmount <= 0) return;

        HP += healAmount;
        if (HP > currentMaxHP) HP = currentMaxHP;

        updatePlayerUI();
    }
    // ==== Store Healing, Percentage ====
    public int CurrentMaxHP => currentMaxHP;

    public bool IsHealthFull()
    {
        return HP >= currentMaxHP;
    }

    public void HealPercentFromStore(float percent)
    {
        if (percent <= 0f) return;

        int healAmount = Mathf.CeilToInt(currentMaxHP * percent);
        HealFromStore(healAmount);
    }
}