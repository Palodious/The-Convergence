using UnityEngine;

public class cameraController : MonoBehaviour
{
    [SerializeField] int sens;
    [SerializeField] int lockVertMin, lockVertMax;
    [SerializeField] bool invertY;

    float camRotX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // get input
        float mouseX = Input.GetAxisRaw("Mouse X") * sens * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sens * Time.deltaTime;

        // use the invertY
        if (invertY)
            camRotX += mouseY;
        else
            camRotX -= mouseY;

        // clamp the camera on the X-axis
        camRotX = Mathf.Clamp(camRotX, lockVertMin, lockVertMax);

        // rotate the camera on the X-axis
        transform.localRotation = Quaternion.Euler(camRotX, 0, 0);

        // rotate the player on the Y-axis
        transform.parent.Rotate(Vector3.up * mouseX);
    }
}