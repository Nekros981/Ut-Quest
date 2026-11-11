using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public sealed class CampusQuestBunkerDoor : MonoBehaviour
{
    static readonly int AbrirId = Animator.StringToHash("Abrir");

    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Animator animator;
    [SerializeField] Collider2D passageBlocker;

    Vector3 anchorCenter;
    Sprite lastSprite;
    bool openRequested;
    bool blockerCleared;
    bool closing;

    public bool IsOpening => openRequested && !HasOpened;
    public bool HasOpened { get; private set; }
    public bool IsClosing => closing;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();

        anchorCenter = spriteRenderer.bounds.center;
        lastSprite = spriteRenderer.sprite;
    }

    public void Open()
    {
        if (HasOpened || openRequested || closing)
            return;

        openRequested = true;
        closing = false;
        animator.speed = 1f;
        animator.SetBool(AbrirId, true);
    }

    public void CloseReverse()
    {
        if (closing || !HasOpened)
            return;

        openRequested = false;
        HasOpened = false;
        closing = true;
        blockerCleared = false;
        animator.SetBool(AbrirId, false);
        animator.speed = -1f;
        animator.Play("Abre", 0, 1f);
    }

    void LateUpdate()
    {
        if (spriteRenderer.sprite != lastSprite)
        {
            var delta = anchorCenter - spriteRenderer.bounds.center;
            transform.position += delta;
            lastSprite = spriteRenderer.sprite;
        }

        if (closing)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Abre") && !animator.IsInTransition(0) && state.normalizedTime <= 0.02f)
            {
                closing = false;
                animator.speed = 1f;
                animator.Play("Cerrada", 0, 0f);
                if (passageBlocker != null)
                    passageBlocker.enabled = true;
            }

            return;
        }

        if (!openRequested || blockerCleared)
            return;

        var openState = animator.GetCurrentAnimatorStateInfo(0);
        if (!openState.IsName("Abre") || openState.normalizedTime < 1f || animator.IsInTransition(0))
            return;

        HasOpened = true;
        blockerCleared = true;
        if (passageBlocker != null)
            passageBlocker.enabled = false;
    }
}
