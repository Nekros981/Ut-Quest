using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class LevelsMenuFactory
{
    const float FrameWidth = 460f;
    const float FrameHeight = 340f;
    const float ButtonHeight = 56f;

    public static LevelsMenu Ensure(
        Transform canvas,
        GameObject templateBorder,
        GameObject templateBtn,
        CanvasGroup mainMenuContent)
    {
        if (canvas == null || templateBorder == null || templateBtn == null)
            return null;

        var existing = canvas.Find("LevelsWindow");
        if (existing != null)
        {
            var menu = existing.GetComponent<LevelsMenu>();
            if (menu != null)
                return menu;
        }

        var levelsWindow = new GameObject(
            "LevelsWindow",
            typeof(RectTransform),
            typeof(CanvasGroup));
        levelsWindow.transform.SetParent(canvas, false);
        Stretch(levelsWindow.GetComponent<RectTransform>());

        var windowGroup = levelsWindow.GetComponent<CanvasGroup>();
        windowGroup.alpha = 0f;
        windowGroup.interactable = false;
        windowGroup.blocksRaycasts = false;

        var frame = Object.Instantiate(templateBorder, levelsWindow.transform);
        frame.name = "LevelsFrame";
        var frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(FrameWidth, FrameHeight);
        frameRect.anchoredPosition = Vector2.zero;

        var inner = frame.transform.Find("Fondo_Oscuro");
        if (inner == null)
        {
            Object.Destroy(levelsWindow);
            return null;
        }

        for (int i = inner.childCount - 1; i >= 0; i--)
            Object.Destroy(inner.GetChild(i).gameObject);

        var rootLayout = inner.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(40, 40, 48, 48);
        rootLayout.spacing = 18;
        rootLayout.childAlignment = TextAnchor.MiddleCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        var btnRectoria = MakeButton(templateBtn, inner, "Btn_Rectoria", "RECTORIA");
        var btnSp = MakeButton(templateBtn, inner, "Btn_SP", "SP");
        var btnIdgs = MakeButton(templateBtn, inner, "Btn_IDGS", "IDGS");

        var levelsMenu = levelsWindow.AddComponent<LevelsMenu>();
        levelsMenu.Init(windowGroup, mainMenuContent);

        Wire(btnRectoria.GetComponent<Button>(), levelsMenu.LoadRectoria);
        Wire(btnSp.GetComponent<Button>(), levelsMenu.LoadSp);
        Wire(btnIdgs.GetComponent<Button>(), levelsMenu.LoadIdgs);

        LevelsMenu.ApplyLevelButtonNavigation(
            btnRectoria.GetComponent<Button>(),
            btnSp.GetComponent<Button>(),
            btnIdgs.GetComponent<Button>());

        return levelsMenu;
    }

    static GameObject MakeButton(GameObject template, Transform parent, string name, string label)
    {
        var btn = Object.Instantiate(template, parent);
        btn.name = name;
        var le = btn.GetComponent<LayoutElement>() ?? btn.AddComponent<LayoutElement>();
        le.preferredHeight = ButtonHeight;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
            tmp.text = label;
        return btn;
    }

    static void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
