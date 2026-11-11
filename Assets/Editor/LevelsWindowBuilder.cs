#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LevelsWindowBuilder
{
    const float FrameWidth = 460f;
    const float FrameHeight = 340f;
    const float ButtonHeight = 56f;
    const float ButtonFontSize = 28f;

    [MenuItem("Campus Quest/Build Levels Window UI")]
    public static void Rebuild()
    {
        var canvas = GameObject.Find("Canvas");
        var templateBorder = GameObject.Find("Canvas/MainMenu_Content/Panel/Borde_Dorado");
        var templateBtn = GameObject.Find("Canvas/MainMenu_Content/Panel/Btn_Opciones");
        var levelsBtn = GameObject.Find("Canvas/MainMenu_Content/Panel/Btn_Levels");
        var mainMenuContent = GameObject.Find("Canvas/MainMenu_Content");

        if (canvas == null || templateBorder == null || templateBtn == null || levelsBtn == null)
        {
            Debug.LogError("LevelsWindowBuilder: missing Canvas, border, Btn_Opciones or Btn_Levels.");
            return;
        }

        var existing = canvas.transform.Find("LevelsWindow");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var levelsWindow = new GameObject(
            "LevelsWindow",
            typeof(RectTransform),
            typeof(CanvasGroup));
        levelsWindow.transform.SetParent(canvas.transform, false);
        var windowRect = levelsWindow.GetComponent<RectTransform>();
        Stretch(windowRect);

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
            Debug.LogError("LevelsWindowBuilder: Fondo_Oscuro missing.");
            Object.DestroyImmediate(levelsWindow);
            return;
        }

        foreach (Transform child in inner)
            Object.DestroyImmediate(child.gameObject);

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

        var levelsMenu = levelsWindow.GetComponent<LevelsMenu>() ?? levelsWindow.AddComponent<LevelsMenu>();
        var so = new SerializedObject(levelsMenu);
        so.FindProperty("windowGroup").objectReferenceValue = windowGroup;
        so.FindProperty("mainMenuContentGroup").objectReferenceValue =
            mainMenuContent != null ? mainMenuContent.GetComponent<CanvasGroup>() : null;
        so.ApplyModifiedPropertiesWithoutUndo();

        WireButton(btnRectoria.GetComponent<Button>(), levelsMenu, nameof(LevelsMenu.LoadRectoria));
        WireButton(btnSp.GetComponent<Button>(), levelsMenu, nameof(LevelsMenu.LoadSp));
        WireButton(btnIdgs.GetComponent<Button>(), levelsMenu, nameof(LevelsMenu.LoadIdgs));
        WireButton(levelsBtn.GetComponent<Button>(), levelsMenu, nameof(LevelsMenu.Open));

        LevelsMenu.ApplyLevelButtonNavigation(
            btnRectoria.GetComponent<Button>(),
            btnSp.GetComponent<Button>(),
            btnIdgs.GetComponent<Button>());

        var mainMenu = Object.FindAnyObjectByType<MainMenu>();
        if (mainMenu != null)
        {
            var mainSo = new SerializedObject(mainMenu);
            var prop = mainSo.FindProperty("levelsMenu");
            if (prop != null)
            {
                prop.objectReferenceValue = levelsMenu;
                mainSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Levels window UI built.");
    }

    static GameObject MakeButton(GameObject template, Transform parent, string name, string label)
    {
        var btn = Object.Instantiate(template, parent);
        btn.name = name;
        var le = btn.GetComponent<LayoutElement>() ?? btn.AddComponent<LayoutElement>();
        le.preferredHeight = ButtonHeight;
        SetButtonLabel(btn, label);
        return btn;
    }

    static void SetButtonLabel(GameObject button, string label)
    {
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
            tmp.text = label;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void WireButton(Button button, LevelsMenu target, string method)
    {
        if (button == null || target == null)
            return;

        var buttonSo = new SerializedObject(button);
        var calls = buttonSo.FindProperty("m_OnClick")
            .FindPropertyRelative("m_PersistentCalls")
            .FindPropertyRelative("m_Calls");
        calls.arraySize = 1;
        var call0 = calls.GetArrayElementAtIndex(0);
        call0.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call0.FindPropertyRelative("m_MethodName").stringValue = method;
        call0.FindPropertyRelative("m_Mode").enumValueIndex = 1;
        call0.FindPropertyRelative("m_CallState").enumValueIndex = 2;
        buttonSo.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
