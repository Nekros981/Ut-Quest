using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CampusQuestSpLoad : MonoBehaviour
{
    const string TargetScene = "Sp_Bosque";
    const float MinimumSeconds = 5f;

    [SerializeField] RuntimeAnimatorController loadAnimator;
    [SerializeField] Sprite initialFrame;

    AsyncOperation loadOperation;
    float elapsed;

    void Awake()
    {
        EnsurePresentation();
    }

    void Start()
    {
        elapsed = 0f;
        loadOperation = SceneManager.LoadSceneAsync(TargetScene);
        if (loadOperation != null)
            loadOperation.allowSceneActivation = false;
    }

    void EnsurePresentation()
    {
        if (GetComponentInChildren<Animator>(true) != null)
            return;

        var canvasGo = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        var bgImage = bg.GetComponent<Image>();
        bgImage.color = new Color32(24, 26, 34, 255);
        bgImage.raycastTarget = false;

        var animGo = new GameObject(
            "AnimacionCarga",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Animator));
        animGo.transform.SetParent(canvasGo.transform, false);
        var animRect = animGo.GetComponent<RectTransform>();
        animRect.anchorMin = new Vector2(0.5f, 0.5f);
        animRect.anchorMax = new Vector2(0.5f, 0.5f);
        animRect.pivot = new Vector2(0.5f, 0.5f);
        animRect.sizeDelta = new Vector2(380f, 460f);
        animRect.anchoredPosition = Vector2.zero;

        var image = animGo.GetComponent<Image>();
        image.sprite = initialFrame;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;

        var animator = animGo.GetComponent<Animator>();
        animator.runtimeAnimatorController = loadAnimator;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (loadOperation == null)
            return;

        elapsed += Time.unscaledDeltaTime;
        if (elapsed < MinimumSeconds || loadOperation.progress < 0.9f)
            return;

        loadOperation.allowSceneActivation = true;
    }
}
