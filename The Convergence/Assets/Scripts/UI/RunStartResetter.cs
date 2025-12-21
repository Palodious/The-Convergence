using UnityEngine;
using UnityEngine.SceneManagement;

public class RunStartResetter : MonoBehaviour
{
    [Header("Scene To Load For Fresh Runs")]
    [SerializeField] private int freshRunSceneBuildIndex = 1;

    public void StartFreshRun()
    {
        SaveManager.Instance?.DeleteSave();

        playerController.ResetAllRuntimePersistence();

        SaveManager.PendingLoad = false;

        if (NewGamePlusManager.Instance != null)
            NewGamePlusManager.Instance.SetCycle(0);

        if (GunUpgradeManager.Instance != null)
            GunUpgradeManager.Instance.ResetToBase();

        if (Store.Instance != null)
            Store.Instance.ResetStoreProgress();

        if (RiftShardManager.Instance != null)
            RiftShardManager.Instance.ResetAmount();

        SceneManager.LoadScene(freshRunSceneBuildIndex);
    }
}
