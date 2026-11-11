#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SpBunkerDoorSetup
{
    const string DoorSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/Open_Door.png";
    const string CloseDoorSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/Close_Door.png";
    const string AnimFolder = "Assets/Animations/Sp";
    const string ClipOpenPath = AnimFolder + "/Puerta_Bunker_Abre.anim";
    const string ClipClosePath = AnimFolder + "/Puerta_Bunker_Cierra.anim";
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

        var closeSprites = LoadCloseDoorSprites();
        if (closeSprites.Length < 9)
            throw new System.InvalidOperationException("Close_Door: faltan sprites _0.._8.");

        var closeClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipClosePath);
        if (closeClip == null)
        {
            closeClip = new AnimationClip();
            AssetDatabase.CreateAsset(closeClip, ClipClosePath);
        }

        WriteSpriteClip(closeClip, closeSprites, 8f, false);
        AssetDatabase.SaveAssets();
        return EnsureController(openClip, closeClip, sprites[0]);
    }

    static Sprite[] LoadDoorSprites() =>
        AssetDatabase.LoadAllAssetsAtPath(DoorSheet)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith("Open_Door_"))
            .OrderBy(s => s.name, System.StringComparer.Ordinal)
            .ToArray();

    static Sprite[] LoadCloseDoorSprites() =>
        AssetDatabase.LoadAllAssetsAtPath(CloseDoorSheet)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith("Close_Door_"))
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

    static RuntimeAnimatorController EnsureController(AnimationClip openClip, AnimationClip closeClip, Sprite closedSprite)
    {
        var closedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipClosedPath);
        if (closedClip == null)
        {
            closedClip = new AnimationClip();
            AssetDatabase.CreateAsset(closedClip, ClipClosedPath);
        }

        WriteSpriteClip(closedClip, new[] { closedSprite }, 1f, true);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            EnsureParameter(controller, "Abrir", AnimatorControllerParameterType.Bool);

            var sm = controller.layers[0].stateMachine;
            var closed = sm.AddState("Cerrada", new Vector3(250f, 0f, 0f));
            closed.motion = closedClip;
            var open = sm.AddState("Abre", new Vector3(500f, 0f, 0f));
            open.motion = openClip;
            var closeState = sm.AddState("Cierra", new Vector3(500f, 120f, 0f));
            closeState.motion = closeClip;
            sm.defaultState = closed;

            var transition = closed.AddTransition(open);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Abrir");
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        WireControllerStates(controller, openClip, closeClip, closedClip);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void WireControllerStates(
        AnimatorController controller,
        AnimationClip openClip,
        AnimationClip closeClip,
        AnimationClip closedClip)
    {
        var sm = controller.layers[0].stateMachine;
        AnimatorState closeState = null;

        foreach (var child in sm.states)
        {
            if (child.state.name == "Cerrada")
                child.state.motion = closedClip;
            if (child.state.name == "Abre")
                child.state.motion = openClip;
            if (child.state.name == "Cierra")
                closeState = child.state;
        }

        if (closeState == null)
        {
            closeState = sm.AddState("Cierra", new Vector3(500f, 120f, 0f));
            closeState.motion = closeClip;
        }
        else
        {
            closeState.motion = closeClip;
        }
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
