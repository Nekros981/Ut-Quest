#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SpBosqueMinimapSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bosque.unity";
    const string HudRootName = "CampusQuest_HUD";
    const string MinimapPanelName = "MinimapPanel";
    const string RenderTexturePath = "Assets/Settings/Minimap.renderTexture";
    const string CameraName = "MinimapCamera";

    [MenuItem("Campus Quest/SP/Minimapa y objetivos")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var rt = EnsureRenderTexture();
        var camGo = EnsureMinimapCamera(rt);
        var panel = EnsureMinimapPanel(rt);
        WireMinimapHud(panel, camGo);
        EnsureToleteMarker();
        EnsureBunkerApproach();
        EnsureBunkerInSight();
        EnsureBunkerStory();
        SpBosqueHatchSetup.WireScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Campus Quest] Minimapa, objetivos y ganchos del bunker en Sp_Bosque.");
    }

    static RenderTexture EnsureRenderTexture()
    {
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (rt != null)
            return rt;

        rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
        {
            name = "Minimap",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        AssetDatabase.CreateAsset(rt, RenderTexturePath);
        AssetDatabase.SaveAssets();
        return rt;
    }

    static GameObject EnsureMinimapCamera(RenderTexture rt)
    {
        var camGo = GameObject.Find(CameraName);
        if (camGo == null)
        {
            camGo = new GameObject(CameraName);
            Undo.RegisterCreatedObjectUndo(camGo, CameraName);
        }

        var cam = EnsureComponent<Camera>(camGo);
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.08f, 0.06f, 1f);
        cam.cullingMask &= ~(1 << 5);
        cam.targetTexture = rt;
        cam.depth = 5;

        var follow = EnsureComponent<CampusQuestMinimapFollow>(camGo);

        var mateo = GameObject.Find("Mateo_Provisional");
        if (mateo != null)
        {
            var so = new SerializedObject(follow);
            so.FindProperty("followTarget").objectReferenceValue = mateo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return camGo;
    }

    static GameObject EnsureMinimapPanel(RenderTexture rt)
    {
        var hud = GameObject.Find(HudRootName);
        if (hud == null)
        {
            Debug.LogWarning("[Campus Quest] Falta " + HudRootName + "; ejecuta el menú de corazones primero.");
            hud = new GameObject(HudRootName);
            Undo.RegisterCreatedObjectUndo(hud, HudRootName);
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hud.AddComponent<CanvasScaler>();
            hud.AddComponent<GraphicRaycaster>();
        }

        var panel = hud.transform.Find(MinimapPanelName)?.gameObject;
        if (panel == null)
        {
            panel = new GameObject(MinimapPanelName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panel, MinimapPanelName);
            panel.transform.SetParent(hud.transform, false);
        }

        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 1f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.pivot = new Vector2(1f, 1f);
        panelRt.anchoredPosition = new Vector2(-16f, -16f);
        panelRt.sizeDelta = new Vector2(260f, 330f);

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
        frameRt.anchoredPosition = new Vector2(0f, 0f);
        frameRt.sizeDelta = new Vector2(220f, 220f);
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
        var raw = mapView.GetComponent<RawImage>();
        raw.texture = rt;
        raw.raycastTarget = false;

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
        objRt.anchoredPosition = new Vector2(0f, -236f);
        objRt.sizeDelta = new Vector2(260f, 80f);

        var tmp = objetivos.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 20f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        tmp.color = Color.white;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        var objectives = EnsureComponent<CampusQuestObjectives>(panel);
        var objSo = new SerializedObject(objectives);
        objSo.FindProperty("label").objectReferenceValue = tmp;
        objSo.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    static void WireMinimapHud(GameObject panel, GameObject camGo)
    {
        var hud = EnsureComponent<CampusQuestMinimapHud>(panel);
        var frame = panel.transform.Find("MapFrame");
        var mapView = frame != null ? frame.Find("MapView") : null;
        var markers = frame != null ? frame.Find("Markers") : null;

        var so = new SerializedObject(hud);
        if (mapView != null)
            so.FindProperty("mapImage").objectReferenceValue = mapView.GetComponent<RawImage>();
        if (markers != null)
            so.FindProperty("markersRoot").objectReferenceValue = markers.GetComponent<RectTransform>();
        so.FindProperty("mapPixels").floatValue = 220f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureToleteMarker()
    {
        var tolete = GameObject.Find("SP_Tolete_Pickup");
        if (tolete == null)
            return;

        EnsureComponent<CampusQuestMapMarker>(tolete);
    }

    static void EnsureBunkerApproach()
    {
        const string name = "CampusQuest_BunkerApproach";
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
        }

        go.transform.position = new Vector3(16.5f, 74f, 0f);
        var box = EnsureComponent<BoxCollider2D>(go);
        box.isTrigger = true;
        box.size = new Vector2(12f, 2f);

        EnsureComponent<CampusQuestBunkerApproach>(go);
    }

    static void EnsureBunkerInSight()
    {
        const string name = "CampusQuest_BunkerInSight";
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
        }

        go.transform.position = new Vector3(18f, 110f, 0f);
        var box = EnsureComponent<BoxCollider2D>(go);
        box.isTrigger = true;
        box.size = new Vector2(12f, 3f);

        EnsureComponent<CampusQuestBunkerInSight>(go);
    }

    static void EnsureBunkerStory()
    {
        const string name = "CampusQuest_BunkerStory";
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
        }

        go.transform.position = new Vector3(18f, 125f, 0f);
        var circle = EnsureComponent<CircleCollider2D>(go);
        circle.isTrigger = true;
        circle.radius = 1.2f;

        EnsureComponent<CampusQuestBunkerStoryInteract>(go);
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null)
            return c;

        c = Undo.AddComponent<T>(go);
        return c != null ? c : go.AddComponent<T>();
    }
}
#endif
