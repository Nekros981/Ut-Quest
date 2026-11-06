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
