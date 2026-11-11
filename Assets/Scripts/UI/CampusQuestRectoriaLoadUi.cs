using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CampusQuestRectoriaLoadUi
{
    const string FontResourcePath = "Fonts & Materials/PixeloidSans SDF";

    public static (TextMeshProUGUI body, TextMeshProUGUI hint) CreateDialogueBox(Transform parent)
    {
        var box = new GameObject("DialogueBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.transform.SetParent(parent, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0f);
        boxRect.anchorMax = new Vector2(0.5f, 0f);
        boxRect.pivot = new Vector2(0.5f, 0f);
        boxRect.sizeDelta = new Vector2(1640f, 280f);
        boxRect.anchoredPosition = new Vector2(0f, 48f);

        box.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

        var body = CreateTmp(box.transform, "BodyText", string.Empty, 36f);
        var bodyRect = body.GetComponent<RectTransform>();
        StretchWithPadding(bodyRect, new Vector2(32f, 64f), new Vector2(-32f, -48f));
        body.alignment = TextAlignmentOptions.TopLeft;

        var hint = CreateTmp(box.transform, "HintText", "Cargando…", 28f);
        var hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.sizeDelta = new Vector2(-64f, 40f);
        hintRect.anchoredPosition = new Vector2(-32f, 16f);
        hint.alignment = TextAlignmentOptions.BottomRight;

        return (body, hint);
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void StretchWithPadding(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;

        var font = Resources.Load<TMP_FontAsset>(FontResourcePath);
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            tmp.font = font;
            if (font.material != null)
                tmp.fontSharedMaterial = font.material;
        }

        return tmp;
    }
}
