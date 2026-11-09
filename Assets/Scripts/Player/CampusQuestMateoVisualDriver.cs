using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CampusQuestMateoVisualDriver : MonoBehaviour
{
    public const int ActionNone = 0;
    public const int ActionShoot = 1;
    public const int ActionMelee = 2;

    public Transform motionRoot;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector3 lastPosition;
    private int direction = 0; // Down 0, Left 1, Right 2, Up 3

    private static readonly int DirectionId = Animator.StringToHash("Direction");
    private static readonly int IsMovingId = Animator.StringToHash("IsMoving");
    private static readonly int ActionId = Animator.StringToHash("Action");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (motionRoot == null)
            motionRoot = transform.parent;
        lastPosition = motionRoot != null ? motionRoot.position : transform.position;
    }

    private void LateUpdate()
    {
        Vector3 current = motionRoot != null ? motionRoot.position : transform.position;
        Vector3 delta = current - lastPosition;
        bool walking = delta.sqrMagnitude > .0000001f;
        if (walking && animator.GetInteger(ActionId) == ActionNone)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                direction = delta.x > 0 ? 2 : 1;
            else
                direction = delta.y > 0 ? 3 : 0;
        }

        animator.SetInteger(DirectionId, direction);
        animator.SetBool(IsMovingId, walking && animator.GetInteger(ActionId) == ActionNone);

        int action = animator.GetInteger(ActionId);
        spriteRenderer.flipX = direction == 1 && (action == ActionShoot || action == ActionMelee);

        if (action != ActionNone && IsCurrentActionStateFinished())
            animator.SetInteger(ActionId, ActionNone);

        lastPosition = current;
    }

    public void PlayDisparo()
    {
        if (animator.GetInteger(ActionId) != ActionNone)
            return;
        animator.SetInteger(ActionId, ActionShoot);
    }

    public void PlayGolpe()
    {
        if (animator.GetInteger(ActionId) != ActionNone)
            return;
        animator.SetInteger(ActionId, ActionMelee);
    }

    bool IsCurrentActionStateFinished()
    {
        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName("Disparo_Down") && !info.IsName("Disparo_Side") && !info.IsName("Disparo_Up") &&
            !info.IsName("Golpe_Down") && !info.IsName("Golpe_Side") && !info.IsName("Golpe_Up"))
            return false;

        return info.normalizedTime >= 1f;
    }
}
