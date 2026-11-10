using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CampusQuestSecuazVisualDriver : MonoBehaviour
{
    public const int ArmaTolete = 0;
    public const int ArmaPistola = 1;
    public const int ActionNone = 0;
    public const int ActionMelee = 1;
    public const int ActionShoot = 2;

    [SerializeField] int armaInicial = ArmaTolete;
    public Transform motionRoot;

    Animator animator;
    SpriteRenderer spriteRenderer;
    Vector3 lastPosition;
    int direction;

    static readonly int DirectionId = Animator.StringToHash("Direction");
    static readonly int IsMovingId = Animator.StringToHash("IsMoving");
    static readonly int ActionId = Animator.StringToHash("Action");
    static readonly int ArmaId = Animator.StringToHash("Arma");
    static readonly int DerrotadoId = Animator.StringToHash("Derrotado");

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (motionRoot == null)
            motionRoot = transform.parent;
        lastPosition = motionRoot != null ? motionRoot.position : transform.position;
        animator.SetInteger(ArmaId, armaInicial);
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

    public Vector2 Facing => direction switch
    {
        1 => Vector2.left,
        2 => Vector2.right,
        3 => Vector2.up,
        _ => Vector2.down,
    };

    public Transform ShooterRoot => motionRoot != null ? motionRoot : transform;

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

    public void SetArma(int arma) => animator.SetInteger(ArmaId, arma);

    public bool IsDefeated => animator != null && animator.GetBool(DerrotadoId);

    public bool IsActing => animator != null && animator.GetInteger(ActionId) != ActionNone;

    public int CurrentArma => animator != null ? animator.GetInteger(ArmaId) : armaInicial;

    public void PlayDerrotado()
    {
        if (animator.GetBool(DerrotadoId))
            return;
        animator.SetInteger(ActionId, ActionNone);
        animator.SetBool(IsMovingId, false);
        animator.SetBool(DerrotadoId, true);
    }

    public bool PlayGolpe()
    {
        if (animator.GetBool(DerrotadoId))
            return false;
        if (animator.GetInteger(ActionId) != ActionNone)
            return false;
        if (animator.GetInteger(ArmaId) != ArmaTolete)
            return false;
        animator.SetInteger(ActionId, ActionMelee);
        return true;
    }

    public bool PlayDisparo()
    {
        if (animator.GetBool(DerrotadoId))
            return false;
        if (animator.GetInteger(ActionId) != ActionNone)
            return false;
        if (animator.GetInteger(ArmaId) != ArmaPistola)
            return false;
        animator.SetInteger(ActionId, ActionShoot);
        return true;
    }

    bool IsCurrentActionStateFinished()
    {
        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName("Golpe_Down") && !info.IsName("Golpe_Side") && !info.IsName("Golpe_Up") &&
            !info.IsName("Disparo_Down") && !info.IsName("Disparo_Side") && !info.IsName("Disparo_Up"))
            return false;
        return info.normalizedTime >= 1f;
    }
}
