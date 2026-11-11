using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CampusQuestRectoriaLoad : MonoBehaviour
{
    const string TargetScene = "Rectoria_Tutorial_Arte_v01";
    const string StoryText =
        "Un apagón inesperado ha despertado antiguos secretos bajo el campus. "
        + "Ahora un estudiante deberá reunir los sellos para poder conseguir su titulo universitario";

    [SerializeField] InputActionAsset inputActions;
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] TextMeshProUGUI bodyText;
    [SerializeField] TextMeshProUGUI hintText;

    AsyncOperation loadOperation;
    bool readyToContinue;

    void OnEnable()
    {
        CampusQuestActiveControls.Changed += OnActiveControlsChanged;
    }

    void OnDisable()
    {
        CampusQuestActiveControls.Changed -= OnActiveControlsChanged;
    }

    void Awake()
    {
        EnsurePresentation();
        CampusQuestUiGamepad.ConfigureUiInputModule(inputActions);
    }

    void EnsurePresentation()
    {
        if (bodyText != null && hintText != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var root = canvas.transform;
        if (root.Find("Background_Rectoria") == null)
        {
            var bg = new GameObject("Background_Rectoria", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(root, false);
            var bgRect = bg.GetComponent<RectTransform>();
            CampusQuestRectoriaLoadUi.Stretch(bgRect);
            var image = bg.GetComponent<Image>();
            image.sprite = backgroundSprite;
            image.preserveAspect = false;
            image.color = Color.white;
            bg.transform.SetAsFirstSibling();
        }

        if (root.Find("DialogueBox") == null)
        {
            var (body, hint) = CampusQuestRectoriaLoadUi.CreateDialogueBox(root);
            bodyText = body;
            hintText = hint;
        }
    }

    void Start()
    {
        if (inputActions != null)
            CampusQuestActiveControls.Bind(inputActions);

        if (bodyText != null)
            bodyText.text = StoryText;

        readyToContinue = false;
        RefreshHint();

        loadOperation = SceneManager.LoadSceneAsync(TargetScene);
        loadOperation.allowSceneActivation = false;
    }

    void Update()
    {
        if (loadOperation == null)
            return;

        if (!readyToContinue && loadOperation.progress >= 0.9f)
        {
            readyToContinue = true;
            RefreshHint();
        }

        if (!readyToContinue)
            return;

        if (CampusQuestActiveControls.SubmitPressedThisFrame())
            loadOperation.allowSceneActivation = true;
    }

    void OnActiveControlsChanged()
    {
        if (readyToContinue)
            RefreshHint();
    }

    void RefreshHint()
    {
        if (hintText == null)
            return;

        hintText.text = readyToContinue
            ? CampusQuestActiveControls.BindingLabel("UI", "Submit") + " · Continuar"
            : "Cargando…";
    }
}
