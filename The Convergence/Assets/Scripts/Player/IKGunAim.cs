using UnityEngine;

public class IKGunAim : MonoBehaviour
{
    [Header("~=~= IK Targets =~=~")]
    [SerializeField] Transform leftHandIKTarget;
    [SerializeField] Transform rightHandIKTarget;
    [SerializeField] Transform gunAimTarget;

    [Header("~=~= IK Settings =~=~")]
    [SerializeField] float aimSpeed = 10f;
    [SerializeField] float ikWeightSpeed = 5f;
    [SerializeField] LayerMask aimLayerMask;
    [SerializeField] float maxAimDistance = 100f;

    private Animator animator;
    private float leftHandIKWeight = 0f;
    private float rightHandIKWeight = 0f;
    private Vector3 smoothedAimPosition;
    private bool isAiming = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on IKGunAim!");
        }

        if (leftHandIKTarget == null || rightHandIKTarget == null || gunAimTarget == null)
        {
            Debug.LogWarning("IK targets not assigned in IKGunAim!");
        }
    }

    void Update()
    {
        UpdateAimTarget();
        UpdateIKWeights();
    }

    void UpdateAimTarget()
    {
        Ray aimRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;
        Vector3 targetPosition;

        if (Physics.Raycast(aimRay, out hit, maxAimDistance, aimLayerMask))
        {
            targetPosition = hit.point;
        }
        else
        {
            targetPosition = Camera.main.transform.position + Camera.main.transform.forward * maxAimDistance;
        }

        smoothedAimPosition = Vector3.Lerp(smoothedAimPosition, targetPosition, Time.deltaTime * aimSpeed);

        if (gunAimTarget != null)
        {
            gunAimTarget.position = smoothedAimPosition;
        }

        isAiming = true;
    }

    void UpdateIKWeights()
    {
        float targetWeight = isAiming ? 1f : 0f;
        leftHandIKWeight = Mathf.Lerp(leftHandIKWeight, targetWeight, Time.deltaTime * ikWeightSpeed);
        rightHandIKWeight = Mathf.Lerp(rightHandIKWeight, targetWeight, Time.deltaTime * ikWeightSpeed);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        if (leftHandIKTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIKTarget.rotation);
        }

        if (rightHandIKTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);
        }

        if (gunAimTarget != null)
        {
            animator.SetLookAtWeight(1f, 0.5f, 1f);
            animator.SetLookAtPosition(gunAimTarget.position);
        }
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    public void ForceFullIK()
    {
        leftHandIKWeight = 1f;
        rightHandIKWeight = 1f;
    }
}