using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class CampusQuestBossIdle : MonoBehaviour
{
    static readonly int DirectionId = Animator.StringToHash("Direction");
    static readonly int IsMovingId = Animator.StringToHash("IsMoving");
    static readonly int ActionId = Animator.StringToHash("Action");

    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (animator == null)
            return;

        animator.SetInteger(DirectionId, 0);
        animator.SetBool(IsMovingId, false);
        animator.SetInteger(ActionId, 0);
    }
}
