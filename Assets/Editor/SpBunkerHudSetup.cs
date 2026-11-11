#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class SpBunkerHudSetup
{
    const string HudRootName = "CampusQuest_HUD";
    const string HeartsName = "Corazones";
    const string DefeatLabelName = "Derrotas";
    const string MinimapPanelName = "MinimapPanel";
    const string RenderTexturePath = "Assets/Settings/Minimap.renderTexture";
    const string UiBarsSheet =
        "Assets/Sprites/Enviroment/Sp/Top-Down RPG 32x32 by Mixel v1.7/User Interface v1.0/Topdown RPG 32x32 - UI Bars.PNG";
    const string HeartFullName = "Topdown RPG 32x32 - UI Bars_448";
    const string HeartEmptyName = "Topdown RPG 32x32 - UI Bars_396";
    const string PixeloidFontPath = "Assets/Fonts/PixeloidSans SDF.asset";
    const string CameraName = "MinimapCamera";

    public static void EnsurePlayHud(Transform mateo)
    {
        EnsureEventSystem();
        var hudRoot = EnsureHudCanvas();
        EnsureHearts(hudRoot);
        var rt = EnsureRenderTexture();
        var camGo = EnsureMinimapCamera(rt, mateo);
        var panel = EnsureMinimapPanel(rt, hudRoot.transform);
        WireMinimapHud(panel, camGo);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(es, "EventSystem");
    }

    static GameObject EnsureHudCanvas()
    {
        var hudRoot = GameObject.Find(HudRootName);
        if (hudRoot == null)
        {
            hudRoot = new GameObject(
                HudRootName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(hudRoot, HudRootName);
        }

        var canvas = RequireComponent<Canvas>(hudRoot);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = RequireComponent<CanvasScaler>(hudRoot);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RequireComponent<GraphicRaycaster>(hudRoot);
        return hudRoot;
    }

    static T RequireComponent<T>(GameObject go) where T : Component
    {
        if (go.TryGetComponent(out T existing) && existing != null)
            return existing;

        Undo.AddComponent<T>(go);
        if (go.TryGetComponent(out existing) && existing != null)
            return existing;

        return go.AddComponent<T>();
    }

    static void EnsureHearts(GameObject hudRoot)
    {
        var heartsRoot = hudRoot.transform.Find(HeartsName)?.gameObject;
        if (heartsRoot == null)
        {
            heartsRoot = new GameObject(HeartsName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(heartsRoot, HeartsName);
            heartsRoot.transform.SetParent(hudRoot.transform, false);
        }

        var heartsRect = heartsRoot.GetComponent<RectTransform>();
        heartsRect.anchorMin = new Vector2(0f, 1f);
        heartsRect.anchorMax = new Vector2(0f, 1f);
        heartsRect.pivot = new Vector2(0f, 1f);
        heartsRect.anchoredPosition = new Vector2(16f, -16f);
        heartsRect.sizeDelta = new Vector2(420f, 56f);

        var layout = heartsRoot.GetComponent<HorizontalLayoutGroup>() ??
                     Undo.AddComponent<HorizontalLayoutGroup>(heartsRoot);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        var heartFull = LoadSprite(HeartFullName);
        var heartEmpty = LoadSprite(HeartEmptyName);
        if (heartFull == null || heartEmpty == null)
        {
            Debug.LogWarning("[Campus Quest] Sp_Bunker: faltan sprites de corazones.");
            return;
        }

        const int heartCount = 6;
        var images = new Image[heartCount];
        for (int i = 0; i < heartCount; i++)
        {
            var childName = "Corazon_" + (i + 1);
            var child = heartsRoot.transform.Find(childName)?.gameObject;
            if (child == null)
            {
                child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(child, childName);
                child.transform.SetParent(heartsRoot.transform, false);
            }

            var image = child.GetComponent<Image>();
            image.sprite = heartFull;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var rt = child.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48f, 48f);

            var layoutElement = RequireComponent<LayoutElement>(child);
            layoutElement.preferredWidth = 48f;
            layoutElement.preferredHeight = 48f;
            images[i] = image;
        }

        var defeatLabel = EnsureDefeatLabel(heartsRoot.transform);
        var counter = RequireComponent<CampusQuestDefeatCounter>(heartsRoot);
        var counterSo = new SerializedObject(counter);
        counterSo.FindProperty("label").objectReferenceValue = defeatLabel;
        counterSo.ApplyModifiedPropertiesWithoutUndo();

        var bar = RequireComponent<CampusQuestHeartBar>(heartsRoot);
        var barSo = new SerializedObject(bar);
        barSo.FindProperty("maxHearts").intValue = heartCount;
        barSo.FindProperty("heartFull").objectReferenceValue = heartFull;
        barSo.FindProperty("heartEmpty").objectReferenceValue = heartEmpty;
        barSo.FindProperty("heartImages").arraySize = heartCount;
        for (int i = 0; i < heartCount; i++)
            barSo.FindProperty("heartImages").GetArrayElementAtIndex(i).objectReferenceValue = images[i];
        barSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static RenderTexture EnsureRenderTexture()
    {
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (rt != null)
            return rt;

        rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
        {
            name = "Minimap",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        AssetDatabase.CreateAsset(rt, RenderTexturePath);
        AssetDatabase.SaveAssets();
        return rt;
    }

    static GameObject EnsureMinimapCamera(RenderTexture rt, Transform mateo)
    {
        var camGo = GameObject.Find(CameraName);
        if (camGo == null)
        {
            camGo = new GameObject(CameraName, typeof(Camera));
            Undo.RegisterCreatedObjectUndo(camGo, CameraName);
        }

        var cam = RequireComponent<Camera>(camGo);
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.08f, 0.06f, 1f);
        cam.cullingMask &= ~(1 << 5);
        cam.targetTexture = rt;
        cam.depth = 5;

        var follow = RequireComponent<CampusQuestMinimapFollow>(camGo);
        if (mateo != null)
        {
            var so = new SerializedObject(follow);
            so.FindProperty("followTarget").objectReferenceValue = mateo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return camGo;
    }

    static GameObject EnsureMinimapPanel(RenderTexture rt, Transform hud)
    {
        var panel = hud.Find(MinimapPanelName)?.gameObject;
        if (panel == null)
        {
            panel = new GameObject(MinimapPanelName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panel, MinimapPanelName);
            panel.transform.SetParent(hud, false);
        }

        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 1f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.pivot = new Vector2(1f, 1f);
        panelRt.anchoredPosition = new Vector2(-16f, -16f);
        panelRt.sizeDelta = new Vector2(370f, 480f);

        var frame = panel.transform.Find("MapFrame")?.gameObject;
        if (frame == null)
        {
            frame = new GameObject("MapFrame", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(frame, "MapFrame");
            frame.transform.SetParent(panel.transform, false);
        }

        var frameRt = frame.GetComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.5f, 1f);
        frameRt.anchorMax = new Vector2(0.5f, 1f);
        frameRt.pivot = new Vector2(0.5f, 1f);
        frameRt.anchoredPosition = Vector2.zero;
        frameRt.sizeDelta = new Vector2(330f, 330f);
        var frameImg = frame.GetComponent<Image>();
        frameImg.color = new Color32(20, 24, 28, 220);
        frameImg.raycastTarget = false;

        var mapView = frame.transform.Find("MapView")?.gameObject;
        if (mapView == null)
        {
            mapView = new GameObject("MapView", typeof(RectTransform), typeof(RawImage));
            Undo.RegisterCreatedObjectUndo(mapView, "MapView");
            mapView.transform.SetParent(frame.transform, false);
        }

        var mapRt = mapView.GetComponent<RectTransform>();
        mapRt.anchorMin = Vector2.zero;
        mapRt.anchorMax = Vector2.one;
        mapRt.offsetMin = new Vector2(4f, 4f);
        mapRt.offsetMax = new Vector2(-4f, -4f);
        mapView.GetComponent<RawImage>().texture = rt;

        var markers = frame.transform.Find("Markers")?.gameObject;
        if (markers == null)
        {
            markers = new GameObject("Markers", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(markers, "Markers");
            markers.transform.SetParent(frame.transform, false);
        }

        var markersRt = markers.GetComponent<RectTransform>();
        markersRt.anchorMin = Vector2.zero;
        markersRt.anchorMax = Vector2.one;
        markersRt.offsetMin = mapRt.offsetMin;
        markersRt.offsetMax = mapRt.offsetMax;

        var objetivos = panel.transform.Find("Objetivos")?.gameObject;
        if (objetivos == null)
        {
            objetivos = new GameObject("Objetivos", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(objetivos, "Objetivos");
            objetivos.transform.SetParent(panel.transform, false);
        }

        var objRt = objetivos.GetComponent<RectTransform>();
        objRt.anchorMin = new Vector2(0.5f, 1f);
        objRt.anchorMax = new Vector2(0.5f, 1f);
        objRt.pivot = new Vector2(0.5f, 1f);
        objRt.anchoredPosition = new Vector2(0f, -346f);
        objRt.sizeDelta = new Vector2(370f, 110f);

        var tmp = objetivos.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 30f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        tmp.color = Color.white;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        var objectives = RequireComponent<CampusQuestObjectives>(panel);
        var objSo = new SerializedObject(objectives);
        objSo.FindProperty("label").objectReferenceValue = tmp;
        objSo.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    static void WireMinimapHud(GameObject panel, GameObject camGo)
    {
        var hud = RequireComponent<CampusQuestMinimapHud>(panel);
        var frame = panel.transform.Find("MapFrame");
        var mapView = frame != null ? frame.Find("MapView") : null;
        var markers = frame != null ? frame.Find("Markers") : null;

        var so = new SerializedObject(hud);
        if (mapView != null)
            so.FindProperty("mapImage").objectReferenceValue = mapView.GetComponent<RawImage>();
        if (markers != null)
            so.FindProperty("markersRoot").objectReferenceValue = markers.GetComponent<RectTransform>();
        so.FindProperty("mapPixels").floatValue = 330f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static TMP_Text EnsureDefeatLabel(Transform parent)
    {
        var child = parent.Find(DefeatLabelName)?.gameObject;
        if (child == null)
        {
            child = new GameObject(DefeatLabelName, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(child, DefeatLabelName);
            child.transform.SetParent(parent, false);
        }

        var layout = RequireComponent<LayoutElement>(child);
        layout.preferredWidth = 90f;
        layout.preferredHeight = 48f;

        var tmp = child.GetComponent<TextMeshProUGUI>();
        tmp.text = "x00";
        tmp.fontSize = 32f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.color = Color.white;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixeloidFontPath);
        if (font != null)
            tmp.font = font;

        return tmp;
    }

    static Sprite LoadSprite(string spriteName) =>
        AssetDatabase.LoadAllAssetsAtPath(UiBarsSheet).OfType<Sprite>()
            .FirstOrDefault(s => s.name == spriteName);
}
#endif
