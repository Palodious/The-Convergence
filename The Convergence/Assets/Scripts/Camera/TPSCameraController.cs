using UnityEngine;

public class TPSCameraController : MonoBehaviour
{
    [Header("Camera Targets")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 2, -4);

    [Header("Camera Settings")]
    [Range(1f, 20f)] public float followSpeed = 10f;

    // Base sensitivity set in Inspector
    [Range(50f, 500f)] public float baseMouseSensitivity = 200f;

    //Preferences
    private const string PREF_KEY = "mouse_sensitivity";
    private float sensitivityMult = 1f;

    [Range(-80f, 0f)] public float minPitch = -40f;
    [Range(0f, 80f)] public float maxPitch = 60f;

    private float yaw;
    private float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target == null && gamemanager.instance != null && gamemanager.instance.player != null)
        {
            target = gamemanager.instance.player.transform;
        }

        // Load once on start
        sensitivityMult = PlayerPrefs.GetFloat(PREF_KEY, 1.0f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        sensitivityMult = PlayerPrefs.GetFloat(PREF_KEY, 1.0f);

        float effectiveSensitivity = baseMouseSensitivity * sensitivityMult;
        float mouseX = Input.GetAxis("Mouse X") * effectiveSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * effectiveSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position + rot * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}