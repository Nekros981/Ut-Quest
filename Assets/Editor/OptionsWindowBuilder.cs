#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class OptionsWindowBuilder
{
    static readonly Color Gold = new Color(0.91f, 0.85f, 0.65f, 1f);
    static readonly Color TrackDark = new Color(0.18f, 0.18f, 0.2f, 1f);
    const float RowHeight = 56f;
    const float TrackHeight = 28f;
    const float BarWidth = 380f;
    const float LabelWidth = 240f;
    const float LabelFontSize = 32f;
    const float TabFontSize = 28f;
    const float VolverFontSize = 32f;
    const float TabHeight = 56f;
    const float TipSize = 28f;
    const float HandleWidth = 14f;
    const float ToggleWidth = 72f;
    const float ToggleHeight = 36f;
    const string OptionsFontPath = "Assets/Fonts/PixeloidSans.ttf";
    const string OptionsFontAssetPath = "Assets/Fonts/PixeloidSans SDF.asset";
    const string SliderTipPath = "Assets/UI/SliderTip.png";
    const string SliderWhitePath = "Assets/UI/SliderWhite.png";
    const string FallbackFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string OptionsGlyphs =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 :/|.,-+()[]";

    static TMP_FontAsset optionsFont;
    static Sprite sliderTipSprite;
    static Sprite sliderWhiteSprite;

    [MenuItem("UtQuest/Rebuild Options Window UI")]
    public static void Rebuild()
    {
        optionsFont = EnsureOptionsFontAsset();
        sliderTipSprite = EnsureSliderTipSprite();
        sliderWhiteSprite = EnsureSliderWhiteSprite();
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
        tabRow.GetComponent<LayoutElement>().preferredHeight = TabHeight;

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
        SetButtonLabel(btnVolver, "VOLVER", VolverFontSize);
        (btnVolver.GetComponent<LayoutElement>() ?? btnVolver.AddComponent<LayoutElement>()).preferredHeight = TabHeight;

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

        OptionsMenu.ApplyTabRowNavigation(
            btnControles.GetComponent<Button>(),
            btnAudio.GetComponent<Button>(),
            btnPantalla.GetComponent<Button>(),
            btnIluminacion.GetComponent<Button>());

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
        (tab.GetComponent<LayoutElement>() ?? tab.AddComponent<LayoutElement>()).preferredHeight = TabHeight;
        SetButtonLabel(tab, label, TabFontSize);
        return tab;
    }

    static void SetButtonLabel(GameObject button, string label, float fontSize)
    {
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
            return;

        tmp.text = label;
        ApplyOptionsFont(tmp, fontSize);
    }

    static void ApplyOptionsFont(TextMeshProUGUI tmp, float fontSize)
    {
        if (optionsFont != null)
        {
            tmp.font = optionsFont;
            if (optionsFont.material != null)
                tmp.fontSharedMaterial = optionsFont.material;
        }

        tmp.fontSize = fontSize;
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
        vlg.childForceExpandHeight = false;
        return panel;
    }

    static void SetFixedRowHeight(LayoutElement layoutElement)
    {
        layoutElement.minHeight = RowHeight;
        layoutElement.preferredHeight = RowHeight;
        layoutElement.flexibleHeight = 0f;
    }

    static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        ApplyOptionsFont(tmp, fontSize);
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        return tmp;
    }

    static Slider CreateLabeledSlider(Transform parent, string label, float min, float max, float defaultValue)
    {
        var row = new GameObject(label + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        SetFixedRowHeight(row.GetComponent<LayoutElement>());

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelGo.transform.SetParent(row.transform, false);
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        ApplyOptionsFont(labelTmp, LabelFontSize);
        labelTmp.color = Color.white;
        var labelLe = labelGo.GetComponent<LayoutElement>();
        labelLe.minWidth = LabelWidth;
        labelLe.preferredWidth = LabelWidth;

        var barGo = new GameObject("Bar", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        barGo.transform.SetParent(row.transform, false);
        var barLe = barGo.GetComponent<LayoutElement>();
        barLe.minWidth = BarWidth;
        barLe.preferredWidth = BarWidth;
        barLe.minHeight = RowHeight;
        barLe.preferredHeight = RowHeight;
        barLe.flexibleWidth = 0f;
        var barLayout = barGo.GetComponent<HorizontalLayoutGroup>();
        barLayout.spacing = 4;
        barLayout.childAlignment = TextAnchor.MiddleCenter;
        barLayout.childControlWidth = true;
        barLayout.childControlHeight = true;
        barLayout.childForceExpandWidth = false;

        CreateBarTip(barGo.transform, "TipLeft", flipX: false);
        var slider = CreateCompactSlider(barGo.transform, min, max, defaultValue);
        CreateBarTip(barGo.transform, "TipRight", flipX: true);

        return slider;
    }

    static void CreateBarTip(Transform parent, string name, bool flipX)
    {
        var tipGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        tipGo.transform.SetParent(parent, false);
        var tipLe = tipGo.GetComponent<LayoutElement>();
        tipLe.minWidth = TipSize;
        tipLe.preferredWidth = TipSize;
        tipLe.minHeight = TipSize;
        tipLe.preferredHeight = TipSize;
        tipLe.flexibleWidth = 0f;

        var image = tipGo.GetComponent<Image>();
        image.sprite = sliderTipSprite;
        image.color = Gold;
        image.raycastTarget = false;
        image.preserveAspect = true;

        if (flipX)
            tipGo.transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    static Slider CreateCompactSlider(Transform parent, float min, float max, float defaultValue)
    {
        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderGo.transform.SetParent(parent, false);
        var sliderLe = sliderGo.GetComponent<LayoutElement>();
        sliderLe.minHeight = TrackHeight;
        sliderLe.preferredHeight = TrackHeight;
        sliderLe.flexibleHeight = 0f;
        sliderLe.flexibleWidth = 1f;
        sliderLe.minWidth = 120f;

        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultValue;
        slider.direction = Slider.Direction.LeftToRight;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderGo.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        CenterHorizontalTrack(bgRect);
        var bgImage = bg.GetComponent<Image>();
        StyleSliderImage(bgImage, TrackDark, raycast: true);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        CenterHorizontalTrack(fillAreaRect);
        ApplyTrackAreaInsets(fillAreaRect);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);
        StyleSliderImage(fill.GetComponent<Image>(), Gold, raycast: false);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        CenterHorizontalTrack(handleAreaRect);
        ApplyTrackAreaInsets(handleAreaRect);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(HandleWidth, 0f);
        var handleImage = handle.GetComponent<Image>();
        StyleSliderImage(handleImage, Gold, raycast: true);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    static void ApplyTrackAreaInsets(RectTransform rt)
    {
        var half = TrackHeight * 0.5f;
        rt.offsetMin = new Vector2(2f, -half);
        rt.offsetMax = new Vector2(-2f, half);
    }

    static void StyleSliderImage(Image image, Color color, bool raycast)
    {
        if (sliderWhiteSprite != null)
            image.sprite = sliderWhiteSprite;

        image.color = color;
        image.raycastTarget = raycast;
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
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        SetFixedRowHeight(row.GetComponent<LayoutElement>());

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelGo.transform.SetParent(row.transform, false);
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "PANTALLA COMPLETA";
        ApplyOptionsFont(labelTmp, LabelFontSize);
        labelTmp.color = Color.white;
        var labelLe = labelGo.GetComponent<LayoutElement>();
        labelLe.minWidth = LabelWidth;
        labelLe.preferredWidth = LabelWidth;

        var toggleGo = new GameObject("FullscreenToggle", typeof(RectTransform), typeof(Toggle), typeof(Image), typeof(LayoutElement));
        toggleGo.transform.SetParent(row.transform, false);
        var toggleLe = toggleGo.GetComponent<LayoutElement>();
        toggleLe.minWidth = ToggleWidth;
        toggleLe.preferredWidth = ToggleWidth;
        toggleLe.minHeight = ToggleHeight;
        toggleLe.preferredHeight = ToggleHeight;
        toggleLe.flexibleWidth = 0f;
        toggleLe.flexibleHeight = 0f;

        var toggleRt = toggleGo.GetComponent<RectTransform>();
        toggleRt.sizeDelta = new Vector2(ToggleWidth, ToggleHeight);

        var bgImage = toggleGo.GetComponent<Image>();
        bgImage.color = TrackDark;

        var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkGo.transform.SetParent(toggleGo.transform, false);
        var checkRect = checkGo.GetComponent<RectTransform>();
        Stretch(checkRect);
        checkRect.offsetMin = new Vector2(4f, 4f);
        checkRect.offsetMax = new Vector2(-4f, -4f);
        checkGo.GetComponent<Image>().color = Gold;

        var toggle = toggleGo.GetComponent<Toggle>();
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkGo.GetComponent<Image>();
        toggle.isOn = Screen.fullScreen;

        return toggle;
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

    static TMP_FontAsset EnsureOptionsFontAsset()
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OptionsFontAssetPath);
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(OptionsFontPath);
        if (fontAsset == null || sourceFont == null)
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontAssetPath);

        if (fontAsset.material != null && fontAsset.atlasTexture != null && fontAsset.HasCharacters(OptionsGlyphs))
            return fontAsset;

        if (!RepairOptionsFont(fontAsset, sourceFont))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontAssetPath);

        return fontAsset;
    }

    static bool RepairOptionsFont(TMP_FontAsset target, Font sourceFont)
    {
        var generated = TMP_FontAsset.CreateFontAsset(
            sourceFont, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        if (generated == null)
            return false;

        generated.TryAddCharacters(OptionsGlyphs, out var missing);
        if (!string.IsNullOrEmpty(missing))
            Debug.LogWarning("OptionsWindowBuilder: PixeloidSans sin estos caracteres: " + missing);

        EditorUtility.CopySerialized(generated, target);
        target.name = "PixeloidSans SDF";

        var atlas = Object.Instantiate(generated.atlasTexture);
        atlas.name = "PixeloidSans Atlas";
        var material = new Material(generated.material);
        material.name = "PixeloidSans Material";
        material.SetTexture(ShaderUtilities.ID_MainTex, atlas);

        var assetPath = AssetDatabase.GetAssetPath(target);
        foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
            AssetDatabase.RemoveObjectFromAsset(subAsset);

        AssetDatabase.AddObjectToAsset(atlas, target);
        AssetDatabase.AddObjectToAsset(material, target);

        var serialized = new SerializedObject(target);
        var textures = serialized.FindProperty("m_AtlasTextures");
        textures.arraySize = 1;
        textures.GetArrayElementAtIndex(0).objectReferenceValue = atlas;
        serialized.FindProperty("m_Material").objectReferenceValue = material;
        serialized.FindProperty("m_AtlasPopulationMode").enumValueIndex = (int)AtlasPopulationMode.Static;
        serialized.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Object.DestroyImmediate(generated);
        target.ReadFontAssetDefinition();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        return target.HasCharacters(OptionsGlyphs);
    }

    static Sprite EnsureSliderTipSprite() => EnsureUiSprite(SliderTipPath);

    static Sprite EnsureSliderWhiteSprite() => EnsureUiSprite(SliderWhitePath);

    static Sprite EnsureUiSprite(string assetPath)
    {
        if (!File.Exists(assetPath))
            return null;

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        else
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }
}
#endif
