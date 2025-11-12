using System.IO;
using UnityEngine;

public static class SaveSystem
{

    public static bool PendingLoad = false;

    private static string Path => System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");

    [System.Serializable]
    public class SaveData
    {
        public string scene;             // current scene name
        public float px, py, pz;         // player position
        public int playerHP;             // player HP
        public int gameGoalCount;        // remaining/collected objectives
    }

    public static void Save(SaveData data)
    {
        var json = JsonUtility.ToJson(data, prettyPrint: false);
        File.WriteAllText(Path, json);
#if UNITY_EDITOR
        Debug.Log($"Saved: {Path}");
#endif
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;
        if (!File.Exists(Path)) return false;
        var json = File.ReadAllText(Path);
        data = JsonUtility.FromJson<SaveData>(json);
        return data != null;
    }

    public static bool HasSave() => File.Exists(Path);
    public static void Delete() { if (File.Exists(Path)) File.Delete(Path); }
}
