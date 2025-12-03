using UnityEngine;

// Tilts a UI panel toward the mouse.
// Uses unscaled time so it animates while paused.
public class uiFaceMouse : MonoBehaviour
{
    // Degrees around X (up/down).
    [SerializeField] private float maxTiltX = 10f;
    // Degrees around Y (left/right).
    [SerializeField] private float maxTiltY = 10f;
    // Higher = snappier.
    [SerializeField] private float smooth = 10f;
    [SerializeField] private bool onlyWhenActive = true;

    private RectTransform rt;
    private Quaternion startRot;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        startRot = rt.localRotation;
    }

    void OnEnable()
    {
        // Reset to starting rotation when shown.
        rt.localRotation = startRot;
    }

    void Update()
    {
        if (onlyWhenActive && !gameObject.activeInHierarchy) return;

        // Mouse position in screen space.
        Vector2 mouse = Input.mousePosition;

        // Normalize relative to screen center (-1..1).
        // Left(-1) - right(1).
        float nx = ((mouse.x / Screen.width) - 0.5f) * 2f;
        // Bottom(-1) - top(1).
        float ny = ((mouse.y / Screen.height) - 0.5f) * 2f;

        // Map to tilt angles (ny = inverted).
        float tiltX = -ny * maxTiltX;
        float tiltY = nx * maxTiltY;

        Quaternion target = Quaternion.Euler(tiltX, tiltY, 0f);
        rt.localRotation = Quaternion.Slerp(rt.localRotation, startRot * target, smooth * Time.unscaledDeltaTime);
    }
}
