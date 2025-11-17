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


    // Expose current objective count; make it tracked and accessible.
    [SerializeField]private int gameGoalCount;


    public TMP_Text gameGoalCountText;
    [SerializeField] public Image playerHPBar;
    [SerializeField] public GameObject playerDamagePanel;
    [SerializeField] public GameObject checkpointPopup;
    [SerializeField] public GameObject surgeOverlay;
    public GameObject spawnPoint;


    public GameObject player;
    public playerController playerScript;
    

    public bool isPaused;

    float timeScaleOrig;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        timeScaleOrig = Time.timeScale;

        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<playerController>();
        spawnPoint = GameObject.FindWithTag("Spawn Point");

        if (SaveSystem.PendingLoad)
        {
            SaveSystem.PendingLoad = false;
            
            // This will restore position/HP/objectives.
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
        playerScript.enabled = false; //
    }
    public void stateUnpause()
    {
        isPaused = false;
        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        menuActive.SetActive(false);
        menuActive = null;
        playerScript.enabled = true; //
    }
    public void updateGameGoal(int amount)
    {
        gameGoalCount += amount;
        gameGoalCountText.text = gameGoalCount.ToString("F0");

        if(gameGoalCount <= 0)
        {
            // You win!!!
            statePause();
            menuActive = menuWin;
            menuActive.SetActive(true);
        }
    }
    public void youLose() 
    { 
        statePause();
        menuActive = menuLose;
        menuActive.SetActive(true);
    }

    // Save & Load system
    public void SaveGame()
    {
        var d = new SaveSystem.SaveData
        {
            scene = SceneManager.GetActiveScene().name,
            px = player.transform.position.x,
            py = player.transform.position.y,
            pz = player.transform.position.z,
            playerHP = playerScript.GetHP(),
            gameGoalCount = gameGoalCount
        };
        SaveSystem.Save(d);
    }

    public void LoadGame()
    {
        if (!SaveSystem.TryLoad(out var d)) { Debug.LogWarning("No save found."); return; }

        // If we’re already in the right scene, just restore state, otherwise load, then restore.
        if (SceneManager.GetActiveScene().name == d.scene)
        {
            RestoreState(d);
        }
        else
        {
            // Load scene, then restore after it’s ready.
            StartCoroutine(LoadThenRestore(d));
        }
    }

    System.Collections.IEnumerator LoadThenRestore(SaveSystem.SaveData d)
    {
        // Make sure we’re unpaused and input is live during the hop.
        stateUnpause();
        var op = SceneManager.LoadSceneAsync(d.scene);
        while (!op.isDone) yield return null;

        // Re-find references because scene changed.
        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<playerController>();

        RestoreState(d);
    }

    void RestoreState(SaveSystem.SaveData d)
    {
        // Position the player & restore stats/UI.
        player.transform.position = new Vector3(d.px, d.py, d.pz);
        playerScript.SetHP(d.playerHP);

        gameGoalCount = d.gameGoalCount;
        if (gameGoalCountText != null)
            gameGoalCountText.text = gameGoalCount.ToString("F0");
    }

    public int GetGameGoalCount()
    {
        return gameGoalCount;
    }
}
