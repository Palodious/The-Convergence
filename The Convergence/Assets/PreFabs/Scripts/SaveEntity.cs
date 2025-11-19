using UnityEngine;
using System.Collections.Generic;

// I put this on anything in the scene that I want the SaveManager to track.
// It gives each object a unique ID so the save file can match records back to real objects.
[ExecuteAlways]    // I want this to run in edit mode too so IDs stay unique while levels are built.
public class SaveEntity : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private string id;

    // This is an optional key I use so the SaveManager can spawn a prefab if this entity doesn't exist in the scene yet.
    public string prefabKey;

    // Public read-only wrapper so other scripts (and SaveManager during normal access) can see the ID.
    public string Id => id;

    // This is an editor-only lookup I use to detect duplicate IDs in the current scene.
    // It never goes into builds with any overhead other than a simple dictionary.
    private static readonly Dictionary<string, SaveEntity> editorLookup = new();

    // OnValidate runs whenever something changes in the inspector, I duplicate an object, or scripts recompile.
    // I use it to keep IDs unique while leels are being built.
    void OnValidate()
    {
        // I only care about edit-time uniqueness checks here.
        if (Application.isPlaying)
            return;

        // If I don't have an ID yet, or this ID is already used by some other SaveEntity, I generate a new one.
        if (string.IsNullOrEmpty(id) || !IsUniqueInEditor(id))
        {
            id = System.Guid.NewGuid().ToString("N");   // I use a GUID so collisions are basically impossible.
        }

        // Track this instance in my editor lookup so I can spot duplicates.
        editorLookup[id] = this;
    }

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
        if (editorLookup.TryGetValue(id, out var existing) && existing == this)
        {
            editorLookup.Remove(id);
        }
    }

    // Awake runs both in play mode and (because of ExecuteAlways) sometimes in the editor.
    // Here I make sure that at runtime I never end up with an empty ID.
    void Awake()
    {
        // In a build, or when the game is actually running, if I somehow don't have an ID,
        // I generate one so this object can still be tracked correctly in the save file.
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString("N");
        }
    }
}