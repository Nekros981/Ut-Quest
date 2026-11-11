#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CargaSpSceneSetup
{
    const string ScenePath = "Assets/Scenes/Carga_Sp.unity";
    const string SheetPath = "Assets/Sprites/Enviroment/Sp/AnimacionCarga.png";
    const string ClipPath = "Assets/Animations/Sp/AnimacionCarga.anim";
    const string ControllerPath = "Assets/Animations/Sp/AnimacionCarga.controller";

    [MenuItem("Campus Quest/Crear escena Carga SP")]
    public static void BuildAll()
    {
        EnsureFolders();
        var controller = EnsureAnimacionCarga();
        BuildScene(controller);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Carga SP: animación, escena y Build Settings listos.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder("Assets/Animations/Sp"))
            AssetDatabase.CreateFolder("Assets/Animations", "Sp");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    static AnimatorController EnsureAnimacionCarga()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith("AnimacionCarga_", System.StringComparison.Ordinal))
            .OrderBy(s => s.name, System.StringComparer.Ordinal)
            .ToArray();

        if (sprites.Length == 0)
            throw new System.InvalidOperationException("No hay sprites AnimacionCarga_ en " + SheetPath);

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        WriteUiSpriteClip(clip, sprites, 8f, true);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        var sm = controller.layers[0].stateMachine;
        AnimatorState loadState = null;
        foreach (var child in sm.states)
        {
            if (child.state.name == "Carga")
            {
                loadState = child.state;
                break;
            }
        }

        if (loadState == null)
        {
            loadState = sm.AddState("Carga", new Vector3(300f, 0f, 0f));
            sm.defaultState = loadState;
        }

        loadState.motion = clip;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void WriteUiSpriteClip(AnimationClip clip, Sprite[] frames, float frameRate, bool loop)
    {
        clip.frameRate = frameRate;
        var keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite"),
            keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    static void BuildScene(AnimatorController controller)
    {
        Scene scene;
        if (File.Exists(ScenePath))
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        else
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<Camera>() != null)
                continue;
            Object.DestroyImmediate(root);
        }

        var canvasGo = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CampusQuestSpLoad));
        SceneManager.MoveGameObjectToScene(canvasGo, scene);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        var bgImage = bg.GetComponent<Image>();
        bgImage.color = new Color32(24, 26, 34, 255);

        var animGo = new GameObject("AnimacionCarga", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Animator));
        animGo.transform.SetParent(canvasGo.transform, false);
        var animRect = animGo.GetComponent<RectTransform>();
        animRect.anchorMin = new Vector2(0.5f, 0.5f);
        animRect.anchorMax = new Vector2(0.5f, 0.5f);
        animRect.pivot = new Vector2(0.5f, 0.5f);
        animRect.sizeDelta = new Vector2(380f, 460f);
        animRect.anchoredPosition = Vector2.zero;

        var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath)
            .OfType<Sprite>()
            .Where(s => s.name == "AnimacionCarga_0")
            .ToArray();
        var image = animGo.GetComponent<Image>();
        image.sprite = sprites.Length > 0 ? sprites[0] : null;
        image.preserveAspect = true;
        image.color = Color.white;

        var animator = animGo.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
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
        var bosqueIndex = -1;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == "Assets/Scenes/Sp_Bosque.unity")
            {
                bosqueIndex = i;
                break;
            }
        }

        var existingIndex = -1;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == ScenePath)
            {
                existingIndex = i;
                break;
            }
        }

        var entry = new EditorBuildSettingsScene(ScenePath, true);
        if (existingIndex >= 0)
            scenes[existingIndex] = entry;
        else if (bosqueIndex >= 0)
            scenes.Insert(bosqueIndex, entry);
        else
            scenes.Add(entry);

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
