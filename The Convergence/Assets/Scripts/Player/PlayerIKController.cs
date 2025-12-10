using UnityEngine;

public class PlayerIKController : MonoBehaviour
{
    public Animator animator;
    public Transform rightHandIKTarget;
    public Transform leftHandIKTarget;
    [Range(0f, 1f)] public float rightHandIKWeight = 1f;
    [Range(0f, 1f)] public float leftHandIKWeight = 1f;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null && gamemanager.instance != null && gamemanager.instance.player != null)
                animator = gamemanager.instance.player.GetComponent<Animator>();
        }

        if (rightHandIKTarget == null && gamemanager.instance != null && gamemanager.instance.player != null)
        {
            var p = gamemanager.instance.player.transform;
            var found = p.Find("RightHandIK") ?? p.Find("RightHand");
            if (found != null) rightHandIKTarget = found;
        }

        if (leftHandIKTarget == null && gamemanager.instance != null && gamemanager.instance.player != null)
        {
            var p = gamemanager.instance.player.transform;
            var found = p.Find("LeftHandIK") ?? p.Find("LeftHand");
            if (found != null) leftHandIKTarget = found;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // Right Hand IK
        if (rightHandIKTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);
        }
        else
        {
            // Reset if target is null
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }

        // Left Hand IK
        if (leftHandIKTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIKTarget.rotation);
        }
        else
        {
            // Reset if target is null
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }
    }
}