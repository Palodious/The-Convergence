using UnityEngine;
using System.Collections;

public class playerController : MonoBehaviour, IDamage
{
    [SerializeField] CharacterController controller;
    [SerializeField] LayerMask ignoreLayer;  // ignore layers for shooting  

    [SerializeField] int HP = 100;
    [SerializeField] int maxHP = 100;  // now actively used to clamp HP
    [SerializeField] int flow = 30;
    [SerializeField] int maxFlow = 100;
    [SerializeField] int speed;
    [SerializeField] int sprintMod;
    [SerializeField] int JumpSpeed;
    [SerializeField] int maxJumps;
    [SerializeField] int gravity;  // gravity applied each frame  

    [SerializeField] int shootDamage;
    [SerializeField] int shootDist;
    [SerializeField] float shootRate;  // time between shots  

    [SerializeField] float glideGravity;  // lower gravity while gliding  

    float originalSpeed;     // store original speed  

    Vector3 moveDir;
    Vector3 playerVel;

    int jumpCount;
    int HPOrig;
    float shootTimer;

    bool isGliding;    // glide state  

    // Modified by playerAbilities during surge
    [HideInInspector] public float damageBoost = 1f;

    void Start()
    {
        HPOrig = maxHP;  // use maxHP as max health for UI calculations
        HP = maxHP;
        originalSpeed = speed;

        updatePlayerUI(); // fill HP bar at start
    }

    void Update()
    {
        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDist, Color.red);
        shootTimer += Time.deltaTime;

        movement();
        sprint();
    }

    public void addFlow(int value)
    {
        flow += value;
        if (flow > maxFlow)
            flow = maxFlow;
    }

    void movement()
    {
        Vector3 moveInput = Input.GetAxis("Horizontal") * transform.right + Input.GetAxis("Vertical") * transform.forward;

        if (controller.isGrounded)
        {
            if (playerVel.y < 0) playerVel.y = -2f;
            jumpCount = 0;
        }
        else
        {
            if (isGliding)
            {
                playerVel.y -= glideGravity * Time.deltaTime;
                playerVel.y = Mathf.Max(playerVel.y, -gravity * 0.4f);
            }
            else
            {
                playerVel.y -= gravity * Time.deltaTime;
            }
        }

        jump();

        Vector3 velocity = moveInput * speed + new Vector3(0, playerVel.y, 0);
        controller.Move(velocity * Time.deltaTime);

        if (!controller.isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.G)) StartGlide();
            if (Input.GetKeyUp(KeyCode.G)) StopGlide();
        }
        else if (isGliding) StopGlide();

        if (Input.GetButton("Fire1") && shootTimer >= shootRate) shoot();
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

    void StartGlide()
    {
        if (!controller.isGrounded && !isGliding)
        {
            isGliding = true;
            playerVel.y = -1f;
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
        }
    }

    public void takeDamage(int amount)
    {
        HP -= amount;

        // Clamp HP so it never goes below 0 or above maxHP
        HP = Mathf.Clamp(HP, 0, maxHP);

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

    // Add this property to expose HP for reading and writing
    public int CurrentHP
    {
        get => HP;
        set => HP = Mathf.Clamp(value, 0, maxHP);
    }
}
