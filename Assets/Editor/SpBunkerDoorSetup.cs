#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SpBunkerDoorSetup
{
    const string DoorSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/Open_Door.png";
    const string AnimFolder = "Assets/Animations/Sp";
    const string ClipOpenPath = AnimFolder + "/Puerta_Bunker_Abre.anim";
    const string ClipClosedPath = AnimFolder + "/Puerta_Bunker_Cerrada.anim";
    const string ControllerPath = AnimFolder + "/Puerta_Bunker.controller";

    public static RuntimeAnimatorController EnsureAnimationAssets()
    {
        Directory.CreateDirectory(AnimFolder);
        var sprites = LoadDoorSprites();
        if (sprites.Length < 9)
            throw new System.InvalidOperationException("Open_Door: faltan sprites _0.._8.");

        var openClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipOpenPath);
        if (openClip == null)
        {
            openClip = new AnimationClip();
            AssetDatabase.CreateAsset(openClip, ClipOpenPath);
        }

        WriteSpriteClip(openClip, sprites, 8f, false);
        return EnsureController(openClip, sprites[0]);
    }

    static Sprite[] LoadDoorSprites() =>
        AssetDatabase.LoadAllAssetsAtPath(DoorSheet)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith("Open_Door_"))
            .OrderBy(s => s.name, System.StringComparer.Ordinal)
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
        var closedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipClosedPath);
        if (closedClip == null)
        {
            closedClip = new AnimationClip();
            AssetDatabase.CreateAsset(closedClip, ClipClosedPath);
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
