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

    public bool IsOpening => openRequested && !HasOpened;
    public bool HasOpened { get; private set; }

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
        if (HasOpened || openRequested)
            return;

        openRequested = true;
        animator.SetBool(AbrirId, true);
    }

    void LateUpdate()
    {
        if (spriteRenderer.sprite != lastSprite)
        {
            var delta = anchorCenter - spriteRenderer.bounds.center;
            transform.position += delta;
            lastSprite = spriteRenderer.sprite;
        }

        if (!openRequested || blockerCleared)
            return;

        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName("Abre") || state.normalizedTime < 1f || animator.IsInTransition(0))
            return;

        HasOpened = true;
        blockerCleared = true;
        if (passageBlocker != null)
            passageBlocker.enabled = false;
    }
}
