using UnityEngine;

public class PlayerIKController : MonoBehaviour
{
    public Animator animator;
    public Transform rightHandIKTarget;
    [Range(0f, 1f)] public float ikWeight = 1f;

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
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || rightHandIKTarget == null) return;

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
        animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandIKTarget.position);
        animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);
    }
}