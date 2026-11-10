#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SpBosqueHatchSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bosque.unity";
    const string HatchSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/Escotilla_Bunker.png";
    const string AnimFolder = "Assets/Animations/Sp";
    const string ClipPath = AnimFolder + "/Escotilla_Bunker_Abre.anim";
    const string ControllerPath = AnimFolder + "/Escotilla_Bunker.controller";
    const string HatchObjectName = "Escotilla_Bunker_0";
    const string BunkerStoryName = "CampusQuest_BunkerStory";

    [MenuItem("Campus Quest/SP/Escotilla del bunker")]
    public static void SetupFromMenu()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureAnimationAssets();
        WireScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Campus Quest] Escotilla del bunker: clip, animator y referencias en Sp_Bosque.");
    }

    public static CampusQuestHatchDoor WireScene()
    {
        var controller = EnsureAnimationAssets();
        var hatchGo = FindHatchObject();
        if (hatchGo == null)
        {
            Debug.LogWarning("[Campus Quest] No se encontró " + HatchObjectName + " bajo SP_BunkerPlaza.");
            return null;
        }

        var sr = hatchGo.GetComponent<SpriteRenderer>() ?? hatchGo.AddComponent<SpriteRenderer>();
        var animator = hatchGo.GetComponent<Animator>();
        if (animator == null)
            animator = hatchGo.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var hatch = hatchGo.GetComponent<CampusQuestHatchDoor>();
        if (hatch == null)
            hatch = hatchGo.AddComponent<CampusQuestHatchDoor>();
        var hatchSo = new SerializedObject(hatch);
        hatchSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
        hatchSo.FindProperty("animator").objectReferenceValue = animator;
        hatchSo.ApplyModifiedPropertiesWithoutUndo();

        var story = GameObject.Find(BunkerStoryName);
        if (story != null)
        {
            var interact = story.GetComponent<CampusQuestBunkerStoryInteract>();
            if (interact == null)
                interact = story.AddComponent<CampusQuestBunkerStoryInteract>();
            var so = new SerializedObject(interact);
            so.FindProperty("hatch").objectReferenceValue = hatch;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return hatch;
    }

    static GameObject FindHatchObject()
    {
        var plaza = GameObject.Find("SP_BunkerPlaza");
        if (plaza != null)
        {
            var child = plaza.transform.Find(HatchObjectName);
            if (child != null)
                return child.gameObject;
        }

        return GameObject.Find(HatchObjectName);
    }

    static RuntimeAnimatorController EnsureAnimationAssets()
    {
        Directory.CreateDirectory(AnimFolder);
        var sprites = LoadHatchSprites();
        if (sprites.Length < 4)
            throw new System.InvalidOperationException("Escotilla_Bunker: faltan sprites _0.._3.");

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        WriteSpriteClip(clip, sprites, 5f, false);
        return EnsureController(clip, sprites[0]);
    }

    static Sprite[] LoadHatchSprites() =>
        AssetDatabase.LoadAllAssetsAtPath(HatchSheet)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith("Escotilla_Bunker_"))
            .OrderBy(s => s.name)
            .ToArray();

    static void WriteSpriteClip(AnimationClip clip, Sprite[] frames, float frameRate, bool loop)
    {
        clip.frameRate = frameRate;
        var keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(
            clip,
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
            keyframes);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    static RuntimeAnimatorController EnsureController(AnimationClip openClip, Sprite closedSprite)
    {
        const string closedClipPath = AnimFolder + "/Escotilla_Bunker_Cerrada.anim";
        var closedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(closedClipPath);
        if (closedClip == null)
        {
            closedClip = new AnimationClip();
            AssetDatabase.CreateAsset(closedClip, closedClipPath);
        }

        WriteSpriteClip(closedClip, new[] { closedSprite }, 1f, true);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null)
        {
            foreach (var child in controller.layers[0].stateMachine.states)
            {
                if (child.state.name == "Cerrada")
                    child.state.motion = closedClip;
                if (child.state.name == "Abre")
                    child.state.motion = openClip;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        EnsureParameter(controller, "Abrir", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;
        var closed = sm.AddState("Cerrada", new Vector3(250f, 0f, 0f));
        closed.motion = closedClip;
        var open = sm.AddState("Abre", new Vector3(500f, 0f, 0f));
        open.motion = openClip;
        sm.defaultState = closed;

        var transition = closed.AddTransition(open);
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Abrir");
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name)
                return;
        }

        controller.AddParameter(name, type);
    }
}
#endif
