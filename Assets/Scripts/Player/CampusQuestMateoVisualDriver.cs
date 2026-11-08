using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class CampusQuestMateoVisualDriver : MonoBehaviour
{
    public Transform motionRoot;
    private Animator animator;
    private Vector3 lastPosition;
    private int direction = 0; // Down 0, Left 1, Right 2, Up 3
    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (motionRoot == null) motionRoot = transform.parent;
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
        if (walking)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) direction = delta.x > 0 ? 2 : 1;
            else direction = delta.y > 0 ? 3 : 0;
        }
        animator.SetInteger("Direction", direction);
        animator.SetBool("IsMoving", walking);
        lastPosition = current;
    }
}
