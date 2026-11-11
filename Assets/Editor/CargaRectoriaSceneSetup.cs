#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CargaRectoriaSceneSetup
{
    const string ScenePath = "Assets/Scenes/Carga_Rectoria.unity";
    const string BackgroundPath = "Assets/Sprites/Enviroment/Rectoria/Rectoria.jpeg";
    const string FontPath = "Assets/Fonts/PixeloidSans SDF.asset";
    const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

    [MenuItem("Campus Quest/Crear escena Carga Rectoria")]
    public static void BuildScene()
    {
        Scene scene;
        if (File.Exists(ScenePath))
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        else
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        ClearExceptCamera();
        EnsureEventSystem();
        var canvas = EnsureCanvas(scene);
        var loader = canvas.GetComponent<CampusQuestRectoriaLoad>()
            ?? canvas.gameObject.AddComponent<CampusQuestRectoriaLoad>();

        var oldBg = canvas.transform.Find("Background_Rectoria");
        if (oldBg != null)
            Object.DestroyImmediate(oldBg.gameObject);
        var oldBox = canvas.transform.Find("DialogueBox");
        if (oldBox != null)
            Object.DestroyImmediate(oldBox.gameObject);

        var bg = CreateBackground(canvas.transform);
        bg.transform.SetAsFirstSibling();

        var box = CreateDialogueBox(canvas.transform);
        var body = box.Find("BodyText").GetComponent<TextMeshProUGUI>();
        var hint = box.Find("HintText").GetComponent<TextMeshProUGUI>();

        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        var so = new SerializedObject(loader);
        so.FindProperty("inputActions").objectReferenceValue = inputActions;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("hintText").objectReferenceValue = hint;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureBuildSettings();
        Debug.Log("Carga Rectoria: escena guardada en " + ScenePath);
    }

    static void ClearExceptCamera()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.GetComponent<Camera>() != null)
                continue;
            Object.DestroyImmediate(root);
        }
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var module = es.GetComponent<InputSystemUIInputModule>();
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (inputActions != null)
        {
            module.actionsAsset = inputActions;
            module.AssignDefaultActions();
        }
    }

    static GameObject EnsureCanvas(Scene scene)
    {
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>() ?? canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasGo.GetComponent<GraphicRaycaster>() == null)
            canvasGo.AddComponent<GraphicRaycaster>();

        return canvasGo;
    }

    static GameObject CreateBackground(Transform parent)
    {
        var go = new GameObject("Background_Rectoria", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        Stretch(rect);

        var image = go.GetComponent<Image>();
        image.sprite = LoadBackgroundSprite();
        image.preserveAspect = false;
        image.color = Color.white;

        return go;
    }

    static Sprite LoadBackgroundSprite()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(BackgroundPath);
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }

    static Transform CreateDialogueBox(Transform parent)
    {
        var box = new GameObject("DialogueBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.transform.SetParent(parent, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0f);
        boxRect.anchorMax = new Vector2(0.5f, 0f);
        boxRect.pivot = new Vector2(0.5f, 0f);
        boxRect.sizeDelta = new Vector2(1640f, 280f);
        boxRect.anchoredPosition = new Vector2(0f, 48f);

        var boxImage = box.GetComponent<Image>();
        boxImage.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

        var body = CreateTmp(box.transform, "BodyText", string.Empty, 36f);
        var bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(32f, 64f);
        bodyRect.offsetMax = new Vector2(-32f, -48f);
        body.alignment = TextAlignmentOptions.TopLeft;

        var hint = CreateTmp(box.transform, "HintText", "Cargando…", 28f);
        var hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.sizeDelta = new Vector2(-64f, 40f);
        hintRect.anchoredPosition = new Vector2(-32f, 16f);
        hint.alignment = TextAlignmentOptions.BottomRight;

        return box.transform;
    }

    static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null)
        {
            tmp.font = font;
            if (font.material != null)
                tmp.fontSharedMaterial = font.material;
        }

        return tmp;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void EnsureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var hasCarga = false;
        foreach (var entry in scenes)
        {
            if (entry.path == ScenePath)
            {
                hasCarga = true;
                break;
            }
        }

        if (!hasCarga)
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
