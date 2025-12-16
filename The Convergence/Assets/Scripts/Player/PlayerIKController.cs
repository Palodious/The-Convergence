using UnityEngine;

public class PlayerIKController : MonoBehaviour
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
    [SerializeField] float lookAtWeight = 1f;
    [SerializeField] float bodyWeight = 0.5f;
    [SerializeField] float headWeight = 1f;
    [SerializeField] float eyesWeight = 0f;
    [SerializeField] float clampWeight = 0.5f;

    private Animator animator;
    private playerController playerController;
    private float leftHandIKWeight = 0f;
    private float rightHandIKWeight = 0f;
    private Vector3 smoothedAimPosition;
    private bool isAiming = false;
    private Transform currentGunTransform;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            //Debug.LogError("Animator component not found on PlayerIKController!");
        }

        if (leftHandIKTarget == null || rightHandIKTarget == null || gunAimTarget == null)
        {
            //Debug.LogWarning("IK targets not assigned in PlayerIKController!");
        }
    }

    void Update()
    {
        UpdateAimTarget();
        UpdateIKWeights();
        UpdateHandTargetPositions();
    }

    void UpdateAimTarget()
    {
        if (Camera.main == null) return;

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
    }

    void UpdateIKWeights()
    {
        if (playerController == null) return;

        bool shouldAim = isAiming && !playerController.IsDead();
        float targetWeight = shouldAim ? 1f : 0f;

        leftHandIKWeight = Mathf.Lerp(leftHandIKWeight, targetWeight, Time.deltaTime * ikWeightSpeed);
        rightHandIKWeight = Mathf.Lerp(rightHandIKWeight, targetWeight, Time.deltaTime * ikWeightSpeed);
    }

    void UpdateHandTargetPositions()
    {
        if (currentGunTransform == null) return;

        if (leftHandIKTarget != null)
        {
            Transform leftHandGrip = FindChildRecursive(currentGunTransform, "LeftHandIK");
            if (leftHandGrip != null)
            {
                leftHandIKTarget.position = leftHandGrip.position;
                leftHandIKTarget.rotation = leftHandGrip.rotation;
            }
        }

        if (rightHandIKTarget != null)
        {
            Transform rightHandGrip = FindChildRecursive(currentGunTransform, "RightHandIK");
            if (rightHandGrip != null)
            {
                rightHandIKTarget.position = rightHandGrip.position;
                rightHandIKTarget.rotation = rightHandGrip.rotation;
            }
        }
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        if (playerController != null && playerController.IsDead())
        {
            ResetIK();
            return;
        }

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

        if (gunAimTarget != null && isAiming)
        {
            animator.SetLookAtWeight(lookAtWeight, bodyWeight, headWeight, eyesWeight, clampWeight);
            animator.SetLookAtPosition(gunAimTarget.position);
        }
        else
        {
            animator.SetLookAtWeight(0f);
        }
    }

    void ResetIK()
    {
        if (animator == null) return;

        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        animator.SetLookAtWeight(0f);
    }

    public void SetPlayerController(playerController controller)
    {
        playerController = controller;
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    public void SetGunTransform(Transform gunTransform)
    {
        currentGunTransform = gunTransform;
    }

    public void ForceFullIK()
    {
        leftHandIKWeight = 1f;
        rightHandIKWeight = 1f;
        isAiming = true;
    }

    public void DisableIK()
    {
        leftHandIKWeight = 0f;
        rightHandIKWeight = 0f;
        isAiming = false;
    }

    public bool IsAiming()
    {
        return isAiming;
    }
}