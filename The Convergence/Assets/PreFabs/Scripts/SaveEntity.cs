using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// I attach this to anything that needs to be saved or loaded.
// It gives each object a unique ID so the save system can identify it later.
// I can also give it a prefabKey ,like a Resources path, so it can respawn missing objects.
public class SaveEntity : MonoBehaviour
{
    [SerializeField] string id;  // This ID is unique per object. It’s generated automatically once.
    [Tooltip("Optional: Resources path or Addressables key so this can respawn if missing on load.")]
    public string prefabKey;     // Example: "Enemies/Grunt" for Resources/Enemies/Grunt.prefab.

    public string Id => id;// I can reference the ID, but not change it in code.

#if UNITY_EDITOR
    void OnValidate()
    {
        // When editing prefabs or scene objects, this auto-generates an ID if one doesn't exist.
        if (!Application.isPlaying && string.IsNullOrEmpty(id))
        {
            Undo.RecordObject(this, "Assign SaveEntity ID");

            // Creates a random unique string.
            id = System.Guid.NewGuid().ToString("N"); 

            // Marks the object as changed in the editor.
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
