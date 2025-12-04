using UnityEngine;

public class TPSCameraController : MonoBehaviour
{
    [Header("Camera Targets")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 2, -4);

    [Header("Camera Settings")]
    [Range(1f, 20f)] public float followSpeed = 10f;
    [Range(50f, 500f)] public float mouseSensitivity = 200f;
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
    }

    void LateUpdate()
    {
        if (target == null) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position + rot * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}