using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// I put this on anything in the scene that I want the SaveManager to track.
// It gives each object a unique ID so the save file can match records back to real objects.
[ExecuteAlways]    // I want this to run in edit mode too so IDs stay unique while levels are built.
public class SaveEntity : MonoBehaviour
{
    [SerializeField] private string id;

    // This is an optional key I use so the SaveManager can spawn a prefab
    // if this entity doesn't exist in the scene yet.
    public string prefabKey;

    // Public read-only wrapper so other scripts (and SaveManager during normal access) can see the ID.
    public string Id => id;

    // Editor-only lookup so I can enforce uniqueness while building levels.
#if UNITY_EDITOR
    static readonly Dictionary<string, SaveEntity> editorLookup = new Dictionary<string, SaveEntity>();
#endif

    void OnValidate()
    {
        #if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        // If it's a PREFAB ASSET in the Project window, I do NOT want an ID. No sir.
        // I want all IDs to be generated on scene instances / clones.
        if (PrefabUtility.IsPartOfPrefabAsset(this))
        {
            // Make sure the prefab asset itself stays blank.
            if (!string.IsNullOrEmpty(id))
            {
                id = string.Empty;
            }
            return;
        }

        // If I don't have an ID yet, or the current one collides with another object,
        // generate a new GUID until it's unique.
        if (string.IsNullOrEmpty(id) || !IsUniqueInEditor(id))
        {
            id = Guid.NewGuid().ToString("N");
        }

        {
            // Track this instance in my editor lookup so I can spot duplicates.
            editorLookup[id] = this;
        }
#endif
    }

#if UNITY_EDITOR

    // This helper checks if a candidate ID is unique among all SaveEntity instances in the editor.
    bool IsUniqueInEditor(string candidate)
    {
        if (!editorLookup.TryGetValue(candidate, out var existing))
            return true; // Nobody has this ID yet, so it's unique.

        // If the existing entry is null or it's literally me, I consider it unique.
        if (existing == null) return true;
        if (existing == this) return true;

        // If I get here, some other object in this scene is already using this ID.
        return false;
    }

    // When a SaveEntity is destroyed (e.g. I delete it from the scene),
    // I clean up the lookup so I don't keep stale references around in the editor.
    void OnDestroy()
    {
        if (!Application.isPlaying && !string.IsNullOrEmpty(id))
        {
            if (editorLookup.TryGetValue(id, out var existing) && existing == this)
            {
                editorLookup.Remove(id);
            }
        }
    }
#endif

    // Awake runs both in play mode and (because of ExecuteAlways) sometimes in the editor.
    // Here I make sure that at runtime I never end up with an empty ID.
    void Awake()
    {
        // In a build, or when the game is actually running, if I somehow don't have an ID,
        // I generate one so this object can still be tracked correctly in the save file.
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString("N");

        }
    }
}