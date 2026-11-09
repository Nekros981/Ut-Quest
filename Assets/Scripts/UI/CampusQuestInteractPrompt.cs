using TMPro;
using UnityEngine;

public sealed class CampusQuestInteractPrompt : MonoBehaviour
{
    const string DefaultKey = "E";
    static readonly Vector2 CanvasSize = new(180f, 56f);
    static readonly Vector2 ActionLabelSize = new(170f, 28f);
    static readonly Vector2 KeyLabelSize = new(40f, 28f);

    [SerializeField] Vector3 worldOffset = new(0f, 0.85f, 0f);
    [SerializeField] Canvas canvas;
    [SerializeField] TMP_Text keyText;
    [SerializeField] TMP_Text actionText;

    void Awake()
    {
        EnsureUi();
        Hide();
    }

    void EnsureUi()
    {
        worldOffset = new Vector3(0f, 0.85f, 0f);

        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
                canvasGo.transform.SetParent(transform, false);
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvasGo.GetComponent<RectTransform>().localScale = Vector3.one * 0.01f;
            }
        }

        var canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = CanvasSize;
        canvasRect.localScale = Vector3.one * 0.01f;

        if (keyText == null)
            keyText = FindOrCreateLabel("Tecla", canvas.transform);
        if (actionText == null)
            actionText = FindOrCreateLabel("Accion", canvas.transform);

        ApplyKeyLayout(keyText);
        ApplyActionLayout(actionText);

        if (keyText != null)
            keyText.text = DefaultKey;
    }

    static void ApplyKeyLayout(TMP_Text text)
    {
        if (text == null)
            return;

        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -16f);
        rt.sizeDelta = KeyLabelSize;
        rt.localEulerAngles = Vector3.zero;

        text.font = TMP_Settings.defaultFontAsset;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(0, 0, 0, 200);
    }

    static void ApplyActionLayout(TMP_Text text)
    {
        if (text == null)
            return;

        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 16f);
        rt.sizeDelta = ActionLabelSize;
        rt.localEulerAngles = Vector3.zero;

        text.font = TMP_Settings.defaultFontAsset;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Normal;
        text.enableWordWrapping = false;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(0, 0, 0, 200);
    }

    static TMP_Text FindOrCreateLabel(string childName, Transform parent)
    {
        var child = parent.Find(childName);
        if (child == null)
        {
            var go = new GameObject(childName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            child = go.transform;
        }

        return child.GetComponent<TextMeshProUGUI>();
    }

    public void Show(CampusQuestInteractable target)
    {
        if (target == null)
        {
            Hide();
            return;
        }

        EnsureUi();
        transform.position = target.transform.position + worldOffset;
        if (actionText != null)
            actionText.text = target.PromptText;
        if (canvas != null)
            canvas.enabled = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.enabled = false;
    }
}
