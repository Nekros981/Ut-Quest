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

    public void SetArma(int arma) => animator.SetInteger(ArmaId, arma);

    public bool IsDefeated => animator != null && animator.GetBool(DerrotadoId);

    public void PlayDerrotado()
    {
        if (animator.GetBool(DerrotadoId))
            return;
        animator.SetInteger(ActionId, ActionNone);
        animator.SetBool(IsMovingId, false);
        animator.SetBool(DerrotadoId, true);
    }

    public void PlayGolpe()
    {
        if (animator.GetBool(DerrotadoId))
            return;
        if (animator.GetInteger(ActionId) != ActionNone)
            return;
        if (animator.GetInteger(ArmaId) != ArmaTolete)
            return;
        animator.SetInteger(ActionId, ActionMelee);
    }

    public void PlayDisparo()
    {
        if (animator.GetBool(DerrotadoId))
            return;
        if (animator.GetInteger(ActionId) != ActionNone)
            return;
        if (animator.GetInteger(ArmaId) != ArmaPistola)
            return;
        animator.SetInteger(ActionId, ActionShoot);
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
