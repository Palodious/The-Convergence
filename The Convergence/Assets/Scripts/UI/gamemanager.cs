using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

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
    public Image playerHPBar;
    [SerializeField] public GameObject playerDamagePanel;
    public GameObject checkpointPopup;
    public GameObject bossDoorPopup;
    [SerializeField] public GameObject surgeOverlay;
    public GameObject spawnPoint;

    public GameObject player;
    public playerController playerScript;

    public bool isPaused;

    float timeScaleOrig;
    [Header("**** Currency UI ****")]
    [SerializeField] private TextMeshProUGUI riftShardTextDisplay;

    [Header("**** New Game+ Settings ****")]
    [SerializeField] private string newGamePlusStartSceneName = "Game Play Scene L1";

    void Awake()
    {
        // Singleton pattern with safety
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        timeScaleOrig = Time.timeScale;

        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerScript = player.GetComponent<playerController>();

        spawnPoint = GameObject.FindWithTag("Spawn Point");
    }

    private void Start()
    {
        if (SaveManager.PendingLoad)
        {
            SaveManager.PendingLoad = false;
            LoadGame();
            return;
        }

        if (SaveManager.IsLoadingFromSave)
            return;

        if (RiftShardManager.Instance != null)
        {
            RiftShardManager.Instance.OnShardAmountChanged += UpdateCoinDisplay;
            // Initialize the display with the current amount
            UpdateCoinDisplay(RiftShardManager.Instance.Amount);
        }
        else
        {
            //  Debug.LogWarning("RiftShardManager not found in scene. Coin display will not update.");
        }
    }

    private void OnDestroy()
    {
        if (RiftShardManager.Instance != null)
        {
            RiftShardManager.Instance.OnShardAmountChanged -= UpdateCoinDisplay;
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (directionalPopup.PopupIsOpen)
                return;

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

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Close");
    }

    public void updateGameGoal(int amount)
    {
        gameGoalCount += amount;

        // Update UI display
        if (gameGoalCountText != null)
            gameGoalCountText.text = gameGoalCount.ToString("F0");


        // NO WIN CONDITION TRIGGERED HERE

    }

    public void OnLevel4BossDefeated()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // Verify this is actually Level 4
        if (currentSceneName == "Game Play Scene L4")
        {
            //Debug.Log("BOSS DEFEATED ON LEVEL 4 - TRIGGERING WIN CONDITION!");
            statePause();

            menuActive = menuWin;
            if (menuActive != null)
            {
                menuActive.SetActive(true);
            }
        }
    }

    public void StartNewGamePlusRun()
    {
        // We should be on the win screen when this is pressed. Unpause first so the next scene isn't stuck.
        stateUnpause();

        if (NewGamePlusManager.Instance != null)
        {0
            NewGamePlusManager.Instance.AdvanceCycle();
        }
        else
        {
            //Debug.LogWarning("StartNewGamePlusRun called, but NewGamePlusManager.Instance is null. New Game+ cycle will not advance.");
        }

        if (SaveManager.Instance != null && player != null && playerScript != null)
        {
            SaveManager.Instance.Save(player, gameGoalCount);
        }

        // Reset run-only counters.
        gameGoalCount = 0;
        if (gameGoalCountText != null)
            gameGoalCountText.text = gameGoalCount.ToString("F0");

        // Load the start scene for the new run.
        if (!string.IsNullOrEmpty(newGamePlusStartSceneName))
        {
            SceneManager.LoadScene(newGamePlusStartSceneName);
        }
        else
        {
            //Debug.LogError("New Game+ start scene name is empty. Set 'newGamePlusStartSceneName' in the Inspector.");
        }
    }

    public void youLose()
    {
        statePause();

        menuActive = menuLose;

        if (menuActive != null)
        {
            menuActive.SetActive(true);
        }
    }

    // Save & Load system
    public void SaveGame()
    {
        if (SaveManager.Instance == null)
        {
            //Debug.LogWarning("SaveGame called but SaveManager.Instance is null. Make sure SaveManager is in the scene.");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_Error");

            return;
        }

        if (player == null || playerScript == null)
        {
            //Debug.LogWarning("SaveGame called but player/playerScript is null.");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySound("UI_Error");

            return;
        }

        SaveManager.Instance.Save(player, gameGoalCount);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySound("UI_Apply");
    }

    public void LoadGame()
    {
        if (SaveManager.Instance == null)
        {
            //Debug.LogWarning("LoadGame called but SaveManager.Instance is null. Make sure SaveManager is in the scene.");

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
            //Debug.LogWarning("No save file found.");
            // Ensure we're not stuck paused if load fails
            stateUnpause();
            yield break;
        }

        System.Func<string, GameObject> spawnFunc = null;
        if (prefabRegistry != null)
            spawnFunc = prefabRegistry.SpawnByKey;

        yield return SaveManager.Instance.LoadAndRestore(data, spawnFunc);
        if (RiftShardManager.Instance != null)
        {
            RiftShardManager.Instance.OnShardAmountChanged -= UpdateCoinDisplay;
            RiftShardManager.Instance.OnShardAmountChanged += UpdateCoinDisplay;
            UpdateCoinDisplay(RiftShardManager.Instance.Amount);
        }

        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerScript = player.GetComponent<playerController>();

        spawnPoint = GameObject.FindWithTag("Spawn Point");

        if (playerScript != null)
        {
            playerScript.RestoreGunVisual(data.playerGunIndex);
        }
        else
        {
            //Debug.LogWarning("LoadGameRoutine: playerScript is null after load.");
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
            //Debug.LogWarning("menuOptions not assigned on gamemanager.");
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

    private void UpdateCoinDisplay(int newAmount)
    {
        if (riftShardTextDisplay != null)
        {
            // Sets the text to show the current rift shard amount
            // You can format this string as needed, e.g., $"{newAmount} SHARDS"
            riftShardTextDisplay.text = $":{newAmount}";
        }
    }

    [Header("Wave Mode Settings")]
    public bool IsWaveModeActive = true;
    public float globalWaveModeRange = 100f;

    private List<enemyAI> allEnemies = new List<enemyAI>();


    public void RegisterEnemy(enemyAI enemy)
    {
        if (!allEnemies.Contains(enemy))
            allEnemies.Add(enemy);
    }

    public void UnregisterEnemy(enemyAI enemy)
    {
        if (allEnemies.Contains(enemy))
            allEnemies.Remove(enemy);
    }
}