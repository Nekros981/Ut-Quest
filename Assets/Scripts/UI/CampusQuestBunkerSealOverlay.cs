using TMPro;
using UnityEngine;

public sealed class CampusQuestBunkerSealOverlay : MonoBehaviour
{
    [SerializeField] Animator sealAnimator;
    [SerializeField] CampusQuestMateoPlayerInput mateoInput;
    [SerializeField] TextMeshProUGUI continueHint;
    [SerializeField] string playStateName = "Sellar";

    bool shown;
    bool mateoWasEnabled;
    bool hintVisible;

    void Awake()
    {
        if (mateoInput == null)
            mateoInput = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
        EnsureContinueHint();
        if (continueHint != null)
            continueHint.enabled = false;
    }

    void EnsureContinueHint()
    {
        if (continueHint != null)
            return;

        var existing = transform.Find("Sello_Hint");
        if (existing != null)
            continueHint = existing.GetComponent<TextMeshProUGUI>();

        if (continueHint != null)
            return;

        var hintGo = new GameObject("Sello_Hint", typeof(RectTransform));
        hintGo.transform.SetParent(transform, false);
        var rect = (RectTransform)hintGo.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(900f, 64f);
        rect.anchoredPosition = new Vector2(0f, 48f);

        continueHint = hintGo.AddComponent<TextMeshProUGUI>();
        continueHint.font = TMP_Settings.defaultFontAsset;
        continueHint.fontSize = 28;
        continueHint.color = new Color32(233, 216, 166, 255);
        continueHint.alignment = TextAlignmentOptions.Center;
        continueHint.raycastTarget = false;
        continueHint.enabled = false;
    }

    void Update()
    {
        if (!shown || !gameObject.activeSelf)
            return;

        if (!SealAnimationFinished())
        {
            if (hintVisible)
                HideContinueHint();
            return;
        }

        ShowContinueHint();

        if (!CampusQuestActiveControls.InteractPressedThisFrame())
            return;

        Dismiss();
    }

    public void Show()
    {
        if (shown)
            return;

        shown = true;
        hintVisible = false;
        HideContinueHint();
        EnsureContinueHint();
        gameObject.SetActive(true);
        SuspendMateo();

        if (sealAnimator != null)
        {
            sealAnimator.Rebind();
            sealAnimator.Update(0f);
            sealAnimator.Play(playStateName, 0, 0f);
        }
    }

    bool SealAnimationFinished()
    {
        if (sealAnimator == null)
            return true;

        var state = sealAnimator.GetCurrentAnimatorStateInfo(0);
        if (sealAnimator.IsInTransition(0))
            return false;

        return state.IsName(playStateName) && state.normalizedTime >= 1f;
    }

    void ShowContinueHint()
    {
        if (continueHint == null || hintVisible)
            return;

        string key = CampusQuestActiveControls.BindingLabel("Player", "Interact");
        continueHint.text = $"{key} · para continuar";
        continueHint.enabled = true;
        hintVisible = true;
    }

    void HideContinueHint()
    {
        hintVisible = false;
        if (continueHint != null)
            continueHint.enabled = false;
    }

    void Dismiss()
    {
        HideContinueHint();
        gameObject.SetActive(false);
        ResumeMateo();
    }

    void SuspendMateo()
    {
        if (mateoInput == null)
            return;

        mateoWasEnabled = mateoInput.enabled;
        mateoInput.enabled = false;

        var body = mateoInput.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    void ResumeMateo()
    {
        if (mateoInput == null)
            return;

        mateoInput.enabled = mateoWasEnabled;

        var body = mateoInput.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }
}
