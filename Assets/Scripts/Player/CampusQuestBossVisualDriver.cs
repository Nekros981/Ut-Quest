using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CampusQuestBossVisualDriver : MonoBehaviour
{
    public const int ActionNone = 0;
    public const int ActionMelee = 1;
    public const int ActionGrenade = 2;

    [SerializeField] Transform motionRoot;

    Animator animator;
    SpriteRenderer spriteRenderer;
    Vector3 lastPosition;
    int direction;

    static readonly int DirectionId = Animator.StringToHash("Direction");
    static readonly int IsMovingId = Animator.StringToHash("IsMoving");
    static readonly int ActionId = Animator.StringToHash("Action");
    static readonly int DerrotadoId = Animator.StringToHash("Derrotado");

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (motionRoot == null)
            motionRoot = transform.parent;
        lastPosition = motionRoot != null ? motionRoot.position : transform.position;
    }

    void LateUpdate()
    {
        if (animator.GetBool(DerrotadoId))
            return;

        Vector3 current = motionRoot != null ? motionRoot.position : transform.position;
        Vector3 delta = current - lastPosition;
        int action = animator.GetInteger(ActionId);
        bool walking = delta.sqrMagnitude > .0000001f;

        if (walking && action == ActionNone)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                direction = delta.x > 0 ? 2 : 1;
            else
                direction = delta.y > 0 ? 3 : 0;
        }

        animator.SetInteger(DirectionId, direction);
        animator.SetBool(IsMovingId, walking && action == ActionNone);
        spriteRenderer.flipX = direction == 1;

        if (action != ActionNone && IsCurrentActionStateFinished())
            animator.SetInteger(ActionId, ActionNone);

        lastPosition = current;
    }

    public Transform MotionRoot => motionRoot != null ? motionRoot : transform;

    public bool IsDefeated => animator != null && animator.GetBool(DerrotadoId);
    public bool IsActing => animator != null && animator.GetInteger(ActionId) != ActionNone;

    public bool IsThrowingGrenade =>
        animator != null && animator.GetInteger(ActionId) == ActionGrenade;

    public void LookAt(Vector2 worldDirection)
    {
        if (worldDirection.sqrMagnitude < 0.0001f)
            return;

        if (Mathf.Abs(worldDirection.x) > Mathf.Abs(worldDirection.y))
            direction = worldDirection.x > 0f ? 2 : 1;
        else
            direction = worldDirection.y > 0f ? 3 : 0;

        animator.SetInteger(DirectionId, direction);
        spriteRenderer.flipX = direction == 1;
    }

    public bool PlayGolpe()
    {
        if (IsDefeated || IsActing)
            return false;
        animator.SetInteger(ActionId, ActionMelee);
        return true;
    }

    public bool PlayGranada()
    {
        if (IsDefeated || IsActing)
            return false;
        animator.SetInteger(ActionId, ActionGrenade);
        return true;
    }

    public void PlayDerrotado()
    {
        if (IsDefeated)
            return;
        animator.SetInteger(ActionId, ActionNone);
        animator.SetBool(IsMovingId, false);
        animator.SetBool(DerrotadoId, true);
    }

    public bool IsGrenadeReleaseFrame()
    {
        if (animator.GetInteger(ActionId) != ActionGrenade)
            return false;

        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName("Granada_Down") && !info.IsName("Granada_Side") && !info.IsName("Granada_Up"))
            return false;

        return info.normalizedTime >= 0.75f;
    }

    bool IsCurrentActionStateFinished()
    {
        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName("Golpe_Down") && !info.IsName("Golpe_Side") && !info.IsName("Golpe_Up") &&
            !info.IsName("Granada_Down") && !info.IsName("Granada_Side") && !info.IsName("Granada_Up"))
            return false;

        return info.normalizedTime >= 0.95f;
    }
}
