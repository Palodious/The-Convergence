using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable] public class SaveRecord { public string typeName; public string json; }

[Serializable]
public class EntityRecord
{
    public string id;
    public string prefabKey;
    public Vector3 pos;
    public Quaternion rot;
    public List<SaveRecord> components = new();
}

[Serializable]
public class SaveData
{
    public string scene;
    public string version = "1.0.0";
    public string playerId;
    public Vector3 playerPos;
    public Quaternion playerRot;
    public int gameGoalCount;
    public int playerGunIndex;
    public List<EntityRecord> entities = new();
}

public class SaveManager : MonoBehaviour
{
    public static bool PendingLoad = false;
    public static bool BlockSaving;
    public static SaveManager Instance { get; private set; }
    public static bool IsLoadingFromSave { get; private set; }

    string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Save(GameObject player, int gameGoalCount)
    {
        if (BlockSaving) return;

        var pc = player.GetComponent<playerController>();

        var data = new SaveData
        {
            scene = SceneManager.GetActiveScene().name,
            playerId = player.GetComponent<SaveEntity>()?.Id,
            playerPos = player.transform.position,
            playerRot = player.transform.rotation,
            gameGoalCount = gameGoalCount,
            playerGunIndex = pc != null ? pc.GetCurrentGunIndex() : 0
        };

        // Save all scene SaveEntities
        var saveEntities = UnityEngine.Object.FindObjectsByType<SaveEntity>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var se in saveEntities)
        {
            if (se == null) continue;
            var record = new EntityRecord
            {
                id = se.Id,
                prefabKey = se.prefabKey,
                pos = se.transform.position,
                rot = se.transform.rotation
            };

            foreach (var mb in se.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is ISaveable isv)
                {
                    var payload = isv.CaptureState();
                    if (payload == null) continue;

                    record.components.Add(new SaveRecord
                    {
                        typeName = mb.GetType().AssemblyQualifiedName,
                        json = JsonUtility.ToJson(payload)
                    });
                }
            }

            data.entities.Add(record);
        }

        // Save singleton/global managers
        SaveGlobalSingleton(GunUpgradeManager.Instance, "GunUpgradeManager", data);
        SaveGlobalSingleton(RiftShardManager.Instance, "RiftShardManager", data);
        // Add other singletons here

        // Write to disk
        var json = JsonUtility.ToJson(data, false);
        string tmpPath = SavePath + ".tmp";

        if (File.Exists(tmpPath)) File.Delete(tmpPath);
        File.WriteAllText(tmpPath, json);

        if (File.Exists(SavePath)) File.Delete(SavePath);
        File.Move(tmpPath, SavePath);
    }

    private void SaveGlobalSingleton(ISaveable singleton, string id, SaveData data)
    {
        if (singleton == null) return;

        var payload = singleton.CaptureState();
        if (payload == null) return;

        var record = new EntityRecord
        {
            id = "global_" + id,
            prefabKey = "",
            pos = Vector3.zero,
            rot = Quaternion.identity,
            components = new List<SaveRecord>
            {
                new SaveRecord
                {
                    typeName = singleton.GetType().AssemblyQualifiedName,
                    json = JsonUtility.ToJson(payload)
                }
            }
        };

        data.entities.Add(record);
    }

    public bool TryLoad(out SaveData data)
    {
        data = null;
        if (!File.Exists(SavePath)) return false;
        data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        return data != null;
    }

    public bool HasSave() => File.Exists(SavePath);

    public IEnumerator LoadAndRestore(SaveData data, Func<string, GameObject> spawnByKey)
    {
        IsLoadingFromSave = true;
        try
        {
            if (SceneManager.GetActiveScene().name != data.scene)
            {
                var op = SceneManager.LoadSceneAsync(data.scene);
                while (!op.isDone) yield return null;
            }
            yield return null;

            var saveEntities = UnityEngine.Object.FindObjectsByType<SaveEntity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            var existing = new Dictionary<string, GameObject>();
            foreach (var se in saveEntities)
            {
                if (se == null || string.IsNullOrEmpty(se.Id) || existing.ContainsKey(se.Id)) continue;
                existing.Add(se.Id, se.gameObject);
            }

            foreach (var kv in existing.ToList())
                if (!data.entities.Any(e => e.id == kv.Key))
                    GameObject.Destroy(kv.Value);

            foreach (var e in data.entities)
            {
                if (!existing.TryGetValue(e.id, out var go))
                {
                    if (!string.IsNullOrEmpty(e.prefabKey) && spawnByKey != null)
                        go = spawnByKey(e.prefabKey);
                    if (!go) continue;

                    var se = go.GetComponent<SaveEntity>() ?? go.AddComponent<SaveEntity>();
                    var f = typeof(SaveEntity).GetField("id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    f?.SetValue(se, e.id);
                    existing[e.id] = go;
                }

                go.transform.SetPositionAndRotation(e.pos, e.rot);

                foreach (var c in e.components)
                {
                    var t = Type.GetType(c.typeName);
                    if (t == null) continue;

                    var comp = go.GetComponent(t) as ISaveable;
                    if (comp == null) continue;

                    var sample = comp.CaptureState();
                    if (sample == null) continue;

                    var payloadType = sample.GetType();
                    var payload = JsonUtility.FromJson(c.json, payloadType);
                    comp.RestoreState(payload);
                }
            }

            // Restore global singletons
            foreach (var e in data.entities.Where(x => x.id.StartsWith("global_")))
            {
                ISaveable singleton = null;
                if (e.id == "global_GunUpgradeManager") singleton = GunUpgradeManager.Instance;
                else if (e.id == "global_RiftShardManager") singleton = RiftShardManager.Instance;
                // Add other singletons here

                if (singleton == null) continue;

                foreach (var c in e.components)
                {
                    var t = Type.GetType(c.typeName);
                    if (t == null) continue;

                    var payload = JsonUtility.FromJson(c.json, t);
                    singleton.RestoreState(payload);
                }
            }

            // Restore player position and weapon index
            if (!string.IsNullOrEmpty(data.playerId) && existing.TryGetValue(data.playerId, out var playerGo) && playerGo != null)
            {
                playerGo.transform.SetPositionAndRotation(data.playerPos, data.playerRot);
                var pc = playerGo.GetComponent<playerController>();
                if (pc != null) pc.RestoreGunVisual(data.playerGunIndex);
            }
            else
            {
                var taggedPlayer = GameObject.FindWithTag("Player");
                if (taggedPlayer != null)
                {
                    taggedPlayer.transform.SetPositionAndRotation(data.playerPos, data.playerRot);
                    var pc = taggedPlayer.GetComponent<playerController>();
                    if (pc != null) pc.RestoreGunVisual(data.playerGunIndex);
                }
            }
        }
        finally
        {
            IsLoadingFromSave = false;
        }
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);

            string tmpPath = SavePath + ".tmp";
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
        catch { }
    }
}
