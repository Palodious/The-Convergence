using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class gamemanager : MonoBehaviour
{
    public static gamemanager instance;

    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;
    [SerializeField] GameObject menuOptions;

    GameObject previousMenu;

    [SerializeField] private int gameGoalCount;

    [SerializeField] private PrefabRegistry prefabRegistry;

    public TMP_Text gameGoalCountText;
    [SerializeField] public Image playerHPBar;
    [SerializeField] public GameObject playerDamagePanel;
    public GameObject checkpointPopup;
    public GameObject bossDoorPopup;
    [SerializeField] public GameObject surgeOverlay;
    public GameObject spawnPoint;

    public GameObject player;
    public playerController playerScript;

    private bool objectivesInitialized = false;

    public bool isPaused;

    float timeScaleOrig;
    public TextMeshProUGUI coinTextDisplay;

    public int currentCoins = 0;

    void Awake()
    {
        instance = this;
        timeScaleOrig = Time.timeScale;

        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerScript = player.GetComponent<playerController>();

        spawnPoint = GameObject.FindWithTag("Spawn Point");
    }

    private void Start()
    {
        // If we came here via Main Menu's Continue, auto-load the save.
        if (SaveManager.PendingLoad)
        {
            SaveManager.PendingLoad = false;
            LoadGame();
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (menuActive == null)
            {
                statePause();
                menuActive = menuPause;
                menuActive.SetActive(true);
            }
            else if (menuActive == menuPause)
            {
                stateUnpause();
            }
        }
    }

    public void statePause()
    {
        isPaused = true;
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (playerScript != null)
            playerScript.enabled = false;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Open");
    }

    public void stateUnpause()
    {
        isPaused = false;
        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (menuActive != null)
        {
            menuActive.SetActive(false);
            menuActive = null;
        }

        if (playerScript != null)
            playerScript.enabled = true;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Close");
    }

    public void updateGameGoal(int amount)
    {
        gameGoalCount += amount;

        // If we're adding goals, mark that the objective system is active.
        if (amount > 0)
            objectivesInitialized = true;

        if (gameGoalCountText != null)
            gameGoalCountText.text = gameGoalCount.ToString("F0");

        // Only allow winning if we actually had objectives and this is not level 4
        // Level 4 win condition is handled separately by OnLevel4BossDefeated()
        if (objectivesInitialized && gameGoalCount <= 0)
        {
            // Check if this is NOT level 4 (boss level)
            if (SceneManager.GetActiveScene().buildIndex != 4)
            {
                // Normal level win condition: all enemies defeated
                statePause();
                menuActive = menuWin;
                if (menuActive != null)
                    menuActive.SetActive(true);
            }
            // If this IS level 4, DO NOT trigger win here - wait for boss defeat
        }
    }

    public void OnLevel4BossDefeated()
    {
        // Only trigger win if this is level 4 (boss level)
        if (SceneManager.GetActiveScene().buildIndex == 4)
        {
            statePause();
            menuActive = menuWin;
            if (menuActive != null)
                menuActive.SetActive(true);
        }
    }

    public void youLose()
    {
        statePause();
        menuActive = menuLose;
        if (menuActive != null)
            menuActive.SetActive(true);
    }

    // Save & Load system
    public void SaveGame()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("SaveGame called but SaveManager.Instance is null. Make sure SaveManager is in the scene.");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_Error");

            return;
        }

        if (player == null || playerScript == null)
        {
            Debug.LogWarning("SaveGame called but player/playerScript is null.");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_Error");

            return;
        }

        SaveManager.Instance.Save(player, playerScript.GetHP(), gameGoalCount);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Apply");
    }

    public void LoadGame()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("LoadGame called but SaveManager.Instance is null. Make sure SaveManager is in the scene.");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_Error");

            return;
        }

        StartCoroutine(LoadGameRoutine());
    }

    IEnumerator LoadGameRoutine()
    {
        if (!SaveManager.Instance.TryLoad(out SaveData data))
        {
            Debug.LogWarning("No save file found.");
            yield break;
        }

        System.Func<string, GameObject> spawnFunc = null;
        if (prefabRegistry != null)
            spawnFunc = prefabRegistry.SpawnByKey;

        yield return SaveManager.Instance.LoadAndRestore(data, spawnFunc);

        if (data.entities != null && data.entities.Count > 0)
        {
            var savedIds = new HashSet<string>(data.entities.Select(e => e.id));
            var allEnemies = FindObjectsByType<enemyAI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var enemy in allEnemies)
            {
                var se = enemy.GetComponent<SaveEntity>();
                if (se == null) continue;

                if (!savedIds.Contains(se.Id))
                {
                    Debug.Log($"[Load Cleanup] Destroying enemy '{enemy.name}' with id {se.Id} that was not in the save file.");
                    Destroy(enemy.gameObject);
                }
            }
        }

        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerScript = player.GetComponent<playerController>();

        spawnPoint = GameObject.FindWithTag("Spawn Point");

        if (playerScript != null)
        {
            playerScript.SetHP(data.playerHP);
            playerScript.RestoreGunVisual(data.playerGunIndex);
        }
        else
        {
            Debug.LogWarning("LoadGameRoutine: playerScript is null after load.");
        }

        gameGoalCount = data.gameGoalCount;
        if (gameGoalCountText != null)
            gameGoalCountText.text = gameGoalCount.ToString("F0");

        stateUnpause();
    }

    public int GetGameGoalCount() => gameGoalCount;

    public void OpenOptionsMenu()
    {
        if (menuOptions == null)
        {
            Debug.LogWarning("menuOptions not assigned on gamemanager.");
            return;
        }

        previousMenu = menuActive;

        if (previousMenu != null)
            previousMenu.SetActive(false);

        menuOptions.SetActive(true);
        menuActive = menuOptions;
    }

    public void CloseOptionsMenu()
    {
        if (menuOptions == null) return;

        menuOptions.SetActive(false);

        if (previousMenu != null)
        {
            menuActive = previousMenu;
            previousMenu.SetActive(true);
            previousMenu = null;
        }
        else
        {
            stateUnpause();
        }
    }

    private void UpdateCoinDisplay()
    {
        if (coinTextDisplay != null)
        {
            // Sets the text to show the current coin amount
            coinTextDisplay.text = $":{currentCoins}";
        }
    }

    public void AddCoins(int amount)
    {
        if (amount > 0)
        {
            currentCoins += amount;

            UpdateCoinDisplay();
        }
    }
}