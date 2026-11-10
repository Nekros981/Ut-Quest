#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameOverSceneSetup
{
    const string GameOverScenePath = "Assets/Scenes/GameOver.unity";
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    const string MateoController = "Assets/Animations/Player/Mateo/Mateo.controller";
    const string MateoIdleSprite = "Assets/Sprites/Characters/Mateo/Frames/caminataA1.png";
    const string TitleFontPath = "Assets/Fonts/PixeloidSans SDF.asset";

    [MenuItem("Campus Quest/SP/Crear escena Game Over")]
    public static void BuildGameOverScene()
    {
        Scene gameOverScene;
        if (File.Exists(GameOverScenePath))
            gameOverScene = EditorSceneManager.OpenScene(GameOverScenePath, OpenSceneMode.Single);
        else
            gameOverScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        ClearSceneExceptCamera();

        var sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
        EditorSceneManager.SetActiveScene(gameOverScene);

        var templatePanel = GameObject.Find("Canvas/MainMenu_Content/Panel");
        var templateBtn = GameObject.Find("Canvas/MainMenu_Content/Panel/Btn_Iniciar");
        if (templatePanel == null || templateBtn == null)
        {
            Debug.LogError("GameOverSceneSetup: no se encontró Panel o Btn_Iniciar en SampleScene.");
            EditorSceneManager.CloseScene(sampleScene, true);
            return;
        }

        EnsureCamera();
        EnsureMateo(gameOverScene);
        var canvas = EnsureCanvas(gameOverScene);
        var gameOver = canvas.GetComponent<CampusQuestGameOver>() ?? canvas.gameObject.AddComponent<CampusQuestGameOver>();

        var oldPanel = canvas.transform.Find("Panel");
        if (oldPanel != null)
            Object.DestroyImmediate(oldPanel.gameObject);
        var oldTitle = canvas.transform.Find("Title_GameOver");
        if (oldTitle != null)
            Object.DestroyImmediate(oldTitle.gameObject);

        var title = CreateTitle(canvas.transform);
        var panel = Object.Instantiate(templatePanel, canvas.transform);
        panel.name = "Panel";
        SceneManager.MoveGameObjectToScene(panel, gameOverScene);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        ApplyGameOverPanelLayout(panel);

        DestroyChild(panel.transform, "Btn_Opciones");
        DestroyChild(panel.transform, "Btn_Levels");

        var btnReiniciar = panel.transform.Find("Btn_Iniciar");
        if (btnReiniciar != null)
        {
            btnReiniciar.name = "Btn_Reiniciar";
            SetButtonLabel(btnReiniciar.gameObject, "REINICIAR");
            WireButton(btnReiniciar.GetComponent<Button>(), gameOver, nameof(CampusQuestGameOver.Restart));
        }

        var btnSalir = Object.Instantiate(templateBtn, panel.transform);
        btnSalir.name = "Btn_Salir";
        SetButtonLabel(btnSalir, "SALIR");
        WireButton(btnSalir.GetComponent<Button>(), gameOver, nameof(CampusQuestGameOver.Quit));

        ApplyGameOverButtonHeights(panel.transform.Find("Btn_Reiniciar"), panel.transform.Find("Btn_Salir"));

        EditorSceneManager.CloseScene(sampleScene, true);

        EditorSceneManager.MarkSceneDirty(gameOverScene);
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(gameOverScene, GameOverScenePath);

        EnsureBuildSettings();
        Debug.Log("Game Over: escena guardada en " + GameOverScenePath);
    }

    static void ClearSceneExceptCamera()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.GetComponent<Camera>() != null)
                continue;
            Object.DestroyImmediate(root);
        }
    }

    static void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    static void EnsureMateo(Scene scene)
    {
        const string name = "GameOver_Mateo";
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                Object.DestroyImmediate(root);
        }

        var mateoRoot = new GameObject(name);
        SceneManager.MoveGameObjectToScene(mateoRoot, scene);
        mateoRoot.transform.position = new Vector3(0f, -3.6f, 0f);

        var visualGo = new GameObject("Mateo_Visual");
        visualGo.transform.SetParent(mateoRoot.transform, false);

        var renderer = visualGo.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 50;
        var idle = AssetDatabase.LoadAssetAtPath<Sprite>(MateoIdleSprite);
        if (idle != null)
            renderer.sprite = idle;

        var animator = visualGo.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MateoController);

        var driver = visualGo.AddComponent<CampusQuestMateoVisualDriver>();
        driver.motionRoot = mateoRoot.transform;
        visualGo.AddComponent<CampusQuestGameOverMateo>();
    }

    static GameObject EnsureCanvas(Scene scene)
    {
        GameObject canvasGo = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Canvas")
            {
                canvasGo = root;
                break;
            }
        }

        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
        }

        var hasEventSystem = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
            {
                hasEventSystem = true;
                break;
            }
        }

        if (!hasEventSystem)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<UnityEngine.EventSystems.EventSystem>();
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            SceneManager.MoveGameObjectToScene(events, scene);
        }

        return canvasGo;
    }

    static GameObject CreateTitle(Transform parent)
    {
        var go = new GameObject("Title_GameOver", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 200f);
        rect.anchoredPosition = new Vector2(0f, 220f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "GAME OVER";
        tmp.fontSize = 72f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
        if (font != null)
        {
            tmp.font = font;
            if (font.material != null)
                tmp.fontSharedMaterial = font.material;
        }

        return go;
    }

    static void ApplyGameOverPanelLayout(GameObject panel)
    {
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(360f, 210f);
        panelRect.anchoredPosition = new Vector2(0f, -20f);

        var layout = panel.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = new RectOffset(28, 28, 28, 28);
            layout.spacing = 14;
        }
    }

    static void ApplyGameOverButtonHeights(Transform btnReiniciar, Transform btnSalir)
    {
        const float buttonHeight = 56f;
        const float buttonWidth = 304f;

        void Style(Transform btn)
        {
            if (btn == null)
                return;

            var element = btn.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = buttonHeight;
            element.preferredWidth = buttonWidth;
        }

        Style(btnReiniciar);
        Style(btnSalir);
    }

    static void DestroyChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null)
            Object.DestroyImmediate(child.gameObject);
    }

    static void SetButtonLabel(GameObject button, string label)
    {
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
            tmp.text = label;
    }

    static void WireButton(Button button, CampusQuestGameOver target, string method)
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

    static void EnsureBuildSettings()
    {
        var scenes = new[]
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/Sp_Bosque.unity",
            GameOverScenePath,
        };

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var path in scenes)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
            {
                Debug.LogWarning("GameOverSceneSetup: falta escena en disco: " + path);
                continue;
            }

            list.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
