using UnityEngine;

public class GunAimAtCrosshair : MonoBehaviour
{
    [Header("Aiming Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Vector3 aimOffset = Vector3.zero;
    [SerializeField] private bool useSmoothRotation = true;

    private Camera mainCamera;
    private Transform playerController;

    void Start()
    {
        mainCamera = Camera.main;

        playerController = transform.parent;
        while (playerController != null && playerController.GetComponent<playerController>() == null)
        {
            playerController = playerController.parent;
        }

        if (playerController == null)
        {
          //  Debug.LogWarning("GunAimAtCrosshair: Could not find playerController in parent hierarchy");
        }
    }

    void Update()
    {
        if (mainCamera == null || playerController == null) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100f);
        }

        Vector3 directionToTarget = targetPoint - transform.position;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        if (aimOffset != Vector3.zero)
        {
            targetRotation *= Quaternion.Euler(aimOffset);
        }

        if (useSmoothRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }

    public void SetAimPoint(Vector3 worldPoint)
    {
        Vector3 directionToTarget = worldPoint - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        if (aimOffset != Vector3.zero)
        {
            targetRotation *= Quaternion.Euler(aimOffset);
        }

        transform.rotation = useSmoothRotation
            ? Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime)
            : targetRotation;
    }
}