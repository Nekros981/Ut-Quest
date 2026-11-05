#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class OptionsWindowBuilder
{
    static readonly Color Gold = new Color(0.91f, 0.85f, 0.65f, 1f);
    static readonly Color TrackDark = new Color(0.18f, 0.18f, 0.2f, 1f);
    const float SliderRowHeight = 44f;
    const float TrackHeight = 14f;
    [MenuItem("UtQuest/Rebuild Options Window UI")]
    public static void Rebuild()
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/MainMixer.mixer");
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/Settings/InputSystem_Actions.inputactions");
        var templateBorder = GameObject.Find("Canvas/MainMenu_Content/Panel/Borde_Dorado");
        var templateBtn = GameObject.Find("Canvas/MainMenu_Content/Panel/Btn_Opciones");
        var optionsWindow = GameObject.Find("Canvas/OptionsWindow");

        if (templateBorder == null || templateBtn == null || optionsWindow == null)
        {
            Debug.LogError("OptionsWindowBuilder: missing scene objects.");
            return;
        }

        var oldPanel = optionsWindow.transform.Find("OptionsPanel");
        if (oldPanel != null)
            Object.DestroyImmediate(oldPanel.gameObject);

        var existingFrame = optionsWindow.transform.Find("OptionsFrame");
        if (existingFrame != null)
            Object.DestroyImmediate(existingFrame.gameObject);

        var frame = Object.Instantiate(templateBorder, optionsWindow.transform);
        frame.name = "OptionsFrame";
        var frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(900f, 520f);
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.SetAsLastSibling();

        var inner = frame.transform.Find("Fondo_Oscuro");
        if (inner == null)
        {
            Debug.LogError("OptionsWindowBuilder: Fondo_Oscuro missing on frame.");
            Object.DestroyImmediate(frame);
            return;
        }

        foreach (Transform child in inner)
            Object.DestroyImmediate(child.gameObject);

        var rootLayout = inner.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(20, 20, 20, 20);
        rootLayout.spacing = 12;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        var tabRow = new GameObject(
            "TabRow",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));
        tabRow.transform.SetParent(inner, false);
        var tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 8;
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabRow.GetComponent<LayoutElement>().preferredHeight = 50f;

        var btnControles = MakeTab(templateBtn, tabRow.transform, "Btn_Controles", "CONTROLES");
        var btnAudio = MakeTab(templateBtn, tabRow.transform, "Btn_Audio", "AUDIO");
        var btnPantalla = MakeTab(templateBtn, tabRow.transform, "Btn_Pantalla", "PANTALLA");
        var btnIluminacion = MakeTab(templateBtn, tabRow.transform, "Btn_Iluminacion", "ILUMINACION");

        var contentRoot = new GameObject("ContentRoot", typeof(RectTransform), typeof(LayoutElement));
        contentRoot.transform.SetParent(inner, false);
        contentRoot.GetComponent<LayoutElement>().minHeight = 280f;
        contentRoot.GetComponent<LayoutElement>().flexibleHeight = 1f;

        var panelControles = MakePanel(contentRoot.transform, "Panel_Controles");
        var controlsText = CreateTmp(panelControles.transform, "ControlsList", "Controles", 28);

        var panelAudio = MakePanel(contentRoot.transform, "Panel_Audio");
        var panelPantalla = MakePanel(contentRoot.transform, "Panel_Pantalla");
        var panelIluminacion = MakePanel(contentRoot.transform, "Panel_Iluminacion");

        var musicSlider = CreateLabeledSlider(panelAudio.transform, "MUSICA", 0f, 1f, 0.8f);
        var sfxSlider = CreateLabeledSlider(panelAudio.transform, "EFECTOS", 0f, 1f, 0.8f);
        var toggle = CreateFullscreenToggle(panelPantalla.transform);
        var lightingSlider = CreateLabeledSlider(panelIluminacion.transform, "INTENSIDAD", 0f, 2f, 1f);

        var btnVolver = Object.Instantiate(templateBtn, inner);
        btnVolver.name = "Btn_Volver";
        SetButtonLabel(btnVolver, "VOLVER");
        (btnVolver.GetComponent<LayoutElement>() ?? btnVolver.AddComponent<LayoutElement>()).preferredHeight = 56f;

        var optionsMenu = optionsWindow.GetComponent<OptionsMenu>() ?? optionsWindow.AddComponent<OptionsMenu>();
        var mainMenuContent = GameObject.Find("Canvas/MainMenu_Content");

        optionsMenu.GetType(); // force compile link
        var so = new SerializedObject(optionsMenu);
        SetRef(so, "windowGroup", optionsWindow.GetComponent<CanvasGroup>());
        SetRef(so, "mainMenuContentGroup", mainMenuContent != null ? mainMenuContent.GetComponent<CanvasGroup>() : null);
        SetRef(so, "panelControles", panelControles);
        SetRef(so, "panelAudio", panelAudio);
        SetRef(so, "panelPantalla", panelPantalla);
        SetRef(so, "panelIluminacion", panelIluminacion);
        SetRef(so, "controlsListText", controlsText);
        SetRef(so, "inputActions", inputActions);
        SetRef(so, "audioMixer", mixer);
        SetRef(so, "musicSlider", musicSlider);
        SetRef(so, "sfxSlider", sfxSlider);
        SetRef(so, "fullscreenToggle", toggle);
        SetRef(so, "globalLight", Object.FindFirstObjectByType<Light2D>());
        SetRef(so, "lightingSlider", lightingSlider);
        so.ApplyModifiedPropertiesWithoutUndo();

        WireButton(btnControles.GetComponent<Button>(), optionsMenu, nameof(OptionsMenu.ShowTabControles));
        WireButton(btnAudio.GetComponent<Button>(), optionsMenu, nameof(OptionsMenu.ShowTabAudio));
        WireButton(btnPantalla.GetComponent<Button>(), optionsMenu, nameof(OptionsMenu.ShowTabPantalla));
        WireButton(btnIluminacion.GetComponent<Button>(), optionsMenu, nameof(OptionsMenu.ShowTabIluminacion));
        WireButton(btnVolver.GetComponent<Button>(), optionsMenu, nameof(OptionsMenu.Close));
        WireButton(templateBtn.GetComponent<Button>(), optionsMenu, nameof(OptionsMenu.Open));

        EnsureMenuMusic(mixer);

        EditorUtility.SetDirty(optionsWindow);
        EditorSceneManager.MarkSceneDirty(optionsWindow.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Options window UI rebuilt.");
    }

    static GameObject MakeTab(GameObject templateBtn, Transform parent, string name, string label)
    {
        var tab = Object.Instantiate(templateBtn, parent);
        tab.name = name;
        (tab.GetComponent<LayoutElement>() ?? tab.AddComponent<LayoutElement>()).preferredHeight = 44f;
        SetButtonLabel(tab, label);
        return tab;
    }

    static void SetButtonLabel(GameObject button, string label)
    {
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
            tmp.text = label;
    }

    static GameObject MakePanel(Transform parent, string name)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 16;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        return panel;
    }

    static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        return tmp;
    }

    static Slider CreateLabeledSlider(Transform parent, string label, float min, float max, float defaultValue)
    {
        var row = new GameObject(label + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 16;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        row.GetComponent<LayoutElement>().minHeight = SliderRowHeight;
        row.GetComponent<LayoutElement>().preferredHeight = SliderRowHeight;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelGo.transform.SetParent(row.transform, false);
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 26;
        labelTmp.color = Color.white;
        labelGo.GetComponent<LayoutElement>().minWidth = 180f;
        labelGo.GetComponent<LayoutElement>().preferredWidth = 180f;

        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderGo.transform.SetParent(row.transform, false);
        var sliderLe = sliderGo.GetComponent<LayoutElement>();
        sliderLe.minHeight = SliderRowHeight;
        sliderLe.preferredHeight = SliderRowHeight;
        sliderLe.minWidth = 280f;
        sliderLe.flexibleWidth = 1f;

        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultValue;
        slider.direction = Slider.Direction.LeftToRight;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderGo.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        CenterHorizontalTrack(bgRect);
        bg.GetComponent<Image>().color = TrackDark;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        CenterHorizontalTrack(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(4f, 0f);
        fillAreaRect.offsetMax = new Vector2(-4f, 0f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);
        fill.GetComponent<Image>().color = Gold;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(4f, 0f);
        handleAreaRect.offsetMax = new Vector2(-4f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(10f, TrackHeight + 6f);
        handle.GetComponent<Image>().color = Gold;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();

        return slider;
    }

    static void CenterHorizontalTrack(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, TrackHeight);
        rt.anchoredPosition = Vector2.zero;
    }

    static void EnsureMenuMusic(AudioMixer mixer)
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
            return;

        var musicGo = GameObject.Find("Canvas/MenuBackgroundMusic");
        if (musicGo == null)
        {
            musicGo = new GameObject("MenuBackgroundMusic");
            musicGo.transform.SetParent(canvas.transform, false);
        }

        var source = musicGo.GetComponent<AudioSource>();
        if (source == null)
            source = musicGo.AddComponent<AudioSource>();

        source.loop = true;
        source.playOnAwake = false;

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/Musica/Sunrise_Over_High_Fields(MP).wav");
        if (clip != null)
            source.clip = clip;

        if (mixer != null)
        {
            foreach (var group in mixer.FindMatchingGroups("Music"))
            {
                if (group != null && group.name == "Music")
                {
                    source.outputAudioMixerGroup = group;
                    break;
                }
            }
        }

        var menuMusic = musicGo.GetComponent<MenuBackgroundMusic>();
        if (menuMusic == null)
            menuMusic = musicGo.AddComponent<MenuBackgroundMusic>();

        var so = new SerializedObject(menuMusic);
        so.FindProperty("menuMusicClip").objectReferenceValue = clip;
        so.FindProperty("audioMixer").objectReferenceValue = mixer;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(musicGo);
    }

    static Toggle CreateFullscreenToggle(Transform parent)
    {
        var row = new GameObject("FullscreenRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().minHeight = 40f;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(row.transform, false);
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "PANTALLA COMPLETA";
        labelTmp.fontSize = 26;
        labelTmp.color = Color.white;

        var toggleGo = new GameObject("FullscreenToggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
        toggleGo.transform.SetParent(row.transform, false);
        toggleGo.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);
        return toggleGo.GetComponent<Toggle>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetRef(SerializedObject so, string property, Object value)
    {
        var prop = so.FindProperty(property);
        if (prop == null)
        {
            Debug.LogError("OptionsWindowBuilder: missing serialized property " + property);
            return;
        }

        prop.objectReferenceValue = value;
    }

    static void WireButton(Button button, OptionsMenu target, string method)
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
