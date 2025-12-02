using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    // Expose current objective count; make it tracked and accessible.
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

    // Update is called once per frame
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
    }

    public void updateGameGoal(int amount)
    {
        gameGoalCount += amount;

        // If we're adding goals, mark that the objective system is active.
        if (amount > 0)
            objectivesInitialized = true;

        if (gameGoalCountText != null)
            gameGoalCountText.text = gameGoalCount.ToString("F0");

        // Only allow winning if we actually had objectives.
        if (objectivesInitialized && gameGoalCount <= 0)
        {
            // You win!!!
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
            return;
        }

        if (player == null || playerScript == null)
        {
            Debug.LogWarning("SaveGame called but player/playerScript is null.");
            return;
        }

        SaveManager.Instance.Save(player, playerScript.GetHP(), gameGoalCount);
    }

    public void LoadGame()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("LoadGame called but SaveManager.Instance is null. Make sure SaveManager is in the scene.");
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

        // Let SaveManager rebuild the scene based on the save.
        yield return SaveManager.Instance.LoadAndRestore(data, spawnFunc);

        // Re-hook references after the world is restored.
        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerScript = player.GetComponent<playerController>();

        spawnPoint = GameObject.FindWithTag("Spawn Point");

        // Restore player / objective values from the save.
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

    public int GetGameGoalCount()
    {
        return gameGoalCount;
    }

    public void OpenOptionsMenu()
    {
        if (menuOptions == null)
        {
            Debug.LogWarning("menuOptions not assigned on gamemanager.");
            return;
        }

        // Remember the menu we came from (pause/win/lose)
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

        // If we came from another menu, go back to it
        if (previousMenu != null)
        {
            menuActive = previousMenu;
            previousMenu.SetActive(true);
            previousMenu = null;
        }
        else
        {
            // If Options was opened with no previous menu, just unpause back to game
            stateUnpause();
        }
    }
}
