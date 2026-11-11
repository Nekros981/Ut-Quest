using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public sealed class CampusQuestBunkerDoor : MonoBehaviour
{
    static readonly int AbrirId = Animator.StringToHash("Abrir");
    const string StateClose = "Cierra";
    const string StateClosed = "Cerrada";
    const string StateOpen = "Abre";

    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Animator animator;
    [SerializeField] Collider2D passageBlocker;
    [SerializeField] bool startHeldOpen;

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

    void Start()
    {
        if (!startHeldOpen)
            return;

        animator.Play(StateOpen, 0, 0.99f);
        animator.speed = 0f;
        animator.Update(0f);
        lastSprite = spriteRenderer.sprite;

        HasOpened = true;
        blockerCleared = true;
        openRequested = false;
        closing = false;

        if (passageBlocker != null)
            passageBlocker.enabled = false;
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

        BeginCloseAnimation();
    }

    public void CloseForBossEncounter()
    {
        if (closing || (!HasOpened && !IsOpening))
            return;

        BeginCloseAnimation();
    }

    void BeginCloseAnimation()
    {
        openRequested = false;
        HasOpened = false;
        closing = true;
        blockerCleared = false;

        animator.SetBool(AbrirId, false);
        animator.speed = 1f;
        animator.Play(StateClose, 0, 0f);
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
            if (state.IsName(StateClose)
                && !animator.IsInTransition(0)
                && state.normalizedTime >= 1f)
            {
                closing = false;
                animator.speed = 1f;
                animator.Play(StateClosed, 0, 0f);
                if (passageBlocker != null)
                    passageBlocker.enabled = true;
            }

            return;
        }

        if (!openRequested || blockerCleared)
            return;

        var openState = animator.GetCurrentAnimatorStateInfo(0);
        if (!openState.IsName(StateOpen) || openState.normalizedTime < 1f || animator.IsInTransition(0))
            return;

        HasOpened = true;
        blockerCleared = true;
        if (passageBlocker != null)
            passageBlocker.enabled = false;
    }
}
