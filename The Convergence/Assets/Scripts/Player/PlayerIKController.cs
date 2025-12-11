using UnityEngine;

public class PlayerIKController : MonoBehaviour
{
    [Header("IK References")]
    public Animator animator;
    public Transform rightHandIKTarget;
    public Transform leftHandIKTarget;
    public Transform aimTarget; // For look-at IK

    [Header("IK Weights")]
    [Range(0f, 1f)] public float rightHandIKWeight = 1f;
    [Range(0f, 1f)] public float leftHandIKWeight = 1f;
    [Range(0f, 1f)] public float lookAtIKWeight = 0f;

    [Header("Look-at IK Settings")]
    [Range(0f, 1f)] public float lookAtBodyWeight = 0.3f;
    [Range(0f, 1f)] public float lookAtHeadWeight = 0.7f;
    [Range(0f, 1f)] public float lookAtEyesWeight = 0f;
    [Range(0f, 1f)] public float lookAtClampWeight = 0.5f;

    // References to gun IK targets (populated by playerController)
    [System.NonSerialized] public Transform gunRightHandIK;
    [System.NonSerialized] public Transform gunLeftHandIK;

    // Smoothing variables
    private float currentRightWeight = 0f;
    private float currentLeftWeight = 0f;
    private float currentLookWeight = 0f;

    [SerializeField] private float weightSmoothSpeed = 8f; // Smooth transition speed

    void Start()
    {
        // Try to find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();

            // Last resort: try to get from gamemanager
            if (animator == null && gamemanager.instance != null && gamemanager.instance.player != null)
            {
                animator = gamemanager.instance.player.GetComponent<Animator>();
            }
        }

        // Initialize weights
        currentRightWeight = rightHandIKWeight;
        currentLeftWeight = leftHandIKWeight;
        currentLookWeight = lookAtIKWeight;

        Debug.Log($"PlayerIKController initialized on {gameObject.name}");
        Debug.Log($"Animator: {animator != null}, Right Target: {rightHandIKTarget != null}, Left Target: {leftHandIKTarget != null}");
    }

    void Update()
    {
        // Smoothly interpolate IK weights for smooth transitions
        currentRightWeight = Mathf.Lerp(currentRightWeight, rightHandIKWeight, Time.deltaTime * weightSmoothSpeed);
        currentLeftWeight = Mathf.Lerp(currentLeftWeight, leftHandIKWeight, Time.deltaTime * weightSmoothSpeed);
        currentLookWeight = Mathf.Lerp(currentLookWeight, lookAtIKWeight, Time.deltaTime * weightSmoothSpeed);

        // Clamp weights to avoid overshooting
        currentRightWeight = Mathf.Clamp01(currentRightWeight);
        currentLeftWeight = Mathf.Clamp01(currentLeftWeight);
        currentLookWeight = Mathf.Clamp01(currentLookWeight);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // LOOK-AT IK (Head/body follows aim target)
        if (aimTarget != null && currentLookWeight > 0.01f)
        {
            animator.SetLookAtWeight(currentLookWeight, lookAtBodyWeight, lookAtHeadWeight, lookAtEyesWeight, lookAtClampWeight);
            animator.SetLookAtPosition(aimTarget.position);
        }
        else
        {
            // Reset look-at if not aiming
            animator.SetLookAtWeight(0f);
        }

        // RIGHT HAND IK
        Transform target = (gunRightHandIK != null) ? gunRightHandIK : rightHandIKTarget;
        if (target != null && currentRightWeight > 0.01f)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentRightWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, currentRightWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, target.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, target.rotation);
        }
        else
        {
            // Reset if target is null or weight is too low
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }

        // LEFT HAND IK
        target = (gunLeftHandIK != null) ? gunLeftHandIK : leftHandIKTarget;
        if (target != null && currentLeftWeight > 0.01f)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentLeftWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentLeftWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, target.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, target.rotation);
        }
        else
        {
            // Reset if target is null
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }
    }

    // Public method to update IK targets from playerController
    public void UpdateGunIKTargets(Transform rightHandTarget, Transform leftHandTarget)
    {
        gunRightHandIK = rightHandTarget;
        gunLeftHandIK = leftHandTarget;
    }

    // Public method to set all IK weights at once
    public void SetIKWeights(float rightWeight, float leftWeight, float lookWeight)
    {
        rightHandIKWeight = Mathf.Clamp01(rightWeight);
        leftHandIKWeight = Mathf.Clamp01(leftWeight);
        lookAtIKWeight = Mathf.Clamp01(lookWeight);
    }
}