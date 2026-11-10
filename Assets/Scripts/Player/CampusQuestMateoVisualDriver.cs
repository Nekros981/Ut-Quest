using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CampusQuestMateoVisualDriver : MonoBehaviour
{
    public const int ActionNone = 0;
    public const int ActionShoot = 1;
    public const int ActionMelee = 2;

    public Transform motionRoot;

    CampusQuestLoadout loadout;
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
        if (motionRoot != null)
            loadout = motionRoot.GetComponent<CampusQuestLoadout>();
        lastPosition = motionRoot != null ? motionRoot.position : transform.position;
    }

    // Turning toward a blocked cell (an NPC or a counter) must also turn the idle sprite.
    public void Face(Vector2Int facing)
    {
        if (facing == Vector2Int.zero) return;
        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y)) direction = facing.x > 0 ? 2 : 1;
        else direction = facing.y > 0 ? 3 : 0;
        if (animator != null) animator.SetInteger("Direction", direction);
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

    public bool PlayDisparo()
    {
        if (loadout == null || !loadout.HasPistol)
            return false;
        if (animator.GetInteger(ActionId) != ActionNone)
            return false;
        animator.SetInteger(ActionId, ActionShoot);
        return true;
    }

    public bool PlayGolpe()
    {
        if (loadout == null || !loadout.HasClub)
            return false;
        if (animator.GetInteger(ActionId) != ActionNone)
            return false;
        animator.SetInteger(ActionId, ActionMelee);
        return true;
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
