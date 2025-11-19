using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Each component's save data gets stored in one of these.
// It includes the type of component and the JSON for its state.
[Serializable] public class SaveRecord { public string typeName; public string json; }

// Each object in the scene gets an EntityRecord that stores its ID, prefab info, transform, and component data.
[Serializable]
public class EntityRecord
{
    public string id;
    public string prefabKey;
    public Vector3 pos;
    public Quaternion rot;
    public List<SaveRecord> components = new();
}

// This holds everything I want saved — the scene, player data, objectives, and all entity states.
[Serializable]
public class SaveData
{
    public string scene;
    public string version = "1.0.0"; // I’ll bump this later if I change the save format.
    public string playerId;
    public Vector3 playerPos;
    public Quaternion playerRot;
    public int playerHP;
    public int gameGoalCount;
    public List<EntityRecord> entities = new();
}

// This script handles saving and loading everything.
// It grabs data from the scene, writes it to disk, and restores it when loading.
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // Set to true by the Main Menu Continue button before loading a saved scene.
    public static bool PendingLoad = false;

    // This is where my save file gets written. Unity gives me a platform-safe path.
    string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    void Awake() => Instance = this; // I keep a static reference so I can call it easily.

    // This creates a new SaveData file, fills it with info, and writes it to disk.
    public void Save(GameObject player, int playerHP, int gameGoalCount)
    {
        // Start by saving core game info like player data and the current scene.
        var data = new SaveData
        {
            scene = SceneManager.GetActiveScene().name,
            playerId = player.GetComponent<SaveEntity>()?.Id,
            playerPos = player.transform.position,
            playerRot = player.transform.rotation,
            playerHP = playerHP,
            gameGoalCount = gameGoalCount
        };

        // Loop through every SaveEntity in the scene (active and inactive) and grab their data.
        foreach (var se in FindObjectsOfType<SaveEntity>(true))
        {
            var record = new EntityRecord
            {
                id = se.Id,
                prefabKey = se.prefabKey,
                pos = se.transform.position,
                rot = se.transform.rotation
            };

            // Ask every component that implements ISaveable for its save data.
            foreach (var mb in se.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is ISaveable isv)
                {
                    var payload = isv.CaptureState(); // I grab its data here.
                    if (payload == null) continue;

                    // Store the component’s type and JSON data.
                    record.components.Add(new SaveRecord
                    {
                        typeName = mb.GetType().AssemblyQualifiedName,
                        json = JsonUtility.ToJson(payload)
                    });
                }
            }

            // Add the completed entity record to my list.
            data.entities.Add(record);
        }

        // Convert the whole save into JSON and write it to disk safely.
        var json = JsonUtility.ToJson(data, false);
        File.WriteAllText(SavePath + ".tmp", json);
        if (File.Exists(SavePath)) File.Delete(SavePath);
        File.Move(SavePath + ".tmp", SavePath);
        Debug.Log($"Saved game to {SavePath}");
    }

    // This checks for a save file and loads it into memory.
    public bool TryLoad(out SaveData data)
    {
        data = null;
        if (!File.Exists(SavePath)) return false; // No file means nothing to load.
        data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        return data != null;
    }
    public bool HasSave()
    {
        return File.Exists(SavePath);
    }


    // This coroutine handles the actual world reconstruction when I load a save.
    public System.Collections.IEnumerator LoadAndRestore(SaveData data, Func<string, GameObject> spawnByKey)
    {
        // If I'm in the wrong scene, load the correct one first.
        if (SceneManager.GetActiveScene().name != data.scene)
        {
            var op = SceneManager.LoadSceneAsync(data.scene);
            while (!op.isDone) yield return null;
        }

        // Get all SaveEntities currently in the scene and build a quick lookup by ID.
        var existing = FindObjectsOfType<SaveEntity>(true).ToDictionary(x => x.Id, x => x.gameObject);

        // Destroy anything that wasn’t in the saved file (it was dead or collected).
        foreach (var kv in existing.ToList())
            if (!data.entities.Any(e => e.id == kv.Key))
                GameObject.Destroy(kv.Value);

        // Now go through all saved entities and make sure they exist and are restored correctly.
        foreach (var e in data.entities)
        {
            // If the object doesn’t exist in the scene anymore, spawn it back in.
            if (!existing.TryGetValue(e.id, out var go))
            {
                if (!string.IsNullOrEmpty(e.prefabKey) && spawnByKey != null)
                    go = spawnByKey(e.prefabKey);
                if (!go) continue;

                // Make sure the new object keeps the same SaveEntity ID so it matches the save.
                var se = go.GetComponent<SaveEntity>() ?? go.AddComponent<SaveEntity>();
                var f = typeof(SaveEntity).GetField("id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                f?.SetValue(se, e.id);
                existing[e.id] = go;
            }

            // Restore transform position/rotation first.
            go.transform.SetPositionAndRotation(e.pos, e.rot);

            // Now go through each component and restore its saved values.
            foreach (var c in e.components)
            {
                var t = Type.GetType(c.typeName);
                if (t == null) continue;
                var comp = go.GetComponent(t) as ISaveable;
                if (comp == null) continue;

                // Deserialize the saved data back into the right type.
                var payloadType = comp.GetType().GetMethod("CaptureState").ReturnType;
                var payload = JsonUtility.FromJson(c.json, payloadType);
                comp.RestoreState(payload);
            }
        }
    }
}
